using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.ModNetwork;

namespace NAK.LuaNetVars
{
    public abstract class MNSyncedBehaviour : IDisposable
    {
        // Add static property for clarity
        protected static string LocalUserId => MetaPort.Instance.ownerId;

        protected enum MessageType : byte
        {
            OwnershipRequest,
            OwnershipResponse,
            OwnershipTransfer,
            StateRequest,
            StateUpdate,
            CustomData
        }

        protected enum OwnershipResponse : byte
        {
            Accepted,
            Rejected
        }

        protected readonly string networkId;
        protected string currentOwnerId;
        private readonly bool autoAcceptTransfers;
        private readonly Dictionary<string, Action<bool>> pendingRequests;
        private bool isInitialized;
        private bool disposedValue;
        private bool isSoftOwner = false;
        private Timer stateRequestTimer;
        private const int StateRequestTimeout = 3000; // 3 seconds

        public string CurrentOwnerId => currentOwnerId;
        public bool HasOwnership => currentOwnerId == LocalUserId;

        protected MNSyncedBehaviour(string networkId, string currentOwnerId = "", bool autoAcceptTransfers = false)
        {
            this.networkId = networkId;
            this.currentOwnerId = currentOwnerId;
            this.autoAcceptTransfers = autoAcceptTransfers;
            this.pendingRequests = new Dictionary<string, Action<bool>>();

            ModNetworkManager.Subscribe(networkId, OnMessageReceived);

            if (!HasOwnership)
                RequestInitialState();
            else
                isInitialized = true;
        }

        private void RequestInitialState()
        {
            using ModNetworkMessage msg = new(networkId);
            msg.Write((byte)MessageType.StateRequest);
            msg.Send();
            
            stateRequestTimer = new Timer(StateRequestTimeoutCallback, null, StateRequestTimeout, Timeout.Infinite);
        }

        private void StateRequestTimeoutCallback(object state)
        {
            // If isInitialized is still false, we assume soft ownership
            if (!isInitialized)
            {
                currentOwnerId = LocalUserId;
                isSoftOwner = true;
                isInitialized = true;
                OnOwnershipChanged(currentOwnerId);
            }

            stateRequestTimer.Dispose();
            stateRequestTimer = null;
        }

        public virtual void RequestOwnership(Action<bool> callback = null)
        {
            if (HasOwnership)
            {
                callback?.Invoke(true);
                return;
            }

            using (ModNetworkMessage msg = new(networkId))
            {
                msg.Write((byte)MessageType.OwnershipRequest);
                msg.Send();
            }

            if (callback != null)
            {
                pendingRequests[LocalUserId] = callback;
            }
        }

        protected void SendNetworkedData(Action<ModNetworkMessage> writeData)
        {
            if (!HasOwnership)
            {
                Debug.LogWarning($"[MNSyncedBehaviour] Cannot send data without ownership. NetworkId: {networkId}");
                return;
            }

            using (ModNetworkMessage msg = new(networkId))
            {
                msg.Write((byte)MessageType.CustomData);
                writeData(msg);
                msg.Send();
            }
        }

        protected virtual void OnMessageReceived(ModNetworkMessage message)
        {
            message.Read(out byte type);
            MessageType messageType = (MessageType)type;

            if (!Enum.IsDefined(typeof(MessageType), messageType))
                return;

            switch (messageType)
            {
                case MessageType.OwnershipRequest:
                    if (!HasOwnership) break;
                    HandleOwnershipRequest(message);
                    break;

                case MessageType.OwnershipResponse:
                    if (message.Sender != currentOwnerId) break;
                    HandleOwnershipResponse(message);
                    break;

                case MessageType.OwnershipTransfer:
                    if (message.Sender != currentOwnerId) break;
                    currentOwnerId = message.Sender;
                    OnOwnershipChanged(currentOwnerId);
                    break;

                case MessageType.StateRequest:
                    if (!HasOwnership) break; // this is the only safeguard against ownership hijacking... idk how to prevent it
                    // TODO: only respond to a StateUpdate if expecting one
                    HandleStateRequest(message);
                    break;

                case MessageType.StateUpdate:
                    // Accept state updates from current owner or if we have soft ownership
                    if (message.Sender != currentOwnerId && !isSoftOwner) break;
                    HandleStateUpdate(message);
                    break;

                case MessageType.CustomData:
                    if (message.Sender != currentOwnerId)
                    {
                        // If we have soft ownership and receive data from real owner, accept it
                        if (isSoftOwner && message.Sender != LocalUserId)
                        {
                            currentOwnerId = message.Sender;
                            isSoftOwner = false;
                            OnOwnershipChanged(currentOwnerId);
                        }
                        else
                        {
                            // Ignore data from non-owner
                            break;
                        }
                    }
                    HandleCustomData(message);
                    break;
            }
        }

