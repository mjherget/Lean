using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Lean.BacktestSettingsUI.Models;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class LeanResultFileService
    {
        private const string StrategyEquityChartName = "Strategy Equity";
        private const string EquitySeriesName = "Equity";
        private readonly LeanBacktestPaths _paths;

        public LeanResultFileService(LeanBacktestPaths paths)
        {
            _paths = paths;
        }

        public BacktestResultsResponse LoadLatest()
        {
            var resultsDirectory = _paths.LauncherWorkingDirectory;
            if (!Directory.Exists(resultsDirectory))
            {
                return EmptyResponse(resultsDirectory, "The Lean results directory does not exist yet.");
            }

            var latestSummary = new DirectoryInfo(resultsDirectory)
                .GetFiles("*-summary.json", SearchOption.TopDirectoryOnly)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestSummary == null)
            {
                return EmptyResponse(resultsDirectory, "Run a backtest to populate local results.");
            }

            var resultId = Path.GetFileNameWithoutExtension(latestSummary.Name);
            if (resultId.EndsWith("-summary", StringComparison.OrdinalIgnoreCase))
            {
                resultId = resultId[..^"-summary".Length];
            }

            var summary = JObject.Parse(File.ReadAllText(latestSummary.FullName));
            return new BacktestResultsResponse
            {
                HasResults = true,
                Message = "Showing the latest completed backtest.",
                ResultId = resultId,
                AlgorithmName = ParseAlgorithmName(summary),
                SourceDirectory = resultsDirectory,
                GeneratedAtUtc = latestSummary.LastWriteTimeUtc,
                Statistics = ParseKeyValuePairs(GetObject(summary, "statistics")),
                RuntimeStatistics = ParseKeyValuePairs(GetObject(summary, "runtimeStatistics")),
                EquitySeries = ParseEquitySeries(summary),
                RecentOrders = ParseRecentOrders(Path.Combine(resultsDirectory, $"{resultId}-order-events.json"))
            };
        }

        private static BacktestResultsResponse EmptyResponse(string sourceDirectory, string message)
        {
            return new BacktestResultsResponse
            {
                HasResults = false,
                Message = message,
                SourceDirectory = sourceDirectory
            };
        }

        private static string ParseAlgorithmName(JObject summary)
        {
            var state = GetObject(summary, "state");
            var stateName = GetString(state, "name");
            if (!string.IsNullOrWhiteSpace(stateName))
            {
                return stateName;
            }

            var algorithmConfiguration = GetObject(summary, "algorithmConfiguration");
            var configuredName = GetString(algorithmConfiguration, "name");
            return string.IsNullOrWhiteSpace(configuredName) ? "Unknown algorithm" : configuredName;
        }

        private static IReadOnlyList<ResultKeyValueResponse> ParseKeyValuePairs(JObject source)
        {
            if (source == null)
            {
                return Array.Empty<ResultKeyValueResponse>();
            }

            return source.Properties()
                .Select(property => new ResultKeyValueResponse
                {
                    Label = property.Name,
                    Value = property.Value.Type == JTokenType.Null
                        ? string.Empty
                        : property.Value.Type == JTokenType.String
                            ? property.Value.Value<string>()
                            : property.Value.ToString(Formatting.None)
                })
                .ToArray();
        }

        private static IReadOnlyList<EquityPointResponse> ParseEquitySeries(JObject summary)
        {
            var charts = GetObject(summary, "charts");
            var strategyEquity = GetObject(charts, StrategyEquityChartName);
            var seriesCollection = GetObject(strategyEquity, "series");
            var equitySeries = GetObject(seriesCollection, EquitySeriesName);
            var values = GetArray(equitySeries, "values");

            if (values == null)
            {
                return Array.Empty<EquityPointResponse>();
            }

            var points = new List<EquityPointResponse>();
            foreach (var pointToken in values)
            {
                if (!TryParseEquityPoint(pointToken, out var point))
                {
                    continue;
                }

                points.Add(point);
            }

            return points;
        }

        private static bool TryParseEquityPoint(JToken pointToken, out EquityPointResponse point)
        {
            point = null;

            if (pointToken is JArray candlestick && candlestick.Count >= 5)
            {
                var time = candlestick[0]?.Value<long?>();
                var value = candlestick[4]?.Value<decimal?>();
                if (time.HasValue && value.HasValue)
                {
                    point = new EquityPointResponse
                    {
                        Time = DateTimeOffset.FromUnixTimeSeconds(time.Value).UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                        Value = value.Value
                    };
                    return true;
                }
            }

            if (pointToken is JObject chartPoint)
            {
                var time = GetLong(chartPoint, "x");
                var value = GetDecimal(chartPoint, "y");
                if (time.HasValue && value.HasValue)
                {
                    point = new EquityPointResponse
                    {
                        Time = DateTimeOffset.FromUnixTimeSeconds(time.Value).UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                        Value = value.Value
                    };
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<RecentOrderResponse> ParseRecentOrders(string orderEventsPath)
        {
            if (!File.Exists(orderEventsPath))
            {
                return Array.Empty<RecentOrderResponse>();
            }

            var orderEvents = JArray.Parse(File.ReadAllText(orderEventsPath));
            return orderEvents
                .OfType<JObject>()
                .Reverse()
                .Take(8)
                .Select(orderEvent => new RecentOrderResponse
                {
                    Time = ParseOrderTime(orderEvent),
                    Symbol = ParseSymbol(orderEvent),
                    Status = GetString(orderEvent, "status") ?? string.Empty,
                    Direction = GetString(orderEvent, "direction") ?? string.Empty,
                    FillQuantity = GetDecimal(orderEvent, "fillQuantity"),
                    FillPrice = GetDecimal(orderEvent, "fillPrice"),
                    Message = GetString(orderEvent, "message") ?? string.Empty
                })
                .ToArray();
        }

        private static string ParseOrderTime(JObject orderEvent)
        {
            var utcTimeToken = GetToken(orderEvent, "utcTime");
            if (utcTimeToken == null)
            {
                return string.Empty;
            }

            if (utcTimeToken.Type == JTokenType.Date)
            {
                return utcTimeToken.Value<DateTime>().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            }

            if (DateTimeOffset.TryParse(utcTimeToken.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var utcTime))
            {
                return utcTime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            }

            return utcTimeToken.ToString();
        }

        private static string ParseSymbol(JObject orderEvent)
        {
            var symbolToken = GetToken(orderEvent, "symbol");
            if (symbolToken == null)
            {
                return string.Empty;
            }

            if (symbolToken.Type == JTokenType.String)
            {
                return symbolToken.Value<string>();
            }

            if (symbolToken is JObject symbolObject)
            {
                var value = GetString(symbolObject, "value");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return symbolToken.ToString(Formatting.None);
        }

        private static JObject GetObject(JObject source, string propertyName)
        {
            return GetToken(source, propertyName) as JObject;
        }

        private static JArray GetArray(JObject source, string propertyName)
        {
            return GetToken(source, propertyName) as JArray;
        }

        private static JToken GetToken(JObject source, string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            return source.Properties()
                .FirstOrDefault(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }

        private static string GetString(JObject source, string propertyName)
        {
            return GetToken(source, propertyName)?.Value<string>();
        }

        private static decimal? GetDecimal(JObject source, string propertyName)
        {
            var token = GetToken(source, propertyName);
            return token?.Value<decimal?>();
        }

        private static long? GetLong(JObject source, string propertyName)
        {
            var token = GetToken(source, propertyName);
            return token?.Value<long?>();
        }
    }
}
