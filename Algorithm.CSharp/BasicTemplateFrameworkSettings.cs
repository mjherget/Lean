using System;
using System.Collections.Generic;
using System.Globalization;

namespace QuantConnect.Algorithm.CSharp
{
    public sealed class BasicTemplateFrameworkSettings
    {
        public const string DefaultTicker = "SPY";
        public static readonly DateTime DefaultStartDate = new(2013, 10, 07);
        public static readonly DateTime DefaultEndDate = new(2013, 10, 11);
        public const decimal DefaultCash = 100000m;
        public const Resolution DefaultResolution = Resolution.Minute;

        private static readonly HashSet<Resolution> SupportedResolutions =
        [
            Resolution.Daily,
            Resolution.Hour,
            Resolution.Minute,
            Resolution.Second,
            Resolution.Tick
        ];

        public string Ticker { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public decimal Cash { get; }
        public Resolution Resolution { get; }

        private BasicTemplateFrameworkSettings(string ticker, DateTime startDate, DateTime endDate, decimal cash, Resolution resolution)
        {
            Ticker = ticker;
            StartDate = startDate;
            EndDate = endDate;
            Cash = cash;
            Resolution = resolution;
        }

        public static BasicTemplateFrameworkSettings FromParameters(IReadOnlyDictionary<string, string> parameters, Action<string> logMessage)
        {
            var ticker = ParseTicker(parameters, logMessage);
            var startDate = ParseDate(parameters, "start-date", DefaultStartDate, logMessage);
            var endDate = ParseDate(parameters, "end-date", DefaultEndDate, logMessage);

            if (startDate > endDate)
            {
                logMessage($"Invalid 'start-date' and 'end-date' parameter combination. Falling back to {DefaultStartDate:yyyy-MM-dd} through {DefaultEndDate:yyyy-MM-dd}.");
                startDate = DefaultStartDate;
                endDate = DefaultEndDate;
            }

            var cash = ParseCash(parameters, logMessage);
            var resolution = ParseResolution(parameters, logMessage);

            return new BasicTemplateFrameworkSettings(ticker, startDate, endDate, cash, resolution);
        }

        private static string ParseTicker(IReadOnlyDictionary<string, string> parameters, Action<string> logMessage)
        {
            if (!parameters.TryGetValue("ticker", out var ticker) || string.IsNullOrWhiteSpace(ticker))
            {
                return DefaultTicker;
            }

            var normalizedTicker = ticker.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedTicker))
            {
                logMessage($"Invalid 'ticker' parameter '{ticker}'. Falling back to {DefaultTicker}.");
                return DefaultTicker;
            }

            return normalizedTicker;
        }

        private static DateTime ParseDate(IReadOnlyDictionary<string, string> parameters, string key, DateTime defaultValue, Action<string> logMessage)
        {
            if (!parameters.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }

            logMessage($"Invalid '{key}' parameter '{value}'. Falling back to {defaultValue:yyyy-MM-dd}.");
            return defaultValue;
        }

        private static decimal ParseCash(IReadOnlyDictionary<string, string> parameters, Action<string> logMessage)
        {
            if (!parameters.TryGetValue("cash", out var value) || string.IsNullOrWhiteSpace(value))
            {
                return DefaultCash;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var cash) && cash > 0)
            {
                return cash;
            }

            logMessage($"Invalid 'cash' parameter '{value}'. Falling back to {DefaultCash.ToString(CultureInfo.InvariantCulture)}.");
            return DefaultCash;
        }

        private static Resolution ParseResolution(IReadOnlyDictionary<string, string> parameters, Action<string> logMessage)
        {
            if (!parameters.TryGetValue("resolution", out var value) || string.IsNullOrWhiteSpace(value))
            {
                return DefaultResolution;
            }

            if (Enum.TryParse(value, true, out Resolution resolution) && SupportedResolutions.Contains(resolution))
            {
                return resolution;
            }

            logMessage($"Invalid 'resolution' parameter '{value}'. Falling back to {DefaultResolution}.");
            return DefaultResolution;
        }
    }
}
