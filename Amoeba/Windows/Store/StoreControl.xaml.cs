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
    /// <summary>
    /// StoreControl.xaml の相互作用ロジック
    /// </summary>
    partial class StoreControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        public static TabType SelectTab { get; set; }

        public StoreControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            StoreDownloadControl storeDownloadControl = new StoreDownloadControl(_amoebaManager, _bufferManager);
            storeDownloadControl.Height = Double.NaN;
            storeDownloadControl.Width = Double.NaN;
            _storeDownloadTabItem.Content = storeDownloadControl;

            StoreUploadControl storeUploadControl = new StoreUploadControl(_amoebaManager, _bufferManager);
            storeUploadControl.Height = Double.NaN;
            storeUploadControl.Width = Double.NaN;
            _storeUploadTabItem.Content = storeUploadControl;

            LibraryControl libraryControl = new LibraryControl(_amoebaManager, _bufferManager);
            libraryControl.Height = Double.NaN;
            libraryControl.Width = Double.NaN;
            _libraryTabItem.Content = libraryControl;
        }

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tabControl.SelectedItem == _storeUploadTabItem)
            {
                StoreControl.SelectTab = TabType.Store_Upload;
            }
            else if (_tabControl.SelectedItem == _storeDownloadTabItem)
            {
                StoreControl.SelectTab = TabType.Store_Download;
            }
            else if (_tabControl.SelectedItem == _libraryTabItem)
            {
                StoreControl.SelectTab = TabType.Store_Library;
            }
            else
            {
                StoreControl.SelectTab = 0;
            }
        }
    }
}
