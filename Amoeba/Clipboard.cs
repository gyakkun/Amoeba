using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Library;
using Library.Collections;
using Library.Net.Amoeba;
using System.Threading;

namespace Amoeba
{
    static class Clipboard
    {
        private static bool _isNodesCached = false;
        private static bool _isSeedsCached = false;

        private static LockedList<Node> _nodeList = new LockedList<Node>();
        private static LockedList<Seed> _seedList = new LockedList<Seed>();

        private static LockedList<Box> _boxList = new LockedList<Box>();
        private static LockedList<Windows.SearchTreeItem> _searchTreeItemList = new LockedList<Windows.SearchTreeItem>();
        private static LockedList<Windows.StoreCategorizeTreeItem> _storeCategorizeTreeItemList = new LockedList<Windows.StoreCategorizeTreeItem>();
        private static LockedList<Windows.StoreTreeItem> _storeTreeItemList = new LockedList<Windows.StoreTreeItem>();

        private static ClipboardWatcher _clipboardWatcher;

        private static object _thisLock = new object();

        static Clipboard()
        {
            _clipboardWatcher = new ClipboardWatcher();
            _clipboardWatcher.DrawClipboard += (sender, e) =>
            {
                lock (_thisLock)
                {
                    _isNodesCached = false;
                    _isSeedsCached = false;

                    _nodeList.Clear();
                    _seedList.Clear();
                    _boxList.Clear();
                    _searchTreeItemList.Clear();
                    _storeCategorizeTreeItemList.Clear();
                    _storeTreeItemList.Clear();
                }
            };
        }

