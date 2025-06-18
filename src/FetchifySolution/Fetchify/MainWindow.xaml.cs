using Fetchify.Helpers;
using Fetchify.Models;
using Fetchify.Services;
using Fetchify.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using WPF = System.Windows; 

namespace Fetchify
{
    public partial class MainWindow : WPF.Window
    {
        public ObservableCollection<ActiveDownload> Downloads { get; set; } = new();
        private DispatcherTimer refreshTimer;
        private Aria2RpcService rpcService = new();

        public MainWindow()
        {
            Aria2ProcessManager.StartAria2WithRPC();
            InitializeComponent();

            DownloadDataGrid.ItemsSource = Downloads;

            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void AddDownload_Click(object sender, WPF.RoutedEventArgs e)
        {
            var addWindow = new AddDownloadWindow();
            addWindow.DownloadStarted += StartDownload;
            addWindow.Owner = this;
            addWindow.ShowDialog();
        }

        private async void StartDownload(DownloadItem item)
        {
            // Actually call the aria2 RPC to add the download
            string gid = await rpcService.AddDownloadAsync(item.Url, item.Directory, item.FileName);

            if (!string.IsNullOrEmpty(gid) && !gid.StartsWith("Error"))
            {
                var activeDownload = new ActiveDownload
                {
                    Gid = gid,
                    FileName = item.FileName,
                    Status = "Queued",
                    Progress = item.Progress,
                    Speed = item.Speed,
                    EstimatedTimeRemaining = item.EstimatedTimeRemaining
                };

                Downloads.Add(activeDownload);
            }
            else
            {
                WPF.MessageBox.Show("Failed to start download: " + gid);
            }
        }

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var updatedDownloads = await rpcService.GetActiveDownloadsAsync();
                Downloads.Clear();
                foreach (var item in updatedDownloads)
                {
                    Downloads.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error refreshing downloads: " + ex.Message);
            }
        }
    }
}
