using Fetchify.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fetchify.Helpers
{
    public static class DownloadHistoryManager
    {
        private static readonly string HistoryFilePath = "downloads.json";

        public static async Task SaveDownloadsAsync(IEnumerable<ActiveDownload> downloads)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(downloads, options);
                await File.WriteAllTextAsync(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving download history: " + ex.Message);
            }
        }

        public static async Task<List<ActiveDownload>> LoadDownloadsAsync()
        {
            try
            {
                if (!File.Exists(HistoryFilePath))
                    return new List<ActiveDownload>();

                string json = await File.ReadAllTextAsync(HistoryFilePath);
                var downloads = JsonSerializer.Deserialize<List<ActiveDownload>>(json);
                return downloads ?? new List<ActiveDownload>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading download history: " + ex.Message);
                return new List<ActiveDownload>();
            }
        }
    }
}
