using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using QuantConnect.Lean.BacktestSettingsUI.Models;
using QuantConnect.Lean.BacktestSettingsUI.Services;

namespace QuantConnect.Tests.BacktestSettingsUI
{
    [TestFixture]
    public class LeanRunServiceTests
    {
        private string _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            File.WriteAllText(Path.Combine(_tempDirectory, "QuantConnect.Lean.sln"), string.Empty);
            Directory.CreateDirectory(Path.Combine(_tempDirectory, "Launcher", "bin", "Debug"));
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
        public async Task RunsBuildBeforeLeanAndReportsCompletion()
        {
            var calls = new List<ProcessSpecification>();
            var runner = new Mock<IProcessRunner>();
            runner
                .Setup(x => x.RunAsync(It.IsAny<ProcessSpecification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProcessSpecification specification, CancellationToken _) =>
                {
                    calls.Add(specification);
                    return 0;
                });

            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner.Object);
            var status = await service.StartAsync(new BacktestSettings("AAPL", "2024-01-02", "2024-02-03", 100000m, "Minute"), CancellationToken.None);

            Assert.IsNotNull(status);
            Assert.AreEqual("building", status.Status);

            var completed = await WaitForCompletionAsync(service);

            Assert.AreEqual("succeeded", completed.Status);
            Assert.That(calls, Has.Count.EqualTo(2));
            Assert.AreEqual("build", calls[0].Arguments[0]);
            Assert.AreEqual(Path.Combine(_tempDirectory, "QuantConnect.Lean.sln"), calls[0].Arguments[1]);
            Assert.AreEqual(Path.Combine(_tempDirectory, "Launcher", "bin", "Debug", "QuantConnect.Lean.Launcher.dll"), calls[1].Arguments[0]);
            Assert.AreEqual("--config", calls[1].Arguments[1]);
            Assert.AreEqual(Path.Combine(_tempDirectory, "Launcher", "config.json"), calls[1].Arguments[2]);
        }

        [Test]
        public async Task RejectsConcurrentRuns()
        {
            var releaseBuild = new TaskCompletionSource<object>();
            var runner = new Mock<IProcessRunner>();
            runner
                .SetupSequence(x => x.RunAsync(It.IsAny<ProcessSpecification>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await releaseBuild.Task;
                    return 0;
                })
                .ReturnsAsync(0);

            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner.Object);
            var initialStatus = await service.StartAsync(new BacktestSettings("AAPL", "2024-01-02", "2024-02-03", 100000m, "Minute"), CancellationToken.None);
            var concurrentAttempt = await service.StartAsync(new BacktestSettings("MSFT", "2024-01-02", "2024-02-03", 100000m, "Minute"), CancellationToken.None);

            Assert.IsNotNull(initialStatus);
            Assert.IsNull(concurrentAttempt);

            releaseBuild.SetResult(null);
            var completed = await WaitForCompletionAsync(service);

            Assert.AreEqual("succeeded", completed.Status);
        }

        private static async Task<RunStatusResponse> WaitForCompletionAsync(LeanRunService service)
        {
            var timeout = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < timeout)
            {
                var status = service.GetStatus();
                if (status.Status is "succeeded" or "failed")
                {
                    return status;
                }

                await Task.Delay(25);
            }

            Assert.Fail("Timed out waiting for LeanRunService to finish.");
            return null;
        }
    }
}