        protected virtual void HandleOwnershipRequest(ModNetworkMessage message)
        {
            if (!HasOwnership)
                return;

            string requesterId = message.Sender;
            var response = autoAcceptTransfers ? OwnershipResponse.Accepted :
                OnOwnershipRequested(requesterId);

            using (ModNetworkMessage responseMsg = new(networkId))
            {
                responseMsg.Write((byte)MessageType.OwnershipResponse);
                responseMsg.Write((byte)response);
                responseMsg.Send();
            }

            if (response == OwnershipResponse.Accepted)
            {
                TransferOwnership(requesterId);
            }
        }

        protected virtual void HandleOwnershipResponse(ModNetworkMessage message)
        {
            message.Read(out byte responseByte);
            OwnershipResponse response = (OwnershipResponse)responseByte;

            if (pendingRequests.TryGetValue(LocalUserId, out var callback))
            {
                bool accepted = response == OwnershipResponse.Accepted;
                callback(accepted);
                pendingRequests.Remove(LocalUserId);

                // Update ownership locally only if accepted
                if (accepted)
                {
                    currentOwnerId = LocalUserId;
                    OnOwnershipChanged(currentOwnerId);
                }
            }
        }

        protected virtual void HandleStateRequest(ModNetworkMessage message)
        {
            if (!HasOwnership)
                return;

            using ModNetworkMessage response = new(networkId, message.Sender);
            response.Write((byte)MessageType.StateUpdate);
            WriteState(response);
            response.Send();
        }

        protected virtual void HandleStateUpdate(ModNetworkMessage message)
        {
            currentOwnerId = message.Sender;
            isSoftOwner = false;
            ReadState(message);
            isInitialized = true;

            // Dispose of the state request timer if it's still running
            if (stateRequestTimer != null)
            {
                stateRequestTimer.Dispose();
                stateRequestTimer = null;
            }
        }

        protected virtual void HandleCustomData(ModNetworkMessage message)
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"[MNSyncedBehaviour] Received custom data before initialization. NetworkId: {networkId}");
                return;
            }

            if (message.Sender != currentOwnerId)
            {
                // If we have soft ownership and receive data from real owner, accept it
                if (isSoftOwner && message.Sender != LocalUserId)
                {
                    currentOwnerId = message.Sender;
                    isSoftOwner = false;
                    OnOwnershipChanged(currentOwnerId);
                }
                else
                {
                    // Ignore data from non-owner
                    return;
                }
            }

            ReadCustomData(message);
        }

        protected virtual void TransferOwnership(string newOwnerId)
        {
            using (ModNetworkMessage msg = new(networkId))
            {
                msg.Write((byte)MessageType.OwnershipTransfer);
                msg.Write(newOwnerId); // Include the new owner ID in transfer message
                msg.Send();
            }

            currentOwnerId = newOwnerId;
            OnOwnershipChanged(newOwnerId);
        }

        protected virtual OwnershipResponse OnOwnershipRequested(string requesterId)
        {
            return OwnershipResponse.Rejected;
        }

        protected virtual void OnOwnershipChanged(string newOwnerId)
        {
            // Override to handle ownership changes
        }

        protected virtual void WriteState(ModNetworkMessage message) { }
        protected virtual void ReadState(ModNetworkMessage message) { }
        protected virtual void ReadCustomData(ModNetworkMessage message) { }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

            if (disposing)
            {
                ModNetworkManager.Unsubscribe(networkId);
                pendingRequests.Clear();

                if (stateRequestTimer != null)
                {
                    stateRequestTimer.Dispose();
                    stateRequestTimer = null;
                }
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}