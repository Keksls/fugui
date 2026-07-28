using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private static Dictionary<string, FuVideoPlayer> _videoPlayers = new Dictionary<string, FuVideoPlayer>();
        #endregion

        #region Methods
        /// <summary>
        /// Get a FuVideoPlayer instance for the given ID
        /// </summary>
        /// <param name="ID">Unique ID of the video player</param>
        /// <returns>FuVideoPlayer instance</returns>
        public FuVideoPlayer GetVideoPlayer(string ID)
        {
            if (!_videoPlayers.ContainsKey(ID))
            {
                _videoPlayers.Add(ID, new FuVideoPlayer(ID));
            }
            return _videoPlayers[ID];
        }

        /// <summary>
        /// Kill and release all resources of a given FuVideoPlayer
        /// </summary>
        /// <param name="ID">Unique ID of the video player</param>
        public void KillVideoPlayer(string ID)
        {
            if (_videoPlayers.ContainsKey(ID))
            {
                try
                {
                    _videoPlayers[ID].Kill();
                }
                finally
                {
                    // A fully visited but failed teardown must not leave a disposed player registered.
                    _videoPlayers.Remove(ID);
                }
            }
        }

        /// <summary>
        /// Releases every video player owned by the current Fugui session.
        /// </summary>
        internal static void DisposeVideoPlayers()
        {
            // Video players are stored statically, so session disposal must explicitly end their ownership.
            System.Exception firstException = null;
            foreach (FuVideoPlayer videoPlayer in _videoPlayers.Values)
            {
                try
                {
                    videoPlayer?.Dispose();
                }
                catch (System.Exception exception)
                {
                    // Continue so one failing player cannot retain every later native video resource.
                    firstException ??= exception;
                }
            }

            _videoPlayers.Clear();
            if (firstException != null)
            {
                throw new System.InvalidOperationException("One or more Fugui video players failed to dispose.", firstException);
            }
        }
        #endregion
    }
}
