using System.Collections.Generic;
using System.Linq;

namespace Fu.Framework
{
    public static class FuNodeId
    {
        private static int _nextId = 1;

        /// <summary>
        /// Generates a new unique integer ID for a node.
        /// </summary>
        public static int New()
        {
            return System.Threading.Interlocked.Increment(ref _nextId);
        }

        /// <summary>
        /// Synchronizes the ID counter with existing node IDs after loading a graph.
        /// </summary>
        public static void Sync(IEnumerable<int> existingIds)
        {
            if (existingIds == null || !existingIds.Any())
                return;

            int maxId = existingIds.Max();
            int current = _nextId;

            if (maxId > current)
                _nextId = maxId;

            if (_nextId == int.MaxValue || _nextId < 0)
                _nextId = 1;
        }

        /// <summary>
        /// Resets the ID counter (use only when starting a new graph).
        /// </summary>
        public static void Reset(int startValue = 1)
        {
            if (startValue < 1)
                startValue = 1;
            _nextId = startValue;
        }
    }
}