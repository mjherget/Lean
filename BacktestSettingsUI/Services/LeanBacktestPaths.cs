using System;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace QuantConnect.Lean.BacktestSettingsUI.Services
{
    public sealed class LeanBacktestPaths
    {
        public string SolutionRoot { get; }
        public string SolutionFilePath { get; }
        public string LauncherConfigFilePath { get; }
        public string LauncherWorkingDirectory { get; }
        public string LauncherAssemblyPath { get; }

        public LeanBacktestPaths(IHostEnvironment hostEnvironment)
            : this(hostEnvironment.ContentRootPath)
        {
        }

        public LeanBacktestPaths(string contentRootPath)
        {
            SolutionRoot = FindSolutionRoot(contentRootPath);
            SolutionFilePath = Path.Combine(SolutionRoot, "QuantConnect.Lean.sln");
            LauncherConfigFilePath = Path.Combine(SolutionRoot, "Launcher", "config.json");
            LauncherWorkingDirectory = Path.Combine(SolutionRoot, "Launcher", "bin", "Debug");
            LauncherAssemblyPath = Path.Combine(LauncherWorkingDirectory, "QuantConnect.Lean.Launcher.dll");
        }

        private static string FindSolutionRoot(string startingPath)
        {
            var directory = new DirectoryInfo(startingPath);
            while (directory != null)
            {
                var solutionPath = Path.Combine(directory.FullName, "QuantConnect.Lean.sln");
                if (File.Exists(solutionPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException($"Unable to locate QuantConnect.Lean.sln from '{startingPath}'.");
        }
    }
}
