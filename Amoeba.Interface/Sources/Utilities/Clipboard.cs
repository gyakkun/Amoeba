using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amoeba.Service;
using Omnius.Security;

namespace Amoeba.Interface
{
    static class Clipboard
    {
        private static object _thisLock = new object();

        private static Stream ToStream<T>(T item)
        {
            MemoryStream stream = null;

            try
            {
                stream = new MemoryStream();
                JsonUtils.Save(stream, item);
                stream.Seek(0, SeekOrigin.Begin);
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
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return JsonUtils.Load<T>(stream);
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

        public static bool ContainsSignatures()
        {
            lock (_thisLock)
            {
                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Signature.Parse(item) != null) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Signature> GetSignatures()
        {
            lock (_thisLock)
            {
                var list = new List<Signature>();

                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var signature = Signature.Parse(item);
                        if (signature == null) continue;

                        list.Add(signature);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetSignatures(IEnumerable<Signature> signatures)
        {
            lock (_thisLock)
            {
                Clipboard.SetText(string.Join("\r\n", signatures.Select(n => n.ToString())));
            }
        }

        public static bool ContainsLocations()
        {
            lock (_thisLock)
            {
                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Location:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Location> GetLocations()
        {
            lock (_thisLock)
            {
                var list = new List<Location>();

                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var location = AmoebaConverter.FromLocationString(item);
                        if (location == null) continue;

                        list.Add(location);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetLocations(IEnumerable<Location> locations)
        {
            lock (_thisLock)
            {
                Clipboard.SetText(string.Join("\r\n", locations.Select(n => AmoebaConverter.ToLocationString(n))));
            }
        }

        public static bool ContainsTags()
        {
            lock (_thisLock)
            {
                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Tag:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Tag> GetTags()
        {
            lock (_thisLock)
            {
                var list = new List<Tag>();

                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var tag = AmoebaConverter.FromTagString(item);
                        if (tag == null) continue;

                        list.Add(tag);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetTags(IEnumerable<Tag> tags)
        {
            lock (_thisLock)
            {
                Clipboard.SetText(string.Join("\r\n", tags.Select(n => AmoebaConverter.ToTagString(n))));
            }
        }

        public static bool ContainsSeeds()
        {
            lock (_thisLock)
            {
                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Seed:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Seed> GetSeeds()
        {
            lock (_thisLock)
            {
                var list = new List<Seed>();

                foreach (string item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
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
                Clipboard.SetText(string.Join("\r\n", seeds.Select(n => AmoebaConverter.ToSeedString(n))));
            }
        }

        public static bool ContainsChatCategoryInfo()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_ChatCategoryInfos");
            }
        }

        public static IEnumerable<ChatCategoryInfo> GetChatCategoryInfos()
        {
            lock (_thisLock)
            {
                try
                {
                    using (var stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_ChatCategoryInfos"))
                    {
                        return Clipboard.FromStream<IEnumerable<ChatCategoryInfo>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return Enumerable.Empty<ChatCategoryInfo>();
            }
        }

        public static void SetChatCategoryInfos(IEnumerable<ChatCategoryInfo> items)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetData("Amoeba_ChatCategoryInfos", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsChatThreadInfo()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_ChatThreadInfos");
            }
        }

        public static IEnumerable<ChatThreadInfo> GetChatThreadInfos()
        {
            lock (_thisLock)
            {
                try
                {
                    using (var stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_ChatThreadInfos"))
                    {
                        return Clipboard.FromStream<IEnumerable<ChatThreadInfo>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return Enumerable.Empty<ChatThreadInfo>();
            }
        }

        public static void SetChatThreadInfos(IEnumerable<ChatThreadInfo> items)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetText(string.Join("\r\n", items.Select(n => AmoebaConverter.ToTagString(n.Tag))));
                dataObject.SetData("Amoeba_ChatThreadInfos", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsSubscribeCategoryInfo()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_SubscribeCategoryInfos");
            }
        }

        public static IEnumerable<SubscribeCategoryInfo> GetSubscribeCategoryInfos()
        {
            lock (_thisLock)
            {
                try
                {
                    using (var stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_SubscribeCategoryInfos"))
                    {
                        return Clipboard.FromStream<IEnumerable<SubscribeCategoryInfo>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return Enumerable.Empty<SubscribeCategoryInfo>();
            }
        }

        public static void SetSubscribeCategoryInfos(IEnumerable<SubscribeCategoryInfo> items)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetData("Amoeba_SubscribeCategoryInfos", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsSubscribeStoreInfo()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Amoeba_SubscribeStoreInfos");
            }
        }

        public static IEnumerable<SubscribeStoreInfo> GetSubscribeStoreInfos()
        {
            lock (_thisLock)
            {
                try
                {
                    using (var stream = (Stream)System.Windows.Clipboard.GetData("Amoeba_SubscribeStoreInfos"))
                    {
                        return Clipboard.FromStream<IEnumerable<SubscribeStoreInfo>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return Enumerable.Empty<SubscribeStoreInfo>();
            }
        }

        public static void SetSubscribeStoreInfos(IEnumerable<SubscribeStoreInfo> items)
        {
            lock (_thisLock)
            {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetText(string.Join("\r\n", items.Select(n => n.AuthorSignature.ToString())));
                dataObject.SetData("Amoeba_SubscribeStoreInfos", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }
    }
}
