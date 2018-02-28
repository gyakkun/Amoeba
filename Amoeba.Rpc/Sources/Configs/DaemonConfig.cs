using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba.Rpc
{
    [DataContract]
    public class DaemonConfig
    {
        public DaemonConfig() { }

        public DaemonConfig(Version version, CommunicationConfig communication, CacheConfig cache, PathsConfig paths)
        {
            this.Version = version;
            this.Communication = communication;
            this.Cache = cache;
            this.Paths = paths;
        }

        [DataMember(Name = nameof(Version))]
        public Version Version { get; private set; }

        [DataMember(Name = nameof(Communication))]
        public CommunicationConfig Communication { get; private set; }

        [DataMember(Name = nameof(Cache))]
        public CacheConfig Cache { get; private set; }

        [DataMember(Name = nameof(Paths))]
        public PathsConfig Paths { get; private set; }

        [DataContract]
        public class CommunicationConfig
        {
            public CommunicationConfig() { }

            public CommunicationConfig(string listenUri)
            {
                this.ListenUri = listenUri;
            }

            [DataMember(Name = nameof(ListenUri))]
            public string ListenUri { get; private set; }
        }

        [DataContract]
        public class CacheConfig
        {
            public CacheConfig() { }

            public CacheConfig(string blocksFilePath)
            {
                this.BlocksFilePath = blocksFilePath;
            }

            [DataMember(Name = nameof(BlocksFilePath))]
            public string BlocksFilePath { get; private set; }
        }

        [DataContract]
        public class PathsConfig
        {
            public PathsConfig() { }

            public PathsConfig(string tempDirectoryPath, string configDirectoryPath, string logDirectoryPath)
            {
                this.TempDirectoryPath = tempDirectoryPath;
                this.ConfigDirectoryPath = configDirectoryPath;
                this.LogDirectoryPath = logDirectoryPath;
            }

            [DataMember(Name = nameof(TempDirectoryPath))]
            public string TempDirectoryPath { get; private set; }

            [DataMember(Name = nameof(ConfigDirectoryPath))]
            public string ConfigDirectoryPath { get; private set; }

            [DataMember(Name = nameof(LogDirectoryPath))]
            public string LogDirectoryPath { get; private set; }
        }
    }
}
