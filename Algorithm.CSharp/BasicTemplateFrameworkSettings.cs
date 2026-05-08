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
        public const bool DefaultSmaEnabled = true;
        public const int DefaultSmaPeriod = 20;
        public const bool DefaultEmaEnabled = true;
        public const int DefaultEmaFastPeriod = 10;
        public const int DefaultEmaSlowPeriod = 20;
        public const bool DefaultRsiEnabled = true;
        public const int DefaultRsiPeriod = 14;
        public const bool DefaultMacdEnabled = false;
        public const int DefaultMacdFastPeriod = 12;
        public const int DefaultMacdSlowPeriod = 26;
        public const int DefaultMacdSignalPeriod = 9;
        public const bool DefaultBollingerBandsEnabled = false;
        public const int DefaultBollingerBandsPeriod = 20;
        public const decimal DefaultBollingerBandsStandardDeviations = 2m;

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
        public bool SmaEnabled { get; }
        public int SmaPeriod { get; }
        public bool EmaEnabled { get; }
        public int EmaFastPeriod { get; }
        public int EmaSlowPeriod { get; }
        public bool RsiEnabled { get; }
        public int RsiPeriod { get; }
        public bool MacdEnabled { get; }
        public int MacdFastPeriod { get; }
        public int MacdSlowPeriod { get; }
        public int MacdSignalPeriod { get; }
        public bool BollingerBandsEnabled { get; }
        public int BollingerBandsPeriod { get; }
        public decimal BollingerBandsStandardDeviations { get; }

        private BasicTemplateFrameworkSettings(
            string ticker,
            DateTime startDate,
            DateTime endDate,
            decimal cash,
            Resolution resolution,
            bool smaEnabled,
            int smaPeriod,
            bool emaEnabled,
            int emaFastPeriod,
            int emaSlowPeriod,
            bool rsiEnabled,
            int rsiPeriod,
            bool macdEnabled,
            int macdFastPeriod,
            int macdSlowPeriod,
            int macdSignalPeriod,
            bool bollingerBandsEnabled,
            int bollingerBandsPeriod,
            decimal bollingerBandsStandardDeviations)
        {
            Ticker = ticker;
            StartDate = startDate;
            EndDate = endDate;
            Cash = cash;
            Resolution = resolution;
            SmaEnabled = smaEnabled;
            SmaPeriod = smaPeriod;
            EmaEnabled = emaEnabled;
            EmaFastPeriod = emaFastPeriod;
            EmaSlowPeriod = emaSlowPeriod;
            RsiEnabled = rsiEnabled;
            RsiPeriod = rsiPeriod;
            MacdEnabled = macdEnabled;
            MacdFastPeriod = macdFastPeriod;
            MacdSlowPeriod = macdSlowPeriod;
            MacdSignalPeriod = macdSignalPeriod;
            BollingerBandsEnabled = bollingerBandsEnabled;
            BollingerBandsPeriod = bollingerBandsPeriod;
            BollingerBandsStandardDeviations = bollingerBandsStandardDeviations;
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
            var smaEnabled = ParseBoolean(parameters, "sma-enabled", DefaultSmaEnabled, logMessage);
            var smaPeriod = ParsePositiveInteger(parameters, "sma-period", DefaultSmaPeriod, logMessage);
            var emaEnabled = ParseBoolean(parameters, "ema-enabled", DefaultEmaEnabled, logMessage);
            var emaFastPeriod = ParsePositiveInteger(parameters, "ema-fast", DefaultEmaFastPeriod, logMessage);
            var emaSlowPeriod = ParsePositiveInteger(parameters, "ema-slow", DefaultEmaSlowPeriod, logMessage);
            var rsiEnabled = ParseBoolean(parameters, "rsi-enabled", DefaultRsiEnabled, logMessage);
            var rsiPeriod = ParsePositiveInteger(parameters, "rsi-period", DefaultRsiPeriod, logMessage);
            var macdEnabled = ParseBoolean(parameters, "macd-enabled", DefaultMacdEnabled, logMessage);
            var macdFastPeriod = ParsePositiveInteger(parameters, "macd-fast", DefaultMacdFastPeriod, logMessage);
            var macdSlowPeriod = ParsePositiveInteger(parameters, "macd-slow", DefaultMacdSlowPeriod, logMessage);
            var macdSignalPeriod = ParsePositiveInteger(parameters, "macd-signal", DefaultMacdSignalPeriod, logMessage);
            var bollingerBandsEnabled = ParseBoolean(parameters, "bb-enabled", DefaultBollingerBandsEnabled, logMessage);
            var bollingerBandsPeriod = ParsePositiveInteger(parameters, "bb-period", DefaultBollingerBandsPeriod, logMessage);
            var bollingerBandsStandardDeviations = ParsePositiveDecimal(parameters, "bb-standard-deviations", DefaultBollingerBandsStandardDeviations, logMessage);

            return new BasicTemplateFrameworkSettings(
                ticker,
                startDate,
                endDate,
                cash,
                resolution,
                smaEnabled,
                smaPeriod,
                emaEnabled,
                emaFastPeriod,
                emaSlowPeriod,
                rsiEnabled,
                rsiPeriod,
                macdEnabled,
                macdFastPeriod,
                macdSlowPeriod,
                macdSignalPeriod,
                bollingerBandsEnabled,
                bollingerBandsPeriod,
                bollingerBandsStandardDeviations);
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

        private static bool ParseBoolean(IReadOnlyDictionary<string, string> parameters, string key, bool defaultValue, Action<string> logMessage)
        {
            if (!parameters.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            logMessage($"Invalid '{key}' parameter '{value}'. Falling back to {defaultValue}.");
            return defaultValue;
        }

        private static int ParsePositiveInteger(IReadOnlyDictionary<string, string> parameters, string key, int defaultValue, Action<string> logMessage)
        {
            if (!parameters.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            logMessage($"Invalid '{key}' parameter '{value}'. Falling back to {defaultValue}.");
            return defaultValue;
        }

        private static decimal ParsePositiveDecimal(IReadOnlyDictionary<string, string> parameters, string key, decimal defaultValue, Action<string> logMessage)
        {
            if (!parameters.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            logMessage($"Invalid '{key}' parameter '{value}'. Falling back to {defaultValue.ToString(CultureInfo.InvariantCulture)}.");
            return defaultValue;
        }
    }
}
