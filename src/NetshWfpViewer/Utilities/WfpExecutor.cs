using System;
using System.Diagnostics;
using System.IO;

namespace NetshWfpViewer.Utilities
{
    internal static class WfpExecutor
    {
        public const string NetshCommand = "netsh wfp show filters";

        public static string GetWfpXml()
        {
            const string wfpFileName = "wfpFilters.xml";

            var tmpDir = Path.GetTempPath();
            var wfpFilePath = Path.Combine(tmpDir, wfpFileName);
            var system32Path = Environment.Is64BitOperatingSystem
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System));
            var cmdPath = Path.Combine(system32Path, "cmd.exe");

            if (File.Exists(wfpFilePath))
            {
                File.Delete(wfpFilePath);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tmpDir,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = cmdPath,
                    Arguments = $"/C {NetshCommand} file={wfpFileName}",
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Command: \"{NetshCommand}\" failed. Exit code: {process.ExitCode}");
            }

            if (File.Exists(wfpFilePath))
            {
                return File.ReadAllText(wfpFilePath);
            }

            throw new FileNotFoundException($"File not found {wfpFileName}");
        }
    }
}
