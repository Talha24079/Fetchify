using System.Collections.ObjectModel;
using Fetchify.Models;

namespace Fetchify.Services
{
    public class DownloadManager
    {
        private static readonly DownloadManager _instance = new DownloadManager();
        public static DownloadManager Instance => _instance;

        public static ObservableCollection<ActiveDownload> Downloads { get; } = new ObservableCollection<ActiveDownload>();
    }
}
