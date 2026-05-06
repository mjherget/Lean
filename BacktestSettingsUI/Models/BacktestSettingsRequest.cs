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

            if (errors.Count > 0)
            {
                return false;
            }

            settings = new BacktestSettings(
                ticker,
                startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cash,
                NormalizeResolution(resolution)
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
    }
}
