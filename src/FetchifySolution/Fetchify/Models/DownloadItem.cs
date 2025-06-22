using System.ComponentModel;

namespace Fetchify.Models
{
    public class DownloadItem : INotifyPropertyChanged
    {
        private string fileName;
        private string status = "Pending";
        private int progress = 0;
        private string speed = "0 KB/s";
        private string estimatedTimeRemaining = "Unknown";
        private string url;
        private string directory;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
