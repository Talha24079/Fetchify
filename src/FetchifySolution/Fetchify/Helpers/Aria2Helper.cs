using System;
using System.Diagnostics;

namespace Fetchify.Helpers
{
    public static class Aria2Helper
    {
        public static void StartDownload(string url, string directory, string outputFileName = "")
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("URL and directory must not be empty.");
            }

            // Construct output argument if provided
            string outputArg = !string.IsNullOrEmpty(outputFileName) ? $"--out=\"{outputFileName}\"" : "";

            string args = $"--dir=\"{directory}\" {outputArg} \"{url}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "aria2c",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                    process.ErrorDataReceived += (sender, e) => Console.WriteLine("Error: " + e.Data);

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start aria2c process: " + ex.Message);
            }
        }
    }
}
