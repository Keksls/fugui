using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuModalSize
    {
        public Vector2 Size { get; private set; }

        public FuModalSize(Vector2 size)
        {
            Size = size;
        }

        #region Presset
        private static FuModalSize _small = new FuModalSize(new Vector2(300f, 0f));
        private static FuModalSize _medium = new FuModalSize(new Vector2(500f, 0f));
        private static FuModalSize _large = new FuModalSize(new Vector2(800f, 0f));
        private static FuModalSize _extraLarge = new FuModalSize(new Vector2(1040f, 0f));
        public static FuModalSize Small { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _small; } }
        public static FuModalSize Medium { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _medium; } }
        public static FuModalSize Large { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _large; } }
        public static FuModalSize ExtraLarge { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _extraLarge; } }
        #endregion
    }
}