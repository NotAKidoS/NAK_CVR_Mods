using UnityEngine;
using UnityEngine.UI;

namespace ABI_RC.Core.Player.Interaction.RaycastImpl
{
public class CVRRaycastDebugManager : MonoBehaviour
{
    #region Singleton

    private static CVRRaycastDebugManager _instance;
    public static CVRRaycastDebugManager Instance => _instance;

    public static void Initialize(Camera camera)
    {
        if (_instance != null) return;
        
        var go = new GameObject("RaycastDebugManager");
        _instance = go.AddComponent<CVRRaycastDebugManager>();
        DontDestroyOnLoad(go);
        
        _instance.Setup(camera);
    }

    #endregion

    #region Private Fields

    private CVRPlayerRaycasterMouse _raycaster;
    private CVRRaycastResult _lastResult;
    private System.Diagnostics.Stopwatch _stopwatch;
    
    // Performance tracking
    private const int ROLLING_AVERAGE_SAMPLES = 60;  // 1 second at 60fps
    private readonly float[] _timeHistory = new float[ROLLING_AVERAGE_SAMPLES];
    private int _currentSampleIndex;
    private float _lastRaycastTime;
    private float _minRaycastTime = float.MaxValue;
    private float _maxRaycastTime;
    private float _rollingAverageTime;
    private bool _historyFilled;
    
    private const int DEBUG_PANEL_WIDTH = 300;
    private const int DEBUG_PANEL_MARGIN = 10;
    private const float MOUSE_CURSOR_SIZE = 24f;
    private const float CURSOR_OFFSET = MOUSE_CURSOR_SIZE / 2f;

    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _boxStyle;
    
    private static readonly Color32 TIMING_COLOR = new(255, 255, 150, 255);    // Yellow
    private static readonly Color32 COHTML_COLOR = new(150, 255, 150, 255);    // Green
    private static readonly Color32 UI_COLOR = new(150, 150, 255, 255);        // Blue
    private static readonly Color32 UNITY_UI_COLOR = new(255, 200, 150, 255);  // Orange
    private static readonly Color32 INTERACT_COLOR = new(255, 150, 150, 255);  // Red
    private static readonly Color32 WATER_COLOR = new(150, 255, 255, 255);     // Cyan
    private static readonly Color32 TELEPATHIC_COLOR = new(255, 150, 255, 255);// Purple
    private static readonly Color32 SELECTABLE_COLOR = new(200, 150, 255, 255);// Light Purple

    #endregion

    #region Setup

    private void Setup(Camera camera)
    {
        _raycaster = new CVRPlayerRaycasterMouse(transform, camera);
        _raycaster.SetLayerMask(Physics.DefaultRaycastLayers);
        _stopwatch = new System.Diagnostics.Stopwatch();
    }

    #endregion

    #region MonoBehaviour

    private void Update()
    {
        _stopwatch.Restart();
        _lastResult = _raycaster.GetRaycastResults();
        _stopwatch.Stop();
        
        UpdatePerformanceMetrics();
    }

    private void UpdatePerformanceMetrics()
    {
        // Calculate current frame time
        _lastRaycastTime = _stopwatch.ElapsedTicks / (float)System.TimeSpan.TicksPerMillisecond;
        
        // Update min/max
        _minRaycastTime = Mathf.Min(_minRaycastTime, _lastRaycastTime);
        _maxRaycastTime = Mathf.Max(_maxRaycastTime, _lastRaycastTime);
        
        // Update rolling average
        _timeHistory[_currentSampleIndex] = _lastRaycastTime;
        
        // Calculate rolling average based on filled samples
        float sum = 0f;
        int sampleCount = _historyFilled ? ROLLING_AVERAGE_SAMPLES : _currentSampleIndex + 1;
        
        for (int i = 0; i < sampleCount; i++)
            sum += _timeHistory[i];
        
        _rollingAverageTime = sum / sampleCount;
        
        // Update index for next frame
        _currentSampleIndex = (_currentSampleIndex + 1) % ROLLING_AVERAGE_SAMPLES;
        if (_currentSampleIndex == 0)
            _historyFilled = true;
    }

    private void OnGUI()
    {
        InitializeStyles();
        DrawDebugPanel();
    }

    #endregion

    #region Drawing Methods

    private void InitializeStyles()
    {
        if (_labelStyle != null) return;
        
        _labelStyle = new GUIStyle
        {
            normal = { textColor = Color.white },
            fontSize = 12,
            padding = new RectOffset(5, 5, 2, 2),
            margin = new RectOffset(5, 5, 0, 0)
        };
        
        _headerStyle = new GUIStyle
        {
            normal = { textColor = Color.white },
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(5, 5, 5, 5)
        };
        
        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(5, 5, 5, 5)
        };
    }

    private void DrawDebugPanel()
    {
        var rect = new Rect(
            Screen.width - DEBUG_PANEL_WIDTH - DEBUG_PANEL_MARGIN, 
            DEBUG_PANEL_MARGIN,
            DEBUG_PANEL_WIDTH, 
            Screen.height - (DEBUG_PANEL_MARGIN * 2)
        );

        GUI.Box(rect, "");
        GUILayout.BeginArea(rect);
        
        GUI.backgroundColor = Color.black;
        GUILayout.Label("Raycast Debug Info", _headerStyle);
        
        DrawPerformanceSection();
        DrawCohtmlSection();
        DrawUnityUISection();
        DrawSelectableSection();
        DrawInteractionSection();
        DrawWaterSection();
        DrawTelepathicSection();
        DrawWorldHitSection();

        GUILayout.EndArea();
    }

