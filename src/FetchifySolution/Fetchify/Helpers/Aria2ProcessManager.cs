using System.Diagnostics;
using System.IO;

namespace Fetchify.Helpers
{
    internal class Aria2ProcessManager
    {
        public static void StartAria2WithRPC()
        {
            string exePath = "aria2c"; // Can also give full path
            string configPath = "aria2.conf"; // Must exist

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
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting aria2c: " + ex.Message);
            }
        }
    }
}
