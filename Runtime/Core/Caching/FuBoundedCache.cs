using System;
using System.Collections.Generic;

namespace Fu
{
    /// <summary>
    /// Fixed-capacity least-recently-used cache that does not allocate on cache hits.
    /// </summary>
    internal sealed class FuBoundedCache<TKey, TValue>
    {
        #region State
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _nodes;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _usage;
        #endregion

        #region Properties
        internal int Count => _nodes.Count;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a bounded cache with the requested capacity and key comparer.
        /// </summary>
        /// <param name="capacity">Maximum number of retained entries.</param>
        /// <param name="comparer">Optional key comparer.</param>
        internal FuBoundedCache(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            // A cache without a positive capacity cannot provide deterministic retention.
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _nodes = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(Math.Min(capacity, 16), comparer);
            _usage = new LinkedList<KeyValuePair<TKey, TValue>>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets a cached value and promotes it as the most recently used entry.
        /// </summary>
        /// <param name="key">Key to resolve.</param>
        /// <param name="value">Resolved value.</param>
        /// <returns>True when the key exists.</returns>
        internal bool TryGetValue(TKey key, out TValue value)
        {
            // Cache hits only relink the existing node and do not allocate.
            if (!_nodes.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue>> node))
            {
                value = default;
                return false;
            }

            Touch(node);
            value = node.Value.Value;
            return true;
        }

        /// <summary>
        /// Adds or replaces a value and evicts the least recently used entry when full.
        /// </summary>
        /// <param name="key">Key to cache.</param>
        /// <param name="value">Value to retain.</param>
        internal void Set(TKey key, TValue value)
        {
            // Existing nodes keep their allocation and are moved to the hot end of the list.
            if (_nodes.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue>> node))
            {
                node.Value = new KeyValuePair<TKey, TValue>(key, value);
                Touch(node);
                return;
            }

            if (_nodes.Count >= _capacity)
            {
                EvictOldest();
            }

            LinkedListNode<KeyValuePair<TKey, TValue>> newNode =
                _usage.AddLast(new KeyValuePair<TKey, TValue>(key, value));
            _nodes.Add(key, newNode);
        }

        /// <summary>
        /// Removes one cached entry.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        /// <returns>True when an entry was removed.</returns>
        internal bool Remove(TKey key)
        {
            // Both indexes are updated together so capacity accounting remains exact.
            if (!_nodes.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue>> node))
            {
                return false;
            }

            _nodes.Remove(key);
            _usage.Remove(node);
            return true;
        }

        /// <summary>
        /// Clears every retained entry.
        /// </summary>
        internal void Clear()
        {
            // Clearing both collections also releases cached values immediately.
            _nodes.Clear();
            _usage.Clear();
        }

        /// <summary>
        /// Moves an existing entry to the most recently used position.
        /// </summary>
        /// <param name="node">Node to promote.</param>
        private void Touch(LinkedListNode<KeyValuePair<TKey, TValue>> node)
        {
            // Relinking a LinkedList node reuses the node allocation.
            if (ReferenceEquals(_usage.Last, node))
            {
                return;
            }

            _usage.Remove(node);
            _usage.AddLast(node);
        }

        /// <summary>
        /// Evicts the least recently used entry.
        /// </summary>
        private void EvictOldest()
        {
            // The list head and dictionary entry always describe the same cache item.
            LinkedListNode<KeyValuePair<TKey, TValue>> oldest = _usage.First;
            if (oldest == null)
            {
                return;
            }

            _usage.RemoveFirst();
            _nodes.Remove(oldest.Value.Key);
        }
        #endregion
    }
}
