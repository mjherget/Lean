using System;
using System.Collections.Generic;

namespace QuantConnect.Lean.BacktestSettingsUI.Models
{
    public sealed class BacktestResultsResponse
    {
        public bool HasResults { get; init; }
        public string Message { get; init; }
        public string ResultId { get; init; }
        public string AlgorithmName { get; init; }
        public string SourceDirectory { get; init; }
        public DateTimeOffset? GeneratedAtUtc { get; init; }
        public IReadOnlyList<ResultKeyValueResponse> Statistics { get; init; } = Array.Empty<ResultKeyValueResponse>();
        public IReadOnlyList<ResultKeyValueResponse> RuntimeStatistics { get; init; } = Array.Empty<ResultKeyValueResponse>();
        public IReadOnlyList<EquityPointResponse> EquitySeries { get; init; } = Array.Empty<EquityPointResponse>();
        public IReadOnlyList<RecentOrderResponse> RecentOrders { get; init; } = Array.Empty<RecentOrderResponse>();
    }

    public sealed class ResultKeyValueResponse
    {
        public string Label { get; init; }
        public string Value { get; init; }
    }

    public sealed class EquityPointResponse
    {
        public string Time { get; init; }
        public decimal Value { get; init; }
    }

    public sealed class RecentOrderResponse
    {
        public string Time { get; init; }
        public string Symbol { get; init; }
        public string Status { get; init; }
        public string Direction { get; init; }
        public decimal? FillQuantity { get; init; }
        public decimal? FillPrice { get; init; }
        public string Message { get; init; }
    }
}
