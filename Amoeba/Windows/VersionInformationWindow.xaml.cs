using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Amoeba.Windows
{
    /// <summary>
    /// VersionInformationWindow.xaml の相互作用ロジック
    /// </summary>
    partial class VersionInformationWindow : Window
    {
        public VersionInformationWindow()
        {
            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<VersionListViewItem> items = new List<VersionListViewItem>();
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll", SearchOption.TopDirectoryOnly));
            files.AddRange(Directory.GetFiles(Directory.GetCurrentDirectory(), "*.exe", SearchOption.TopDirectoryOnly));
            files.Sort((x, y) =>
            {
                return System.IO.Path.GetFileName(x).CompareTo(System.IO.Path.GetFileName(y));
            });

            foreach (var path in files)
            {
                var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                VersionListViewItem item = new VersionListViewItem();
                item.FileName = System.IO.Path.GetFileName(path);
                item.Version = info.FileVersion;

                items.Add(item);
            }

            foreach (var item in items)
            {
                _versionListView.Items.Add(item);
            }
        }

        private void _licenseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "notepad.exe";
                info.Arguments = @"Properties\Amoeba.License";
                info.UseShellExecute = true;

                using (Process process = Process.Start(info))
                {
                    process.WaitForExit();
                }
            }
            catch (Exception)
            {

            }
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private class VersionListViewItem
        {
            public string FileName { get; set; }
            public string Version { get; set; }
        }
    }
}
