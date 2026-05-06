using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public interface IProcessRunner
    {
        IManagedProcess Start(ProcessSpecification specification);
    }

    public interface IManagedProcess
    {
        Task<int> WaitForExitAsync(CancellationToken cancellationToken);
        void Stop();
    }

    public sealed class ProcessSpecification
    {
        public string FileName { get; init; }
        public string WorkingDirectory { get; init; }
        public IReadOnlyList<string> Arguments { get; init; }
    }
}
