using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Systems.GameEventSystem;
using ABI.CCK.Components;
using UnityEngine;
using Object = UnityEngine.Object;
using ABI_RC.Core;
using Newtonsoft.Json;

namespace NAK.CustomSpawnPoint;

internal static class SpawnPointManager
{
    #region Fields

    private static string currentWorldId = string.Empty;
    private static SpawnPointData? currentSpawnPoint;

    private static string requestedWorldId = string.Empty;
    private static SpawnPointData? requestedSpawnPoint;

    private static Dictionary<string, SpawnPointData> spawnPoints = new();
    private static readonly string jsonFilePath = Path.Combine("UserData", "customspawnpoints.json");
        
    private static GameObject[] customSpawnPointsArray;
    private static GameObject[] originalSpawnPointsArray;

    #endregion Fields

    #region Initialization

    internal static void Init()
    {
            LoadSpawnpoints();
            CVRGameEventSystem.World.OnLoad.AddListener(OnWorldLoaded);
            CVRGameEventSystem.World.OnUnload.AddListener(OnWorldUnloaded);
            MelonLoader.MelonCoroutines.Start(WaitMainMenuUi());
        }

    private static System.Collections.IEnumerator WaitMainMenuUi()
    {
            while (ViewManager.Instance == null)
                yield return null;
            while (ViewManager.Instance.gameMenuView == null)
                yield return null;
            while (ViewManager.Instance.gameMenuView.Listener == null)
                yield return null;

            ViewManager.Instance.OnUiConfirm.AddListener(OnClearSpawnpointConfirm);
            ViewManager.Instance.gameMenuView.Listener.FinishLoad += (_) =>
            {
                ViewManager.Instance.gameMenuView.View._view.ExecuteScript(spawnpointJs);
            };
            ViewManager.Instance.gameMenuView.Listener.ReadyForBindings += () =>
            {
                // listen for setting the spawn point on our custom button
                ViewManager.Instance.gameMenuView.View.BindCall("NAKCallSetSpawnpoint", SetSpawnPoint);
            };
            
            // create our custom spawn point object
            GameObject customSpawnPointObject = new("[CustomSpawnPoint]");
            Object.DontDestroyOnLoad(customSpawnPointObject);

            // add to array so we can easily replace worlds spawn points
            customSpawnPointsArray = new[] { customSpawnPointObject };
        }

    #endregion Initialization
        
    #region Game Events
        
    private static void OnWorldLoaded(string worldId)
    {
            CVRWorld world = CVRWorld.Instance;
            if (world == null) return;
            
            CustomSpawnPointMod.Logger.Msg("World loaded: " + worldId);

            currentWorldId = worldId;
            currentSpawnPoint = spawnPoints.TryGetValue(currentWorldId, out SpawnPointData spawnPoint) ? spawnPoint : null;
            originalSpawnPointsArray ??= world.spawns; // cache the original spawn points array, if null its fine

            if (currentSpawnPoint.HasValue)
            {
                UpdateCustomSpawnPointTransform(currentSpawnPoint.Value);
                world.spawns = customSpawnPointsArray; // set the custom spawn points array
                
                // CVRWorld.Awake already moved player, but OnWorldLoaded is invoked in OnEnable
                RootLogic.Instance.SpawnOnWorldInstance();
            }
        }

    private static void OnWorldUnloaded(string worldId)
    {
            ClearCurrentWorldState();
        }

    internal static void OnRequestWorldDetailsPage(string worldId)
    {
            //CustomSpawnPointMod.Logger.Msg("Requesting world details page for world: " + worldId);
            
            requestedWorldId = worldId;
            requestedSpawnPoint = spawnPoints.TryGetValue(requestedWorldId, out SpawnPointData spawnPoint) ? spawnPoint : null;

            bool hasSpawnpoint = requestedSpawnPoint.HasValue;
            UpdateMenuButtonState(hasSpawnpoint, worldId == currentWorldId && CVRWorld.Instance != null && CVRWorld.Instance.allowFlying);
        }
        
    private static void OnClearSpawnpointConfirm(string id, string value, string data)
    {
            if (id != "nak_clear_spawnpoint") return;
            if (value == "true") ClearSpawnPoint();
        }

    #endregion Game Events

    #region Spawnpoint Management
        
    public static void SetSpawnPoint()
        => SetSpawnPointForWorld(currentWorldId);
        
    public static void ClearSpawnPoint()
        => ClearSpawnPointForWorld(currentWorldId);
        
