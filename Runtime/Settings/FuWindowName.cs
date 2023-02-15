using System;

namespace Fu
{
    public struct FuWindowName : IComparable<FuWindowName>, IEquatable<FuWindowName>
    {
        ushort _id;
        string _name;
        public ushort ID { get { return _id; } }
        public string Name { get { return _name; } }

        public FuWindowName(ushort id, string name)
        {
            _id = id;
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
