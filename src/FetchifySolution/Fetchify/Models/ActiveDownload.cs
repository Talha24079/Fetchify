using System.ComponentModel;
using System.IO;

namespace Fetchify.Models
{
    public class ActiveDownload : INotifyPropertyChanged
    {
        private string gid;
        private string fileName;
        private string status;
        private int progress;
        private string speed;
        private string estimatedTimeRemaining;
        private string totalSize;
        private string url;
        private string directory;
        public string FullFilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Directory) || string.IsNullOrWhiteSpace(FileName))
                    return string.Empty;

                return Path.Combine(Directory, FileName);
            }
        }

        public string Aria2TempFilePath => FullFilePath + ".aria2";

        private DateTime startedAt;
        public string Url
        {
            get => url;
            set { url = value; OnPropertyChanged(nameof(Url)); }
        }

        public string Directory
        {
            get => directory;
            set { directory = value; OnPropertyChanged(nameof(Directory)); }
        }

        public DateTime StartedAt
        {
            get => startedAt;
            set { startedAt = value; OnPropertyChanged(nameof(StartedAt)); }
        }

        public string TotalSize
        {
            get => totalSize;
            set { totalSize = value; OnPropertyChanged(nameof(TotalSize)); }
        }

        public string Gid
        {
            get => gid;
            set { gid = value; OnPropertyChanged(nameof(Gid)); }
        }

        public string FileName
        {
            get => fileName;
            set { fileName = value; OnPropertyChanged(nameof(FileName)); }
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        public int Progress
        {
            get => progress;
            set { progress = value; OnPropertyChanged(nameof(Progress)); }
        }

        public string Speed
        {
            get => speed;
            set { speed = value; OnPropertyChanged(nameof(Speed)); }
        }

        public string EstimatedTimeRemaining
        {
            get => estimatedTimeRemaining;
            set { estimatedTimeRemaining = value; OnPropertyChanged(nameof(EstimatedTimeRemaining)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
