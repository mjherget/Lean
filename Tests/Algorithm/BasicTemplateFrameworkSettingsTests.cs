using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class BasicTemplateFrameworkSettingsTests
    {
        [Test]
        public void UsesDefaultsWhenParametersAreMissing()
        {
            var messages = new List<string>();

            var settings = BasicTemplateFrameworkSettings.FromParameters(new Dictionary<string, string>(), messages.Add);

            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultTicker, settings.Ticker);
            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultStartDate, settings.StartDate);
            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultEndDate, settings.EndDate);
            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultCash, settings.Cash);
            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultResolution, settings.Resolution);
            Assert.IsEmpty(messages);
        }

        [Test]
        public void ParsesValidOverrides()
        {
            var parameters = new Dictionary<string, string>
            {
                ["ticker"] = "aapl",
                ["start-date"] = "2024-01-02",
                ["end-date"] = "2024-02-03",
                ["cash"] = "250000.50",
                ["resolution"] = "hour"
            };

            var settings = BasicTemplateFrameworkSettings.FromParameters(parameters, _ => { });

            Assert.AreEqual("AAPL", settings.Ticker);
            Assert.AreEqual(new DateTime(2024, 1, 2), settings.StartDate);
            Assert.AreEqual(new DateTime(2024, 2, 3), settings.EndDate);
            Assert.AreEqual(250000.50m, settings.Cash);
            Assert.AreEqual(Resolution.Hour, settings.Resolution);
        }

        [Test]
        public void FallsBackAndLogsForInvalidValues()
        {
            var messages = new List<string>();
            var parameters = new Dictionary<string, string>
            {
                ["start-date"] = "bad-date",
                ["end-date"] = "2023-01-01",
                ["cash"] = "-1",
                ["resolution"] = "millisecond"
            };

            var settings = BasicTemplateFrameworkSettings.FromParameters(parameters, messages.Add);

            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultTicker, settings.Ticker);
            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultStartDate, settings.StartDate);
            Assert.AreEqual(new DateTime(2023, 1, 1), settings.EndDate);
            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultCash, settings.Cash);
            Assert.AreEqual(BasicTemplateFrameworkSettings.DefaultResolution, settings.Resolution);
            Assert.That(messages, Has.Count.EqualTo(3));
            Assert.That(messages[0], Does.Contain("start-date"));
            Assert.That(messages[1], Does.Contain("cash"));
            Assert.That(messages[2], Does.Contain("resolution"));
        }
    }
}
