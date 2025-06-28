using Fetchify.Models;
using System.IO;
using System.Text.Json;

namespace Fetchify.Helpers
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = "settings.json";

        public static SettingsModel CurrentSettings { get; private set; } = new SettingsModel();

        public static void Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    CurrentSettings = JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
                    if (string.IsNullOrWhiteSpace(CurrentSettings.DefaultDownloadDirectory))
                    {
                        CurrentSettings.DefaultDownloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                }
                catch
                {
                    CurrentSettings = new SettingsModel(); // Fallback
                }
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Optional: handle error
            }
        }

        public static void RestoreDefaults()
        {
            CurrentSettings = new SettingsModel();
            Save();
        }

    }
}
