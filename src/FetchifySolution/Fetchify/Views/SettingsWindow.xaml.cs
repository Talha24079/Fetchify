using Fetchify.Helpers;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using WinForms = System.Windows.Forms;
using WPF = System.Windows;

namespace Fetchify.Views
{
    public partial class SettingsWindow : WPF.Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var s = SettingsManager.CurrentSettings;
            DownloadDirBox.Text = s.DefaultDownloadDirectory;
            MaxConcurrentBox.Text = s.MaxConcurrentDownloads.ToString();
            RpcHostBox.Text = s.Aria2RpcHost;
            RpcPortBox.Text = s.Aria2RpcPort.ToString();
            TokenBox.Password = s.Aria2Token;
            AutoStartCheck.IsChecked = s.AutoStartAria2;
            NotificationsCheck.IsChecked = s.EnableNotifications;
        }

        private void Save_Click(object sender, WPF.RoutedEventArgs e)
        {
            string directory = DownloadDirBox.Text.Trim();
            string maxText = MaxConcurrentBox.Text.Trim();
            string host = RpcHostBox.Text.Trim();
            string portText = RpcPortBox.Text.Trim();
            string token = TokenBox.Password?.Trim();

            // Validate Directory
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                WPF.MessageBox.Show("Please select a valid download directory.", "Validation Error", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Warning);
                return;
            }

            // Validate Max Concurrent Downloads
            if (!int.TryParse(maxText, out int maxConcurrent) || maxConcurrent < 1 || maxConcurrent > 100)
            {
                WPF.MessageBox.Show("Max Concurrent Downloads must be a number between 1 and 100.", "Validation Error", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Warning);
                return;
            }

            // Validate RPC Host
            if (string.IsNullOrWhiteSpace(host) || !IsValidHost(host))
            {
                WPF.MessageBox.Show("Please enter a valid RPC host (e.g., localhost or 192.168.0.1).", "Validation Error", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Warning);
                return;
            }

            // Validate RPC Port
            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                WPF.MessageBox.Show("RPC Port must be a number between 1 and 65535.", "Validation Error", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Warning);
                return;
            }

            // Optional: Token validation
            if (!string.IsNullOrEmpty(token) && token.Length < 4)
            {
                WPF.MessageBox.Show("Token must be at least 4 characters long if used.", "Validation Error", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Warning);
                return;
            }

            // Save if all validated
            var s = SettingsManager.CurrentSettings;
            s.DefaultDownloadDirectory = directory;
            s.MaxConcurrentDownloads = maxConcurrent;
            s.Aria2RpcHost = host;
            s.Aria2RpcPort = port;
            s.Aria2Token = token;
            s.AutoStartAria2 = AutoStartCheck.IsChecked == true;
            s.EnableNotifications = NotificationsCheck.IsChecked == true;

            SettingsManager.Save();
            this.DialogResult = true;
            this.Close();
        }

        private bool IsValidHost(string host)
        {
            // Allow localhost, IP addresses, and simple hostnames
            return Regex.IsMatch(host, @"^(localhost|[\w\-\.]+)$");
        }

        private void Cancel_Click(object sender, WPF.RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Browse_Click(object sender, WPF.RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    DownloadDirBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = WPF.MessageBox.Show("Are you sure you want to restore default settings?", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            SettingsManager.RestoreDefaults();
            LoadSettings();
        }

    }
}
