using System;

namespace Fu
{
    public struct FuWindowName : IComparable<FuWindowName>, IEquatable<FuWindowName>
    {
        ushort _id;
        string _name;
        bool _autoInstantiateWindowOnlayoutSet;
        short _idleFPS;
        public ushort ID { get { return _id; } }
        public string Name { get { return _name; } }
        /// <summary>
        /// Whatever Fugui will instantiate the window once the layout is set
        ///</summary>
        public bool AutoInstantiateWindowOnlayoutSet { get { return _autoInstantiateWindowOnlayoutSet; } }
        /// <summary>
        /// Idle FPS of the window (-1 to let fugui handle it auto)
        ///</summary>
        public short IdleFPS { get { return _idleFPS; } }

        public FuWindowName(ushort id, string name, bool autoInstantiateWindowOnlayoutSet, short idleFPS)
        {
            _id = id;
            _name = name;
            _autoInstantiateWindowOnlayoutSet = autoInstantiateWindowOnlayoutSet;
            _idleFPS = idleFPS;
        }

        public FuWindowName(FuWindowName windowName)
        {
            _id = windowName.ID;
            _name = windowName.Name;
            _autoInstantiateWindowOnlayoutSet = windowName.AutoInstantiateWindowOnlayoutSet;
            _idleFPS = windowName.IdleFPS;
        }

        public void SetAutoInstantiateOnLayoutSet(bool autoInstantiateWindowOnlayoutSet)
        {
            _autoInstantiateWindowOnlayoutSet = autoInstantiateWindowOnlayoutSet;
        }

        public void SetIdleFPS(short idleFPS)
        {
            _idleFPS = idleFPS;
        }

        public void SetName(string name)
        {
            _name = name;
        }

        public int CompareTo(FuWindowName other)
        {
            return _id == other.ID ? 1 : 0;
        }

        public bool Equals(FuWindowName other)
        {
            return _id == other.ID;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public override string ToString()
        {
            return _name;
        }

        public override bool Equals(object obj)
        {
            return obj is FuWindowName && ((FuWindowName)obj).Equals(this);
        }
    }
}