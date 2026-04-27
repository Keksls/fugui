using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Window Name data structure.
    /// </summary>
    [Serializable]
    public struct FuWindowName : IComparable<FuWindowName>, IEquatable<FuWindowName>
    {
        #region State
        [SerializeField]
        private ushort _id;
        [SerializeField]
        private string _name;
        [SerializeField]
        private bool _autoInstantiateWindowOnlayoutSet;
        [SerializeField]
        private short _idleFPS;

        public ushort ID { get { return _id; } }
        public string Name { get { return _name; } }
        /// <summary>
        /// Whatever Fugui will instantiate the window once the layout is set
        /// </summary>
        public bool AutoInstantiateWindowOnlayoutSet { get { return _autoInstantiateWindowOnlayoutSet; } }
        /// <summary>
        /// Idle FPS of the window (-1 to let fugui handle it auto)
        /// </summary>
        public short IdleFPS { get { return _idleFPS; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Fu Window Name class.
        /// </summary>
        /// <param name="id">The id value.</param>
        /// <param name="name">The name value.</param>
        /// <param name="autoInstantiateWindowOnlayoutSet">The auto Instantiate Window Onlayout Set value.</param>
        /// <param name="idleFPS">The idle FPS value.</param>
        public FuWindowName(ushort id, string name, bool autoInstantiateWindowOnlayoutSet, short idleFPS)
        {
            _id = id;
            _name = name;
            _autoInstantiateWindowOnlayoutSet = autoInstantiateWindowOnlayoutSet;
            _idleFPS = idleFPS;
        }

        /// <summary>
        /// Initializes a new instance of the Fu Window Name class.
        /// </summary>
        /// <param name="windowName">The window Name value.</param>
        public FuWindowName(FuWindowName windowName)
        {
            _id = windowName.ID;
            _name = windowName.Name;
            _autoInstantiateWindowOnlayoutSet = windowName.AutoInstantiateWindowOnlayoutSet;
            _idleFPS = windowName.IdleFPS;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the auto instantiate on layout set.
        /// </summary>
        /// <param name="autoInstantiateWindowOnlayoutSet">The auto Instantiate Window Onlayout Set value.</param>
        public void SetAutoInstantiateOnLayoutSet(bool autoInstantiateWindowOnlayoutSet)
        {
            _autoInstantiateWindowOnlayoutSet = autoInstantiateWindowOnlayoutSet;
        }

        /// <summary>
        /// Sets the idle fps.
        /// </summary>
        /// <param name="idleFPS">The idle FPS value.</param>
        public void SetIdleFPS(short idleFPS)
        {
            _idleFPS = idleFPS;
        }

        /// <summary>
        /// Sets the name.
        /// </summary>
        /// <param name="name">The name value.</param>
        public void SetName(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Returns the compare to result.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns>The result of the operation.</returns>
        public int CompareTo(FuWindowName other)
        {
            return _id == other.ID ? 1 : 0;
        }

        /// <summary>
        /// Returns the equals result.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns>The result of the operation.</returns>
        public bool Equals(FuWindowName other)
        {
            return _id == other.ID;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public override int GetHashCode()
        {
            return _id;
        }

        /// <summary>
        /// Returns the to string result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public override string ToString()
        {
            return _name;
        }

        /// <summary>
        /// Returns the equals result.
        /// </summary>
        /// <param name="obj">The obj value.</param>
        /// <returns>The result of the operation.</returns>
        public override bool Equals(object obj)
        {
            return obj is FuWindowName && ((FuWindowName)obj).Equals(this);
        }
        #endregion
    }
}