namespace QuantConnect.Lean.BacktestSettingsUI.Models
{
    public sealed class BacktestSettingsResponse
    {
        public string AlgorithmName { get; init; }
        public string Ticker { get; init; }
        public string StartDate { get; init; }
        public string EndDate { get; init; }
        public decimal Cash { get; init; }
        public string Resolution { get; init; }
        public bool SmaEnabled { get; init; }
        public int SmaPeriod { get; init; }
        public bool EmaEnabled { get; init; }
        public int EmaFastPeriod { get; init; }
        public int EmaSlowPeriod { get; init; }
        public bool RsiEnabled { get; init; }
        public int RsiPeriod { get; init; }
        public bool MacdEnabled { get; init; }
        public int MacdFastPeriod { get; init; }
        public int MacdSlowPeriod { get; init; }
        public int MacdSignalPeriod { get; init; }
        public bool BollingerBandsEnabled { get; init; }
        public int BollingerBandsPeriod { get; init; }
        public decimal BollingerBandsStandardDeviations { get; init; }

        public BacktestSettings ToSettings()
        {
            return new BacktestSettings(
                Ticker,
                StartDate,
                EndDate,
                Cash,
                Resolution,
                SmaEnabled,
                SmaPeriod,
                EmaEnabled,
                EmaFastPeriod,
                EmaSlowPeriod,
                RsiEnabled,
                RsiPeriod,
                MacdEnabled,
                MacdFastPeriod,
                MacdSlowPeriod,
                MacdSignalPeriod,
                BollingerBandsEnabled,
                BollingerBandsPeriod,
                BollingerBandsStandardDeviations);
        }
    }
}
