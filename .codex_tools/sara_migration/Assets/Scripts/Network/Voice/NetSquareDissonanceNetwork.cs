using Dissonance;
using Dissonance.Audio.Playback;
using Dissonance.Networking;
using NetSquare.Client;
using NetSquare.Core;
using Saravr.Network.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Saravr.Network.Voice
{
    /// <summary>
    /// Minimal Dissonance network adapter backed by NetSquare world UDP synchronization.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetSquareDissonanceNetwork : MonoBehaviour, ICommsNetwork
    {
        /// <summary>
        /// Version byte written first in every SARA voice payload.
        /// </summary>
        private const byte PacketVersion = 1;

        /// <summary>
        /// Flag bit indicating that Dissonance should play the packet with positional audio.
        /// </summary>
        private const byte PositionalFlag = 1;

        /// <summary>
        /// Sequence rollback tolerated as normal UDP reordering before treating it as a new speech session.
        /// </summary>
        private const uint SequenceResetTolerance = 16;

        /// <summary>
        /// Dissonance room used for the world-wide voice channel.
        /// </summary>
        [SerializeField] private string roomName = "SaraVoice";

        /// <summary>
        /// Safety cap for a single encoded voice packet sent through NetSquare UDP synchronization.
        /// </summary>
        [SerializeField, Min(64)] private int maxVoicePacketBytes = 1200;

        /// <summary>
        /// Time without voice packets after which a remote speaker is considered silent.
        /// </summary>
        [SerializeField, Min(0.05f)] private float speakingTimeoutSeconds = 0.35f;

        /// <summary>
        /// Remote Dissonance peers currently known by this adapter, keyed by Dissonance player id.
        /// </summary>
        private readonly Dictionary<string, RemotePeerState> peersByPlayerId = new Dictionary<string, RemotePeerState>();

        /// <summary>
        /// Reusable list used when removing peers without mutating dictionaries during enumeration.
        /// </summary>
        private readonly List<string> peersToRemove = new List<string>();

        /// <summary>
        /// Reusable list used when ending speaking states after their timeout.
        /// </summary>
        private readonly List<string> speakersToStop = new List<string>();

        /// <summary>
        /// Snapshot of Dissonance player ids derived from the current SARA session roster.
        /// </summary>
        private readonly HashSet<string> sessionPlayerIds = new HashSet<string>();

        /// <summary>
        /// Dissonance room registry passed during network initialization.
        /// </summary>
        private Rooms rooms;

        /// <summary>
        /// Dissonance room channel collection used to detect when the local user starts broadcasting.
        /// </summary>
        private RoomChannels roomChannels;

        /// <summary>
        /// Codec settings required by Dissonance when announcing remote peers.
        /// </summary>
        private CodecSettings codecSettings;

        /// <summary>
        /// Whether codec settings have been received from Dissonance.
        /// </summary>
        private bool hasCodecSettings;

        /// <summary>
        /// Whether Dissonance has initialized this network adapter.
        /// </summary>
        private bool initialized;

        /// <summary>
        /// Whether NetSquare world events are currently subscribed.
        /// </summary>
        private bool worldEventsRegistered;

        /// <summary>
        /// Whether Dissonance currently has an open broadcast channel for the configured room.
        /// </summary>
        private bool hasOpenBroadcastRoom;

        /// <summary>
        /// Positional flag captured from the active Dissonance broadcast channel.
        /// </summary>
        private bool activeBroadcastIsPositional;

        /// <summary>
        /// Priority captured from the active Dissonance broadcast channel.
        /// </summary>
        private ChannelPriority activeBroadcastPriority = ChannelPriority.Default;

        /// <summary>
        /// Monotonic sequence id written into outgoing Dissonance voice packets.
        /// </summary>
        private uint localSequenceId;

        /// <summary>
        /// Gets or sets the Dissonance room used for world voice.
        /// </summary>
        public string RoomName
        {
            get { return roomName; }
            set { roomName = string.IsNullOrWhiteSpace(value) ? "SaraVoice" : value.Trim(); }
        }

        /// <summary>
        /// Gets the Dissonance connection state from the NetSquare world state.
        /// </summary>
        public ConnectionStatus Status
        {
            get { return IsWorldVoiceReady() ? ConnectionStatus.Connected : ConnectionStatus.Disconnected; }
        }

        /// <summary>
        /// Gets the Dissonance network mode. SARA clients are always regular Dissonance clients.
        /// </summary>
        public NetworkMode Mode
        {
            get { return NetworkMode.Client; }
        }

        /// <summary>
        /// Raised when the Dissonance network mode is available.
        /// </summary>
        public event Action<NetworkMode> ModeChanged;

        /// <summary>
        /// Raised when a remote NetSquare client is announced as a Dissonance player.
        /// </summary>
        public event Action<string, CodecSettings> PlayerJoined;

        /// <summary>
        /// Raised when a remote NetSquare client leaves the Dissonance player list.
        /// </summary>
        public event Action<string> PlayerLeft;

        /// <summary>
        /// Raised when an encoded voice packet has been decoded from a NetSquare synchronization message.
        /// </summary>
        public event Action<VoicePacket> VoicePacketReceived;
#pragma warning disable CS0067
        /// <summary>
        /// Required by Dissonance, but unused because this transport only carries voice.
        /// </summary>
        public event Action<TextMessage> TextPacketReceived;
#pragma warning restore CS0067
        /// <summary>
        /// Raised when a remote peer starts speaking.
        /// </summary>
        public event Action<string> PlayerStartedSpeaking;

        /// <summary>
        /// Raised when a remote peer stops speaking.
        /// </summary>
        public event Action<string> PlayerStoppedSpeaking;
#pragma warning disable CS0067
        /// <summary>
        /// Required by Dissonance, but unused because rooms are represented by local receipt triggers.
        /// </summary>
        public event Action<RoomEvent> PlayerEnteredRoom;

        /// <summary>
        /// Required by Dissonance, but unused because rooms are represented by local receipt triggers.
        /// </summary>
        public event Action<RoomEvent> PlayerExitedRoom;
#pragma warning restore CS0067

        /// <summary>
        /// Initializes the adapter with Dissonance runtime state.
        /// </summary>
        public void Initialize(string playerName, Rooms rooms, PlayerChannels playerChannels, RoomChannels roomChannels, CodecSettings codecSettings)
        {
            this.rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            this.roomChannels = roomChannels ?? throw new ArgumentNullException(nameof(roomChannels));
            this.codecSettings = codecSettings;
            hasCodecSettings = true;
            initialized = true;

            NSClient.AddAction(Messages.ToClients_VoicePacket, OnVoicePacketReceived);

            this.roomChannels.OpenedChannel += RoomChannels_OnOpenedChannel;
            this.roomChannels.ClosedChannel += RoomChannels_OnClosedChannel;
            Sara.Events.OnSessionUpdated += Events_OnSessionUpdated;

            RegisterWorldEvents();
            SyncSessionPlayers(Sara.CurrentSession);
            ModeChanged?.Invoke(NetworkMode.Client);
        }

        /// <summary>
        /// Subscribes to NetSquare events when Unity enables the component.
        /// </summary>
        private void OnEnable()
        {
            RegisterWorldEvents();
        }

        /// <summary>
        /// Releases all Dissonance and NetSquare subscriptions when Unity disables the component.
        /// </summary>
        private void OnDisable()
        {
            UnregisterWorldEvents();

            if (roomChannels != null)
            {
                roomChannels.OpenedChannel -= RoomChannels_OnOpenedChannel;
                roomChannels.ClosedChannel -= RoomChannels_OnClosedChannel;
            }

            Sara.Events.OnSessionUpdated -= Events_OnSessionUpdated;
            StopAllSpeakers();
            ClearPeers();
            initialized = false;
        }

        /// <summary>
        /// Maintains event subscriptions and remote speaking timeouts each frame.
        /// </summary>
        private void Update()
        {
            // NetSquare can become available after this component is enabled, so retry registration until it succeeds.
            RegisterWorldEvents();

            if (!initialized)
                return;

            if (!IsWorldVoiceReady())
            {
                StopAllSpeakers();
                return;
            }

            float now = Time.unscaledTime;
            speakersToStop.Clear();
            foreach (KeyValuePair<string, RemotePeerState> pair in peersByPlayerId)
            {
                if (pair.Value.IsSpeaking && pair.Value.SpeakingDeadline <= now)
                    speakersToStop.Add(pair.Key);
            }

            for (int i = 0; i < speakersToStop.Count; i++)
                SetSpeaking(speakersToStop[i], false);
        }

        /// <summary>
        /// Sends a Dissonance voice packet through the NetSquare world UDP broadcast path.
        /// </summary>
        public void SendVoice(ArraySegment<byte> data)
        {
            if (!initialized || !IsWorldVoiceReady() || !hasOpenBroadcastRoom)
                return;

            if (data.Array == null || data.Count <= 0 || data.Count > maxVoicePacketBytes)
                return;

            // Packet layout: version, sender client id, sequence id, flags, priority, encoded Opus bytes.
            NetworkMessage message = new NetworkMessage(Messages.ToClients_VoicePacket, NSClient.ClientID)
                .Set(PacketVersion)
                .Set(NSClient.ClientID)
                .Set(localSequenceId++)
                .Set(GetCurrentFlags())
                .Set(EncodePriority(activeBroadcastPriority))
                .Set(data.Array, data.Offset, data.Count, true);

            try
            {
                NSClient.Client.WorldsManager.BroadcastUnreliable(message);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Ignores Dissonance text chat because SARA only uses this adapter for voice packets.
        /// </summary>
        public void SendText(string data, ChannelType recipientType, string recipientId)
        {
            // Text chat is not part of the SARA voice MVP. Keep Dissonance text disabled for this transport.
        }

        /// <summary>
        /// Subscribes to NetSquare world synchronization callbacks.
        /// </summary>
        private void RegisterWorldEvents()
        {
            if (worldEventsRegistered || NSClient.Client == null || NSClient.Client.WorldsManager == null)
                return;

            NSClient.Client.WorldsManager.OnClientLeaveWorld += WorldsManager_OnClientLeaveWorld;
            worldEventsRegistered = true;
        }

        /// <summary>
        /// Unsubscribes from NetSquare world synchronization callbacks.
        /// </summary>
        private void UnregisterWorldEvents()
        {
            if (!worldEventsRegistered || NSClient.Client == null || NSClient.Client.WorldsManager == null)
            {
                worldEventsRegistered = false;
                return;
            }

            NSClient.Client.WorldsManager.OnClientLeaveWorld -= WorldsManager_OnClientLeaveWorld;
            worldEventsRegistered = false;
        }

        /// <summary>
        /// Converts a NetSquare UDP synchronization message back into a Dissonance voice packet.
        /// </summary>
        private void OnVoicePacketReceived(NetworkMessage message)
        {
            if (!initialized)
                return;

            try
            {
                // Read exactly the layout written in SendVoice.
                message.RestartRead();
                byte version = message.Serializer.GetByte();
                if (version != PacketVersion)
                    return;

                uint bodySenderClientId = message.Serializer.GetUInt();
                uint senderClientId = bodySenderClientId != 0 ? bodySenderClientId : message.ClientID;
                uint sequenceId = message.Serializer.GetUInt();
                byte flags = message.Serializer.GetByte();
                ChannelPriority priority = DecodePriority(message.Serializer.GetByte());
                byte[] encodedAudio = message.Serializer.GetByteArray();

                // Ignore loopback and malformed packets. The server may include the sender in its fanout.
                if (senderClientId == 0
                    || senderClientId == NSClient.ClientID
                    || bodySenderClientId == NSClient.ClientID
                    || message.ClientID == NSClient.ClientID
                    || encodedAudio == null
                    || encodedAudio.Length == 0)
                    return;

                if (!IsListeningToRoom())
                    return;

                if (IsSenderMuted(senderClientId))
                    return;

                string playerId = SaraDissonanceVoiceIds.FromClientId(senderClientId);
                EnsurePeer(playerId);
                if (!peersByPlayerId.TryGetValue(playerId, out RemotePeerState peerState))
                    return;

                RestartSpeakingSessionIfSequenceReset(playerId, peerState, sequenceId);
                peerState.HasReceivedSequence = true;
                peerState.LastSequenceId = sequenceId;
                SetSpeaking(playerId, true);

                // Dissonance playback needs the target channel metadata alongside the encoded audio.
                bool positional = (flags & PositionalFlag) != 0;
                List<RemoteChannel> channels = new List<RemoteChannel>(1)
                {
                    new RemoteChannel(roomName, ChannelType.Room, new PlaybackOptions(positional, 1f, priority))
                };

                VoicePacketReceived?.Invoke(new VoicePacket(
                    playerId,
                    priority,
                    1f,
                    positional,
                    new ArraySegment<byte>(encodedAudio),
                    sequenceId,
                    channels));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Invalid Dissonance voice packet: " + ex.Message);
            }
        }

        /// <summary>
        /// Removes Dissonance state for a NetSquare client leaving the current world.
        /// </summary>
        private void WorldsManager_OnClientLeaveWorld(uint clientId)
        {
            if (clientId == 0)
                return;

            RemovePeer(SaraDissonanceVoiceIds.FromClientId(clientId));
        }

        /// <summary>
        /// Refreshes remote Dissonance peers when the SARA session roster changes.
        /// </summary>
        private void Events_OnSessionUpdated(SaraSession session)
        {
            SyncSessionPlayers(session);
        }

        /// <summary>
        /// Synchronizes Dissonance remote peers with the SARA session user list.
        /// </summary>
        private void SyncSessionPlayers(SaraSession session)
        {
            if (!initialized || !hasCodecSettings)
                return;

            sessionPlayerIds.Clear();

            // Pre-announce known session users so Dissonance can create playback state before their first packet.
            if (session != null && session.IsMultiplayer && session.Users != null)
            {
                for (int i = 0; i < session.Users.Length; i++)
                {
                    SaraSessionUser sessionUser = session.Users[i];
                    if (sessionUser == null || sessionUser.ClientID == 0 || sessionUser.ClientID == NSClient.ClientID)
                        continue;

                    string playerId = SaraDissonanceVoiceIds.FromClientId(sessionUser.ClientID);
                    sessionPlayerIds.Add(playerId);
                    EnsurePeer(playerId);
                }
            }

            // Remove stale peers which are no longer listed in the active session.
            peersToRemove.Clear();
            foreach (string playerId in peersByPlayerId.Keys)
            {
                if (!sessionPlayerIds.Contains(playerId))
                    peersToRemove.Add(playerId);
            }

            for (int i = 0; i < peersToRemove.Count; i++)
                RemovePeer(peersToRemove[i]);
        }

        /// <summary>
        /// Ensures a Dissonance remote peer exists for the specified player id.
        /// </summary>
        private void EnsurePeer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId) || peersByPlayerId.ContainsKey(playerId) || !hasCodecSettings)
                return;

            peersByPlayerId[playerId] = new RemotePeerState();
            PlayerJoined?.Invoke(playerId, codecSettings);
        }

        /// <summary>
        /// Removes a Dissonance remote peer and emits the matching leave event.
        /// </summary>
        private void RemovePeer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId) || !peersByPlayerId.ContainsKey(playerId))
                return;

            SetSpeaking(playerId, false);
            peersByPlayerId.Remove(playerId);
            PlayerLeft?.Invoke(playerId);
        }

        /// <summary>
        /// Removes every tracked remote Dissonance peer.
        /// </summary>
        private void ClearPeers()
        {
            peersToRemove.Clear();
            foreach (string playerId in peersByPlayerId.Keys)
                peersToRemove.Add(playerId);

            for (int i = 0; i < peersToRemove.Count; i++)
                RemovePeer(peersToRemove[i]);
        }

        /// <summary>
        /// Updates Dissonance speaking state for a remote peer.
        /// </summary>
        private void SetSpeaking(string playerId, bool speaking)
        {
            if (!peersByPlayerId.TryGetValue(playerId, out RemotePeerState state))
                return;

            if (speaking)
            {
                // Every received packet extends the speaking deadline.
                state.SpeakingDeadline = Time.unscaledTime + speakingTimeoutSeconds;
                if (!state.IsSpeaking)
                {
                    state.IsSpeaking = true;
                    PlayerStartedSpeaking?.Invoke(playerId);
                }
            }
            else if (state.IsSpeaking)
            {
                state.IsSpeaking = false;
                PlayerStoppedSpeaking?.Invoke(playerId);
            }
        }

        /// <summary>
        /// Forces every tracked remote speaker into a stopped state.
        /// </summary>
        private void StopAllSpeakers()
        {
            speakersToStop.Clear();
            foreach (KeyValuePair<string, RemotePeerState> pair in peersByPlayerId)
            {
                if (pair.Value.IsSpeaking)
                    speakersToStop.Add(pair.Key);
            }

            for (int i = 0; i < speakersToStop.Count; i++)
                SetSpeaking(speakersToStop[i], false);
        }

        /// <summary>
        /// Captures Dissonance broadcast channel settings when the local user starts transmitting.
        /// </summary>
        private void RoomChannels_OnOpenedChannel(RoomName channel, ChannelProperties properties)
        {
            if (channel.Name != roomName)
                return;

            // Dissonance playback buffers start each speech session at sequence zero.
            localSequenceId = 0;
            hasOpenBroadcastRoom = true;
            activeBroadcastIsPositional = properties != null && properties.Positional;
            activeBroadcastPriority = properties != null ? properties.Priority : ChannelPriority.Default;
        }

        /// <summary>
        /// Marks the local broadcast room as closed when Dissonance stops transmitting.
        /// </summary>
        private void RoomChannels_OnClosedChannel(RoomName channel, ChannelProperties properties)
        {
            if (channel.Name == roomName)
                hasOpenBroadcastRoom = false;
        }

        /// <summary>
        /// Returns whether SARA and NetSquare are ready to carry voice packets for the current world.
        /// </summary>
        private bool IsWorldVoiceReady()
        {
            return Sara.CurrentSession != null
                && Sara.CurrentSession.IsMultiplayer
                && NSClient.IsConnected
                && NSClient.ClientID != 0
                && NSClient.Client != null
                && NSClient.Client.WorldsManager != null
                && NSClient.Client.WorldsManager.IsInWorld;
        }

        /// <summary>
        /// Returns whether the local Dissonance runtime is listening to the configured voice room.
        /// </summary>
        private bool IsListeningToRoom()
        {
            return rooms != null && rooms.Contains(roomName);
        }

        /// <summary>
        /// Returns whether a sender should be suppressed by session moderation state.
        /// </summary>
        private static bool IsSenderMuted(uint senderClientId)
        {
            if (Sara.Network == null || !Sara.Network.TryGetUser(senderClientId, out SaraUser user) || user == null)
                return false;

            return user.IsMuted || !user.CanTalk;
        }

        /// <summary>
        /// Builds the compact packet flags byte for the active Dissonance broadcast channel.
        /// </summary>
        private byte GetCurrentFlags()
        {
            return activeBroadcastIsPositional ? PositionalFlag : (byte)0;
        }

        /// <summary>
        /// Converts Dissonance channel priority to a stable byte representation.
        /// </summary>
        private static byte EncodePriority(ChannelPriority priority)
        {
            switch (priority)
            {
                case ChannelPriority.Low:
                    return 0;
                case ChannelPriority.Medium:
                    return 2;
                case ChannelPriority.High:
                    return 3;
                case ChannelPriority.None:
                case ChannelPriority.Default:
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Converts the stable byte representation back to Dissonance channel priority.
        /// </summary>
        private static ChannelPriority DecodePriority(byte priority)
        {
            switch (priority)
            {
                case 0:
                    return ChannelPriority.Low;
                case 2:
                    return ChannelPriority.Medium;
                case 3:
                    return ChannelPriority.High;
                case 1:
                default:
                    return ChannelPriority.Default;
            }
        }

        /// <summary>
        /// Restarts a Dissonance speaking session when a remote sender begins a new sequence before our timeout fired.
        /// </summary>
        private void RestartSpeakingSessionIfSequenceReset(string playerId, RemotePeerState state, uint sequenceId)
        {
            if (!state.IsSpeaking || !state.HasReceivedSequence)
                return;

            // A fresh Dissonance send session starts at zero. Accept one lost first packet by also treating one as fresh.
            if ((sequenceId <= 1 && state.LastSequenceId > sequenceId)
                || (sequenceId < state.LastSequenceId && state.LastSequenceId - sequenceId > SequenceResetTolerance))
            {
                SetSpeaking(playerId, false);
            }
        }

        /// <summary>
        /// Tracks transient playback state for one remote Dissonance peer.
        /// </summary>
        private sealed class RemotePeerState
        {
            /// <summary>
            /// Whether the peer is currently announced as speaking to Dissonance.
            /// </summary>
            public bool IsSpeaking;

            /// <summary>
            /// Unscaled time at which the peer should be considered silent if no more packets arrive.
            /// </summary>
            public float SpeakingDeadline;

            /// <summary>
            /// Whether at least one sequence id has been received from this peer.
            /// </summary>
            public bool HasReceivedSequence;

            /// <summary>
            /// Last Dissonance sequence id received from this peer.
            /// </summary>
            public uint LastSequenceId;
        }
    }
}
