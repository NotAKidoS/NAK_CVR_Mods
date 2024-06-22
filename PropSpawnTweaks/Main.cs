using System.Reflection;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using DarkRift;
using HarmonyLib;
using MelonLoader;
using NAK.PropSpawnTweaks.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.PropSpawnTweaks;

public class PropSpawnTweaksMod : MelonMod
{
    private static readonly ObjectPool<LoadingPropHex> Loading_Hex_Pool = new(0, () => new LoadingPropHex());
    private static readonly List<LoadingPropHex> Loading_Hex_List = new();
    private static GameObject loadingHexContainer;
    private static GameObject loadingHexPrefab;

    #region Melon Events

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // create prop placeholder container
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(PropSpawnTweaksMod).GetMethod(nameof(OnPlayerSetupStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch( // make drop prop actually usable
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.DropProp),
                BindingFlags.Public | BindingFlags.Instance),
            new HarmonyMethod(typeof(PropSpawnTweaksMod).GetMethod(nameof(OnDropProp),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch( // spawn prop placeholder
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.SpawnPropFromNetwork),
                BindingFlags.Public | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(PropSpawnTweaksMod).GetMethod(nameof(OnPropSpawnedFromNetwork),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        LoadAssetBundle();
    }

    public override void OnUpdate()
    {
        if (Loading_Hex_List.Count <= 0)
            return;

        for (int i = Loading_Hex_List.Count - 1; i >= 0; i--)
        {
            LoadingPropHex loadingHex = Loading_Hex_List[i];
            if (loadingHex.propData == null || (loadingHex.propData.Wrapper != null
                                                && loadingHex.propData.Spawnable != null))
            {
                loadingHex.Reset();
                Loading_Hex_Pool.Give(loadingHex);
                Loading_Hex_List.RemoveAt(i);
                return;
            }

            loadingHex.Update();
        }
    }

    #endregion Melon Events

    #region Asset Bundle Loading

    private const string LoadingHexagonAssets = "loading_hexagon.assets";
    private const string LoadingHexagonPrefab = "Assets/Mods/PropSpawnTweaks/Loading_Hexagon_Root.prefab";

    private void LoadAssetBundle()
    {
        LoggerInstance.Msg($"Loading required asset bundle...");
        using Stream resourceStream = MelonAssembly.Assembly.GetManifestResourceStream(LoadingHexagonAssets);
        using MemoryStream memoryStream = new();
        if (resourceStream == null) {
            LoggerInstance.Error($"Failed to load {LoadingHexagonAssets}!");
            return;
        }
        
        resourceStream.CopyTo(memoryStream);
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        if (assetBundle == null) {
            LoggerInstance.Error($"Failed to load {LoadingHexagonAssets}! Asset bundle is null!");
            return;
        }
        
        loadingHexPrefab = assetBundle.LoadAsset<GameObject>(LoadingHexagonPrefab);
        if (loadingHexPrefab == null) {
            LoggerInstance.Error($"Failed to load {LoadingHexagonPrefab}! Prefab is null!");
            return;
        }
        
        // modify prefab so nameplate billboard tmp shader is used
        MeshRenderer tmp = loadingHexPrefab.GetComponentInChildren<MeshRenderer>();
        tmp.sharedMaterial.shader = Shader.Find("Alpha Blend Interactive/TextMeshPro/Mobile/Distance Field-BillboardFacing");
        
        LoggerInstance.Msg("Asset bundle successfully loaded!");
    }

    #endregion Asset Bundle Loading
    
    #region Harmony Patches

    private static void OnPlayerSetupStart()
    {
        if (loadingHexContainer != null) return;
        loadingHexContainer = new GameObject("NAK.LoadingHexContainer");
        Object.DontDestroyOnLoad(loadingHexContainer);
    }

    private static bool OnDropProp(string propGuid, ref PlayerSetup __instance)
    {
        Vector3 position = __instance.activeCam.transform.position + __instance.GetPlayerForward() * 1.5f; // 1f -> 1.5f

        if (Physics.Raycast(position,
                __instance.CharacterController.GetGravityDirection(), // align with gravity, not player up
                out RaycastHit raycastHit, 4f, __instance.dropPlacementMask))
        {
            // native method passes false, so DropProp doesn't align with gravity :)
            CVRSyncHelper.SpawnProp(propGuid, raycastHit.point.x, raycastHit.point.y, raycastHit.point.z, true);
            return false;
        }

        // unlike original, we will still spawn prop even if raycast fails, giving the method actual utility :3

        // hack- we want to align with *our* rotation, not affecting gravity
        Vector3 ogGravity = __instance.CharacterController.GetGravityDirection();
        __instance.CharacterController.gravity = -__instance.transform.up; // align with our rotation

        // spawn prop with useTargetLocationGravity false, so it pulls our gravity dir we've modified
        CVRSyncHelper.SpawnProp(propGuid, position.x, position.y, position.z, false);

        __instance.CharacterController.gravity = ogGravity; // restore gravity
        return false;
    }

    private static void OnPropSpawnedFromNetwork(Message message)
    {
        // thank
        // https://feedback.abinteractive.net/p/gameeventsystem-spawnable-onload-is-kinda-useless

        using DarkRiftReader reader = message.GetReader();
        var assetId = reader.ReadString();
        var instanceId = reader.ReadString();
        CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find(match => match.InstanceId == instanceId);
        if (propData == null)
            return; // props blocked by filter or player blocks, or just broken

        if (!CVRDownloadManager.Instance._downloadTasks.TryGetValue(assetId, out DownloadTask downloadTask))
            return; // no download task, no prop placeholder
        
        // create loading hex
        LoadingPropHex loadingHex = Loading_Hex_Pool.Take();
        loadingHex.downloadTask = downloadTask;
        loadingHex.Initialize(propData);

        // add to list
        Loading_Hex_List.Add(loadingHex);
    }

    #endregion Harmony Patches
    
    #region LoadingPropHex Class
    
    private class LoadingPropHex
    {
        public DownloadTask downloadTask;
        public CVRSyncHelper.PropData propData;
        private GameObject loadingObject;
        private PropLoadingHexagon loadingHexComponent;
        
        // ReSharper disable once ParameterHidesMember
        public void Initialize(CVRSyncHelper.PropData propData)
        {
            this.propData = propData;
            if (loadingObject == null)
            {
                loadingObject = Object.Instantiate(loadingHexPrefab, Vector3.zero, Quaternion.identity, loadingHexContainer.transform);
                loadingHexComponent = loadingObject.GetComponent<PropLoadingHexagon>();
                
                Update(); // set initial position

                float avatarHeight = PlayerSetup.Instance._avatarHeight;
                Transform hexTransform = loadingObject.transform;
                hexTransform.localScale = Vector3.one * avatarHeight / 4f; // scale modifier
                hexTransform.GetChild(0).localPosition = Vector3.up * avatarHeight * 2f; // position modifier
            }
        }

        public void Update()
        {
            string text;
            float progress = downloadTask.Progress;
            
            if (downloadTask == null 
                || downloadTask.Status == DownloadTask.ExecutionStatus.Complete 
                || downloadTask.Progress >= 100f)
                text = "LOADING";
            else if (downloadTask.Status == DownloadTask.ExecutionStatus.Failed)
                text = "ERROR";
            else
                text = $"{progress} %";
            
            loadingHexComponent.SetLoadingText(text);
            loadingHexComponent.SetLoadingShape(progress);
            loadingObject.transform.SetPositionAndRotation(
                new Vector3(propData.PositionX, propData.PositionY, propData.PositionZ),
                Quaternion.Euler(propData.RotationX, propData.RotationY, propData.RotationZ));
        }

        public void Reset()
        {
            if (loadingObject != null)
            {
                Object.Destroy(loadingObject);
                loadingObject = null;
            }

            propData = null;
            downloadTask = null;
            loadingObject = null;
            loadingHexComponent = null;
        }
    }
    
    #endregion LoadingPropHex Class
}