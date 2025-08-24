using System.Collections.Generic;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private static Dictionary<string, FuVideoPlayer> _videoPlayers = new Dictionary<string, FuVideoPlayer>();

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
                _videoPlayers[ID].Kill();
                _videoPlayers.Remove(ID);
            }
        }
    }
}