using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class TickerSearchService
    {
        private static readonly string[] SymbolSourceDirectories =
        [
            "daily",
            "hour",
            "minute",
            "second",
            "tick",
            "map_files",
            "factor_files"
        ];

        private readonly Lazy<IReadOnlyList<string>> _symbols;

        public TickerSearchService(LeanBacktestPaths paths)
        {
            _symbols = new Lazy<IReadOnlyList<string>>(() => LoadSymbols(paths.SolutionRoot));
        }

        public IReadOnlyList<string> Search(string query, int maxResults = 50)
        {
            var normalizedQuery = (query ?? string.Empty).Trim().ToUpperInvariant();
            var symbols = _symbols.Value;

            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return symbols.Take(maxResults).ToArray();
            }

            return symbols
                .Where(symbol => symbol.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .Concat(symbols.Where(symbol =>
                    !symbol.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                    && symbol.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxResults)
                .ToArray();
        }

        private static IReadOnlyList<string> LoadSymbols(string solutionRoot)
        {
            var equityDataRoot = Path.Combine(solutionRoot, "Data", "equity", "usa");
            if (!Directory.Exists(equityDataRoot))
            {
                return Array.Empty<string>();
            }

            var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var directoryName in SymbolSourceDirectories)
            {
                var directoryPath = Path.Combine(equityDataRoot, directoryName);
                if (!Directory.Exists(directoryPath))
                {
                    continue;
                }

                foreach (var filePath in Directory.EnumerateFiles(directoryPath))
                {
                    var symbol = NormalizeSymbolFileName(filePath);
                    if (!string.IsNullOrWhiteSpace(symbol))
                    {
                        symbols.Add(symbol);
                    }
                }
            }

            return symbols
                .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeSymbolFileName(string filePath)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrWhiteSpace(name) || name.Equals("readme", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var separatorIndex = name.IndexOf('_', StringComparison.Ordinal);
            if (separatorIndex > 0)
            {
                name = name[..separatorIndex];
            }

            return name.Trim().ToUpperInvariant();
        }
    }
}
