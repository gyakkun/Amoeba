using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amoeba.Properties;
using Library;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// LibraryControl.xaml の相互作用ロジック
    /// </summary>
    partial class LibraryControl : UserControl
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private List<object> _listViewItemCollection;

        public LibraryControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _listViewItemCollection = new List<object>();

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;
            _boxTreeViewItem.Value = Settings.Instance.LibraryControl_Box;

            this.Update();
        }

        private static bool BoxDigitalSignatureCheck(ref Box box)
        {
            bool flag = true;
            var seedList = new List<Seed>();
            var boxList = new List<Box>();
            boxList.Add(box);

            for (int i = 0; i < boxList.Count; i++)
            {
                boxList.AddRange(boxList[i].Boxes);
                seedList.AddRange(boxList[i].Seeds);
            }

            foreach (var item in seedList)
            {
                if (!item.VerifyCertificate())
                {
                    flag = false;

                    item.CreateCertificate(null);
                }
            }

            foreach (var item in boxList)
            {
                if (!item.VerifyCertificate())
                {
                    flag = false;

                    item.CreateCertificate(null);
                }
            }

            return flag;
        }

        private bool DigitalSignatureRelease(IEnumerable<Box> boxTreeViewItemCollectiion)
        {
            List<Box> boxList = new List<Box>();
            StringBuilder builder = new StringBuilder();

            foreach (var item in boxTreeViewItemCollectiion)
            {
                if (item.Certificate != null)
                {
                    boxList.Add(item);
                    builder.AppendLine(string.Format("\"{0}\"", item.Name));
                }
            }

            if (boxList.Count == 0) return true;

            if (MessageBox.Show(
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.LibraryControl_DigitalSignatureAnnulled_Message),
                    "Digital Signature",
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                foreach (var item in boxList)
                {
                    item.CreateCertificate(null);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static long GetBoxLength(Box box)
        {
            long length = 0;

            foreach (var item in box.Seeds)
            {
                length += item.Length;
            }

            foreach (var item in box.Boxes)
            {
                length += LibraryControl.GetBoxLength(item);
            }

            return length;
        }

        private static string GetNormalizedPath(string path)
        {
            string filePath = path;

            foreach (char ic in System.IO.Path.GetInvalidFileNameChars())
            {
                filePath = filePath.Replace(ic.ToString(), "-");
            }
            foreach (char ic in System.IO.Path.GetInvalidPathChars())
            {
                filePath = filePath.Replace(ic.ToString(), "-");
            }

            return filePath;
        }

        private void Update()
        {
            Settings.Instance.LibraryControl_Box = _boxTreeViewItem.Value;

            _boxTreeView_SelectedItemChanged(this, null);
        }

        #region _listView

        private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _listView_PreviewMouseLeftButtonDown(this, null);
        }

        private void _listView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void _listView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_listView.GetCurrentIndex(e.GetPosition) < 0) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
                if (selectBoxListViewItem == null) return;

                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var selectBox = selectBoxListViewItem.Value;
                var item = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => n.Value == selectBox);

                try
                {
                    selectBoxTreeViewItem.IsExpanded = true;
                    item.IsSelected = true;
                }
                catch (Exception)
                {

                }
            }
            else if (_listView.SelectedItem is SeedListViewItem)
            {
                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var selectSeedListViewItem = _listView.SelectedItem as SeedListViewItem;
                if (selectSeedListViewItem == null) return;

                var seed = selectSeedListViewItem.Value;
                string baseDirectory = "";

                foreach (var item in _boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value.Name))
                {
                    baseDirectory += string.Format("{0}{1}", LibraryControl.GetNormalizedPath(item), "/");
                }

                var path = System.IO.Path.Combine(baseDirectory, seed.Name);

                if (File.Exists(System.IO.Path.Combine(_amoebaManager.DownloadDirectory, path))) return;

                if (_amoebaManager.DownloadingInformation.Any(n =>
                    (n.Contains("Path") && ((string)n["Path"]) == baseDirectory)
                    && (n.Contains("Name") && ((string)n["Name"]) == seed.Name)))
                {
                    return;
                }

                _amoebaManager.Download(seed.DeepClone(), baseDirectory);
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_listViewEditMenuItem != null) _listViewEditMenuItem.IsEnabled = (_listView.SelectedItems.Count > 0);
            if (_listViewDeleteMenuItem != null) _listViewDeleteMenuItem.IsEnabled = (_listView.SelectedItems.Count > 0);
            if (_listViewCutMenuItem != null) _listViewCutMenuItem.IsEnabled = (_listView.SelectedItems.Count > 0);
            if (_listViewCopyMenuItem != null) _listViewCopyMenuItem.IsEnabled = (_listView.SelectedItems.Count > 0);
            if (_listViewCopyInfoMenuItem != null) _listViewCopyInfoMenuItem.IsEnabled = (_listView.SelectedItems.Count > 0);
            if (_listViewUploadMenuItem != null) _listViewUploadMenuItem.IsEnabled = (_listView.SelectedItems.Count > 0);
            if (_listViewDownloadMenuItem != null) _listViewDownloadMenuItem.IsEnabled = (_listView.SelectedItems.Count > 0);
        }

        private void _listViewAddBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var box = new Box() { Name = "New Box" };

            BoxEditWindow window = new BoxEditWindow(ref box);

            if (true == window.ShowDialog())
            {
                selectBoxTreeViewItem.Value.Boxes.Add(box);
                selectBoxTreeViewItem.IsExpanded = true;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItem is BoxListViewItem)
            {
                var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
                if (selectBoxListViewItem == null) return;

                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectBox = selectBoxListViewItem.Value;

                BoxEditWindow window = new BoxEditWindow(ref selectBox);
                window.ShowDialog();

                selectBoxTreeViewItem.Update();
            }
            else if (_listView.SelectedItem is SeedListViewItem)
            {
                var selectSeedListViewItem = _listView.SelectedItem as SeedListViewItem;
                if (selectSeedListViewItem == null) return;

                var selectSeedTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                if (selectSeedTreeViewItem == null) return;

                if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectSeedTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectSeed = selectSeedListViewItem.Value;

                SeedEditWindow window = new SeedEditWindow(ref selectSeed, _amoebaManager);
                window.ShowDialog();

                selectSeedTreeViewItem.Update();
            }

            this.Update();
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            foreach (var item in boxes)
            {
                selectBoxTreeViewItem.Value.Boxes.Remove(item);
            }

            foreach (var item in seeds)
            {
                selectBoxTreeViewItem.Value.Seeds.Remove(item);
            }

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            Clipboard.SetBoxes(boxes.Select(n => n.DeepClone()));
            Clipboard.SetSeeds(seeds.Select(n => n.DeepClone()));

            foreach (var item in boxes)
            {
                selectBoxTreeViewItem.Value.Boxes.Remove(item);
            }

            foreach (var item in seeds)
            {
                selectBoxTreeViewItem.Value.Seeds.Remove(item);
            }

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            Clipboard.SetBoxes(boxes.Select(n => n.DeepClone()));
            Clipboard.SetSeeds(seeds.Select(n => n.DeepClone()));
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            StringBuilder builder = new StringBuilder();

            foreach (var item in boxes)
            {
                builder.AppendLine(MessageConverter.ToInfoMessage(item));
            }

            foreach (var item in seeds)
            {
                builder.AppendLine(MessageConverter.ToInfoMessage(item));
            }

            Clipboard.SetText(builder.ToString());
        }

        private void _listViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes().Select(n => n.DeepClone()));
            selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds().Select(n => n.DeepClone()));

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(LanguagesManager.Instance.LibraryControl_KeyUpload_Message, "Key Upload", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                var keys = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

                foreach (var key in keys)
                {
                    _amoebaManager.Upload(key);
                }
            }
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);
            string baseDirectory = "";

            foreach (var item in _boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value.Name))
            {
                baseDirectory += string.Format("{0}{1}", LibraryControl.GetNormalizedPath(item), "/");
            }

            foreach (var seed in seeds)
            {
                var downloadDirectory = baseDirectory;
                var path = System.IO.Path.Combine(baseDirectory, seed.Name);

                if (File.Exists(System.IO.Path.Combine(_amoebaManager.DownloadDirectory, path))) continue;

                if (_amoebaManager.DownloadingInformation.Any(n =>
                    (n.Contains("Path") && ((string)n["Path"]) == downloadDirectory)
                    && (n.Contains("Name") && ((string)n["Name"]) == seed.Name)))
                {
                    continue;
                }

                _amoebaManager.Download(seed.DeepClone(), downloadDirectory);
            }

            foreach (var box in boxes)
            {
                var boxList = new List<Box>();
                var seedList = new List<Seed>();
                var keyDictionary = new Dictionary<string, List<Seed>>();

                boxList.Add(box);

                for (int i = 0; i < boxList.Count; i++)
                {
                    boxList.AddRange(boxList[i].Boxes);
                }

                foreach (var item in boxList)
                {
                    seedList.AddRange(item.Seeds);
                }

                foreach (var seed in seedList)
                {
                    List<string> paths = new List<string>();
                    Box tempDirectory = null;

                    for (int i = boxList.Count - 1; i >= 0; i--)
                    {
                        if (tempDirectory == null)
                        {
                            if (boxList[i].Seeds.Contains(seed))
                            {
                                paths.Add(boxList[i].Name);
                                tempDirectory = boxList[i];
                            }
                        }
                        else
                        {
                            if (boxList[i].Boxes.Contains(tempDirectory))
                            {
                                paths.Add(boxList[i].Name);
                                tempDirectory = boxList[i];
                            }
                        }
                    }

                    string path = "";
                    paths.Reverse();

                    foreach (var item in paths)
                    {
                        path += string.Format("{0}{1}", LibraryControl.GetNormalizedPath(item), "/");
                    }

                    if (!keyDictionary.ContainsKey(path))
                        keyDictionary[path] = new List<Seed>();

                    keyDictionary[path].Add(seed);
                }

                foreach (var path in keyDictionary.Keys)
                {
                    foreach (var seed in keyDictionary[path])
                    {
                        var downloadDirectory = System.IO.Path.Combine(baseDirectory, path);
                        var path2 = System.IO.Path.Combine(downloadDirectory, seed.Name);

                        if (File.Exists(System.IO.Path.Combine(_amoebaManager.DownloadDirectory, path2)))
                        {
                            continue;
                        }

                        if (_amoebaManager.DownloadingInformation.Any(n =>
                            (n.Contains("Path") && ((string)n["Path"]) == downloadDirectory)
                            && (n.Contains("Name") && ((string)n["Name"]) == seed.Name)))
                        {
                            continue;
                        }

                        _amoebaManager.Download(seed.DeepClone(), downloadDirectory);
                    }
                }
            }
        }

        #endregion

        #region _boxTreeView

        private void _boxTreeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void _boxTreeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            foreach (string filePath in ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)))
            {
                if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    try
                    {
                        var box = AmoebaConverter.FromBoxStream(stream);

                        if (!LibraryControl.BoxDigitalSignatureCheck(ref box))
                        {
                            if (MessageBox.Show(
                                LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                "Digital Signature",
                                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                selectBoxTreeViewItem.Value.Boxes.Add(box);
                                selectBoxTreeViewItem.Update();
                            }
                        }
                        else
                        {
                            selectBoxTreeViewItem.Value.Boxes.Add(box);
                            selectBoxTreeViewItem.Update();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            this.Update();
        }

        private void _boxTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _boxTreeView_SelectedItemChanged(this, null);
        }

        private void _boxTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            _listViewItemCollection.Clear();

            foreach (var item in selectBoxTreeViewItem.Value.Boxes)
            {
                var boxesListViewItem = new BoxListViewItem();
                boxesListViewItem.Name = item.Name;
                boxesListViewItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                boxesListViewItem.CreationTime = item.CreationTime;
                boxesListViewItem.Length = LibraryControl.GetBoxLength(item);
                boxesListViewItem.Comment = item.Comment;
                boxesListViewItem.Value = item;

                _listViewItemCollection.Add(boxesListViewItem);
            }

            foreach (var item in selectBoxTreeViewItem.Value.Seeds)
            {
                var seedListViewItem = new SeedListViewItem();
                seedListViewItem.Name = item.Name;
                seedListViewItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                seedListViewItem.Keywords = item.Keywords.Select(n => n.Value);
                seedListViewItem.CreationTime = item.CreationTime;
                seedListViewItem.Length = item.Length;
                seedListViewItem.Comment = item.Comment;
                seedListViewItem.Value = item;

                _listViewItemCollection.Add(seedListViewItem);
            }

            this.Sort();
        }

        private void _boxTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (_boxTreeViewCutContextMenuItem != null) _boxTreeViewCutContextMenuItem.IsEnabled = (selectBoxTreeViewItem != _boxTreeViewItem);
            if (_boxTreeViewDeleteContextMenuItem != null) _boxTreeViewDeleteContextMenuItem.IsEnabled = (selectBoxTreeViewItem != _boxTreeViewItem);
        }

        private void _boxTreeViewAddBoxContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var box = new Box() { Name = "New Box" };

            BoxEditWindow window = new BoxEditWindow(ref box);

            if (true == window.ShowDialog())
            {
                selectBoxTreeViewItem.Value.Boxes.Add(box);
                selectBoxTreeViewItem.IsExpanded = true;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
        }

        private void _boxTreeViewEditContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var directory = selectBoxTreeViewItem.Value;
            BoxEditWindow window = new BoxEditWindow(ref directory);
            window.ShowDialog();

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _boxTreeViewDeleteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null || selectBoxTreeViewItem == _boxTreeViewItem) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var list = _boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            list[list.Count - 2].Value.Boxes.Remove(selectBoxTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _boxTreeViewCutContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null || selectBoxTreeViewItem == _boxTreeViewItem) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            Clipboard.SetBoxes(new List<Box>() { selectBoxTreeViewItem.Value.DeepClone() });

            var list = _boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            list[list.Count - 2].Value.Boxes.Remove(selectBoxTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _boxTreeViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            Clipboard.SetBoxes(new List<Box>() { selectBoxTreeViewItem.Value.DeepClone() });
        }

        private void _boxTreeViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes().Select(n => n.DeepClone()));
            selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds().Select(n => n.DeepClone()));

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _boxTreeViewImportContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";
                dialog.ShowDialog();

                foreach (var filePath in dialog.FileNames)
                {
                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        try
                        {
                            var directory = AmoebaConverter.FromBoxStream(stream);

                            if (!LibraryControl.BoxDigitalSignatureCheck(ref directory))
                            {
                                if (MessageBox.Show(
                                        LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                        "Digital Signature",
                                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                {
                                    selectBoxTreeViewItem.Value.Boxes.Add(directory);
                                    selectBoxTreeViewItem.Update();
                                }
                            }
                            else
                            {
                                selectBoxTreeViewItem.Value.Boxes.Add(directory);
                                selectBoxTreeViewItem.Update();
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                this.Update();
            }
        }

        private void _boxTreeViewExportContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.FileName = selectBoxTreeViewItem.Value.Name;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";
                dialog.ShowDialog();

                var fileName = dialog.FileName;

                using (FileStream stream = new FileStream(fileName, FileMode.Create))
                using (Stream directoryStream = AmoebaConverter.ToBoxStream(selectBoxTreeViewItem.Value))
                {
                    int i = -1;
                    byte[] buffer = _bufferManager.TakeBuffer(1024);

                    while ((i = directoryStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, i);
                    }

                    _bufferManager.ReturnBuffer(buffer);
                }

                this.Update();
            }
        }

        #endregion

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private string _lastHeaderClicked = LanguagesManager.Instance.LibraryControl_Name;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                _listView.SelectedIndex = -1;

                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != _lastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (_lastDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
            else
            {
                if (_lastHeaderClicked != null)
                {
                    Sort(_lastHeaderClicked, _lastDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listViewItemCollection.Sort(delegate(object x, object y)
            {
                return x.GetHashCode().CompareTo(y.GetHashCode());
            });

            if (sortBy == LanguagesManager.Instance.LibraryControl_Name)
            {
                _listViewItemCollection.Sort(delegate(object x, object y)
                {
                    string xName = "";
                    string yName = "";

                    if (x is BoxListViewItem && y is SeedListViewItem)
                    {
                        return 1;
                    }
                    else if (x is SeedListViewItem && y is BoxListViewItem)
                    {
                        return -1;
                    }
                    else if (x is SeedListViewItem && y is SeedListViewItem)
                    {
                        xName = ((SeedListViewItem)x).Name == null ? "" : ((SeedListViewItem)x).Name;
                        yName = ((SeedListViewItem)y).Name == null ? "" : ((SeedListViewItem)y).Name;
                    }
                    else if (x is BoxListViewItem && y is BoxListViewItem)
                    {
                        xName = ((BoxListViewItem)x).Name == null ? "" : ((BoxListViewItem)x).Name;
                        yName = ((BoxListViewItem)y).Name == null ? "" : ((BoxListViewItem)y).Name;
                    }

                    return xName.CompareTo(yName);
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Signature)
            {
                _listViewItemCollection.Sort(delegate(object x, object y)
                {
                    string xSignature = "";
                    string ySignature = "";

                    if (x is BoxListViewItem && y is SeedListViewItem)
                    {
                        return 1;
                    }
                    else if (x is SeedListViewItem && y is BoxListViewItem)
                    {
                        return -1;
                    }
                    else if (x is SeedListViewItem && y is SeedListViewItem)
                    {
                        xSignature = ((SeedListViewItem)x).Signature == null ? "" : ((SeedListViewItem)x).Signature;
                        ySignature = ((SeedListViewItem)y).Signature == null ? "" : ((SeedListViewItem)y).Signature;
                    }
                    else if (x is BoxListViewItem && y is BoxListViewItem)
                    {
                        xSignature = ((BoxListViewItem)x).Signature == null ? "" : ((BoxListViewItem)x).Signature;
                        ySignature = ((BoxListViewItem)y).Signature == null ? "" : ((BoxListViewItem)y).Signature;
                    }

                    return xSignature.CompareTo(ySignature);
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Length)
            {
                _listViewItemCollection.Sort(delegate(object x, object y)
                {
                    long xLength = 0;
                    long yLength = 0;

                    if (x is BoxListViewItem && y is SeedListViewItem)
                    {
                        return 1;
                    }
                    else if (x is SeedListViewItem && y is BoxListViewItem)
                    {
                        return -1;
                    }
                    else if (x is SeedListViewItem && y is SeedListViewItem)
                    {
                        xLength = ((SeedListViewItem)x).Length;
                        yLength = ((SeedListViewItem)y).Length;
                    }
                    else if (x is BoxListViewItem && y is BoxListViewItem)
                    {
                        xLength = ((BoxListViewItem)x).Length;
                        yLength = ((BoxListViewItem)y).Length;
                    }

                    return xLength.CompareTo(yLength);
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Keywords)
            {
                _listViewItemCollection.Sort(delegate(object x, object y)
                {
                    IEnumerable<string> xKeywords = new string[] { };
                    IEnumerable<string> yKeywords = new string[] { };

                    if (x is BoxListViewItem && y is SeedListViewItem)
                    {
                        return 1;
                    }
                    else if (x is SeedListViewItem && y is BoxListViewItem)
                    {
                        return -1;
                    }
                    else if (x is SeedListViewItem && y is SeedListViewItem)
                    {
                        xKeywords = ((SeedListViewItem)x).Keywords;
                        yKeywords = ((SeedListViewItem)y).Keywords;
                    }

                    StringBuilder xBuilder = new StringBuilder();
                    foreach (var item in xKeywords) xBuilder.Append(item);
                    StringBuilder yBuilder = new StringBuilder();
                    foreach (var item in yKeywords) yBuilder.Append(item);

                    return xBuilder.ToString().CompareTo(yBuilder.ToString());
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_CreationTime)
            {
                _listViewItemCollection.Sort(delegate(object x, object y)
                {
                    DateTime xCreationTime = DateTime.MinValue;
                    DateTime yCreationTime = DateTime.MinValue;

                    if (x is BoxListViewItem && y is SeedListViewItem)
                    {
                        return 1;
                    }
                    else if (x is SeedListViewItem && y is BoxListViewItem)
                    {
                        return -1;
                    }
                    else if (x is SeedListViewItem && y is SeedListViewItem)
                    {
                        xCreationTime = ((SeedListViewItem)x).CreationTime;
                        yCreationTime = ((SeedListViewItem)y).CreationTime;
                    }
                    else if (x is BoxListViewItem && y is BoxListViewItem)
                    {
                        xCreationTime = ((BoxListViewItem)x).CreationTime;
                        yCreationTime = ((BoxListViewItem)y).CreationTime;
                    }

                    return xCreationTime.CompareTo(yCreationTime);
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Comment)
            {
                _listViewItemCollection.Sort(delegate(object x, object y)
                {
                    string xComment = "";
                    string yComment = "";

                    if (x is BoxListViewItem && y is SeedListViewItem)
                    {
                        return 1;
                    }
                    else if (x is SeedListViewItem && y is BoxListViewItem)
                    {
                        return -1;
                    }
                    else if (x is SeedListViewItem && y is SeedListViewItem)
                    {
                        xComment = ((SeedListViewItem)x).Comment == null ? "" : ((SeedListViewItem)x).Comment;
                        yComment = ((SeedListViewItem)y).Comment == null ? "" : ((SeedListViewItem)y).Comment;
                    }
                    else if (x is BoxListViewItem && y is BoxListViewItem)
                    {
                        xComment = ((BoxListViewItem)x).Comment == null ? "" : ((SeedListViewItem)x).Comment;
                        yComment = ((BoxListViewItem)y).Comment == null ? "" : ((SeedListViewItem)y).Comment;
                    }

                    return xComment.CompareTo(yComment);
                });
            }

            if (direction == ListSortDirection.Descending)
            {
                _listViewItemCollection.Reverse();
            }

            _listView.Items.Refresh();
        }

        #endregion

        private class SeedListViewItem
        {
            public string Name { get; set; }
            public string Signature { get; set; }
            public IEnumerable<string> Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public Seed Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Value == null) return 0;
                else return this.Value.GetHashCode();
            }
        }

        private class BoxListViewItem
        {
            public string Name { get; set; }
            public string Signature { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public Box Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Value == null) return 0;
                else return this.Value.GetHashCode();
            }
        }
    }

    public class BoxTreeViewItem : TreeViewItem
    {
        private Box _value = new Box();

        public BoxTreeViewItem()
        {

        }

        public BoxTreeViewItem(Box box)
        {
            this.Value = box;
        }

        public void Update()
        {
            this.Header = _value.Name;

            List<BoxTreeViewItem> list = new List<BoxTreeViewItem>();

            foreach (var item in _value.Boxes)
            {
                list.Add(new BoxTreeViewItem(item));
            }

            foreach (var item in this.Items.OfType<BoxTreeViewItem>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    this.Items.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!this.Items.OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    this.Items.Add(item);
                }
            }
        }

        public Box Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }
    }
}
