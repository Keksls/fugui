using Newtonsoft.Json;

namespace Fu.Framework
{
        /// <summary>
        /// Serialize/Deserialize port values based on DataType.
        /// </summary>
        public static class FuPortValueSerializer
        {
            #region Methods
            /// <summary>
            /// Serialize a port runtime value to JSON according to its DataType.
            /// </summary>
            public static string ToJson(FuNodalGraph graph, string dataType, object value)
            {
                if (value == null)
                    return null;

                FuNodalType type = graph.Registry.GetType(dataType);
                if (type != null && type.SerializationFunc != null)
                {
                    // Custom serialization if provided
                    return type.SerializationFunc(value);
                }

                return JsonConvert.SerializeObject(value);
            }

            /// <summary>
            /// Deserialize JSON to a runtime value according to DataType.
            /// </summary>
            public static object FromJson(FuNodalGraph graph, string dataType, string json)
            {
                if (string.IsNullOrEmpty(json))
                    return null;

                FuNodalType type = graph.Registry.GetType(dataType);
                if (type != null && type.DeserializationFunc != null)
                {
                    // Custom deserialization if provided
                    return type.DeserializationFunc(json);
                }

                // Unknown types: return raw string or attempt object
                return JsonConvert.DeserializeObject<object>(json);
            }
            #endregion
        }
}
