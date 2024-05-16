using System;
using System.Diagnostics;
using System.IO;

namespace NetshWfpViewer.Utilities
{
    internal static class WfpExecutor
    {
        public static bool TryGetWfpXml(out string wfpXml, out string error)
        {
            wfpXml = error = string.Empty;

            try
            {
                const string command = "netsh wfp show filters";
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
                        Arguments = $"/C {command} file={wfpFileName}",
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    error = $"Command: \"{command}\" failed. Exit code: {process.ExitCode}";
                    return false;
                }

                if (File.Exists(wfpFilePath))
                {
                    wfpXml = File.ReadAllText(wfpFilePath);
                    return true;
                }

                error = "File not found";
            }
            catch(Exception ex)
            {
                error = ex.Message;
            }

            return false;
        }
    }
}
