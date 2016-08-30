using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using Library;
using Library.Io;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Amoeba
{
    class Config
    {
        public StartupSetting Startup { get; private set; }
        public CatharsisSetting Catharsis { get; private set; }
        public CacheSetting Cache { get; private set; }
        public ColorsSetting Colors { get; private set; }

        public void Load(string directoryPath)
        {
            // Init
            {
                {
                    var startupSetting = new StartupSetting();

                    startupSetting.ProcessSettings.Add(new ProcessSetting()
                    {
                        Path = @"Assemblies/Tor/tor.exe",
                        Arguments = "-f torrc DataDirectory " + @"../../../Work/Tor",
                        WorkingDirectory = @"Assemblies/Tor",
                    });

                    startupSetting.ProcessSettings.Add(new ProcessSetting()
                    {
                        Path = @"Assemblies/Polipo/polipo.exe",
                        Arguments = "-c polipo.conf",
                        WorkingDirectory = @"Assemblies/Polipo",
                    });

                    this.Startup = startupSetting;
                }

                {
                    var catharsisSettings = new CatharsisSetting();
                    catharsisSettings.Ipv4AddressFilters.Add(new Ipv4AddressFilter("tcp:127.0.0.1:18118", null, new string[] { @"Catharsis_Ipv4.txt" }));

                    this.Catharsis = catharsisSettings;
                }

                {
                    var cacheSettings = new CacheSetting();
                    cacheSettings.Path = Path.Combine(directoryPath, "Cache.blocks");

                    this.Cache = cacheSettings;
                }

                {
                    var colorsSetting = new ColorsSetting();
                    colorsSetting.Tree_Hit = System.Windows.Media.Colors.LightPink;

                    this.Colors = colorsSetting;
                }
            }

            // Obsolete
            {
                var startupSettingsPath = Path.Combine(directoryPath, "Startup.settings");
                var catharsisSettingsPath = Path.Combine(directoryPath, "Catharsis.settings");
                var cacheSettingsPath = Path.Combine(directoryPath, "Cache.settings");
                var colorsSettingsPath = Path.Combine(directoryPath, "Colors.settings");

                if (File.Exists(startupSettingsPath))
                {
                    this.Startup = Obsolete.GetStartupSetting(startupSettingsPath);
                    File.Delete(startupSettingsPath);
                }

                if (File.Exists(catharsisSettingsPath))
                {
                    this.Catharsis = Obsolete.GetCatharsisSetting(catharsisSettingsPath);
                    File.Delete(catharsisSettingsPath);
                }

                if (File.Exists(cacheSettingsPath))
                {
                    this.Cache = Obsolete.GetCacheSetting(cacheSettingsPath);
                    File.Delete(cacheSettingsPath);
                }

                if (File.Exists(colorsSettingsPath))
                {
                    this.Colors = Obsolete.GetColorsSetting(colorsSettingsPath);
                    File.Delete(colorsSettingsPath);
                }
            }

            // Config
            {
                var startupConfigPath = Path.Combine(directoryPath, "Startup.config");
                var catharsisConfigPath = Path.Combine(directoryPath, "Catharsis.config");
                var cacheConfigPath = Path.Combine(directoryPath, "Cache.config");
                var colorsConfigPath = Path.Combine(directoryPath, "Colors.config");

                if (!File.Exists(startupConfigPath))
                {
                    this.SaveObject(this.Startup, startupConfigPath);
                }

                if (!File.Exists(catharsisConfigPath))
                {
                    this.SaveObject(this.Catharsis, catharsisConfigPath);
                }

                if (!File.Exists(cacheConfigPath))
                {
                    this.SaveObject(this.Cache, cacheConfigPath);
                }

                if (!File.Exists(colorsConfigPath))
                {
                    this.SaveObject(this.Colors, colorsConfigPath);
                }

                this.Startup = this.LoadObject<StartupSetting>(startupConfigPath);
                this.Catharsis = this.LoadObject<CatharsisSetting>(catharsisConfigPath);
                this.Cache = this.LoadObject<CacheSetting>(cacheConfigPath);
                this.Colors = this.LoadObject<ColorsSetting>(colorsConfigPath);
            }
        }

        private T LoadObject<T>(string configPath)
        {
            try
            {
                using (var streamReader = new StreamReader(configPath, new UTF8Encoding(false)))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var serializer = new JsonSerializer();
                    serializer.MissingMemberHandling = MissingMemberHandling.Ignore;

                    serializer.TypeNameHandling = TypeNameHandling.None;
                    serializer.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter());
                    serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    serializer.Converters.Add(new ColorJsonConverter());
                    serializer.ContractResolver = new CustomContractResolver();

                    return serializer.Deserialize<T>(jsonTextReader);
                }
            }
            catch (Exception e)
            {
                Log.Warning(e);
            }

            return default(T);
        }

        private void SaveObject<T>(T value, string configPath)
        {
            try
            {
                using (var streamWriter = new StreamWriter(configPath, false, new UTF8Encoding(false)))
                using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                {
                    var serializer = new JsonSerializer();
                    serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

                    serializer.TypeNameHandling = TypeNameHandling.None;
                    serializer.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter());
                    serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    serializer.Converters.Add(new ColorJsonConverter());
                    serializer.ContractResolver = new CustomContractResolver();

                    serializer.Serialize(jsonTextWriter, value);
                }
            }
            catch (Exception e)
            {
                Log.Warning(e);
            }
        }

        public class ColorJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Color);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return (Color)System.Windows.Media.ColorConverter.ConvertFromString((string)reader.Value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((Color)value).ToString());
            }
        }

        class CustomContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                if (objectType.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                {
                    return base.CreateArrayContract(objectType);
                }

                if (System.Attribute.GetCustomAttributes(objectType).Any(n => n is DataContractAttribute))
                {
                    var objectContract = base.CreateObjectContract(objectType);
                    objectContract.DefaultCreatorNonPublic = false;
                    objectContract.DefaultCreator = () => FormatterServices.GetUninitializedObject(objectContract.CreatedType);

                    return objectContract;
                }

                return base.CreateContract(objectType);
            }
        }

        [Obsolete]
        private static class Obsolete
        {
            public static StartupSetting GetStartupSetting(string filePath)
            {
                try
                {
                    var startupSetting = new StartupSetting();

                    using (StreamReader reader = new StreamReader(filePath, new UTF8Encoding(false)))
                    using (XmlTextReader xml = new XmlTextReader(reader))
                    {
                        while (xml.Read())
                        {
                            if (xml.NodeType == XmlNodeType.Element)
                            {
                                if (xml.LocalName == "Process")
                                {
                                    string path = null;
                                    string arguments = null;
                                    string workingDirectory = null;

                                    using (var xmlSubtree = xml.ReadSubtree())
                                    {
                                        while (xmlSubtree.Read())
                                        {
                                            if (xmlSubtree.NodeType == XmlNodeType.Element)
                                            {
                                                if (xmlSubtree.LocalName == "Path")
                                                {
                                                    try
                                                    {
                                                        path = xmlSubtree.ReadString();
                                                    }
                                                    catch (Exception)
                                                    {

                                                    }
                                                }
                                                else if (xml.LocalName == "Arguments")
                                                {
                                                    try
                                                    {
                                                        arguments = xmlSubtree.ReadString();
                                                    }
                                                    catch (Exception)
                                                    {

                                                    }
                                                }
                                                else if (xmlSubtree.LocalName == "WorkingDirectory")
                                                {
                                                    try
                                                    {
                                                        workingDirectory = xmlSubtree.ReadString();
                                                    }
                                                    catch (Exception)
                                                    {

                                                    }
                                                }
                                            }
                                        }
                                    }

                                    startupSetting.ProcessSettings.Add(new ProcessSetting()
                                    {
                                        Path = path,
                                        Arguments = arguments,
                                        WorkingDirectory = workingDirectory
                                    });
                                }
                            }
                        }
                    }

                    return startupSetting;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public static CatharsisSetting GetCatharsisSetting(string filePath)
            {
                try
                {
                    var catharsisSetting = new CatharsisSetting();

                    using (StreamReader r = new StreamReader(filePath, new UTF8Encoding(false)))
                    using (XmlTextReader xml = new XmlTextReader(r))
                    {
                        while (xml.Read())
                        {
                            if (xml.NodeType == XmlNodeType.Element)
                            {
                                if (xml.LocalName == "Ipv4AddressFilter")
                                {
                                    string proxyUri = null;
                                    var urls = new List<string>();
                                    var paths = new List<string>();

                                    using (var xmlSubtree = xml.ReadSubtree())
                                    {
                                        while (xmlSubtree.Read())
                                        {
                                            if (xmlSubtree.NodeType == XmlNodeType.Element)
                                            {
                                                if (xmlSubtree.LocalName == "Proxy")
                                                {
                                                    using (var xmlSubtree2 = xmlSubtree.ReadSubtree())
                                                    {
                                                        while (xmlSubtree2.Read())
                                                        {
                                                            if (xmlSubtree2.NodeType == XmlNodeType.Element)
                                                            {
                                                                if (xmlSubtree2.LocalName == "Uri")
                                                                {
                                                                    try
                                                                    {
                                                                        proxyUri = xmlSubtree2.ReadString();
                                                                    }
                                                                    catch (Exception)
                                                                    {

                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (xmlSubtree.LocalName == "Targets")
                                                {
                                                    using (var xmlSubtree2 = xmlSubtree.ReadSubtree())
                                                    {
                                                        while (xmlSubtree2.Read())
                                                        {
                                                            if (xmlSubtree2.NodeType == XmlNodeType.Element)
                                                            {
                                                                if (xmlSubtree2.LocalName == "Url")
                                                                {
                                                                    try
                                                                    {
                                                                        urls.Add(xmlSubtree2.ReadString());
                                                                    }
                                                                    catch (Exception)
                                                                    {

                                                                    }
                                                                }
                                                                else if (xmlSubtree2.LocalName == "Path")
                                                                {
                                                                    try
                                                                    {
                                                                        paths.Add(xmlSubtree2.ReadString());
                                                                    }
                                                                    catch (Exception)
                                                                    {

                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    catharsisSetting.Ipv4AddressFilters.Add(new Ipv4AddressFilter(proxyUri, urls, paths));
                                }
                            }
                        }
                    }

                    return catharsisSetting;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public static CacheSetting GetCacheSetting(string filePath)
            {
                try
                {
                    var cacheSetting = new CacheSetting();

                    using (StreamReader reader = new StreamReader(filePath, new UTF8Encoding(false)))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            var index = line.IndexOf(' ');
                            var name = line.Substring(0, index);
                            var value = line.Substring(index + 1);

                            if (name == "Path")
                            {
                                cacheSetting.Path = value;
                            }
                        }
                    }

                    return cacheSetting;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public static ColorsSetting GetColorsSetting(string filePath)
            {
                try
                {
                    var colorsSetting = new ColorsSetting();

                    {
                        Type type = typeof(ColorsSetting);

                        using (StreamReader reader = new StreamReader(filePath, new UTF8Encoding(false)))
                        {
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                var index = line.IndexOf(' ');
                                var name = line.Substring(0, index);
                                var value = line.Substring(index + 1);

                                var property = type.GetProperty(name);
                                if (property == null) continue;

                                property.SetValue(colorsSetting, (Color)System.Windows.Media.ColorConverter.ConvertFromString(value), null);
                            }
                        }
                    }

                    return colorsSetting;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [DataContract(Name = "StartupSetting")]
        public class StartupSetting
        {
            private List<ProcessSetting> _processSettings;

            [DataMember(Name = "ProcessSettings")]
            public List<ProcessSetting> ProcessSettings
            {
                get
                {
                    if (_processSettings == null)
                        _processSettings = new List<ProcessSetting>();

                    return _processSettings;
                }
            }
        }

        [DataContract(Name = "ProcessSetting")]
        public class ProcessSetting
        {
            [DataMember(Name = "Path")]
            public string Path { get; set; }

            [DataMember(Name = "Arguments")]
            public string Arguments { get; set; }

            [DataMember(Name = "WorkingDirectory")]
            public string WorkingDirectory { get; set; }
        }

        [DataContract(Name = "CatharsisSetting")]
        public class CatharsisSetting
        {
            private List<Ipv4AddressFilter> _ipv4AddressFilters;

            [DataMember(Name = "Ipv4AddressFilters")]
            public List<Ipv4AddressFilter> Ipv4AddressFilters
            {
                get
                {
                    if (_ipv4AddressFilters == null)
                        _ipv4AddressFilters = new List<Ipv4AddressFilter>();

                    return _ipv4AddressFilters;
                }
            }
        }

        [DataContract(Name = "CacheSetting")]
        public class CacheSetting
        {
            [DataMember(Name = "Path")]
            public string Path { get; set; }
        }

        [DataContract(Name = "ColorsSetting")]
        public class ColorsSetting
        {
            [DataMember(Name = "Tree_Hit")]
            public System.Windows.Media.Color Tree_Hit { get; set; }
        }
    }
}
