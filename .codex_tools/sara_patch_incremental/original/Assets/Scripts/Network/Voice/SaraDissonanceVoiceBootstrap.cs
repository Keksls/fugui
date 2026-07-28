using Dissonance;
using NetSquare.Client;
using Saravr.Network.Common;
using UnityEngine;

namespace Saravr.Network.Voice
{
    /// <summary>
    /// Creates and owns the Dissonance voice runtime for NetSquare multiplayer sessions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaraDissonanceVoiceBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Dissonance room joined and broadcast to by every client in the same NetSquare world.
        /// </summary>
        [SerializeField] private string roomName = "SaraVoice";

        /// <summary>
        /// Local microphone activation mode used by the Dissonance broadcast trigger.
        /// </summary>
        [SerializeField] private CommActivationMode activationMode = CommActivationMode.VoiceActivation;

        /// <summary>
        /// Voice channel priority sent to Dissonance when the local user broadcasts.
        /// </summary>
        [SerializeField] private ChannelPriority priority = ChannelPriority.Default;

        /// <summary>
        /// Whether the broadcast trigger should ask Dissonance to attach positional data.
        /// </summary>
        [SerializeField] private bool broadcastPosition;

        /// <summary>
        /// Scene or prefab GameObject containing the Dissonance voice components controlled by this bootstrap.
        /// </summary>
        [SerializeField] private GameObject voiceRoot;

        /// <summary>
        /// Scene or prefab Dissonance singleton activated for the active NetSquare world.
        /// </summary>
        [SerializeField] private DissonanceComms comms;

        /// <summary>
        /// Runtime trigger responsible for opening the local Dissonance room broadcast channel.
        /// </summary>
        [SerializeField] private VoiceBroadcastTrigger broadcastTrigger;

        /// <summary>
        /// Runtime trigger responsible for joining the Dissonance room and receiving voice.
        /// </summary>
        [SerializeField] private VoiceReceiptTrigger receiptTrigger;

        /// <summary>
        /// Runtime network adapter connecting Dissonance to NetSquare world synchronization.
        /// </summary>
        [SerializeField] private NetSquareDissonanceNetwork network;

        /// <summary>
        /// Client id for which the current runtime was created.
        /// </summary>
        private uint activeClientId;

        /// <summary>
        /// World id for which the current runtime was created.
        /// </summary>
        private ushort activeWorldId;

        /// <summary>
        /// Session code for which the current runtime was created.
        /// </summary>
        private string activeSessionCode = string.Empty;

        /// <summary>
        /// Client id used the first time Dissonance LocalPlayerName was assigned.
        /// </summary>
        private uint assignedClientId;

        /// <summary>
        /// Whether the duplicate Dissonance singleton warning has already been logged.
        /// </summary>
        private bool warnedExistingDissonance;

        /// <summary>
        /// Whether missing or invalid scene references have already been logged.
        /// </summary>
        private bool warnedInvalidReferences;

        /// <summary>
        /// Ensures a preconfigured rig does not start Dissonance before NetSquare has a client id.
        /// </summary>
        private void Awake()
        {
            if (!ShouldRunVoice())
                DeactivateVoiceRoot();
        }

        /// <summary>
        /// Creates or destroys voice runtime state based on the current SARA multiplayer state.
        /// </summary>
        private void Update()
        {
            // Voice only exists while the local client is inside a multiplayer NetSquare world.
            if (ShouldRunVoice())
                EnsureVoiceRoot();
            else
                DeactivateVoiceRoot();

            SyncMuteState();
        }

        /// <summary>
        /// Returns whether all prerequisites for world voice are currently satisfied.
        /// </summary>
        private bool ShouldRunVoice()
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
        /// Configures and activates the preconfigured Dissonance rig for the active NetSquare world when needed.
        /// </summary>
        private void EnsureVoiceRoot()
        {
            if (!ValidateReferences())
                return;

            ushort worldId = Sara.CurrentSession != null ? Sara.CurrentSession.WorldId : (ushort)0;
            string sessionCode = Sara.Network != null ? Sara.Network.CurrentSessionCode : string.Empty;

            // Reuse the runtime while the same client remains in the same session/world.
            if (voiceRoot.activeSelf
                && activeClientId == NSClient.ClientID
                && activeWorldId == worldId
                && activeSessionCode == sessionCode)
                return;

            DeactivateVoiceRoot();

            // Dissonance is designed around one active singleton, so avoid activating over another rig.
            DissonanceComms existingComms = DissonanceComms.GetSingleton();
            if (existingComms != null && existingComms != comms)
            {
                if (!warnedExistingDissonance)
                {
                    Debug.LogWarning("SARA voice did not activate because another DissonanceComms singleton already exists.");
                    warnedExistingDissonance = true;
                }

                return;
            }

            if (!AssignLocalPlayerName())
                return;

            // Keep scene/prefab settings authoritative, but enforce the shared SARA room contract.
            network.RoomName = roomName;
            receiptTrigger.RoomName = roomName;
            receiptTrigger.UseColliderTrigger = false;
            broadcastTrigger.ChannelType = CommTriggerTarget.Room;
            broadcastTrigger.RoomName = roomName;
            broadcastTrigger.Priority = priority;
            broadcastTrigger.BroadcastPosition = broadcastPosition;
            broadcastTrigger.UseColliderTrigger = false;
            ApplyVoiceSettings();

            // Activate only after LocalPlayerName and room settings are assigned, before Dissonance Start runs.
            voiceRoot.SetActive(true);

            activeClientId = NSClient.ClientID;
            activeWorldId = worldId;
            activeSessionCode = sessionCode;
            SyncMuteState();
        }

