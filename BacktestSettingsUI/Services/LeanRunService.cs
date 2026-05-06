using System;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Lean.BacktestSettingsUI.Models;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class LeanRunService
    {
        private const string IdleAction = "idle";
        private readonly LeanBacktestPaths _paths;
        private readonly IProcessRunner _processRunner;
        private readonly object _stateLock = new();

        private CancellationTokenSource _activeCancellationSource;
        private IManagedProcess _activeProcess;
        private RunStatusResponse _currentStatus = CreateStatus(
            runId: null,
            actionKind: IdleAction,
            status: "idle",
            message: "No backtest has been run yet.");

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

        public Task<RunStatusResponse> BuildAsync(CancellationToken cancellationToken)
        {
            return StartManagedActionAsync("build", "Building Lean.", _ => ExecuteBuildAsync(rebuild: false, cancellationToken: _));
        }

        public Task<RunStatusResponse> RebuildAsync(CancellationToken cancellationToken)
        {
            return StartManagedActionAsync("rebuild", "Rebuilding Lean.", _ => ExecuteBuildAsync(rebuild: true, cancellationToken: _));
        }

        public Task<RunStatusResponse> StartAsync(BacktestSettings settings, CancellationToken cancellationToken)
        {
            return StartManagedActionAsync("run", $"Building Lean for {settings.Ticker}.", token => ExecuteRunAsync(settings, token));
        }

        public Task<RunStatusResponse> StopAsync(CancellationToken cancellationToken)
        {
            IManagedProcess processToStop;
            CancellationTokenSource cancellationSource;
            RunStatusResponse stoppingStatus;

            lock (_stateLock)
            {
                if (_currentStatus.Status is not ("building" or "running"))
                {
                    return Task.FromResult(_currentStatus);
                }

                var stoppingMessage = _currentStatus.Status == "building"
                    ? "Stopping current build."
                    : "Stopping Lean backtest.";

                _currentStatus = CreateStatus(
                    runId: _currentStatus.RunId,
                    actionKind: _currentStatus.ActionKind,
                    status: "stopping",
                    message: stoppingMessage,
                    startedAtUtc: _currentStatus.StartedAtUtc);
                processToStop = _activeProcess;
                cancellationSource = _activeCancellationSource;
                stoppingStatus = _currentStatus;
            }

            processToStop?.Stop();
            cancellationSource?.Cancel();
            return Task.FromResult(stoppingStatus);
        }

        private Task<RunStatusResponse> StartManagedActionAsync(
            string actionKind,
            string message,
            Func<CancellationToken, Task> executeAction)
        {
            RunStatusResponse startingStatus;
            CancellationTokenSource operationCts;

            lock (_stateLock)
            {
                if (IsActive(_currentStatus.Status))
                {
                    return Task.FromResult<RunStatusResponse>(null);
                }

                operationCts = new CancellationTokenSource();
                _activeCancellationSource = operationCts;

                startingStatus = CreateStatus(
                    runId: Guid.NewGuid().ToString("N"),
                    actionKind: actionKind,
                    status: "building",
                    message: message,
                    startedAtUtc: DateTimeOffset.UtcNow);

                _currentStatus = startingStatus;
            }

            _ = Task.Run(() => ExecuteManagedActionAsync(startingStatus, operationCts, executeAction), CancellationToken.None);
            return Task.FromResult(startingStatus);
        }

        private async Task ExecuteManagedActionAsync(
            RunStatusResponse startingStatus,
            CancellationTokenSource operationCts,
            Func<CancellationToken, Task> executeAction)
        {
            try
            {
                await executeAction(operationCts.Token);
            }
            catch (OperationCanceledException) when (operationCts.IsCancellationRequested)
            {
                CompleteAction(startingStatus.ActionKind, "stopped", null, GetStoppedMessage(startingStatus.ActionKind), startedAtUtc: startingStatus.StartedAtUtc);
            }
            catch (Exception exception)
            {
                CompleteAction(startingStatus.ActionKind, "failed", null, $"Operation failed: {exception.Message}", startedAtUtc: startingStatus.StartedAtUtc);
            }
            finally
            {
                lock (_stateLock)
                {
                    if (ReferenceEquals(_activeCancellationSource, operationCts))
                    {
                        _activeCancellationSource.Dispose();
                        _activeCancellationSource = null;
                        _activeProcess = null;
                    }
                }
            }
        }

        private async Task ExecuteBuildAsync(bool rebuild, CancellationToken cancellationToken)
        {
            var buildExitCode = await RunProcessAsync(new ProcessSpecification
            {
                FileName = "dotnet",
                WorkingDirectory = _paths.SolutionRoot,
                Arguments = rebuild
                    ? ["build", _paths.SolutionFilePath, "/t:Rebuild", "/p:Configuration=Debug"]
                    : ["build", _paths.SolutionFilePath, "/p:Configuration=Debug"]
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            CompleteAction(
                rebuild ? "rebuild" : "build",
                buildExitCode == 0 ? "succeeded" : "failed",
                buildExitCode,
                buildExitCode == 0
                    ? (rebuild ? "Lean rebuild completed successfully." : "Lean build completed successfully.")
                    : (rebuild ? "Lean rebuild failed." : "Lean build failed."));
        }

        private async Task ExecuteRunAsync(BacktestSettings settings, CancellationToken cancellationToken)
        {
            var buildExitCode = await RunProcessAsync(new ProcessSpecification
            {
                FileName = "dotnet",
                WorkingDirectory = _paths.SolutionRoot,
                Arguments = ["build", _paths.SolutionFilePath, "/p:Configuration=Debug"]
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (buildExitCode != 0)
            {
                CompleteAction("run", "failed", buildExitCode, "Lean build failed.");
                return;
            }

            var runExitCode = await RunProcessAsync(new ProcessSpecification
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
            }, cancellationToken, () => UpdateStatus("run", "running", "Launching Lean backtest."));

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            CompleteAction("run", runExitCode == 0 ? "succeeded" : "failed", runExitCode, runExitCode == 0 ? "Backtest completed successfully." : "Lean backtest failed.");
        }

        private async Task<int> RunProcessAsync(ProcessSpecification specification, CancellationToken cancellationToken, Action onStarted = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var process = _processRunner.Start(specification);
            lock (_stateLock)
            {
                _activeProcess = process;
            }

            onStarted?.Invoke();

            try
            {
                return await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                lock (_stateLock)
                {
                    if (ReferenceEquals(_activeProcess, process))
                    {
                        _activeProcess = null;
                    }
                }
            }
        }

        private void UpdateStatus(string actionKind, string status, string message)
        {
            lock (_stateLock)
            {
                _currentStatus = CreateStatus(
                    runId: _currentStatus.RunId,
                    actionKind: actionKind,
                    status: status,
                    message: message,
                    startedAtUtc: _currentStatus.StartedAtUtc);
            }
        }

        private void CompleteAction(string actionKind, string status, int? exitCode, string message, DateTimeOffset? startedAtUtc = null)
        {
            lock (_stateLock)
            {
                _currentStatus = CreateStatus(
                    runId: _currentStatus.RunId,
                    actionKind: actionKind,
                    status: status,
                    message: message,
                    exitCode: exitCode,
                    startedAtUtc: startedAtUtc ?? _currentStatus.StartedAtUtc,
                    finishedAtUtc: DateTimeOffset.UtcNow);
            }
        }

        private static RunStatusResponse CreateStatus(
            string runId,
            string actionKind,
            string status,
            string message,
            int? exitCode = null,
            DateTimeOffset? startedAtUtc = null,
            DateTimeOffset? finishedAtUtc = null)
        {
            var isBusy = IsActive(status);
            var canStop = status is "building" or "running";
            return new RunStatusResponse
            {
                RunId = runId,
                ActionKind = actionKind,
                Status = status,
                ExitCode = exitCode,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = finishedAtUtc,
                Message = message,
                CanBuild = !isBusy,
                CanRebuild = !isBusy,
                CanStart = !isBusy,
                CanStop = canStop
            };
        }

        private static bool IsActive(string status)
        {
            return status is "building" or "running" or "stopping";
        }

        private static string GetStoppedMessage(string actionKind)
        {
            return actionKind switch
            {
                "build" => "Lean build stopped.",
                "rebuild" => "Lean rebuild stopped.",
                "run" => "Backtest stopped by user.",
                _ => "Operation stopped."
            };
        }
    }
}
