using ABI_RC.Systems.ModNetwork;
using UnityEngine;

namespace NAK.LuaNetVars;


// Test implementation
public class TestSyncedBehaviour : MNSyncedBehaviour
{
    private readonly System.Random random = new();
    private int testValue;
    private int incrementValue;

    public TestSyncedBehaviour(string networkId) : base(networkId, autoAcceptTransfers: true)
    {
        Debug.Log($"[TestSyncedBehaviour] Initialized. NetworkId: {networkId}");
    }

    public void SendTestMessage()
    {
        if (!HasOwnership) return;

        SendNetworkedData(msg => {
            testValue = random.Next(1000);
            incrementValue++;
            msg.Write(testValue);
            msg.Write(incrementValue);
        });
    }

    protected override void WriteState(ModNetworkMessage message)
    {
        message.Write(testValue);
        message.Write(incrementValue);
    }

    protected override void ReadState(ModNetworkMessage message)
    {
        message.Read(out testValue);
        message.Read(out incrementValue);
        Debug.Log($"[TestSyncedBehaviour] State synchronized. TestValue: {testValue}, IncrementValue: {incrementValue}");
    }

    protected override void ReadCustomData(ModNetworkMessage message)
    {
        message.Read(out int receivedValue);
        message.Read(out int receivedIncrement);
        testValue = receivedValue;
        incrementValue = receivedIncrement;
        Debug.Log($"[TestSyncedBehaviour] Received custom data: TestValue: {testValue}, IncrementValue: {incrementValue}");
    }

    protected override void OnOwnershipChanged(string newOwnerId)
    {
        Debug.Log($"[TestSyncedBehaviour] Ownership changed to: {newOwnerId}");
    }

    protected override OwnershipResponse OnOwnershipRequested(string requesterId)
    {
        Debug.Log($"[TestSyncedBehaviour] Ownership requested by: {requesterId}");
        return OwnershipResponse.Accepted;
    }
}