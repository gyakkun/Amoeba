using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Xml;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Amoeba;

namespace Amoeba
{
    static class Clipboard
    {
        private static object _thisLock = new object();

        private static Stream ToStream<T>(T item)
        {
            var ds = new DataContractSerializer(typeof(T));

            MemoryStream stream = null;

            try
            {
                stream = new MemoryStream();

                using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(wrapperStream, new UTF8Encoding(false)))
                {
                    ds.WriteObject(xmlDictionaryWriter, item);
                }
            }
            catch (Exception)
            {
                if (stream != null)
                    stream.Dispose();
            }

            return stream;
        }

        private static T FromStream<T>(Stream stream)
        {
            var ds = new DataContractSerializer(typeof(T));

            using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                return (T)ds.ReadObject(xmlDictionaryReader);
            }
        }

        public static void Clear()
        {
            lock (_thisLock)
            {
                System.Windows.Clipboard.Clear();
            }
        }

        public static bool ContainsPaths()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsFileDropList();
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

        public static bool ContainsText()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsText();
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

        public static bool ContainsNodes()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Node:") || item.StartsWith("Node@")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Node> GetNodes()
        {
            lock (_thisLock)
            {
                var list = new List<Node>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var node = AmoebaConverter.FromNodeString(item);
                        if (node == null) continue;

                        list.Add(node);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetNodes(IEnumerable<Node> nodes)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in nodes)
                    {
                        sb.AppendLine(AmoebaConverter.ToNodeString(item));
                    }

                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        public static bool ContainsSeeds()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Seed:") || item.StartsWith("Seed@")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Seed> GetSeeds()
        {
            lock (_thisLock)
            {
                var list = new List<Seed>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var seed = AmoebaConverter.FromSeedString(item);
                        if (seed == null) continue;

                        list.Add(seed);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetSeeds(IEnumerable<Seed> seeds)
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
            }
        }

        public static bool ContainsBoxes()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_Boxes");
            }
        }

        public static IEnumerable<Box> GetBoxes()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_Boxes"))
                    {
                        return Clipboard.FromStream<IEnumerable<Box>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Box[0];
            }
        }

        public static void SetBoxes(IEnumerable<Box> boxes)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetData("Amoeba_Boxes", Clipboard.ToStream(boxes));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static void SetBoxAndSeeds(IEnumerable<Box> boxes, IEnumerable<Seed> seeds)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();

                {
                    var sb = new StringBuilder();

                    foreach (var item in seeds)
                    {
                        sb.AppendLine(AmoebaConverter.ToSeedString(item));
                    }

                    dataObject.SetText(sb.ToString());
                }

                dataObject.SetData("Amoeba_Boxes", Clipboard.ToStream(boxes));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsSearchTreeItems()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_SearchTreeItems");
            }
        }

        public static IEnumerable<Windows.SearchTreeItem> GetSearchTreeItems()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_SearchTreeItems"))
                    {
                        return Clipboard.FromStream<IEnumerable<Windows.SearchTreeItem>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Windows.SearchTreeItem[0];
            }
        }

        public static void SetSearchTreeItems(IEnumerable<Windows.SearchTreeItem> items)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetData("Amoeba_SearchTreeItems", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsStoreCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_StoreCategorizeTreeItems");
            }
        }

        public static IEnumerable<Windows.StoreCategorizeTreeItem> GetStoreCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_StoreCategorizeTreeItems"))
                    {
                        return Clipboard.FromStream<IEnumerable<Windows.StoreCategorizeTreeItem>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Windows.StoreCategorizeTreeItem[0];
            }
        }

        public static void SetStoreCategorizeTreeItems(IEnumerable<Windows.StoreCategorizeTreeItem> items)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetData("Amoeba_StoreCategorizeTreeItems", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsStoreTreeItems()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_StoreTreeItems");
            }
        }

        public static IEnumerable<Windows.StoreTreeItem> GetStoreTreeItems()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_StoreTreeItems"))
                    {
                        return Clipboard.FromStream<IEnumerable<Windows.StoreTreeItem>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Windows.StoreTreeItem[0];
            }
        }

        public static void SetStoreTreeItems(IEnumerable<Windows.StoreTreeItem> items)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();

                {
                    var sb = new StringBuilder();

                    foreach (var item in items)
                    {
                        sb.AppendLine(item.Signature);
                    }

                    dataObject.SetText(sb.ToString());
                }

                dataObject.SetData("Amoeba_StoreTreeItems", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }
    }
}
