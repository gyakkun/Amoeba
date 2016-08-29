using System;
using System.Collections.Generic;
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
using Amoeba.Properties;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;
using Library;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Amoeba.Windows
{
    /// <summary>
    /// SeedInformationWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SeedInformationWindow : Window
    {
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private AmoebaManager _amoebaManager;
        private Seed _seed;

        private ObservableCollectionEx<string> _storeSignatureCollection = new ObservableCollectionEx<string>();

        public SeedInformationWindow(Seed seed, AmoebaManager amoebaManager)
        {
            if (seed == null) throw new ArgumentNullException(nameof(seed));
            if (amoebaManager == null) throw new ArgumentNullException(nameof(amoebaManager));

            _seed = seed;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            lock (_seed.ThisLock)
            {
                _nameTextBox.Text = _seed.Name;
                _keywordsTextBox.Text = string.Join(", ", _seed.Keywords);
                _signatureTextBox.Text = seed.Certificate?.ToString();
                _creationTimeTextBox.Text = seed.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                _lengthTextBox.Text = string.Format("{0} ({1:#,0} Byte)", NetworkConverter.ToSizeString(_seed.Length), _seed.Length);
                _commentTextBox.Text = _seed.Comment;
            }

            try
            {
                _storeSignatureListView.ItemsSource = _storeSignatureCollection;

                _storeTabItem.Cursor = Cursors.Wait;

                Task.Run(() =>
                {
                    return this.GetSignature(_seed);
                }).ContinueWith(task =>
                {
                    foreach (var signature in task.Result)
                    {
                        _storeSignatureCollection.Add(signature);
                    }

                    _storeTabItem.Cursor = null;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception)
            {

            }
        }

        private IEnumerable<string> GetSignature(Seed targetSeed)
        {
            var signatures = new HashSet<string>();

            {
                var storeTreeItems = new List<StoreTreeItem>();

                {
                    var storeCategorizeTreeItems = new List<StoreCategorizeTreeItem>();
                    storeCategorizeTreeItems.Add(Settings.Instance.StoreUploadControl_StoreCategorizeTreeItem);
                    storeCategorizeTreeItems.Add(Settings.Instance.StoreDownloadControl_StoreCategorizeTreeItem);

                    for (int i = 0; i < storeCategorizeTreeItems.Count; i++)
                    {
                        storeCategorizeTreeItems.AddRange(storeCategorizeTreeItems[i].Children);
                        storeTreeItems.AddRange(storeCategorizeTreeItems[i].StoreTreeItems);
                    }
                }

                foreach (var storeTreeItem in storeTreeItems)
                {
                    var boxList = new List<Box>();
                    var seedList = new HashSet<Seed>();

                    boxList.AddRange(storeTreeItem.Boxes);

                    for (int i = 0; i < boxList.Count; i++)
                    {
                        boxList.AddRange(boxList[i].Boxes);
                        seedList.UnionWith(boxList[i].Seeds);
                    }

                    if (seedList.Contains(targetSeed))
                    {
                        signatures.Add(storeTreeItem.Signature);
                    }
                }
            }

            {
                var searchSignatures = new HashSet<string>();

                foreach (var linkItem in Settings.Instance.Cache_LinkItems.Values.ToArray())
                {
                    searchSignatures.Add(linkItem.Signature);
                    searchSignatures.UnionWith(linkItem.TrustSignatures);
                }

                foreach (var signature in searchSignatures)
                {
                    var store = _amoebaManager.GetStore(signature);
                    if (store == null) continue;

                    var boxList = new List<Box>();
                    var seedList = new HashSet<Seed>();

                    boxList.AddRange(store.Boxes);

                    for (int i = 0; i < boxList.Count; i++)
                    {
                        boxList.AddRange(boxList[i].Boxes);
                        seedList.UnionWith(boxList[i].Seeds);
                    }

                    if (seedList.Contains(targetSeed))
                    {
                        signatures.Add(signature);
                    }
                }
            }

            return signatures.ToArray();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        #region _storeSignature

        private void _storeSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _storeSignatureListView.SelectedItems;

            _storeSignatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _storeSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _storeSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