        public static bool ContainsPaths()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsFileDropList();
            }
        }

        public static bool ContainsText()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsText();
            }
        }

        public static bool ContainsNodes()
        {
            lock (_thisLock)
            {
                if (_isNodesCached) return _nodeList.Count != 0;
                else return Clipboard.GetNodes().Count() != 0;
            }
        }

        public static bool ContainsSeeds()
        {
            lock (_thisLock)
            {
                if (_isSeedsCached) return _seedList.Count != 0;
                else return Clipboard.GetSeeds().Count() != 0;
            }
        }

        public static bool ContainsBoxes()
        {
            lock (_thisLock)
            {
                return _boxList.Count != 0;
            }
        }

        public static bool ContainsSearchTreeItems()
        {
            lock (_thisLock)
            {
                return _searchTreeItemList.Count != 0;
            }
        }

        public static bool ContainsStoreCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _storeCategorizeTreeItemList.Count != 0;
            }
        }

        public static bool ContainsStoreTreeItems()
        {
            lock (_thisLock)
            {
                return _storeTreeItemList.Count != 0;
            }
        }

        public static IEnumerable<string> GetPaths()
        {
            lock (_thisLock)
            {
                try
                {
                    return System.Windows.Clipboard.GetFileDropList().Cast<string>();
                }
                catch (Exception)
                {

                }

                return new string[0];
            }
        }

        public static void SetPaths(IEnumerable<string> collection)
        {
            lock (_thisLock)
            {
                try
                {
                    var list = new System.Collections.Specialized.StringCollection();
                    list.AddRange(collection.ToArray());
                    System.Windows.Clipboard.SetFileDropList(list);
                }
                catch (Exception)
                {

                }
            }
        }

        public static string GetText()
        {
            lock (_thisLock)
            {
                try
                {
                    return System.Windows.Clipboard.GetText();
                }
                catch (Exception)
                {

                }

                return "";
            }
        }

        public static void SetText(string text)
        {
            lock (_thisLock)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                }
                catch (Exception)
                {

                }
            }
        }

        public static IEnumerable<Node> GetNodes()
        {
            lock (_thisLock)
            {
                if (!_isNodesCached)
                {
                    foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            _nodeList.Add(AmoebaConverter.FromNodeString(item));
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _isNodesCached = true;
                }

                return _nodeList.Where(n => n != null).Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetNodes(IEnumerable<Node> nodes)
        {
            lock (_thisLock)
            {
                var sb = new StringBuilder();

                foreach (var item in nodes)
                {
                    sb.AppendLine(AmoebaConverter.ToNodeString(item));
                }

                Clipboard.SetText(sb.ToString());
            }
        }

        public static void SetBoxAndSeeds(IEnumerable<Box> boxes, IEnumerable<Seed> seeds)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in seeds)
                    {
                        sb.AppendLine(AmoebaConverter.ToSeedString(item));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                {
                    _boxList.Clear();
                    _boxList.AddRange(boxes.Select(n => n.DeepClone()));
                }
            }
        }

        public static IEnumerable<Seed> GetSeeds()
        {
            lock (_thisLock)
            {
                if (!_isSeedsCached)
                {
                    foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var seed = AmoebaConverter.FromSeedString(item);
                            if (seed == null) continue;

                            if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                            _seedList.Add(seed);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _isSeedsCached = true;
                }

                return _seedList.Where(n => n != null).Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetSeeds(IEnumerable<Seed> seeds)
        {
            lock (_thisLock)
            {
                var sb = new StringBuilder();

                foreach (var item in seeds)
                {
                    sb.AppendLine(AmoebaConverter.ToSeedString(item));
                }

                Clipboard.SetText(sb.ToString());
            }
        }

        public static IEnumerable<Box> GetBoxes()
        {
            lock (_thisLock)
            {
                return _boxList.Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetBoxes(IEnumerable<Box> boxes)
        {
            lock (_thisLock)
            {
                System.Windows.Clipboard.Clear();

                _boxList.Clear();
                _boxList.AddRange(boxes.Select(n => n.DeepClone()));
            }
        }

        public static IEnumerable<Windows.SearchTreeItem> GetSearchTreeItems()
        {
            lock (_thisLock)
            {
                return _searchTreeItemList.Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetSearchTreeItems(IEnumerable<Windows.SearchTreeItem> searchTreeItems)
        {
            lock (_thisLock)
            {
                System.Windows.Clipboard.Clear();

                _searchTreeItemList.Clear();
                _searchTreeItemList.AddRange(searchTreeItems.Select(n => n.DeepClone()));
            }
        }

        public static IEnumerable<Windows.StoreCategorizeTreeItem> GetStoreCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _storeCategorizeTreeItemList.Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetStoreCategorizeTreeItems(IEnumerable<Windows.StoreCategorizeTreeItem> storeCategorizeTreeItems)
        {
            lock (_thisLock)
            {
                System.Windows.Clipboard.Clear();

                _storeCategorizeTreeItemList.Clear();
                _storeCategorizeTreeItemList.AddRange(storeCategorizeTreeItems.Select(n => n.DeepClone()));
            }
        }

        public static IEnumerable<Windows.StoreTreeItem> GetStoreTreeItems()
        {
            lock (_thisLock)
            {
                return _storeTreeItemList.Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetStoreTreeItems(IEnumerable<Windows.StoreTreeItem> storeTreeItems)
        {
            lock (_thisLock)
            {
                System.Windows.Clipboard.Clear();

                {
                    var sb = new StringBuilder();

                    foreach (var item in storeTreeItems)
                    {
                        sb.AppendLine(item.Signature);
                    }

                    Clipboard.SetText(sb.ToString());
                }

                _storeTreeItemList.Clear();
                _storeTreeItemList.AddRange(storeTreeItems.Select(n => n.DeepClone()));
            }
        }

        public class ClipboardWatcher : IDisposable
        {
            private ClipboardWatcherForm _form;

            public event EventHandler DrawClipboard;

            public ClipboardWatcher()
            {
                _form = new ClipboardWatcherForm();
                _form.StartWatch(this.OnDrawClipboard);
            }

            ~ClipboardWatcher()
            {
                this.Dispose();
            }

            private void OnDrawClipboard()
            {
                if (DrawClipboard != null)
                {
                    DrawClipboard(this, EventArgs.Empty);
                }
            }

            public void Dispose()
            {
                _form.Dispose();
            }

            private class ClipboardWatcherForm : System.Windows.Forms.Form
            {
                [DllImport("user32.dll")]
                private static extern IntPtr SetClipboardViewer(IntPtr hwnd);

                [DllImport("user32.dll")]
                private static extern bool ChangeClipboardChain(IntPtr hwnd, IntPtr hWndNext);

                private const int WM_DRAWCLIPBOARD = 0x0308;
                private const int WM_CHANGECBCHAIN = 0x030D;

                private IntPtr _nextHandle;
                private Action _drawClipboard;

                public void StartWatch(Action drawClipboard)
                {
                    _drawClipboard = drawClipboard;
                    _nextHandle = SetClipboardViewer(this.Handle);
                }

                protected override void WndProc(ref System.Windows.Forms.Message m)
                {
                    if (m.Msg == WM_DRAWCLIPBOARD)
                    {
                        _drawClipboard();
                    }
                    else if (m.Msg == WM_CHANGECBCHAIN)
                    {
                        if (m.WParam == _nextHandle)
                        {
                            _nextHandle = m.LParam;
                        }
                    }

                    base.WndProc(ref m);
                }

                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        ChangeClipboardChain(this.Handle, _nextHandle);
                        base.Dispose(disposing);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
    }
}
