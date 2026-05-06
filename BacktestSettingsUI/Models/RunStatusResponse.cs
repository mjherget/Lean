using System;

namespace QuantConnect.Lean.BacktestSettingsUI.Models
{
    public sealed class RunStatusResponse
    {
        public string RunId { get; init; }
        public string Status { get; init; }
        public int? ExitCode { get; init; }
        public DateTimeOffset? StartedAtUtc { get; init; }
        public DateTimeOffset? FinishedAtUtc { get; init; }
        public string Message { get; init; }
    }
}
