using System;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Lean.BacktestSettingsUI.Models;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class LeanRunService
    {
        private readonly LeanBacktestPaths _paths;
        private readonly IProcessRunner _processRunner;
        private readonly object _stateLock = new();
        private RunStatusResponse _currentStatus = new() { Status = "idle", Message = "No backtest has been run yet." };

        public LeanRunService(LeanBacktestPaths paths, IProcessRunner processRunner)
        {
            _paths = paths;
            _processRunner = processRunner;
        }

        public RunStatusResponse GetStatus()
        {
            lock (_stateLock)
            {
                return _currentStatus;
            }
        }

        public Task<RunStatusResponse> StartAsync(BacktestSettings settings, CancellationToken cancellationToken)
        {
            RunStatusResponse startingStatus;
            lock (_stateLock)
            {
                if (_currentStatus.Status is "building" or "running")
                {
                    return Task.FromResult<RunStatusResponse>(null);
                }

                startingStatus = new RunStatusResponse
                {
                    RunId = Guid.NewGuid().ToString("N"),
                    Status = "building",
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    Message = $"Building Lean for {settings.Ticker}."
                };
                _currentStatus = startingStatus;
            }

            _ = Task.Run(() => ExecuteRunAsync(startingStatus), CancellationToken.None);
            return Task.FromResult(startingStatus);
        }

        private async Task ExecuteRunAsync(RunStatusResponse startingStatus)
        {
            try
            {
                var buildExitCode = await _processRunner.RunAsync(new ProcessSpecification
                {
                    FileName = "dotnet",
                    WorkingDirectory = _paths.SolutionRoot,
                    Arguments =
                    [
                        "build",
                        _paths.SolutionFilePath,
                        "/p:Configuration=Debug"
                    ]
                }, CancellationToken.None);

                if (buildExitCode != 0)
                {
                    CompleteRun("failed", buildExitCode, "Lean build failed.");
                    return;
                }

                UpdateStatus("running", startingStatus.RunId, startingStatus.StartedAtUtc, "Launching Lean backtest.");

                var runExitCode = await _processRunner.RunAsync(new ProcessSpecification
                {
                    FileName = "dotnet",
                    WorkingDirectory = _paths.LauncherWorkingDirectory,
                    Arguments =
                    [
                        _paths.LauncherAssemblyPath,
                        "--config",
                        _paths.LauncherConfigFilePath,
                        "--close-automatically",
                        "true"
                    ]
                }, CancellationToken.None);

                CompleteRun(runExitCode == 0 ? "succeeded" : "failed", runExitCode, runExitCode == 0 ? "Backtest completed successfully." : "Lean backtest failed.");
            }
            catch (Exception exception)
            {
                CompleteRun("failed", null, $"Backtest failed: {exception.Message}");
            }
        }

        private void UpdateStatus(string status, string runId, DateTimeOffset? startedAtUtc, string message)
        {
            lock (_stateLock)
            {
                _currentStatus = new RunStatusResponse
                {
                    RunId = runId,
                    Status = status,
                    StartedAtUtc = startedAtUtc,
                    Message = message
                };
            }
        }

        private void CompleteRun(string status, int? exitCode, string message)
        {
            lock (_stateLock)
            {
                _currentStatus = new RunStatusResponse
                {
                    RunId = _currentStatus.RunId,
                    Status = status,
                    ExitCode = exitCode,
                    StartedAtUtc = _currentStatus.StartedAtUtc,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    Message = message
                };
            }
        }
    }
}
