MainWindow.xaml:
<Window x:Class="Fetchify.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Fetchify" Height="450" Width="800">
    <Grid>
        <Button Content="Add Download"
        Width="120"
        Height="30"
        HorizontalAlignment="Left"
        VerticalAlignment="Top"
        Margin="10"
        Click="AddDownload_Click"/>

        <ListView Name="DownloadListView" Margin="10,50,10,10">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="File Name" DisplayMemberBinding="{Binding FileName}" Width="200"/>
                    <GridViewColumn Header="Status" DisplayMemberBinding="{Binding Status}" Width="100"/>
                    <GridViewColumn Header="Progress" Width="100">
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding Progress, StringFormat={}{0}%}" HorizontalAlignment="Center"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Header="Speed" DisplayMemberBinding="{Binding Speed}" Width="100"/>
                    <GridViewColumn Header="ETA" DisplayMemberBinding="{Binding EstimatedTimeRemaining}" Width="120"/>
                </GridView>
            </ListView.View>
        </ListView>
    </Grid>
</Window>
------------------------------------------------------------------------------------------------------------------
MainWindow.xaml.cs:
using System;
using System.IO;
using System.Windows;
using Fetchify.Helpers;
using Fetchify.Models;
using Fetchify.Services;
using WinForms = System.Windows.Forms;
using WPF = System.Windows;

namespace Fetchify.Views
{
    public partial class AddDownloadWindow : Window
    {
        public string DownloadUrl { get; private set; }
        public delegate void DownloadStartedHandler(DownloadItem item);
        public event DownloadStartedHandler DownloadStarted;
        public AddDownloadWindow()
        {
            InitializeComponent();
        }

        private void StartDownload_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text.Trim();         // Make sure this is not empty
            string directory = DirectoryTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(directory))
            {
                WPF.MessageBox.Show("URL and directory are required.");
                return;
            }

            string fileName = System.IO.Path.GetFileName(new Uri(url).AbsolutePath);
            string outputArg = $"--out=\"{fileName}\"";

            // This is only to call aria2c directly; not needed if using RPC
            // Aria2Helper.StartDownload(url, directory, outputArg);

            var item = new DownloadItem
            {
                Url = url,               // Make sure this exists in the model
                Directory = directory,   // Make sure this exists in the model
                FileName = fileName,
                Status = "Starting",
                Progress = 0,
                Speed = "",
                EstimatedTimeRemaining = ""
            };

            DownloadStarted?.Invoke(item);  // Must match delegate (object sender, DownloadItem item)
            Close();
        }






        private void BrowseDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    DirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
