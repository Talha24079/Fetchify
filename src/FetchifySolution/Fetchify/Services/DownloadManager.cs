using System.Collections.ObjectModel;
using Fetchify.Models;

namespace Fetchify.Services
{
    public static class DownloadManager
    {
        public static ObservableCollection<ActiveDownload> Downloads { get; } = new ObservableCollection<ActiveDownload>();
    }
}
