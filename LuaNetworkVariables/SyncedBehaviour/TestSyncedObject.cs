using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.LuaNetVars;

public class TestSyncedObject : MonoBehaviour
{
    private const string TEST_NETWORK_ID = "test.synced.object.1";
    private TestSyncedBehaviour syncBehaviour;
    private float messageTimer = 0f;
    private const float MESSAGE_INTERVAL = 2f;

    private void Start()
    {
        syncBehaviour = new TestSyncedBehaviour(TEST_NETWORK_ID);
        Debug.Log($"TestSyncedObject started. Local Player ID: {MetaPort.Instance.ownerId}");
    }

    private void Update()
    {
        // Request ownership on Space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Requesting ownership...");
            syncBehaviour.RequestOwnership((success) =>
            {
                Debug.Log($"Ownership request {(success ? "accepted" : "rejected")}");
            });
        }

        // If we have ownership, send custom data periodically
        if (syncBehaviour.HasOwnership)
        {
            messageTimer += Time.deltaTime;
            if (messageTimer >= MESSAGE_INTERVAL)
            {
                messageTimer = 0f;
                syncBehaviour.SendTestMessage();
            }
        }
    }
}