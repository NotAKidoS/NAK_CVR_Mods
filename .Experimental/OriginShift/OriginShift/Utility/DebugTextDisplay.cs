#if !UNITY_EDITOR
using ABI_RC.Core.Player;
using NAK.OriginShift.Components;
using UnityEngine;

namespace NAK.OriginShift.Utility;

public class DebugTextDisplay : MonoBehaviour
{
    #region Private Variables
    
    private string _debugText = "Initializing...";
    private Color _textColor = Color.white;
    
    private bool _originShiftEventOccurred;
    private const float _blendDuration = 1.0f;
    private float _blendTime;

    #endregion Private Variables
    
    #region Unity Events

    private void OnEnable()
    {
        OriginShiftManager.OnPostOriginShifted += OnPostOriginShifted;
    }

    private void OnDisable()
    {
        OriginShiftManager.OnPostOriginShifted -= OnPostOriginShifted;
    }

    private void OnGUI()
    {
        GUIStyle style = new()
        {
            fontSize = 25,
            normal = { textColor = _textColor },
            alignment = TextAnchor.UpperRight
        };

        float screenWidth = Screen.width;
        var xPosition = screenWidth - 10;
        float yPosition = 10;

        GUI.Label(new Rect(xPosition - 490, yPosition, 500, 150), _debugText, style);
    }

    #endregion Unity Events

    #region Public Methods

    public void UpdateDebugText(string newText)
    {
        _debugText = newText;
    }

    #endregion Public Methods

    private void Update()
    {
        Vector3 currentChunk = OriginShiftManager.Instance.ChunkOffset;
        Vector3 localCoordinates = PlayerSetup.Instance.GetPlayerPosition();
        Vector3 absoluteCoordinates = localCoordinates;

        // absolute coordinates can be reconstructed using current chunk and threshold
        absoluteCoordinates += currentChunk * OriginShiftController.ORIGIN_SHIFT_THRESHOLD;

        // Update the debug text with the current coordinates
        UpdateDebugText($"Local Coordinates:\n{localCoordinates}\n\n" +
                        $"Absolute Coordinates:\n{absoluteCoordinates}\n\n" +
                        $"Current Chunk:\n{currentChunk}");

        // Blend back to white if the origin shift event occurred
        if (_originShiftEventOccurred)
        {
            _blendTime += Time.deltaTime;
            _textColor = Color.Lerp(Color.red, Color.white, _blendTime / _blendDuration);
            if (_blendTime >= _blendDuration)
            {
                _originShiftEventOccurred = false;
                _blendTime = 0.0f;
                _textColor = Color.white;
            }
        }
    }

    #region Origin Shift Events

    private void OnPostOriginShifted(Vector3 _)
    {
        _originShiftEventOccurred = true;
        _textColor = Color.green;
        _blendTime = 0.0f;
    }

    #endregion Origin Shift Events
}
#endif