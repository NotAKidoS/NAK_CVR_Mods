using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using DarkRift;
using HarmonyLib;
using MelonLoader;
using NAK.PropLoadingHexagon.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.PropLoadingHexagon;

public class PropLoadingHexagonMod : MelonMod
{
    internal static Action<GameObject> OnPropPlaceholderCreated;
    
    private static readonly List<LoadingPropMarker> Loading_Hex_List = new();
    private static GameObject loadingHexContainer;
    private static GameObject loadingHexPrefab;
    
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(PropLoadingHexagon));

    private static readonly MelonPreferences_Entry<bool> EntryKeepIndicatorWhenFiltered =
        Category.CreateEntry("keep_indicator_when_filtered", false,
            "Keep Loading Hex When Filtered", description: "Keeps the loading hexagon when the prop is filtered.");
    
    #endregion Melon Preferences

    #region Melon Events

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // create prop placeholder container
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(PropLoadingHexagonMod).GetMethod(nameof(OnPlayerSetupStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch( // spawn prop placeholder
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.SpawnPropFromNetwork),
                BindingFlags.Public | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(PropLoadingHexagonMod).GetMethod(nameof(OnPropSpawnedFromNetwork),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch( // delete mode on prop placeholder
            typeof(ControllerRay).GetMethod(nameof(ControllerRay.DeleteSpawnable),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(PropLoadingHexagonMod).GetMethod(nameof(OnDeleteSpawnableCheck),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // this luckily is called for all props, not just our own, otherwise we'd be fucked
        HarmonyInstance.Patch( // check for if prop failed to load
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeleteMyPropByInstanceIdOverNetwork),
                BindingFlags.Public | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(PropLoadingHexagonMod).GetMethod(nameof(OnDeleteMyPropByInstanceIdOverNetwork),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        LoadAssetBundle();
        
        InitializeIntegration("TheClapper", Integrations.TheClapperIntegration.Init);
    }

    public override void OnUpdate()
    {
        if (Loading_Hex_List.Count <= 0)
            return;

        for (int i = Loading_Hex_List.Count - 1; i >= 0; i--)
        {
            LoadingPropMarker marker = Loading_Hex_List[i];
            if (marker.propData == null // i dont think can happen
                || string.IsNullOrEmpty(marker.propData.InstanceId) // prop data likely recycled 
                || (marker.propData.Wrapper != null && marker.propData.Spawnable != null)) // prop has spawned
            {
                marker.Reset();
                Loading_Hex_List.RemoveAt(i);
                return;
            }
            
            if (marker.IsLoadingCanceled)
            {
                marker.Cancel();
                marker.Reset();
                Loading_Hex_List.RemoveAt(i);
                return;
            }
            
            marker.Update();
        }
    }

    #endregion Melon Events

    #region Integrations

    private void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        LoggerInstance.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
    }

    #endregion Integrations

    #region Asset Bundle Loading

    private const string LoadingHexagonAssets = "loading_hexagon.assets";
    private const string LoadingHexagonPrefab = "Assets/Mods/PropLoadingHexagon/Loading_Hexagon_Root.prefab";

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
        tmp.sharedMaterial.SetFloat(Shader.PropertyToID("_FadeStartDistance"), 0f);
        tmp.sharedMaterial.SetFloat(Shader.PropertyToID("_FadeEndDistance"), 0f);
        
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
        
        // create loadingPropLoadingHexagonPropHex
        LoadingPropMarker loadingHex = new() { downloadTask = downloadTask };
        loadingHex.Initialize(propData);
        OnPropPlaceholderCreated?.Invoke(loadingHex.loadingObject);

        // add to list
        Loading_Hex_List.Add(loadingHex);
    }

    private static void OnDeleteSpawnableCheck(ref ControllerRay __instance)
    {
        if (!__instance._interactDown)
            return; // not interacted, no need to check
        
        if (PlayerSetup.Instance.GetCurrentPropSelectionMode() 
            != PlayerSetup.PropSelectionMode.Delete)
            return; // not in delete mode, no need to check
        
        LoadingHexagonController propLoadingHex = __instance.hitTransform.GetComponentInParent<LoadingHexagonController>();
        if (propLoadingHex != null) propLoadingHex.IsLoadingCanceled = true; // cancel loading
    }

    private static void OnDeleteMyPropByInstanceIdOverNetwork(string instanceId)
    {
        // find prop data
        CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find(match => match.InstanceId == instanceId);
        if (propData == null) return; // shouldn't happen

        // check if shits null to nuke loading hex
        if (propData.Wrapper != null && propData.Spawnable != null)
            return;
        
        LoadingPropMarker marker = Loading_Hex_List.Find(match => match.propData == propData);
        if (marker == null) return;

        if (EntryKeepIndicatorWhenFiltered.Value)
        {
            marker.IsLikelyBlocked = true;
            return;
        }
        
        marker.Cancel();
        marker.Reset();
        Loading_Hex_List.Remove(marker);
    }
    
    #endregion Harmony Patches
    
    #region LoadingPropMarker Class
    
    private class LoadingPropMarker
    {
        public bool IsLikelyBlocked { get; set; }
        
        internal DownloadTask downloadTask;
        internal CVRSyncHelper.PropData propData;
        internal GameObject loadingObject;
        private LoadingHexagonController loadingHexComponent;
        
        // ReSharper disable once ParameterHidesMember
        public void Initialize(CVRSyncHelper.PropData propData)
        {
            this.propData = propData;
            if (loadingObject == null)
            {
                loadingObject = Object.Instantiate(loadingHexPrefab, Vector3.zero, Quaternion.identity, loadingHexContainer.transform);
                loadingHexComponent = loadingObject.GetComponent<LoadingHexagonController>();
                
                float avatarHeight = PlayerSetup.Instance._avatarHeight;
                Transform hexTransform = loadingObject.transform;
                hexTransform.localScale = Vector3.one * avatarHeight / 4f; // scale modifier
                hexTransform.GetChild(0).position = Vector3.up * avatarHeight / 2f; // position modifier
                
                Update(); // set initial position and rotation
            }
        }
        
        public bool IsLoadingCanceled 
            => loadingHexComponent.IsLoadingCanceled;

        public void Update()
        {
            string text;
            if (!IsLikelyBlocked)
            {
                float progress = downloadTask.Progress;
                if (downloadTask == null 
                    || downloadTask.Status == DownloadTask.ExecutionStatus.Complete 
                    || downloadTask.Progress >= 100f)
                    text = "Loading";
                else if (downloadTask.Status == DownloadTask.ExecutionStatus.Failed)
                    text = "Error";
                else
                    text = $"{progress} %";
                
                loadingHexComponent.SetLoadingShape(progress);
            }
            else
            {
                text = "Filtered";
            }
            
            loadingHexComponent.SetLoadingText(text);
            loadingObject.transform.SetPositionAndRotation(
                new Vector3(propData.PositionX, propData.PositionY, propData.PositionZ),
                Quaternion.Euler(propData.RotationX, propData.RotationY, propData.RotationZ));
        }

        public void Cancel()
        {
            CVRDownloadManager.Instance.CancelAttachment(propData.InstanceId);
            if (propData.Spawnable == null)
                propData.Recycle();
            else
                propData.Spawnable.Delete();
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
            IsLikelyBlocked = false;
        }
    }
    
    #endregion LoadingPropMarker Class
}