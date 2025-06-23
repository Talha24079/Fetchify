using System;

namespace Fetchify.Models
{
    public class SettingsModel
    {
        public string DefaultDownloadDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public int MaxConcurrentDownloads { get; set; } = 3;
        public string Aria2RpcHost { get; set; } = "localhost";
        public int Aria2RpcPort { get; set; } = 6800;
        public string Aria2Token { get; set; } = "";
        public bool AutoStartAria2 { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
    }
}
