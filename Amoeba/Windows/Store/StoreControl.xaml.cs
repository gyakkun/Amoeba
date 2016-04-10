using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Amoeba.Properties;
using Library;
using Library.Collections;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    enum StoreControlTabType
    {
        Download,
        Upload,
        Library,
    }

    /// <summary>
    /// StoreControl.xaml の相互作用ロジック
    /// </summary>
    partial class StoreControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile StoreControlTabType _selectedTab;

        public StoreControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            var storeDownloadControl = new StoreDownloadControl(this, _amoebaManager, _bufferManager);
            storeDownloadControl.Height = Double.NaN;
            storeDownloadControl.Width = Double.NaN;
            _storeDownloadTabItem.Content = storeDownloadControl;

            var storeUploadControl = new StoreUploadControl(this, _amoebaManager, _bufferManager);
            storeUploadControl.Height = Double.NaN;
            storeUploadControl.Width = Double.NaN;
            _storeUploadTabItem.Content = storeUploadControl;

            var libraryControl = new LibraryControl(this, _amoebaManager, _bufferManager);
            libraryControl.Height = Double.NaN;
            libraryControl.Width = Double.NaN;
            _libraryTabItem.Content = libraryControl;
        }

        public StoreControlTabType SelectedTab
        {
            get
            {
                return _selectedTab;
            }
            private set
            {
                _selectedTab = value;
            }
        }

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != _tabControl) return;

            if (_tabControl.SelectedItem == _storeUploadTabItem)
            {
                this.SelectedTab = StoreControlTabType.Upload;
            }
            else if (_tabControl.SelectedItem == _storeDownloadTabItem)
            {
                this.SelectedTab = StoreControlTabType.Download;
            }
            else if (_tabControl.SelectedItem == _libraryTabItem)
            {
                this.SelectedTab = StoreControlTabType.Library;
            }
            else
            {
                this.SelectedTab = 0;
            }

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
        }
    }
}