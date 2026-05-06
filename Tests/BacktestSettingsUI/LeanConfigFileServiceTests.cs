using System;
using System.IO;
using NUnit.Framework;
using QuantConnect.Lean.BacktestSettingsUI.Models;
using QuantConnect.Lean.BacktestSettingsUI.Services;

namespace QuantConnect.Tests.BacktestSettingsUI
{
    [TestFixture]
    public class LeanConfigFileServiceTests
    {
        private string _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            File.WriteAllText(Path.Combine(_tempDirectory, "QuantConnect.Lean.sln"), string.Empty);
            Directory.CreateDirectory(Path.Combine(_tempDirectory, "Launcher"));
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
        public void SavesManagedParametersWithoutTouchingUnrelatedContent()
        {
            const string configText = """
{
  "algorithm-type-name": "BasicTemplateFrameworkAlgorithm",
  "data-folder": "../../../Data/",

  // parameters to set in the algorithm (the below are just samples)
  "parameters": {
    // Intrinio account user and password
    "intrinio-username": "",
    "intrinio-password": "",

    "ema-fast": 10,
    "ema-slow": 20
  },

  "live-data-url": "ws://www.quantconnect.com/api/v2/live/data/"
}
""";

            var configPath = Path.Combine(_tempDirectory, "Launcher", "config.json");
            File.WriteAllText(configPath, configText);

            var service = new LeanConfigFileService(new LeanBacktestPaths(_tempDirectory));
            var saved = service.Save(new BacktestSettings("AAPL", "2024-01-02", "2024-02-03", 250000m, "Hour"));
            var updatedText = File.ReadAllText(configPath);

            Assert.AreEqual("BasicTemplateFrameworkAlgorithm", saved.AlgorithmName);
            Assert.That(updatedText, Does.Contain("// Intrinio account user and password"));
            Assert.That(updatedText, Does.Contain("\"intrinio-username\": \"\""));
            Assert.That(updatedText, Does.Contain("\"ema-fast\": 10"));
            Assert.That(updatedText, Does.Contain("\"ema-slow\": 20,"));
            Assert.That(updatedText, Does.Contain("\"ticker\": \"AAPL\""));
            Assert.That(updatedText, Does.Contain("\"start-date\": \"2024-01-02\""));
            Assert.That(updatedText, Does.Contain("\"end-date\": \"2024-02-03\""));
            Assert.That(updatedText, Does.Contain("\"cash\": 250000"));
            Assert.That(updatedText, Does.Contain("\"resolution\": \"Hour\""));
            Assert.That(updatedText, Does.Contain("\"live-data-url\": \"ws://www.quantconnect.com/api/v2/live/data/\""));
        }

        [Test]
        public void LoadFallsBackToDefaultsForMissingOrInvalidManagedParameters()
        {
            const string configText = """
{
  "algorithm-type-name": "BasicTemplateFrameworkAlgorithm",
  "parameters": {
    "ticker": "",
    "start-date": "not-a-date",
    "cash": -5,
    "resolution": "Weekly"
  }
}
""";

            File.WriteAllText(Path.Combine(_tempDirectory, "Launcher", "config.json"), configText);

            var service = new LeanConfigFileService(new LeanBacktestPaths(_tempDirectory));
            var loaded = service.Load();

            Assert.AreEqual(BacktestSettings.DefaultTicker, loaded.Ticker);
            Assert.AreEqual(BacktestSettings.DefaultStartDate, loaded.StartDate);
            Assert.AreEqual(BacktestSettings.DefaultEndDate, loaded.EndDate);
            Assert.AreEqual(BacktestSettings.DefaultCash, loaded.Cash);
            Assert.AreEqual(BacktestSettings.DefaultResolution, loaded.Resolution);
        }
    }
}
