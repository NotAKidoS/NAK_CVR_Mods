using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Components;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NAK.SuperAwesomeMod.UExplorer;

public class UEMenuHelper : MenuPositionHelperBase
{
    #region Singleton

    public static void Create()
    {
        if (Instance != null)
            return;

        _universeLibCanvas = GameObject.Find("UniverseLibCanvas");
        if (_universeLibCanvas == null)
        {
            MelonLogger.Error(
                "Failed to create UniverseLibCanvas"); // TODO: mod logger, casue https://github.com/knah/VRCMods/pull/227
            return;
        }

        _explorerRoot = _universeLibCanvas.transform.Find("com.sinai.unityexplorer_Root").gameObject;

        // Fix the canvas so it renders in the UI camera
        // _universeLibCanvas.SetLayerRecursive(CVRLayers.UIInternal);

        Transform menuParent = new GameObject("UEMenuParent").transform;
        menuParent.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        menuParent.localScale = Vector3.one;

        DontDestroyOnLoad(menuParent.gameObject);

        Transform offsetTransform = new GameObject("UEMenuOffset").transform;
        offsetTransform.SetParent(menuParent, false);
        offsetTransform.localScale = Vector3.one;

        Transform contentTransform = new GameObject("UEMenuContent").transform;
        contentTransform.SetParent(offsetTransform, false);
        contentTransform.localScale = Vector3.one;

        Instance = menuParent.AddComponentIfMissing<UEMenuHelper>();
        Instance.menuTransform = contentTransform;
        // Instance._offsetTransform = offsetTransform; // Got in MenuPositionHelperBase.Start
        
        // Apply the component filters done in worlds
        // foreach (Component c in _universeLibCanvas.GetComponentsInChildren<Component>(true))
        //     SetupCollidersOnUnityUi(c);
        
        CVRCanvasWrapper.AddForCanvas(_universeLibCanvas.GetComponent<Canvas>(), true);
        
        Instance.ConfigureUECanvasRenderMode(RenderMode.WorldSpace);
    }

    public static UEMenuHelper Instance { get; private set; }

    private static GameObject _universeLibCanvas;
    private static GameObject _explorerRoot;
    private static RenderMode _currentRenderMode = RenderMode.WorldSpace;

    #endregion Singleton

    #region Overrides

    public override bool IsMenuOpen => _explorerRoot.activeInHierarchy;

    public override float MenuScaleModifier => !MetaPort.Instance.isUsingVr ? 1f : 0.3f;
    public override float MenuDistanceModifier => !MetaPort.Instance.isUsingVr ? 1.2f : 1f;

    #endregion Overrides

    #region Unity Events

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            ToggleUeCanvasRenderMode();
    }

    #endregion Unity Events

    #region Private Methods

    private void ToggleUeCanvasRenderMode()
    {
        ConfigureUECanvasRenderMode(_currentRenderMode == RenderMode.WorldSpace
            ? RenderMode.ScreenSpaceOverlay
            : RenderMode.WorldSpace);
    }

    private void ConfigureUECanvasRenderMode(RenderMode targetMode)
    {
        _currentRenderMode = targetMode;
        var canvases = _universeLibCanvas.GetComponentsInChildren<Canvas>(true);

        if (targetMode == RenderMode.WorldSpace)
        {
            foreach (Canvas canvas in canvases)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = PlayerSetup.Instance.activeCam;
            }

            _universeLibCanvas.transform.SetParent(menuTransform, false);
            _universeLibCanvas.transform.localScale = Vector3.one * 0.0032f;

            // Center the canvas on the menuTransform
            CenterCanvasOnMenuTransform(_universeLibCanvas, menuTransform);
            return;
        }

        foreach (Canvas canvas in canvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }

        _universeLibCanvas.transform.SetParent(null, false);
        _universeLibCanvas.transform.localScale = Vector3.one;
    }

    private void CenterCanvasOnMenuTransform(GameObject canvasRoot, Transform parentTransform)
    {
        // Find all the rectTransforms under the canvas, determine their bounds, and center the canvas local position
        // on the menuTransform
        
        RectTransform canvasTransform = _explorerRoot.transform as RectTransform;
        
        // get the extents of the rectTransform
        Vector3[] corners = new Vector3[4];
        canvasTransform.GetWorldCorners(corners);
        
        // now center by offsettings its localPosition
        Vector3 center = (corners[0] + corners[2]) / 2f;
        Vector3 extents = (corners[2] - corners[0]) / 2f;
        Vector3 offset = center - extents;
        offset.z = 0f; // set z to 0 to avoid depth issues
        canvasTransform.localPosition = offset;
        MelonLogger.Msg($"Centered canvas on menuTransform: {canvasTransform.localPosition}");
    }

    private static bool IsFloatValid(float val)
        => (!float.IsNaN(val) && !float.IsInfinity(val));
    
    private static void SetupCollidersOnUnityUi(Component c)
    {
        GameObject go = c.gameObject;
        
        if (c is Button)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();
            col.isTrigger = true;
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y,
                    0.05f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.05f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.05f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.05f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);
            }
        }

        if (c is Toggle)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();
            col.isTrigger = true;
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(Mathf.Max(rectTransform.sizeDelta.x, rectTransform.rect.width),
                    rectTransform.sizeDelta.y, 0.05f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.05f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.05f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.05f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);

                //Check Child if Size = 0
                if (col.size.x + col.size.y == 0f && go.transform.childCount > 0)
                {
                    var childRectTransform = go.transform.GetChild(0).GetComponent<RectTransform>();
                    if (childRectTransform != null)
                    {
                        newSize = new Vector3(
                            Mathf.Max(childRectTransform.sizeDelta.x, rectTransform.rect.width),
                            childRectTransform.sizeDelta.y, 0.1f);
                        if (!IsFloatValid(newSize.x))
                            newSize.x = 0.05f;
                        if (!IsFloatValid(newSize.y))
                            newSize.y = 0.05f;
                        if (!IsFloatValid(newSize.z))
                            newSize.z = 0.05f;
                        col.size = newSize;

                        col.center = Vector3.zero;
                    }
                }
            }
        }

        if (c is Slider)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y,
                    0.05f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.05f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.05f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.05f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);
            }

            col.isTrigger = true;
        }

        if (c is EventTrigger)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y,
                    0.025f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.05f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.05f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.05f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);
            }

            col.isTrigger = true;
        }

        if (c is InputField || c is TMP_InputField)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();
            col.isTrigger = true;
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y,
                    0.05f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.05f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.05f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.05f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);
            }
        }

        if (c is ScrollRect)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();
            col.isTrigger = true;
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y,
                    0.025f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.025f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.025f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.025f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);
            }
        }

        if (c is Dropdown)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();
            col.isTrigger = true;
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y,
                    0.05f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.05f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.05f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.05f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);
            }
        }
        
        //Canvas
        if (c is Canvas)
        {
            BoxCollider col = go.AddComponentIfMissing<BoxCollider>();

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform)
            {
                Vector3 newSize = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y,
                    0.0125f / rectTransform.lossyScale.z);
                if (!IsFloatValid(newSize.x))
                    newSize.x = 0.0125f;
                if (!IsFloatValid(newSize.y))
                    newSize.y = 0.0125f;
                if (!IsFloatValid(newSize.z))
                    newSize.z = 0.0125f;
                col.size = newSize;

                col.center = new Vector3(col.size.x * (0.5f - rectTransform.pivot.x),
                    col.size.y * (0.5f - rectTransform.pivot.y), 0f);
            }

            col.isTrigger = true;
        }

    }
    
    #endregion Private Methods
}