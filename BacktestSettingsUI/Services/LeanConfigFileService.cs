using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Lean.BacktestSettingsUI.Models;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class LeanConfigFileService
    {
        private static readonly string[] ManagedParameterKeys =
        [
            "ticker",
            "start-date",
            "end-date",
            "cash",
            "resolution"
        ];

        private static readonly Regex PropertyLineRegex = new(@"^(?<indent>\s*)""(?<key>[^""]+)""\s*:\s*(?<value>.+?)(?<comment>\s*//.*)?$", RegexOptions.Compiled);

        private readonly LeanBacktestPaths _paths;

        public LeanConfigFileService(LeanBacktestPaths paths)
        {
            _paths = paths;
        }

        public BacktestSettingsResponse Load()
        {
            var text = File.ReadAllText(_paths.LauncherConfigFilePath);
            var root = ParseConfig(text);
            var parameters = root["parameters"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();

            return BuildResponse(root.Value<string>("algorithm-type-name") ?? string.Empty, parameters);
        }

        public BacktestSettingsResponse Save(BacktestSettings settings)
        {
            var fileText = File.ReadAllText(_paths.LauncherConfigFilePath);
            var updatedText = UpsertManagedParameters(fileText, settings.ToParameterDictionary());
            File.WriteAllText(_paths.LauncherConfigFilePath, updatedText);

            var root = ParseConfig(updatedText);
            var parameters = root["parameters"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            return BuildResponse(root.Value<string>("algorithm-type-name") ?? string.Empty, parameters);
        }

        private static JObject ParseConfig(string text)
        {
            using var stringReader = new StringReader(text);
            using var jsonReader = new JsonTextReader(stringReader)
            {
                DateParseHandling = DateParseHandling.None
            };

            return JObject.Load(jsonReader, new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore
            });
        }

        private static BacktestSettingsResponse BuildResponse(string algorithmName, IReadOnlyDictionary<string, string> parameters)
        {
            return new BacktestSettingsResponse
            {
                AlgorithmName = algorithmName,
                Ticker = NormalizeTicker(parameters.TryGetValue("ticker", out var ticker) ? ticker : null),
                StartDate = NormalizeDate(parameters.TryGetValue("start-date", out var startDate) ? startDate : null, BacktestSettings.DefaultStartDate),
                EndDate = NormalizeDate(parameters.TryGetValue("end-date", out var endDate) ? endDate : null, BacktestSettings.DefaultEndDate),
                Cash = NormalizeCash(parameters.TryGetValue("cash", out var cash) ? cash : null),
                Resolution = NormalizeResolution(parameters.TryGetValue("resolution", out var resolution) ? resolution : null)
            };
        }

        private static string NormalizeTicker(string ticker)
        {
            return string.IsNullOrWhiteSpace(ticker)
                ? BacktestSettings.DefaultTicker
                : ticker.Trim().ToUpperInvariant();
        }

        private static string NormalizeDate(string value, string fallback)
        {
            return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : fallback;
        }

        private static decimal NormalizeCash(string value)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var cash) && cash > 0
                ? cash
                : BacktestSettings.DefaultCash;
        }

        private static string NormalizeResolution(string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
            {
                return BacktestSettings.DefaultResolution;
            }

            foreach (var supportedResolution in new[] { "Daily", "Hour", "Minute", "Second", "Tick" })
            {
                if (supportedResolution.Equals(resolution.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return supportedResolution;
                }
            }

            return BacktestSettings.DefaultResolution;
        }

        private static string UpsertManagedParameters(string fileText, IReadOnlyDictionary<string, string> parameterValues)
        {
            var sectionBounds = FindParametersSection(fileText);
            var sectionText = fileText.Substring(sectionBounds.startIndex, sectionBounds.length);
            var newline = sectionText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            var lines = sectionText.Split(new[] { newline }, StringSplitOptions.None).ToList();

            var closingBraceIndex = lines.FindLastIndex(line => line.Trim() == "}");
            if (closingBraceIndex < 0)
            {
                throw new InvalidOperationException("Unable to locate the closing brace for the parameters section.");
            }

            var propertyIndent = DetectPropertyIndent(lines, closingBraceIndex);
            var replacedKeys = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < lines.Count; i++)
            {
                var match = PropertyLineRegex.Match(lines[i]);
                if (!match.Success)
                {
                    continue;
                }

                var key = match.Groups["key"].Value;
                if (!parameterValues.TryGetValue(key, out var value))
                {
                    continue;
                }

                replacedKeys.Add(key);
                var comment = match.Groups["comment"].Value;
                var hasComma = HasTrailingComma(lines[i]);
                var formattedValue = FormatParameterValue(key, value);
                lines[i] = $"{match.Groups["indent"].Value}\"{key}\": {formattedValue}{(hasComma ? "," : string.Empty)}{comment}";
            }

            var missingKeys = ManagedParameterKeys.Where(key => !replacedKeys.Contains(key)).ToList();
            if (missingKeys.Count > 0)
            {
                EnsurePreviousPropertyHasComma(lines, closingBraceIndex);

                for (var i = 0; i < missingKeys.Count; i++)
                {
                    var key = missingKeys[i];
                    var trailingComma = i < missingKeys.Count - 1 ? "," : string.Empty;
                    lines.Insert(closingBraceIndex + i, $"{propertyIndent}\"{key}\": {FormatParameterValue(key, parameterValues[key])}{trailingComma}");
                }
            }

            var updatedSectionText = string.Join(newline, lines);
            return fileText.Remove(sectionBounds.startIndex, sectionBounds.length).Insert(sectionBounds.startIndex, updatedSectionText);
        }

        private static (int startIndex, int length) FindParametersSection(string fileText)
        {
            var keyIndex = fileText.IndexOf("\"parameters\"", StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                throw new InvalidOperationException("Unable to locate the parameters section in Launcher/config.json.");
            }

            var openBraceIndex = fileText.IndexOf('{', keyIndex);
            if (openBraceIndex < 0)
            {
                throw new InvalidOperationException("Unable to locate the opening brace for the parameters section.");
            }

            var closeBraceIndex = FindMatchingBrace(fileText, openBraceIndex);
            var lineStart = fileText.LastIndexOf('\n', keyIndex);
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            return (lineStart, closeBraceIndex - lineStart + 1);
        }

        private static int FindMatchingBrace(string text, int openBraceIndex)
        {
            var depth = 0;
            var inString = false;
            var inLineComment = false;
            var inBlockComment = false;

            for (var i = openBraceIndex; i < text.Length; i++)
            {
                var current = text[i];
                var next = i + 1 < text.Length ? text[i + 1] : '\0';

                if (inLineComment)
                {
                    if (current == '\n')
                    {
                        inLineComment = false;
                    }

                    continue;
                }

                if (inBlockComment)
                {
                    if (current == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    continue;
                }

                if (inString)
                {
                    if (current == '\\')
                    {
                        i++;
                        continue;
                    }

                    if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '/' && next == '/')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (current == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            throw new InvalidOperationException("Unable to match the parameters section braces in Launcher/config.json.");
        }

        private static string DetectPropertyIndent(IReadOnlyList<string> lines, int closingBraceIndex)
        {
            foreach (var line in lines)
            {
                var match = PropertyLineRegex.Match(line);
                if (match.Success)
                {
                    return match.Groups["indent"].Value;
                }
            }

            var closingIndent = Regex.Match(lines[closingBraceIndex], @"^\s*").Value;
            return closingIndent + "  ";
        }

        private static bool HasTrailingComma(string line)
        {
            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            var candidate = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
            return candidate.TrimEnd().EndsWith(",", StringComparison.Ordinal);
        }

        private static void EnsurePreviousPropertyHasComma(IList<string> lines, int closingBraceIndex)
        {
            for (var i = closingBraceIndex - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i]) || lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!PropertyLineRegex.IsMatch(lines[i]))
                {
                    continue;
                }

                if (!HasTrailingComma(lines[i]))
                {
                    lines[i] += ",";
                }

                return;
            }
        }

        private static string FormatParameterValue(string key, string value)
        {
            if (key == "cash")
            {
                return decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            }

            return JsonConvert.ToString(value);
        }
    }
}
