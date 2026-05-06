using System;

namespace QuantConnect.Lean.BacktestSettingsUI.Models
{
    public sealed class RunStatusResponse
    {
        public string RunId { get; init; }
        public string ActionKind { get; init; }
        public string Status { get; init; }
        public int? ExitCode { get; init; }
        public DateTimeOffset? StartedAtUtc { get; init; }
        public DateTimeOffset? FinishedAtUtc { get; init; }
        public string Message { get; init; }
        public bool CanBuild { get; init; }
        public bool CanRebuild { get; init; }
        public bool CanStart { get; init; }
        public bool CanStop { get; init; }
    }
}
