using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Lean.BacktestSettingsUI.Models;

namespace QuantConnect.Tests.BacktestSettingsUI
{
    [TestFixture]
    public class BacktestSettingsRequestTests
    {
        [Test]
        public void NormalizesValidRequest()
        {
            var request = new BacktestSettingsRequest
            {
                Ticker = " msft ",
                StartDate = "2024-01-02",
                EndDate = "2024-02-03",
                Cash = "50000.25",
                Resolution = "second"
            };

            var success = request.TryNormalize(out var settings, out var errors);

            Assert.IsTrue(success);
            Assert.IsNotNull(settings);
            Assert.IsEmpty(errors);
            Assert.AreEqual("MSFT", settings.Ticker);
            Assert.AreEqual("2024-01-02", settings.StartDate);
            Assert.AreEqual("2024-02-03", settings.EndDate);
            Assert.AreEqual(50000.25m, settings.Cash);
            Assert.AreEqual("Second", settings.Resolution);
        }

        [Test]
        public void RejectsEmptyTicker()
        {
            var success = CreateValidRequest(ticker: " ").TryNormalize(out _, out var errors);

            Assert.IsFalse(success);
            Assert.That(errors.ContainsKey("ticker"));
        }

        [Test]
        public void RejectsInvalidStartDate()
        {
            var success = CreateValidRequest(startDate: "2024/01/02").TryNormalize(out _, out var errors);

            Assert.IsFalse(success);
            Assert.That(errors.ContainsKey("startDate"));
        }

        [Test]
        public void RejectsInvalidEndDate()
        {
            var success = CreateValidRequest(endDate: "2024/02/03").TryNormalize(out _, out var errors);

            Assert.IsFalse(success);
            Assert.That(errors.ContainsKey("endDate"));
        }

        [Test]
        public void RejectsReversedDateRange()
        {
            var success = CreateValidRequest(startDate: "2024-03-04", endDate: "2024-03-01").TryNormalize(out _, out var errors);

            Assert.IsFalse(success);
            Assert.That(errors.ContainsKey("dateRange"));
        }

        [Test]
        public void RejectsNonPositiveCash()
        {
            var success = CreateValidRequest(cash: "0").TryNormalize(out _, out var errors);

            Assert.IsFalse(success);
            Assert.That(errors.ContainsKey("cash"));
        }

        [Test]
        public void RejectsUnsupportedResolution()
        {
            var success = CreateValidRequest(resolution: "Weekly").TryNormalize(out _, out var errors);

            Assert.IsFalse(success);
            Assert.That(errors.ContainsKey("resolution"));
        }

        private static BacktestSettingsRequest CreateValidRequest(
            string ticker = "AAPL",
            string startDate = "2024-01-02",
            string endDate = "2024-02-03",
            string cash = "100000",
            string resolution = "Minute")
        {
            return new BacktestSettingsRequest
            {
                Ticker = ticker,
                StartDate = startDate,
                EndDate = endDate,
                Cash = cash,
                Resolution = resolution
            };
        }
    }
}
