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
                var safeDownloads = downloads.Select(d => new ActiveDownload
                {
                    Gid = d.Gid,
                    FileName = d.FileName,
                    Directory = d.Directory,
                    Url = d.Url,
                    Status = d.Status,
                    Progress = d.Progress,
                    EstimatedTimeRemaining = "--",
                    Speed = "0",
                    TotalSize = d.TotalSize
                }).ToList();

                string json = JsonSerializer.Serialize(safeDownloads, options);
                System.Diagnostics.Debug.WriteLine($"Saving {downloads} downloads to {HistoryFilePath}");
                await File.WriteAllTextAsync(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving download history: " + ex.Message);
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
                System.Diagnostics.Debug.WriteLine("Error loading download history: " + ex.Message);
                return new List<ActiveDownload>();
            }
        }
    }
}
