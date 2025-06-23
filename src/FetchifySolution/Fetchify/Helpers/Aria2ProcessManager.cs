using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Fetchify.Helpers
{
    internal static class Aria2ProcessManager
    {
        private static Process? aria2Process;

        public static void StartAria2WithRPC()
        {
            string exePath = "aria2c";
            string configPath = "aria2.conf";

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Missing aria2.conf! Aria2 will not start.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"--conf-path=\"{configPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                aria2Process = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting aria2c: " + ex.Message);
            }
        }

        public static void StopAria2()
        {
            try
            {
                // Preferred: kill the instance we started
                if (aria2Process != null && !aria2Process.HasExited)
                {
                    aria2Process.Kill(true);
                    aria2Process.Dispose();
                }

                // Fallback: kill any remaining aria2c processes (safe only if you're not running other aria2c manually)
                foreach (var proc in Process.GetProcessesByName("aria2c"))
                {
                    try
                    {
                        proc.Kill(true);
                        proc.Dispose();
                    }
                    catch { }
                }

                aria2Process = null;
            }
            catch
            {
                // Optional: log
            }
        }
    }
}
