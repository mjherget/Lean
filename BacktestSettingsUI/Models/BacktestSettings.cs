using System;
using System.Collections.Generic;
using System.Globalization;

namespace QuantConnect.Lean.BacktestSettingsUI.Models
{
    public sealed class BacktestSettings
    {
        public const string DefaultTicker = "SPY";
        public const string DefaultStartDate = "2013-10-07";
        public const string DefaultEndDate = "2013-10-11";
        public const decimal DefaultCash = 100000m;
        public const string DefaultResolution = "Minute";

        public string Ticker { get; }
        public string StartDate { get; }
        public string EndDate { get; }
        public decimal Cash { get; }
        public string Resolution { get; }

        public BacktestSettings(string ticker, string startDate, string endDate, decimal cash, string resolution)
        {
            Ticker = ticker;
            StartDate = startDate;
            EndDate = endDate;
            Cash = cash;
            Resolution = resolution;
        }

        public Dictionary<string, string> ToParameterDictionary()
        {
            return new Dictionary<string, string>
            {
                ["ticker"] = Ticker,
                ["start-date"] = StartDate,
                ["end-date"] = EndDate,
                ["cash"] = Cash.ToString(CultureInfo.InvariantCulture),
                ["resolution"] = Resolution
            };
        }
    }
}
