using System;
using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Nodal Type type.
        /// </summary>
        public sealed class FuNodalType
        {
            #region State
            public string Name { get; private set; }
            public Type Type { get; private set; }
            public Color? Color { get; private set; }
            public object DefaultValue { get; private set; }
            public Func<object, bool> ValidationFunc { get; private set; } = null;
            public Func<object, string> SerializationFunc { get; private set; } = null;
            public Func<string, object> DeserializationFunc { get; private set; } = null;
            #endregion

            #region Methods
            /// <summary>
            /// Create a new FuNodalType instance with the specified parameters.
            /// </summary>
            /// <typeparam name="T"> The type of the nodal type. Must be a non-nullable type.</typeparam>
            /// <param name="name"> The name of the nodal type.</param>
            /// <param name="defaultValue"> The default value for the nodal type.</param>
            /// <param name="serializationFunc"> A function that serializes the nodal type value to a string.</param>
            /// <param name="deserializationFunc"> A function that deserializes a string to the nodal type value.</param>
            /// <param name="validationFunc"> An optional function that validates the nodal type value. If not provided, a default validation function that checks if the value is of type T will be used.</param>
            /// <param name="color"> An optional color associated with the nodal type.</param>
            /// <returns> A new instance of FuNodalType with the specified parameters.</returns>
            public static FuNodalType Create<T>(string name, T defaultValue, Func<T, string> serializationFunc, Func<string, T> deserializationFunc, Func<object, bool> validationFunc = null, Color? color = null)
            {
                return new FuNodalType()
                {
                    Type = typeof(T),
                    Name = name,
                    DefaultValue = defaultValue,
                    Color = color,
                    ValidationFunc = validationFunc == null ? (obj) => obj is T : validationFunc,
                    SerializationFunc = (obj) => serializationFunc((T)obj),
                    DeserializationFunc = (str) => deserializationFunc(str)
                };
            }
            #endregion
        }
}