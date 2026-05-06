using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class SystemProcessRunner : IProcessRunner
    {
        public IManagedProcess Start(ProcessSpecification specification)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = specification.FileName,
                    WorkingDirectory = specification.WorkingDirectory,
                    UseShellExecute = false
                }
            };

            foreach (var argument in specification.Arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.Start();
            return new ManagedProcess(process);
        }

        private sealed class ManagedProcess : IManagedProcess
        {
            private readonly Process _process;

            public ManagedProcess(Process process)
            {
                _process = process;
            }

            public async Task<int> WaitForExitAsync(CancellationToken cancellationToken)
            {
                using (_process)
                {
                    await _process.WaitForExitAsync(cancellationToken);
                    return _process.ExitCode;
                }
            }

            public void Stop()
            {
                try
                {
                    if (_process.HasExited)
                    {
                        return;
                    }

                    _process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort stop; the run service will reflect the final process outcome.
                }
            }
        }
    }
}
