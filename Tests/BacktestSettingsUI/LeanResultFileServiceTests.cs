using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Lean.BacktestSettingsUI.Services;

namespace QuantConnect.Tests.BacktestSettingsUI
{
    [TestFixture]
    public class LeanResultFileServiceTests
    {
        private string _tempDirectory;
        private string _resultsDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _resultsDirectory = Path.Combine(_tempDirectory, "Launcher", "bin", "Debug");

            Directory.CreateDirectory(_resultsDirectory);
            File.WriteAllText(Path.Combine(_tempDirectory, "QuantConnect.Lean.sln"), string.Empty);
            File.WriteAllText(Path.Combine(_tempDirectory, "Launcher", "config.json"), "{}");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public void ReturnsEmptyResponseWhenNoResultsExist()
        {
            var service = new LeanResultFileService(new LeanBacktestPaths(_tempDirectory));

            var results = service.LoadLatest();

            Assert.IsFalse(results.HasResults);
            Assert.That(results.Message, Does.Contain("Run a backtest"));
            Assert.AreEqual(_resultsDirectory, results.SourceDirectory);
        }

        [Test]
        public void LoadsLatestSummaryEquityAndRecentOrders()
        {
            File.WriteAllText(Path.Combine(_resultsDirectory, "older-summary.json"), "{\"state\":{\"name\":\"Old\"}}");
            File.SetLastWriteTimeUtc(Path.Combine(_resultsDirectory, "older-summary.json"), new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            File.WriteAllText(Path.Combine(_resultsDirectory, "run-123-summary.json"), @"
{
  ""state"": { ""name"": ""Basic Template Framework Algorithm"" },
  ""statistics"": {
    ""Total Trades"": ""4"",
    ""Net Profit"": ""12.34%""
  },
  ""runtimeStatistics"": {
    ""Return"": ""12.34%"",
    ""Unrealized"": ""$0.00""
  },
  ""charts"": {
    ""Strategy Equity"": {
      ""series"": {
        ""Equity"": {
          ""values"": [
            [1704067200, 100000, 101000, 99500, 100500],
            [1704153600, 100500, 102000, 100200, 101250]
          ]
        }
      }
    }
  }
}");
            File.WriteAllText(Path.Combine(_resultsDirectory, "run-123-order-events.json"), @"
[
  {
    ""utcTime"": ""2024-01-02T14:31:00Z"",
    ""symbol"": { ""value"": ""AAPL"" },
    ""status"": ""Filled"",
    ""direction"": ""Buy"",
    ""fillQuantity"": 10,
    ""fillPrice"": 189.12,
    ""message"": ""Fill completed""
  },
  {
    ""utcTime"": ""2024-01-03T15:35:00Z"",
    ""symbol"": ""AAPL"",
    ""status"": ""Submitted"",
    ""direction"": ""Sell"",
    ""fillQuantity"": 0,
    ""fillPrice"": 0,
    ""message"": ""Waiting for fill""
  }
]");

            var latestSummaryPath = Path.Combine(_resultsDirectory, "run-123-summary.json");
            File.SetLastWriteTimeUtc(latestSummaryPath, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));

            var service = new LeanResultFileService(new LeanBacktestPaths(_tempDirectory));

            var results = service.LoadLatest();

            Assert.IsTrue(results.HasResults);
            Assert.AreEqual("run-123", results.ResultId);
            Assert.AreEqual("Basic Template Framework Algorithm", results.AlgorithmName);
            Assert.AreEqual(2, results.Statistics.Count);
            Assert.AreEqual("Total Trades", results.Statistics[0].Label);
            Assert.AreEqual("4", results.Statistics[0].Value);
            Assert.AreEqual(2, results.RuntimeStatistics.Count);
            Assert.AreEqual(2, results.EquitySeries.Count);
            Assert.AreEqual("2024-01-01T00:00:00.0000000Z", results.EquitySeries[0].Time);
            Assert.AreEqual(100500m, results.EquitySeries[0].Value);
            Assert.AreEqual(2, results.RecentOrders.Count);
            Assert.AreEqual("Submitted", results.RecentOrders[0].Status);
            Assert.AreEqual("Filled", results.RecentOrders[1].Status);
            Assert.AreEqual("AAPL", results.RecentOrders[1].Symbol);
            Assert.AreEqual(_resultsDirectory, results.SourceDirectory);
            Assert.AreEqual(new DateTimeOffset(new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)), results.GeneratedAtUtc);
            Assert.That(results.Message, Does.Contain("latest completed backtest"));
            Assert.That(results.Statistics.Select(item => item.Label), Does.Contain("Net Profit"));
        }
    }
}
