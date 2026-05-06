using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public interface IProcessRunner
    {
        Task<int> RunAsync(ProcessSpecification specification, CancellationToken cancellationToken);
    }

    public sealed class ProcessSpecification
    {
        public string FileName { get; init; }
        public string WorkingDirectory { get; init; }
        public IReadOnlyList<string> Arguments { get; init; }
    }
}
