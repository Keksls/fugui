// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Fu.Framework.Nodal
{
    public interface INodalSerializer
    {
        string SaveToJson(FuNodalGraph graph);
        FuNodalGraph LoadFromJson(string json);
    }

    public sealed class JsonNodalSerializer : INodalSerializer
    {
        private readonly DataContractJsonSerializerSettings _settings =
            new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true,
                DateTimeFormat = new DateTimeFormat("o")
            };

        public string SaveToJson(FuNodalGraph graph)
        {
            var ser = new DataContractJsonSerializer(typeof(FuNodalGraph), _settings);
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, graph);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public FuNodalGraph LoadFromJson(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json ?? string.Empty);
            var ser = new DataContractJsonSerializer(typeof(FuNodalGraph), _settings);
            using (var ms = new MemoryStream(bytes))
            {
                return (FuNodalGraph)ser.ReadObject(ms);
            }
        }
    }
}
