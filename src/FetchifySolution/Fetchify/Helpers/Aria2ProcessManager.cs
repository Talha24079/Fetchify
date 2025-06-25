using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WPF = System.Windows;

namespace Fetchify.Helpers
{
    internal static class Aria2ProcessManager
    {
        private static Process? aria2Process;
        private static readonly string LogFile = "aria2.log";

        public static void StartAria2WithRPC()
        {
            string exePath = "aria2c";
            string configPath = "aria2.conf";

            if (!File.Exists(configPath))
            {
                WPF.MessageBox.Show("Missing 'aria2.conf'. Aria2 cannot start.", "Startup Error",
                    WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
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
                aria2Process = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                aria2Process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        Log("[Aria2 Output] " + e.Data);
                };

                aria2Process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        Log("[Aria2 Error] " + e.Data);
                };

                aria2Process.Start();
                aria2Process.BeginOutputReadLine();
                aria2Process.BeginErrorReadLine();

                Log("[Aria2] ✅ aria2c started successfully.");
                Task.Delay(1000).Wait(); // Wait 1 second for RPC to bind
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show("Failed to start Aria2:\n\n" + ex.Message, "Aria2 Startup Error",
                    WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
                Log("[Aria2] ❌ Error starting aria2c: " + ex.Message);
            }
        }

        public static void StopAria2()
        {
            try
            {
                if (aria2Process != null && !aria2Process.HasExited)
                {
                    aria2Process.Kill(true);
                    aria2Process.Dispose();
                    Log("[Aria2] 🔴 aria2c process terminated.");
                }

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
            catch (Exception ex)
            {
                Log("[Aria2] ⚠️ Error stopping aria2c: " + ex.Message);
            }
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now}] {message}\n");
            }
            catch
            {
                // Silent fail if logging fails
            }
        }
    }
}
