using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIModalSize
    {
        public Vector2 Size { get; private set; }

        public UIModalSize(Vector2 size)
        {
            Size = size;
        }

        #region Presset
        private static UIModalSize _small = new UIModalSize(new Vector2(300f, 0f));
        private static UIModalSize _medium = new UIModalSize(new Vector2(500f, 0f));
        private static UIModalSize _large = new UIModalSize(new Vector2(800f, 0f));
        private static UIModalSize _extraLarge = new UIModalSize(new Vector2(1040f, 0f));
        public static UIModalSize Small { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _small; } }
        public static UIModalSize Medium { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _medium; } }
        public static UIModalSize Large { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _large; } }
        public static UIModalSize ExtraLarge { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _extraLarge; } }
        #endregion
    }
}