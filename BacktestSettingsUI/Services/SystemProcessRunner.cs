using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class SystemProcessRunner : IProcessRunner
    {
        public async Task<int> RunAsync(ProcessSpecification specification, CancellationToken cancellationToken)
        {
            using var process = new Process
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
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
    }
}
