using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Library;
using Library.Net.Amoeba;

namespace Amoeba
{
    static class Clipboard
    {
        private static List<Box> _boxList = new List<Box>();
        private static List<Windows.SearchTreeItem> _searchTreeItemList = new List<Windows.SearchTreeItem>();

        public static IEnumerable<string> GetPaths()
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

        public static void SetPaths(IEnumerable<string> collection)
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

        public static string GetText()
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

        public static void SetText(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch (Exception)
            {

            }
        }

        public static IEnumerable<Node> GetNodes()
        {
            var list = new List<Node>();

            foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!item.StartsWith("Node@")) continue;
                
                try
                {
                    list.Add(AmoebaConverter.FromNodeString(item));
                }
                catch (Exception)
                {

                }
            }

            return list;
        }

        public static void SetNodes(IEnumerable<Node> nodes)
        {
            var sb = new StringBuilder();

            foreach (var item in nodes)
            {
                sb.AppendLine(AmoebaConverter.ToNodeString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        public static IEnumerable<Seed> GetSeeds()
        {
            var list = new List<Seed>();

            foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!item.StartsWith("Seed@")) continue;

                try
                {
                    list.Add(AmoebaConverter.FromSeedString(item));
                }
                catch (Exception)
                {

                }
            }

            return list;
        }

        public static void SetSeeds(IEnumerable<Seed> seeds)
        {
            var sb = new StringBuilder();

            foreach (var item in seeds)
            {
                sb.AppendLine(AmoebaConverter.ToSeedString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        public static IEnumerable<Box> GetBoxes()
        {
            return _boxList;
        }

        public static void SetBoxes(IEnumerable<Box> boxes)
        {
            _boxList.Clear();
            _boxList.AddRange(boxes);
        }

        public static IEnumerable<Windows.SearchTreeItem> GetSearchTreeItems()
        {
            return _searchTreeItemList;
        }

        public static void SetSearchTreeItems(List<Windows.SearchTreeItem> searchTreeItems)
        {
            _searchTreeItemList.Clear();
            _searchTreeItemList.AddRange(searchTreeItems);
        }
    }
}
