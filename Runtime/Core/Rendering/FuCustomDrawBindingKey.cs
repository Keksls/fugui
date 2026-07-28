#if FU_CUSTOM_MATERIALS_ENABLED
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Reference-identity key used to reuse custom material and texture bindings.
    /// </summary>
    internal readonly struct FuCustomDrawBindingKey : IEquatable<FuCustomDrawBindingKey>
    {
        private readonly FuDrawMaterial _drawMaterial;
        private readonly Texture _texture;

        /// <summary>
        /// Creates a binding lookup key without taking ownership of its resources.
        /// </summary>
        /// <param name="drawMaterial">Custom material configuration.</param>
        /// <param name="texture">Texture paired with the custom material.</param>
        internal FuCustomDrawBindingKey(FuDrawMaterial drawMaterial, Texture texture)
        {
            // Preserve exact references so two otherwise identical materials remain independent.
            _drawMaterial = drawMaterial;
            _texture = texture;
        }

        /// <summary>
        /// Compares two binding keys by managed object identity.
        /// </summary>
        /// <param name="other">Binding key to compare.</param>
        /// <returns>True when both keys reference the same material configuration and texture.</returns>
        public bool Equals(FuCustomDrawBindingKey other)
        {
            // Unity value equality is unsuitable because destroyed objects compare equal to null.
            return ReferenceEquals(_drawMaterial, other._drawMaterial) &&
                   ReferenceEquals(_texture, other._texture);
        }

        /// <summary>
        /// Compares this binding key with an arbitrary object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when the object is an equivalent binding key.</returns>
        public override bool Equals(object obj)
        {
            // Delegate typed comparisons to the identity-based implementation.
            return obj is FuCustomDrawBindingKey other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code based on managed object identity.
        /// </summary>
        /// <returns>Stable hash code for the referenced material configuration and texture.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // UnityEngine.Object equality changes after destruction, so identity hashing is required.
                int materialHash = _drawMaterial != null ? RuntimeHelpers.GetHashCode(_drawMaterial) : 0;
                int textureHash = !ReferenceEquals(_texture, null) ? RuntimeHelpers.GetHashCode(_texture) : 0;
                return (materialHash * 397) ^ textureHash;
            }
        }
    }
}
#endif
