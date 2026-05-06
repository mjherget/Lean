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

        public BacktestSettings ToSettings()
        {
            return new BacktestSettings(Ticker, StartDate, EndDate, Cash, Resolution);
        }
    }
}
