using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;

namespace NAK.PropsButBetter;

public class GlobalButton
{
    private static readonly Dictionary<string, GlobalButton> _buttonsByElementId = new();
    
    private readonly CustomElement _element;
    
    public event Action OnPress;

    public bool Disabled
    {
        get => _element.Disabled;
        set => _element.Disabled = value;
    }

    public static void ListenForQM()
    {
        CVR_MenuManager.Instance.cohtmlView.View.BindCall("GlobalButton-Click", (Action<string>)HandleButtonClick);
    }
    
    public GlobalButton(string iconName, string tooltip, int x, int y, int width = 80, int height = 80)
    {
        _element = new CustomElement(
            $$$"""{"c": "whatever-i-want", "s": [{"c": "icon", "a": {"style": "background-image: url('UILib/Images/{{{iconName}}}.png'); background-size: contain; background-repeat: no-repeat; width: 100%; height: 100%;"}}], "x": "GlobalButton-Click", "a": {"id": "CVRUI-QMUI-Custom-[UUID]", "style": "position: absolute; left: {{{x}}}px; top: {{{y}}}px; width: {{{width}}}px; height: {{{height}}}px; margin: 0;", "data-tooltip": "{{{tooltip}}}"}}""",
            ElementType.GlobalElement
        );
        _element.AddAction("GlobalButton-Click", "engine.call(\"GlobalButton-Click\", e.currentTarget.id);");
        _element.OnElementGenerated += OnElementGenerated;
    }
    
    private void OnElementGenerated()
    {
        _buttonsByElementId[_element.ElementID] = this;
    }
    
    private static void HandleButtonClick(string elementId)
    {
        if (_buttonsByElementId.TryGetValue(elementId, out GlobalButton button)) 
            button.OnPress?.Invoke();
    }
    
    public void Delete()
    {
        _buttonsByElementId.Remove(_element.ElementID);
        _element.Delete();
    }
}