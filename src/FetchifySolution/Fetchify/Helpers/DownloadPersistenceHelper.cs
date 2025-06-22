using Fetchify.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Fetchify.Helpers
{
    public static class DownloadPersistenceHelper
    {
        private static readonly string SavePath = "downloads.json";

        public static void SaveDownloadsToFile(List<ActiveDownload> downloads)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(downloads, options);
            File.WriteAllText(SavePath, json);
        }

        public static List<ActiveDownload> LoadDownloadsFromFile()
        {
            if (!File.Exists(SavePath))
                return new List<ActiveDownload>();

            var json = File.ReadAllText(SavePath);
            return JsonSerializer.Deserialize<List<ActiveDownload>>(json) ?? new List<ActiveDownload>();
        }
    }
}
