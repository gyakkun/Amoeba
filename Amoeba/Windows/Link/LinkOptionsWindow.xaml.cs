using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Amoeba.Properties;
using Library;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// Interaction logic for LinkOptionsWindow.xaml
    /// </summary>
    partial class LinkOptionsWindow : Window
    {
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<LinkViewModel> _downloadCollection = new ObservableCollectionEx<LinkViewModel>();
        private ObservableCollectionEx<LinkViewModel> _uploadCollection = new ObservableCollectionEx<LinkViewModel>();

        public LinkOptionsWindow(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            InitializeComponent();

            try
            {
                foreach (var item in Settings.Instance.LinkOptionsWindow_DownloadLinkItems)
                {
                    var viewModel = new LinkViewModel();
                    viewModel.Signature = item.Signature;
                    viewModel.TrustSignatures.AddRange(item.TrustSignatures);

                    _downloadCollection.Add(viewModel);
                }

                foreach (var item in Settings.Instance.LinkOptionsWindow_UploadLinkItems)
                {
                    var viewModel = new LinkViewModel();
                    viewModel.Signature = item.Signature;
                    viewModel.TrustSignatures.AddRange(item.TrustSignatures);

                    _uploadCollection.Add(viewModel);
                }
            }
            catch (Exception)
            {
                throw;
            }

            _downloadLinkListView.ItemsSource = _downloadCollection;
            _uploadLinkListView.ItemsSource = _uploadCollection;

            this.Sort();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        private void Sort()
        {
            _downloadLinkListView.Items.SortDescriptions.Clear();
            _downloadLinkListView.Items.SortDescriptions.Add(new SortDescription("Signature", ListSortDirection.Ascending));
            _downloadTrustSignatureListView.Items.SortDescriptions.Clear();
            _downloadTrustSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
            _uploadLinkListView.Items.SortDescriptions.Clear();
            _uploadLinkListView.Items.SortDescriptions.Add(new SortDescription("Signature", ListSortDirection.Ascending));
            _uploadTrustSignatureListView.Items.SortDescriptions.Clear();
            _uploadTrustSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
        }

        #region _downloadLink

        private void _downloadLinkListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _downloadLinkListView.SelectedItems;

            _downloadLinkListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _downloadLinkListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _downloadLinkListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Signature.Check(line[0]);
                }

                _downloadLinkListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _downloadLinkListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _downloadLinkListView.SelectedItems.OfType<LinkViewModel>().ToArray())
            {
                _downloadCollection.Remove(item);
            }
        }

        private void _downloadLinkListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _downloadLinkListViewCopyMenuItem_Click(null, null);
            _downloadLinkListViewDeleteMenuItem_Click(null, null);
        }

        private void _downloadLinkListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _downloadLinkListView.SelectedItems.OfType<LinkViewModel>().ToArray())
            {
                sb.AppendLine(item.Signature);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _downloadLinkListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Signature.Check(signature)) continue;

                var item = new LinkViewModel()
                {
                    Signature = signature,
                };

                if (_downloadCollection.Any(n => n.Signature == signature)) continue;
                _downloadCollection.Add(item);
            }
        }

        #endregion

        #region _downloadTrustSignature

        private void _downloadTrustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_downloadLinkListView.SelectedIndex == -1)
            {
                _downloadTrustSignatureListViewCopyMenuItem.IsEnabled = false;
            }
            else
            {
                var selectItems = _downloadTrustSignatureListView.SelectedItems;

                _downloadTrustSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            }
        }

        private void _downloadTrustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _downloadTrustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _uploadLink

        private void _uploadLinkListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _uploadLinkListView.SelectedItems;

            _uploadLinkListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _uploadLinkListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _uploadLinkListViewNemMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SignatureWindow window = new SignatureWindow();
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                var signature = window.DigitalSignature.ToString();

                var item = new LinkViewModel()
                {
                    Signature = signature,
                };

                if (_uploadCollection.Any(n => n.Signature == signature)) return;
                _uploadCollection.Add(item);
            }
        }

        private void _uploadLinkListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _uploadLinkListView.SelectedItems.OfType<LinkViewModel>().ToArray())
            {
                _uploadCollection.Remove(item);
            }
        }

        private void _uploadLinkListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _uploadLinkListView.SelectedItems.OfType<LinkViewModel>().ToArray())
            {
                sb.AppendLine(item.Signature);
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _uploadTrustSignature

        private void _uploadTrustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_uploadLinkListView.SelectedIndex == -1)
            {
                _uploadTrustSignatureListViewDeleteMenuItem.IsEnabled = false;
                _uploadTrustSignatureListViewCutMenuItem.IsEnabled = false;
                _uploadTrustSignatureListViewCopyMenuItem.IsEnabled = false;
                _uploadTrustSignatureListViewPasteMenuItem.IsEnabled = false;
            }
            else
            {
                var selectItems = _uploadTrustSignatureListView.SelectedItems;

                _uploadTrustSignatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
                _uploadTrustSignatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
                _uploadTrustSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

                {
                    bool flag = false;

                    if (Clipboard.ContainsText())
                    {
                        var line = Clipboard.GetText().Split('\r', '\n');
                        flag = Signature.Check(line[0]);
                    }

                    _uploadTrustSignatureListViewPasteMenuItem.IsEnabled = flag;
                }
            }
        }

        private void _uploadTrustSignatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = _uploadLinkListView.SelectedItem as LinkViewModel;
            if (viewModel == null) return;

            foreach (var item in _uploadTrustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                viewModel.TrustSignatures.Remove(item);
            }
        }

        private void _uploadTrustSignatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _uploadTrustSignatureListViewCopyMenuItem_Click(null, null);
            _uploadTrustSignatureListViewDeleteMenuItem_Click(null, null);
        }

        private void _uploadTrustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _uploadTrustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _uploadTrustSignatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = _uploadLinkListView.SelectedItem as LinkViewModel;
            if (viewModel == null) return;

            foreach (var signature in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Signature.Check(signature)) continue;

                if (viewModel.TrustSignatures.Contains(signature)) continue;
                viewModel.TrustSignatures.Add(signature);
            }
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            {
                List<LinkItem> downloadCollection = new List<LinkItem>();
                List<LinkItem> uploadCollection = new List<LinkItem>();

                foreach (var item in _downloadCollection)
                {
                    var LinkItem = new LinkItem();
                    LinkItem.Signature = item.Signature;
                    LinkItem.TrustSignatures.AddRange(item.TrustSignatures);
                    LinkItem.TrustSignatures.Sort();

                    downloadCollection.Add(LinkItem);
                }

                foreach (var item in _uploadCollection)
                {
                    var LinkItem = new LinkItem();
                    LinkItem.Signature = item.Signature;
                    LinkItem.TrustSignatures.AddRange(item.TrustSignatures);
                    LinkItem.TrustSignatures.Sort();

                    uploadCollection.Add(LinkItem);
                }

                foreach (var item in Settings.Instance.LinkOptionsWindow_DownloadLinkItems.ToArray())
                {
                    if (!downloadCollection.Contains(item))
                    {
                        Settings.Instance.LinkOptionsWindow_DownloadLinkItems.Remove(item);
                    }
                }

                foreach (var item in downloadCollection)
                {
                    if (!Settings.Instance.LinkOptionsWindow_DownloadLinkItems.Contains(item))
                    {
                        Settings.Instance.LinkOptionsWindow_DownloadLinkItems.Add(item);
                    }
                }

                foreach (var item in Settings.Instance.LinkOptionsWindow_UploadLinkItems.ToArray())
                {
                    if (!uploadCollection.Contains(item))
                    {
                        Settings.Instance.LinkOptionsWindow_UploadLinkItems.Remove(item);
                    }
                }

                foreach (var item in uploadCollection)
                {
                    if (!Settings.Instance.LinkOptionsWindow_UploadLinkItems.Contains(item))
                    {
                        Settings.Instance.LinkOptionsWindow_UploadLinkItems.Add(item);

                        {
                            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == item.Signature);
                            if (digitalSignature == null) return;

                            var link = new Link();
                            link.TrustSignatures.AddRange(item.TrustSignatures);

                            _amoebaManager.Upload(link, digitalSignature);
                        }
                    }
                }
            }

            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private class LinkViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            string _signature;

            public LinkViewModel()
            {
                this.TrustSignatures = new ObservableCollectionEx<string>();
            }

            public string Signature
            {
                get
                {
                    return _signature;
                }
                set
                {
                    if (value != _signature)
                    {
                        _signature = value;

                        this.NotifyPropertyChanged("Signature");
                    }
                }
            }

            public ObservableCollectionEx<string> TrustSignatures
            {
                get;
                private set;
            }
        }
    }
}