    private static void SetSpawnPointForWorld(string worldId)
    {
            CustomSpawnPointMod.Logger.Msg("Setting spawn point for world: " + worldId);
            
            Vector3 playerPosition = PlayerSetup.Instance.GetPlayerPosition();
            Quaternion playerRotation = PlayerSetup.Instance.GetPlayerRotation();

            // Update or create the spawn point data
            SpawnPointData spawnPoint = new()
            {
                Position = playerPosition,
                Rotation = playerRotation.eulerAngles
            };

            spawnPoints[worldId] = spawnPoint;

            // update the current world state if applicable
            if (worldId == currentWorldId)
            {
                currentSpawnPoint = spawnPoint;
                UpdateCustomSpawnPointTransform(spawnPoint);
                // update the custom spawn points array
                if (CVRWorld.Instance != null) CVRWorld.Instance.spawns = customSpawnPointsArray;
                ViewManager.Instance.NotifyUser("(Local) Client", "Set custom spawnpoint", 2f);
            }

            SaveSpawnpoints();
            UpdateMenuButtonState(true, worldId == currentWorldId);
        }

    private static void ClearSpawnPointForWorld(string worldId)
    {
            CustomSpawnPointMod.Logger.Msg("Clearing spawn point for world: " + worldId);
            
            if (spawnPoints.ContainsKey(worldId))
                spawnPoints.Remove(worldId);

            if (worldId == currentWorldId)
            {
                currentSpawnPoint = null;
                
                // restore the original spawn points array
                if (CVRWorld.Instance != null) CVRWorld.Instance.spawns = originalSpawnPointsArray;
                ViewManager.Instance.NotifyUser("(Local) Client", "Cleared custom spawnpoint", 2f);
            }

            SaveSpawnpoints();
            UpdateMenuButtonState(false, worldId == currentWorldId);
        }

    private static void UpdateCustomSpawnPointTransform(SpawnPointData spawnPoint)
    {
            customSpawnPointsArray[0].transform.SetPositionAndRotation(spawnPoint.Position, Quaternion.Euler(spawnPoint.Rotation));
        }

    private static void UpdateMenuButtonState(bool hasSpawnpoint, bool isInWorld)
    {
            ViewManager.Instance.gameMenuView.View.TriggerEvent("NAKUpdateSpawnpointStatus", hasSpawnpoint.ToString(), isInWorld.ToString());
        }

    private static void ClearCurrentWorldState()
    {
            currentWorldId = string.Empty;
            currentSpawnPoint = null;
            originalSpawnPointsArray = null;
        }

    #endregion Spawnpoint Management

    #region JSON Management

    private static void LoadSpawnpoints()
    {
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                spawnPoints = JsonConvert.DeserializeObject<Dictionary<string, SpawnPointData>>(json);
            }
            else
            {
                SaveSpawnpoints(); // create the file if it doesn't exist
            }
        }

    private static void SaveSpawnpoints()
    {
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(spawnPoints, Formatting.Indented, 
                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } // death
            ));
        }

    #endregion JSON Management
        
    #region Spawnpoint JS

    private const string spawnpointJs = @"
let hasSpawnpointForThisWorld = false;
let spawnpointButton = null;

// replace the screenshot button with ours
spawnpointButton = document.querySelector('.action-btn.button.data-worldPreload.row2.col2.disabled');
if (spawnpointButton) {
    spawnpointButton.classList.remove('disabled');
    spawnpointButton.innerHTML = '<img src=""gfx/respawn.svg""><span>Spawnpoint</span>';
    spawnpointButton.setAttribute('onclick', 'onClickSpawnpointButton();');
}

function onClickSpawnpointButton(){
    if (spawnpointButton.classList.contains('disabled')) {
        return;
    }

    if (hasSpawnpointForThisWorld) {
        uiConfirmShow(""Custom Spawnpoint"", ""Are you sure you want to clear your spawnpoint?"", ""nak_clear_spawnpoint"", """");
    } else {
        engine.call('NAKCallSetSpawnpoint');
    }
}

engine.on('NAKUpdateSpawnpointStatus', function (hasSpawnpoint, isInWorld) {
    hasSpawnpointForThisWorld = hasSpawnpoint === 'True';
    if (spawnpointButton) {
        spawnpointButton.classList.toggle('disabled', isInWorld !== 'True');
    }
});
";

    #endregion Spawnpoint JS
}

#region Serializable
    
[Serializable]
public struct SpawnPointData
{
    public Vector3 Position;
    public Vector3 Rotation;
}
    
#endregion Serializable