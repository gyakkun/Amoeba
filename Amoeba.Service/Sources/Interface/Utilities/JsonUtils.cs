using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Omnius.Base;
using Omnius.Io;
using System.Linq;

namespace Amoeba.Service
{
    class JsonUtils
    {
        private static BufferManager bufferManager;

        public static Stream Serialize<T>(T value)
        {
            var stream = new BufferStream(BufferManager.Instance);

            using (var compressStream = new GZipStream(stream, CompressionMode.Compress, true))
            {
                using (var streamWriter = new StreamWriter(compressStream, new UTF8Encoding(false)))
                using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                {
                    var serializer = new JsonSerializer();
                    serializer.Formatting = Newtonsoft.Json.Formatting.None;
                    serializer.TypeNameHandling = TypeNameHandling.None;

                    serializer.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter());
                    serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    serializer.ContractResolver = new CustomContractResolver();

                    serializer.Serialize(jsonTextWriter, value);
                }
            }

            return stream;
        }

        public static T Deserialize<T>(Stream stream)
        {
            using (var decompressStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                using (var streamReader = new StreamReader(decompressStream, new UTF8Encoding(false)))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var serializer = new JsonSerializer();
                    serializer.MissingMemberHandling = MissingMemberHandling.Ignore;

                    serializer.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter());
                    serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    serializer.ContractResolver = new CustomContractResolver();

                    return serializer.Deserialize<T>(jsonTextReader);
                }
            }
        }

        class CustomContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                if (objectType.GetTypeInfo().GetInterfaces()
                    .Any(type => type.IsConstructedGenericType
                        && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                        && type.GetGenericTypeDefinition().GenericTypeArguments.First() != typeof(string)))
                {
                    return base.CreateArrayContract(objectType);
                }

                if (objectType.GetTypeInfo().CustomAttributes.Any(n => n.AttributeType == typeof(DataContractAttribute)))
                {
                    var objectContract = base.CreateObjectContract(objectType);
                    objectContract.DefaultCreatorNonPublic = true;
                    objectContract.DefaultCreator = () => Activator.CreateInstance(objectType, true);

                    return objectContract;
                }

                return base.CreateContract(objectType);
            }
        }
    }
}
