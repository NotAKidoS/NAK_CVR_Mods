using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.UI.UILib;

namespace NAK.PropsButBetter;

public class UndoRedoButtons
{
    private const string UndoButtonId = "PropsButBetter-UndoButton";
    private const string RedoButtonId = "PropsButBetter-RedoButton";
    
    public event Action OnUndo;
    public event Action OnRedo;
    
    private static UndoRedoButtons _instance;
    
    public static void ListenForButtons()
    {
        CVR_MenuManager.Instance.cohtmlView.View.BindCall("UndoRedoButtons-Undo", (Action)HandleUndoStatic);
        CVR_MenuManager.Instance.cohtmlView.View.BindCall("UndoRedoButtons-Redo", (Action)HandleRedoStatic);
    }
    
    public UndoRedoButtons()
    {
        _instance = this;
        QuickMenuAPI.OnMenuGenerated += OnMenuGenerate;
        if (CVR_MenuManager.IsReadyStatic) GenerateCohtml();
    }
    
    private void OnMenuGenerate(CVR_MenuManager _)
        => GenerateCohtml();
    
    private void GenerateCohtml()
    {
        string script = $@"
(function() {{
    var root = document.getElementById('CVRUI-QMUI-Root');
    if (!root) return;
    
    // Remove existing buttons if they exist
    var existingUndo = document.getElementById('{UndoButtonId}');
    if (existingUndo) existingUndo.remove();
    var existingRedo = document.getElementById('{RedoButtonId}');
    if (existingRedo) existingRedo.remove();
    
    // Create undo button
    var undoButton = document.createElement('div');
    undoButton.id = '{UndoButtonId}';
    undoButton.className = 'button';
    undoButton.setAttribute('data-tooltip', 'Undo last Prop spawn.');
    undoButton.setAttribute('onclick', ""engine.call('UndoRedoButtons-Undo');"");
    undoButton.style.cssText = 'position: absolute; left: 900px; top: 25px; width: 140px; height: 140px; margin: 0; z-index: 9999; cursor: pointer;';
    
    var undoIcon = document.createElement('div');
    undoIcon.style.cssText = 'background-image: url(""UILib/Images/PropsButBetter/PropsButBetter-undo.png""); background-size: contain; background-repeat: no-repeat; width: 100%; height: 100%; pointer-events: none;';
    undoButton.appendChild(undoIcon);
    
    // Create redo button
    var redoButton = document.createElement('div');
    redoButton.id = '{RedoButtonId}';
    redoButton.className = 'button';
    redoButton.setAttribute('data-tooltip', 'Redo last Prop spawn.');
    redoButton.setAttribute('onclick', ""engine.call('UndoRedoButtons-Redo');"");
    redoButton.style.cssText = 'position: absolute; left: 1060px; top: 25px; width: 140px; height: 140px; margin: 0; z-index: 9999; cursor: pointer;';
    
    var redoIcon = document.createElement('div');
    redoIcon.style.cssText = 'background-image: url(""UILib/Images/PropsButBetter/PropsButBetter-redo.png""); background-size: contain; background-repeat: no-repeat; width: 100%; height: 100%; pointer-events: none;';
    redoButton.appendChild(redoIcon);
    
    // Append to root
    root.appendChild(undoButton);
    root.appendChild(redoButton);
}})();
";
        CVR_MenuManager.Instance.cohtmlView.View._view.ExecuteScript(script);
        SetUndoHidden(true);
        SetRedoHidden(true);
    }
    
    private static void HandleUndoStatic()
    {
        _instance?.OnUndo?.Invoke();
    }
    
    private static void HandleRedoStatic()
    {
        _instance?.OnRedo?.Invoke();
    }
    
    private bool _isUndoDisabled;
    public void SetUndoDisabled(bool disabled)
    {
        if (_isUndoDisabled == disabled) return;
        _isUndoDisabled = disabled;
        if (!CVR_MenuManager.IsReadyStatic) return;
        CVR_MenuManager.Instance.cohtmlView.View.InternalView.TriggerEvent("CVRUI-QMUI-SetDisabled", UndoButtonId, disabled);
    }
    
    private bool _isRedoDisabled;
    public void SetRedoDisabled(bool disabled)
    {
        if (_isRedoDisabled == disabled) return;
        _isRedoDisabled = disabled;
        if (!CVR_MenuManager.IsReadyStatic) return;
        CVR_MenuManager.Instance.cohtmlView.View.InternalView.TriggerEvent("CVRUI-QMUI-SetDisabled", RedoButtonId, disabled);
    }
    
    private bool _isUndoHidden;
    public void SetUndoHidden(bool hidden)
    {
        if (_isUndoHidden == hidden) return;
        _isUndoHidden = hidden;
        if (!CVR_MenuManager.IsReadyStatic) return;
        CVR_MenuManager.Instance.cohtmlView.View.InternalView.TriggerEvent("CVRUI-QMUI-SetHidden", UndoButtonId, hidden);
    }
    
    private bool _isRedoHidden;
    public void SetRedoHidden(bool hidden)
    {
        if (_isRedoHidden == hidden) return;
        _isRedoHidden = hidden;
        if (!CVR_MenuManager.IsReadyStatic) return;
        CVR_MenuManager.Instance.cohtmlView.View.InternalView.TriggerEvent("CVRUI-QMUI-SetHidden", RedoButtonId, hidden);
    }
    
    public void Cleanup()
    {
        QuickMenuAPI.OnMenuGenerated -= OnMenuGenerate;
        if (!CVR_MenuManager.IsReadyStatic) return;
        CVR_MenuManager.Instance.cohtmlView.View.InternalView.TriggerEvent("CVRUI-QMUI-DeleteElement", UndoButtonId);
        CVR_MenuManager.Instance.cohtmlView.View.InternalView.TriggerEvent("CVRUI-QMUI-DeleteElement", RedoButtonId);
    }
}