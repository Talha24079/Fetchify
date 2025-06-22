using Fetchify.Helpers;
using Fetchify.Models;
using Fetchify.Services;
using Fetchify.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
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

            _ = LoadPreviousDownloadsAsync();
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
                    EstimatedTimeRemaining = item.EstimatedTimeRemaining,
                    Url = item.Url,
                    Directory = item.Directory,
                    StartedAt = DateTime.Now
                };

                Downloads.Add(activeDownload);
                await DownloadHistoryManager.SaveDownloadsAsync(Downloads);

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
                var updatedList = await rpcService.GetAllDownloadsAsync();

                foreach (var updatedItem in updatedList)
                {
                    var existing = Downloads.FirstOrDefault(d => d.Gid == updatedItem.Gid);
                    if (existing != null)
                    {
                        // Update properties
                        existing.Status = updatedItem.Status;
                        existing.Progress = updatedItem.Progress;
                        existing.Speed = updatedItem.Speed;
                        existing.EstimatedTimeRemaining = updatedItem.EstimatedTimeRemaining;
                        existing.TotalSize = updatedItem.TotalSize;
                    }
                    else
                    {
                        Downloads.Add(updatedItem);
                    }
                }

                // Optional: remove items that no longer exist (only if desired)
                var gidsInUpdated = updatedList.Select(d => d.Gid).ToHashSet();
                for (int i = Downloads.Count - 1; i >= 0; i--)
                {
                    if (!gidsInUpdated.Contains(Downloads[i].Gid))
                    {
                        Downloads.RemoveAt(i);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Refresh error: " + ex.Message);
            }
        }


        private async void PauseDownload_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
            {
                WPF.MessageBox.Show("Please select a download to pause.");
                return;
            }

            bool success = await Aria2Helper.PauseDownloadAsync(selected.Gid);
            WPF.MessageBox.Show(success ? "Paused successfully." : "Pause failed.");
            await DownloadHistoryManager.SaveDownloadsAsync(Downloads);
        }

        private async void ResumeDownload_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
            {
                WPF.MessageBox.Show("Please select a download to resume.");
                return;
            }

            bool success = await Aria2Helper.ResumeDownloadAsync(selected.Gid);
            WPF.MessageBox.Show(success ? "Resumed successfully." : "Resume failed.");
            await DownloadHistoryManager.SaveDownloadsAsync(Downloads);

        }

        private async Task LoadPreviousDownloadsAsync()
        {
            try
            {
                var savedDownloads = await DownloadHistoryManager.LoadDownloadsAsync();
                Downloads.Clear();

                foreach (var download in savedDownloads)
                {
                    Downloads.Add(download);
                }

                var liveDownloads = await rpcService.GetAllDownloadsAsync();

                foreach (var item in liveDownloads)
                {
                    var existing = Downloads.FirstOrDefault(d => d.Gid == item.Gid);
                    if (existing != null)
                    {
                        existing.Status = item.Status;
                        existing.Progress = item.Progress;
                        existing.Speed = item.Speed;
                        existing.EstimatedTimeRemaining = item.EstimatedTimeRemaining;
                        existing.TotalSize = item.TotalSize;
                    }
                    else
                    {
                        Downloads.Add(item);
                    }
                }

                await DownloadHistoryManager.SaveDownloadsAsync(Downloads);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading previous downloads: " + ex.Message);
            }
        }

        private async void RemoveDownload_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
            {
                WPF.MessageBox.Show("Please select a download to remove.");
                return;
            }

            var result = WPF.MessageBox.Show(
                $"Are you sure you want to remove the download: {selected.FileName}?\nThis will permanently remove it from Fetchify and aria2.",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            // This now handles paused, active, and stopped tasks
            bool removed = await Aria2Helper.RemoveDownloadAsync(selected.Gid);

            if (removed)
            {
                Downloads.Remove(selected);
                DownloadPersistenceHelper.SaveDownloadsToFile(Downloads.ToList());
                WPF.MessageBox.Show("Download removed successfully.");
            }
            else
            {
                WPF.MessageBox.Show("Failed to remove the download from aria2.");
            }
        }





    }
}