    private void DrawPerformanceSection()
    {
        GUI.backgroundColor = TIMING_COLOR;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("Performance", _headerStyle);
        DrawLabel("Last Raycast", $"{_lastRaycastTime:F3} ms");
        DrawLabel("Average (1s)", $"{_rollingAverageTime:F3} ms");
        DrawLabel("Min", $"{_minRaycastTime:F3} ms");
        DrawLabel("Max", $"{_maxRaycastTime:F3} ms");
        GUILayout.EndVertical();
    }

    private void DrawCohtmlSection()
    {
        if (!_lastResult.hitCohtml) return;
        
        GUI.backgroundColor = COHTML_COLOR;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("COHTML Hit", _headerStyle);
        DrawLabel("View", _lastResult.hitCohtmlView.name);
        DrawLabel("Coords", _lastResult.hitCohtmlCoords.ToString());
        GUILayout.EndVertical();
    }

    private void DrawUnityUISection()
    {
        if (!_lastResult.hitUnityUi || _lastResult.hitCanvasElement == null) return;

        GUI.backgroundColor = UNITY_UI_COLOR;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("Unity UI Hit", _headerStyle);
        
        var canvasElement = _lastResult.hitCanvasElement;
        var gameObject = canvasElement as MonoBehaviour;
        
        DrawLabel("Canvas Element", gameObject != null ? gameObject.name : "Unknown");
        DrawLabel("Element Type", canvasElement.GetType().Name);
        
        if (gameObject != null)
        {
            DrawLabel("GameObject", gameObject.gameObject.name);
            
            if (gameObject.transform.parent != null)
                DrawLabel("Parent", gameObject.transform.parent.name);
        }
            
        GUILayout.EndVertical();
    }
    
    private void DrawSelectableSection()
    {
        if (_lastResult.hitSelectable == null) return;
        
        GUI.backgroundColor = SELECTABLE_COLOR;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("UI Selectable", _headerStyle);
        DrawLabel("Selectable", _lastResult.hitSelectable.name);
        DrawLabel("Selectable Type", _lastResult.hitSelectable.GetType().Name);
        DrawLabel("Is Interactable", _lastResult.hitSelectable.interactable.ToString());
        DrawLabel("Navigation Mode", _lastResult.hitSelectable.navigation.mode.ToString());
        
        if (_lastResult.hitSelectable is Toggle toggle)
            DrawLabel("Toggle State", toggle.isOn.ToString());
        else if (_lastResult.hitSelectable is Slider slider)
            DrawLabel("Slider Value", slider.value.ToString("F2"));
        else if (_lastResult.hitSelectable is Scrollbar scrollbar)
            DrawLabel("Scrollbar Value", scrollbar.value.ToString("F2"));
            
        GUILayout.EndVertical();
    }

    private void DrawInteractionSection()
    {
        if (!_lastResult.hitPickupable && !_lastResult.hitInteractable) return;
        
        GUI.backgroundColor = INTERACT_COLOR;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("Interaction", _headerStyle);
        if (_lastResult.hitPickupable)
            DrawLabel("Pickupable", _lastResult.hitPickupable.name);
        if (_lastResult.hitInteractable)
            DrawLabel("Interactable", _lastResult.hitInteractable.name);
        DrawLabel("Is Proximity", _lastResult.isProximityHit.ToString());
        GUILayout.EndVertical();
    }

    private void DrawWaterSection()
    {
        if (!_lastResult.hitWater || !_lastResult.waterHit.HasValue) return;
        
        GUI.backgroundColor = WATER_COLOR;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("Water Surface", _headerStyle);
        DrawLabel("Hit Point", _lastResult.waterHit.Value.point.ToString("F2"));
        DrawLabel("Surface Normal", _lastResult.waterHit.Value.normal.ToString("F2"));
        GUILayout.EndVertical();
    }

    private void DrawTelepathicSection()
    {
        if (!_lastResult.hasTelepathicGrabCandidate) return;
        
        GUI.backgroundColor = TELEPATHIC_COLOR;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("Telepathic Grab", _headerStyle);
        DrawLabel("Target", _lastResult.telepathicPickupable.name);
        DrawLabel("Grab Point", _lastResult.telepathicGrabPoint.ToString("F2"));
        GUILayout.EndVertical();
    }

    private void DrawWorldHitSection()
    {
        if (_lastResult.hitCohtml || 
            _lastResult.hitPickupable || 
            _lastResult.hitInteractable ||
            _lastResult.hitUnityUi ||
            !_lastResult.hitWorld ||
            _lastResult.hit.collider == null) return;
        
        GUI.backgroundColor = Color.grey;
        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("World Hit", _headerStyle);
        DrawLabel("Object", _lastResult.hit.collider.name);
        DrawLabel("Distance", _lastResult.hit.distance.ToString("F2"));
        DrawLabel("Point", _lastResult.hit.point.ToString("F2"));
        GUILayout.EndVertical();
    }

    private void DrawLabel(string label, string value)
    {
        GUILayout.Label($"{label}: {value}", _labelStyle);
    }

    #endregion
}
}