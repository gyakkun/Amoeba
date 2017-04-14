using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Data;
using System.Xml;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Amoeba.Interface
{
    partial class LanguagesManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static readonly LanguagesManager _defaultInstance = new LanguagesManager();

        private Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private string _currentLanguage;

        public LanguagesManager()
        {
#if DEBUG
            string path = @"C:\Local\Projects\Alliance-Network\Amoeba\Amoeba.Interface\bin\Debug\Core\Resources\Languages";

            if (!Directory.Exists(path)) path = AmoebaEnvironment.Paths.LanguagesPath;
#else
            string path = AmoebaEnvironment.Paths.LanguagesPath;
#endif

            this.Load(path);
        }

        private void Load(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            _dic.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var dic = new Dictionary<string, string>();

                    using (var xml = new XmlTextReader(path))
                    {
                        while (xml.Read())
                        {
                            if (xml.NodeType == XmlNodeType.Element)
                            {
                                if (xml.LocalName == "Property")
                                {
                                    dic.Add(xml.GetAttribute("Name"), xml.GetAttribute("Value"));
                                }
                            }
                        }
                    }

                    _dic[Path.GetFileNameWithoutExtension(path)] = dic;
                }
                catch (XmlException)
                {

                }
            }

            this.SetCurrentLanguage("English");
        }

        public static LanguagesManager Instance
        {
            get
            {
                return _defaultInstance;
            }
        }

        public IEnumerable<string> Languages
        {
            get
            {
                var dic = new Dictionary<string, string>();

                foreach (string path in _dic.Keys.ToList())
                {
                    dic[System.IO.Path.GetFileNameWithoutExtension(path)] = path;
                }

                var pairs = dic.ToList();

                pairs.Sort((x, y) =>
                {
                    return x.Key.CompareTo(y.Key);
                });

                return pairs.Select(n => n.Value).ToArray();
            }
        }

        public string CurrentLanguage
        {
            get
            {
                return _currentLanguage;
            }
        }

        public void SetCurrentLanguage(string value)
        {
            if (!_dic.ContainsKey(value)) throw new ArgumentException();

            if (value != _currentLanguage)
            {
                _currentLanguage = value;
                this.OnPropertyChanged(null);
            }
        }

        public string Translate(string key)
        {
            if (_currentLanguage == null) return null;

            if (_dic[_currentLanguage].TryGetValue(key, out string result))
            {
                return Regex.Unescape(result);
            }

            return null;
        }
    }
}
