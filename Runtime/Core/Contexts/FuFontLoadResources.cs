using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Fu
{
    /// <summary>
    /// Owns temporary native allocations required until an ImGui font atlas has finished building.
    /// </summary>
    internal sealed class FuFontLoadResources : IDisposable
    {
        private readonly List<IntPtr> _allocations = new List<IntPtr>();
        private bool _isDisposed;

        /// <summary>
        /// Transfers ownership of an ImGui allocation to this build scope.
        /// </summary>
        /// <param name="allocation">Allocation that must remain valid until the atlas build completes.</param>
        internal void Own(IntPtr allocation)
        {
            // Ignore null pointers so callers can transfer ownership without additional branches.
            if (allocation != IntPtr.Zero)
            {
                _allocations.Add(allocation);
            }
        }

        /// <summary>
        /// Releases every temporary ImGui allocation owned by this build scope.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            // Glyph-range buffers are borrowed by ImGui only until ImFontAtlas.Build returns.
            Exception firstException = null;
            for (int i = _allocations.Count - 1; i >= 0; i--)
            {
                try
                {
                    ImGui.MemFree(_allocations[i]);
                }
                catch (Exception exception)
                {
                    // Continue so one allocator failure cannot retain the remaining native buffers.
                    firstException ??= exception;
                }
            }

            _allocations.Clear();
            _isDisposed = true;
            if (firstException != null)
            {
                throw new InvalidOperationException("One or more temporary ImGui font allocations failed to release.", firstException);
            }
        }
    }
}
