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

        public string Ticker { get; }
        public string StartDate { get; }
        public string EndDate { get; }
        public decimal Cash { get; }
        public string Resolution { get; }
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

        public BacktestSettings(
            string ticker,
            string startDate,
            string endDate,
            decimal cash,
            string resolution,
            bool smaEnabled = DefaultSmaEnabled,
            int smaPeriod = DefaultSmaPeriod,
            bool emaEnabled = DefaultEmaEnabled,
            int emaFastPeriod = DefaultEmaFastPeriod,
            int emaSlowPeriod = DefaultEmaSlowPeriod,
            bool rsiEnabled = DefaultRsiEnabled,
            int rsiPeriod = DefaultRsiPeriod,
            bool macdEnabled = DefaultMacdEnabled,
            int macdFastPeriod = DefaultMacdFastPeriod,
            int macdSlowPeriod = DefaultMacdSlowPeriod,
            int macdSignalPeriod = DefaultMacdSignalPeriod,
            bool bollingerBandsEnabled = DefaultBollingerBandsEnabled,
            int bollingerBandsPeriod = DefaultBollingerBandsPeriod,
            decimal bollingerBandsStandardDeviations = DefaultBollingerBandsStandardDeviations)
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

        public Dictionary<string, string> ToParameterDictionary()
        {
            return new Dictionary<string, string>
            {
                ["ticker"] = Ticker,
                ["start-date"] = StartDate,
                ["end-date"] = EndDate,
                ["cash"] = Cash.ToString(CultureInfo.InvariantCulture),
                ["resolution"] = Resolution,
                ["sma-enabled"] = FormatBoolean(SmaEnabled),
                ["sma-period"] = SmaPeriod.ToString(CultureInfo.InvariantCulture),
                ["ema-enabled"] = FormatBoolean(EmaEnabled),
                ["ema-fast"] = EmaFastPeriod.ToString(CultureInfo.InvariantCulture),
                ["ema-slow"] = EmaSlowPeriod.ToString(CultureInfo.InvariantCulture),
                ["rsi-enabled"] = FormatBoolean(RsiEnabled),
                ["rsi-period"] = RsiPeriod.ToString(CultureInfo.InvariantCulture),
                ["macd-enabled"] = FormatBoolean(MacdEnabled),
                ["macd-fast"] = MacdFastPeriod.ToString(CultureInfo.InvariantCulture),
                ["macd-slow"] = MacdSlowPeriod.ToString(CultureInfo.InvariantCulture),
                ["macd-signal"] = MacdSignalPeriod.ToString(CultureInfo.InvariantCulture),
                ["bb-enabled"] = FormatBoolean(BollingerBandsEnabled),
                ["bb-period"] = BollingerBandsPeriod.ToString(CultureInfo.InvariantCulture),
                ["bb-standard-deviations"] = BollingerBandsStandardDeviations.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static string FormatBoolean(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