        /// <summary>
        /// Validates that the scene or prefab has all required Dissonance voice references assigned.
        /// </summary>
        private bool ValidateReferences()
        {
            if (voiceRoot == null
                || comms == null
                || broadcastTrigger == null
                || receiptTrigger == null
                || network == null)
            {
                LogInvalidReferences("SARA voice rig references are incomplete.");
                return false;
            }

            if (comms.gameObject != network.gameObject)
            {
                LogInvalidReferences("DissonanceComms and NetSquareDissonanceNetwork must be on the same Voice Rig GameObject.");
                return false;
            }

            if (broadcastTrigger.gameObject != voiceRoot || receiptTrigger.gameObject != voiceRoot || comms.gameObject != voiceRoot)
            {
                LogInvalidReferences("All SARA voice components must be assigned from the configured Voice Rig GameObject.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs one invalid-reference warning to avoid spamming every frame.
        /// </summary>
        private void LogInvalidReferences(string message)
        {
            if (warnedInvalidReferences)
                return;

            Debug.LogWarning(message, this);
            warnedInvalidReferences = true;
        }

        /// <summary>
        /// Assigns Dissonance LocalPlayerName before the rig is activated.
        /// </summary>
        private bool AssignLocalPlayerName()
        {
            string playerName = SaraDissonanceVoiceIds.FromClientId(NSClient.ClientID);

            if (assignedClientId != 0 && assignedClientId != NSClient.ClientID)
            {
                Debug.LogWarning("SARA voice cannot reuse a scene DissonanceComms with a different NetSquare client id. Reload the scene before reconnecting with another client id.", this);
                return false;
            }

            // Dissonance locks LocalPlayerName during Start, so this must happen while voiceRoot is inactive.
            comms.LocalPlayerName = playerName;
            assignedClientId = NSClient.ClientID;
            return true;
        }

        /// <summary>
        /// Applies SARA role and moderation state to Dissonance microphone muting.
        /// </summary>
        private void SyncMuteState()
        {
            if (comms == null && broadcastTrigger == null)
                return;

            // Unknown users stay unmuted so local/offline flows keep microphone behavior predictable.
            bool forceMuted = false;
            SaraUser user = Sara.Network != null ? Sara.Network.LocalUser : null;
            if (user != null)
                forceMuted = user.IsMuted || !user.CanTalk;

            ApplyVoiceSettings(forceMuted);
        }

        /// <summary>
        /// Applies persisted user voice preferences plus any role/moderation mute.
        /// </summary>
        private void ApplyVoiceSettings(bool forceMuted = false)
        {
            SaraUser user = Sara.Network != null ? Sara.Network.LocalUser : null;
            bool useUserMutePreference = user == null || !user.IsObservator;
            SaraVoiceSettings.Apply(comms, broadcastTrigger, forceMuted, activationMode, useUserMutePreference);
        }

        /// <summary>
        /// Deactivates the configured Dissonance rig and clears active world state.
        /// </summary>
        private void DeactivateVoiceRoot()
        {
            if (voiceRoot != null)
                voiceRoot.SetActive(false);

            activeClientId = 0;
            activeWorldId = 0;
            activeSessionCode = string.Empty;
        }

        /// <summary>
        /// Cleans up the generated voice runtime when Unity disables this component.
        /// </summary>
        private void OnDisable()
        {
            DeactivateVoiceRoot();
        }

        /// <summary>
        /// Cleans up the generated voice runtime when Unity destroys this component.
        /// </summary>
        private void OnDestroy()
        {
            DeactivateVoiceRoot();
        }
    }
}
