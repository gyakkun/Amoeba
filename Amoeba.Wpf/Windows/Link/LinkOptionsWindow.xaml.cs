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
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<string> _downloadSignatureCollection = new ObservableCollectionEx<string>();

        private ObservableCollectionEx<LinkViewModel> _uploadLinkCollection = new ObservableCollectionEx<LinkViewModel>();

        public LinkOptionsWindow(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _downloadSignatureCollection.AddRange(Settings.Instance.Global_TrustSignatures);

            _downloadSignatureListView.ItemsSource = _downloadSignatureCollection;

            foreach (var item in Settings.Instance.Global_LinkItems)
            {
                var viewModel = new LinkViewModel();
                viewModel.Signature = item.Signature;
                viewModel.TrustSignatures.AddRange(item.TrustSignatures);
                viewModel.DeleteSignatures.AddRange(item.DeleteSignatures);

                _uploadLinkCollection.Add(viewModel);
            }

            _uploadLinkListView.ItemsSource = _uploadLinkCollection;

            this.Sort();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        private void Sort()
        {
            _downloadSignatureListView.Items.SortDescriptions.Clear();
            _downloadSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));

            _uploadLinkListView.Items.SortDescriptions.Clear();
            _uploadLinkListView.Items.SortDescriptions.Add(new SortDescription("Signature", ListSortDirection.Ascending));
            _uploadTrustSignatureListView.Items.SortDescriptions.Clear();
            _uploadTrustSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
        }

        #region _downloadSignatureListView

        private void _downloadSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _downloadSignatureListView.SelectedItems;

            _downloadSignatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _downloadSignatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _downloadSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Signature.Check(line[0]);
                }

                _downloadSignatureListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _downloadSignatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in _downloadSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _downloadSignatureCollection.Remove(signature);
            }
        }

        private void _downloadSignatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _downloadSignatureListViewCopyMenuItem_Click(null, null);
            _downloadSignatureListViewDeleteMenuItem_Click(null, null);
        }

        private void _downloadSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var signature in _downloadSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(signature);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _downloadSignatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Signature.Check(signature)) continue;

                _downloadSignatureCollection.Add(signature);
            }
        }

        #endregion

        #region _uploadLinkListView

        private void _uploadLinkListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _uploadLinkListView.SelectedItems;

            _uploadLinkListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _uploadLinkListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _uploadLinkListViewNemMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new SignatureWindow();
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                var signature = window.DigitalSignature.ToString();

                var item = new LinkViewModel()
                {
                    Signature = signature,
                };

                if (_uploadLinkCollection.Any(n => n.Signature == signature)) return;
                _uploadLinkCollection.Add(item);
            }
        }

        private void _uploadLinkListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _uploadLinkListView.SelectedItems.OfType<LinkViewModel>().ToArray())
            {
                _uploadLinkCollection.Remove(item);
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

        #region _uploadTrustSignatureListView

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

                _uploadTrustSignatureListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _uploadTrustSignatureListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _uploadTrustSignatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

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

        #region _uploadUntrustSignatureListView

        private void _uploadUntrustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_uploadLinkListView.SelectedIndex == -1)
            {
                _uploadUntrustSignatureListViewDeleteMenuItem.IsEnabled = false;
                _uploadUntrustSignatureListViewCutMenuItem.IsEnabled = false;
                _uploadUntrustSignatureListViewCopyMenuItem.IsEnabled = false;
                _uploadUntrustSignatureListViewPasteMenuItem.IsEnabled = false;
            }
            else
            {
                var selectItems = _uploadUntrustSignatureListView.SelectedItems;

                _uploadUntrustSignatureListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _uploadUntrustSignatureListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _uploadUntrustSignatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

                {
                    bool flag = false;

                    if (Clipboard.ContainsText())
                    {
                        var line = Clipboard.GetText().Split('\r', '\n');
                        flag = Signature.Check(line[0]);
                    }

                    _uploadUntrustSignatureListViewPasteMenuItem.IsEnabled = flag;
                }
            }
        }

        private void _uploadUntrustSignatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = _uploadLinkListView.SelectedItem as LinkViewModel;
            if (viewModel == null) return;

            foreach (var item in _uploadUntrustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                viewModel.DeleteSignatures.Remove(item);
            }
        }

        private void _uploadUntrustSignatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _uploadUntrustSignatureListViewCopyMenuItem_Click(null, null);
            _uploadUntrustSignatureListViewDeleteMenuItem_Click(null, null);
        }

        private void _uploadUntrustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _uploadUntrustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _uploadUntrustSignatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = _uploadLinkListView.SelectedItem as LinkViewModel;
            if (viewModel == null) return;

            foreach (var signature in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Signature.Check(signature)) continue;

                if (viewModel.DeleteSignatures.Contains(signature)) continue;
                viewModel.DeleteSignatures.Add(signature);
            }
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Settings.Instance.ThisLock)
            {
                lock (Settings.Instance.Global_TrustSignatures.ThisLock)
                {
                    Settings.Instance.Global_TrustSignatures.Clear();
                    Settings.Instance.Global_TrustSignatures.AddRange(_downloadSignatureCollection);
                }
            }

            lock (Settings.Instance.ThisLock)
            {
                var uploadLinkCollection = new List<LinkItem>();

                foreach (var item in _uploadLinkCollection)
                {
                    var linkItem = new LinkItem();
                    linkItem.Signature = item.Signature;
                    linkItem.TrustSignatures.AddRange(item.TrustSignatures);
                    linkItem.DeleteSignatures.AddRange(item.DeleteSignatures);

                    uploadLinkCollection.Add(linkItem);
                }

                foreach (var item in Settings.Instance.Global_LinkItems.ToArray())
                {
                    if (!uploadLinkCollection.Contains(item))
                    {
                        Settings.Instance.Global_LinkItems.Remove(item);
                    }
                }

                foreach (var item in uploadLinkCollection)
                {
                    if (!Settings.Instance.Global_LinkItems.Contains(item))
                    {
                        Settings.Instance.Global_LinkItems.Add(item);

                        {
                            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == item.Signature);
                            if (digitalSignature == null) return;

                            var link = new Link();
                            link.TrustSignatures.AddRange(item.TrustSignatures);
                            link.DeleteSignatures.AddRange(item.DeleteSignatures);

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

        class LinkViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            private string _signature;

            public LinkViewModel()
            {
                this.TrustSignatures = new ObservableCollectionEx<string>();
                this.DeleteSignatures = new ObservableCollectionEx<string>();
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

                        this.NotifyPropertyChanged(nameof(this.Signature));
                    }
                }
            }

            public ObservableCollectionEx<string> TrustSignatures { get; private set; }
            public ObservableCollectionEx<string> DeleteSignatures { get; private set; }
        }
    }
}
