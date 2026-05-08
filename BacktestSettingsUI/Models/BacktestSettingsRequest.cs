using System;
using System.Collections.Generic;
using System.Globalization;

namespace QuantConnect.Lean.BacktestSettingsUI.Models
{
    public sealed class BacktestSettingsRequest
    {
        private static readonly HashSet<string> SupportedResolutions = new(StringComparer.OrdinalIgnoreCase)
        {
            "Daily",
            "Hour",
            "Minute",
            "Second",
            "Tick"
        };

        public string Ticker { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Cash { get; set; }
        public string Resolution { get; set; }
        public bool SmaEnabled { get; set; }
        public string SmaPeriod { get; set; }
        public bool EmaEnabled { get; set; }
        public string EmaFastPeriod { get; set; }
        public string EmaSlowPeriod { get; set; }
        public bool RsiEnabled { get; set; }
        public string RsiPeriod { get; set; }
        public bool MacdEnabled { get; set; }
        public string MacdFastPeriod { get; set; }
        public string MacdSlowPeriod { get; set; }
        public string MacdSignalPeriod { get; set; }
        public bool BollingerBandsEnabled { get; set; }
        public string BollingerBandsPeriod { get; set; }
        public string BollingerBandsStandardDeviations { get; set; }

        public bool TryNormalize(out BacktestSettings settings, out Dictionary<string, string[]> errors)
        {
            errors = new Dictionary<string, string[]>();
            settings = null;

            var ticker = (Ticker ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(ticker))
            {
                errors["ticker"] = ["Ticker is required."];
            }

            if (!DateTime.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            {
                errors["startDate"] = ["Start date must use yyyy-MM-dd."];
            }

            if (!DateTime.TryParseExact(EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            {
                errors["endDate"] = ["End date must use yyyy-MM-dd."];
            }

            if (errors.Count == 0 && startDate > endDate)
            {
                errors["dateRange"] = ["Start date must be on or before end date."];
            }

            if (!decimal.TryParse(Cash, NumberStyles.Number, CultureInfo.InvariantCulture, out var cash) || cash <= 0)
            {
                errors["cash"] = ["Cash must be a positive number."];
            }

            var resolution = (Resolution ?? string.Empty).Trim();
            if (!SupportedResolutions.Contains(resolution))
            {
                errors["resolution"] = ["Resolution must be one of Daily, Hour, Minute, Second, or Tick."];
            }

            var smaPeriod = ParsePositiveInteger(SmaPeriod, BacktestSettings.DefaultSmaPeriod, "smaPeriod", "SMA period", errors);
            var emaFastPeriod = ParsePositiveInteger(EmaFastPeriod, BacktestSettings.DefaultEmaFastPeriod, "emaFastPeriod", "EMA fast period", errors);
            var emaSlowPeriod = ParsePositiveInteger(EmaSlowPeriod, BacktestSettings.DefaultEmaSlowPeriod, "emaSlowPeriod", "EMA slow period", errors);
            var rsiPeriod = ParsePositiveInteger(RsiPeriod, BacktestSettings.DefaultRsiPeriod, "rsiPeriod", "RSI period", errors);
            var macdFastPeriod = ParsePositiveInteger(MacdFastPeriod, BacktestSettings.DefaultMacdFastPeriod, "macdFastPeriod", "MACD fast period", errors);
            var macdSlowPeriod = ParsePositiveInteger(MacdSlowPeriod, BacktestSettings.DefaultMacdSlowPeriod, "macdSlowPeriod", "MACD slow period", errors);
            var macdSignalPeriod = ParsePositiveInteger(MacdSignalPeriod, BacktestSettings.DefaultMacdSignalPeriod, "macdSignalPeriod", "MACD signal period", errors);
            var bollingerBandsPeriod = ParsePositiveInteger(BollingerBandsPeriod, BacktestSettings.DefaultBollingerBandsPeriod, "bollingerBandsPeriod", "Bollinger Bands period", errors);
            var bollingerBandsStandardDeviations = ParsePositiveDecimal(
                BollingerBandsStandardDeviations,
                BacktestSettings.DefaultBollingerBandsStandardDeviations,
                "bollingerBandsStandardDeviations",
                "Bollinger Bands standard deviations",
                errors);

            if (EmaEnabled && emaFastPeriod >= emaSlowPeriod)
            {
                errors["emaPeriods"] = ["EMA fast period must be less than EMA slow period."];
            }

            if (MacdEnabled && macdFastPeriod >= macdSlowPeriod)
            {
                errors["macdPeriods"] = ["MACD fast period must be less than MACD slow period."];
            }

            if (errors.Count > 0)
            {
                return false;
            }

            settings = new BacktestSettings(
                ticker,
                startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cash,
                NormalizeResolution(resolution),
                SmaEnabled,
                smaPeriod,
                EmaEnabled,
                emaFastPeriod,
                emaSlowPeriod,
                RsiEnabled,
                rsiPeriod,
                MacdEnabled,
                macdFastPeriod,
                macdSlowPeriod,
                macdSignalPeriod,
                BollingerBandsEnabled,
                bollingerBandsPeriod,
                bollingerBandsStandardDeviations
            );

            return true;
        }

        private static string NormalizeResolution(string resolution)
        {
            foreach (var supportedResolution in SupportedResolutions)
            {
                if (supportedResolution.Equals(resolution, StringComparison.OrdinalIgnoreCase))
                {
                    return supportedResolution;
                }
            }

            return resolution;
        }

        private static int ParsePositiveInteger(
            string value,
            int fallback,
            string fieldName,
            string displayName,
            Dictionary<string, string[]> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            errors[fieldName] = [$"{displayName} must be a positive whole number."];
            return fallback;
        }

        private static decimal ParsePositiveDecimal(
            string value,
            decimal fallback,
            string fieldName,
            string displayName,
            Dictionary<string, string[]> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            errors[fieldName] = [$"{displayName} must be a positive number."];
            return fallback;
        }
    }
}
