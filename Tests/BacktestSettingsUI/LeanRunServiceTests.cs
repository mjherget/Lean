using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task BuildOnlyRunsDotnetBuildAndReportsCompletion()
        {
            var runner = new FakeProcessRunner(FakeManagedProcess.Completed(0));
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            var status = await service.BuildAsync(CancellationToken.None);
            var completed = await WaitForCompletionAsync(service);

            Assert.IsNotNull(status);
            Assert.AreEqual("build", status.ActionKind);
            Assert.AreEqual("building", status.Status);
            Assert.AreEqual("succeeded", completed.Status);
            Assert.AreEqual("build", completed.ActionKind);
            Assert.That(runner.Calls, Has.Count.EqualTo(1));
            Assert.AreEqual("build", runner.Calls[0].Arguments[0]);
            Assert.False(completed.CanStop);
            Assert.True(completed.CanBuild);
        }

        [Test]
        public async Task RebuildRunsDotnetBuildWithRebuildTarget()
        {
            var runner = new FakeProcessRunner(FakeManagedProcess.Completed(0));
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            await service.RebuildAsync(CancellationToken.None);
            var completed = await WaitForCompletionAsync(service);

            Assert.AreEqual("succeeded", completed.Status);
            Assert.AreEqual("rebuild", completed.ActionKind);
            CollectionAssert.Contains(runner.Calls[0].Arguments, "/t:Rebuild");
        }

        [Test]
        public async Task RunsBuildBeforeLeanAndReportsCompletion()
        {
            var runner = new FakeProcessRunner(FakeManagedProcess.Completed(0), FakeManagedProcess.Completed(0));
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            var status = await service.StartAsync(new BacktestSettings("AAPL", "2024-01-02", "2024-02-03", 100000m, "Minute"), CancellationToken.None);
            var completed = await WaitForCompletionAsync(service);

            Assert.IsNotNull(status);
            Assert.AreEqual("building", status.Status);
            Assert.AreEqual("succeeded", completed.Status);
            Assert.That(runner.Calls, Has.Count.EqualTo(2));
            Assert.AreEqual("build", runner.Calls[0].Arguments[0]);
            Assert.AreEqual(Path.Combine(_tempDirectory, "QuantConnect.Lean.sln"), runner.Calls[0].Arguments[1]);
            Assert.AreEqual(Path.Combine(_tempDirectory, "Launcher", "bin", "Debug", "QuantConnect.Lean.Launcher.dll"), runner.Calls[1].Arguments[0]);
            Assert.AreEqual("--config", runner.Calls[1].Arguments[1]);
            Assert.AreEqual(Path.Combine(_tempDirectory, "Launcher", "config.json"), runner.Calls[1].Arguments[2]);
        }

        [Test]
        public async Task BuildFailurePreventsLeanLaunch()
        {
            var runner = new FakeProcessRunner(FakeManagedProcess.Completed(1));
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            await service.StartAsync(new BacktestSettings("AAPL", "2024-01-02", "2024-02-03", 100000m, "Minute"), CancellationToken.None);
            var completed = await WaitForCompletionAsync(service);

            Assert.AreEqual("failed", completed.Status);
            Assert.That(runner.Calls, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task RejectsConcurrentActions()
        {
            var buildProcess = new FakeManagedProcess();
            var runner = new FakeProcessRunner(buildProcess);
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            var initialStatus = await service.BuildAsync(CancellationToken.None);
            var concurrentBuild = await service.RebuildAsync(CancellationToken.None);
            var concurrentRun = await service.StartAsync(new BacktestSettings("MSFT", "2024-01-02", "2024-02-03", 100000m, "Minute"), CancellationToken.None);

            Assert.IsNotNull(initialStatus);
            Assert.IsNull(concurrentBuild);
            Assert.IsNull(concurrentRun);

            buildProcess.SetExitCode(0);
            var completed = await WaitForCompletionAsync(service);
            Assert.AreEqual("succeeded", completed.Status);
        }

        [Test]
        public async Task StopDuringBuildTransitionsToStopped()
        {
            var buildProcess = new FakeManagedProcess();
            var runner = new FakeProcessRunner(buildProcess);
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            await service.BuildAsync(CancellationToken.None);
            var stopStatus = await service.StopAsync(CancellationToken.None);
            var completed = await WaitForCompletionAsync(service);

            Assert.That(stopStatus.Status, Is.EqualTo("stopping").Or.EqualTo("stopped"));
            Assert.True(buildProcess.StopCalled);
            Assert.AreEqual("stopped", completed.Status);
            Assert.AreEqual("build", completed.ActionKind);
        }

        [Test]
        public async Task StopDuringRunTransitionsToStopped()
        {
            var buildProcess = FakeManagedProcess.Completed(0);
            var leanProcess = new FakeManagedProcess();
            var runner = new FakeProcessRunner(buildProcess, leanProcess);
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            await service.StartAsync(new BacktestSettings("AAPL", "2024-01-02", "2024-02-03", 100000m, "Minute"), CancellationToken.None);
            await WaitForStatusAsync(service, "running");

            var stopStatus = await service.StopAsync(CancellationToken.None);
            var completed = await WaitForCompletionAsync(service);

            Assert.That(stopStatus.Status, Is.EqualTo("stopping").Or.EqualTo("stopped"));
            Assert.True(leanProcess.StopCalled);
            Assert.AreEqual("stopped", completed.Status);
            Assert.AreEqual("run", completed.ActionKind);
        }

        [Test]
        public async Task StopWhileIdleIsNoOp()
        {
            var runner = new FakeProcessRunner();
            var service = new LeanRunService(new LeanBacktestPaths(_tempDirectory), runner);

            var status = await service.StopAsync(CancellationToken.None);

            Assert.AreEqual("idle", status.Status);
            Assert.False(status.CanStop);
            Assert.AreEqual("No backtest has been run yet.", status.Message);
        }

        private static async Task<RunStatusResponse> WaitForCompletionAsync(LeanRunService service)
        {
            var timeout = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < timeout)
            {
                var status = service.GetStatus();
                if (status.Status is "succeeded" or "failed" or "stopped")
                {
                    return status;
                }

                await Task.Delay(25);
            }

            Assert.Fail("Timed out waiting for LeanRunService to finish.");
            return null;
        }

        private static async Task WaitForStatusAsync(LeanRunService service, string expectedStatus)
        {
            var timeout = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < timeout)
            {
                if (service.GetStatus().Status == expectedStatus)
                {
                    return;
                }

                await Task.Delay(25);
            }

            Assert.Fail($"Timed out waiting for status '{expectedStatus}'.");
        }

        private sealed class FakeProcessRunner : IProcessRunner
        {
            private readonly Queue<FakeManagedProcess> _processes;

            public List<ProcessSpecification> Calls { get; } = new();

            public FakeProcessRunner(params FakeManagedProcess[] processes)
            {
                _processes = new Queue<FakeManagedProcess>(processes);
            }

            public IManagedProcess Start(ProcessSpecification specification)
            {
                Calls.Add(specification);
                return _processes.Count > 0 ? _processes.Dequeue() : FakeManagedProcess.Completed(0);
            }
        }

        private sealed class FakeManagedProcess : IManagedProcess
        {
            private readonly TaskCompletionSource<int> _exitCodeSource = new();

            public bool StopCalled { get; private set; }

            public static FakeManagedProcess Completed(int exitCode)
            {
                var process = new FakeManagedProcess();
                process.SetExitCode(exitCode);
                return process;
            }

            public void SetExitCode(int exitCode)
            {
                _exitCodeSource.TrySetResult(exitCode);
            }

            public async Task<int> WaitForExitAsync(CancellationToken cancellationToken)
            {
                return await _exitCodeSource.Task.WaitAsync(cancellationToken);
            }

            public void Stop()
            {
                StopCalled = true;
            }
        }
    }
}
