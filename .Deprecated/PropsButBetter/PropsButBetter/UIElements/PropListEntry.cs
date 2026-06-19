using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Util;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using ABI_RC.Systems.UI.UILib.UIObjects.Objects;

namespace NAK.PropsButBetter;

public class PropListEntry
{
    private readonly CustomElement _element;
    private readonly CustomEngineOnFunction _updateFunction;
    private readonly CustomEngineOnFunction _setChildIndexFunction;
    private CancellationTokenSource _cts;

    private readonly string _instanceId;
    private readonly string _propId;
    private readonly string _propName;
    private readonly string _spawnerUsername;
    private string _currentImageUrl;
    private int _childIndex;
    private bool _isDestroyed;
    
    // Used to track if this entry is still needed or not by QuickMenuPropList
    internal int LastUpdatedCycle;
    
    public PropListEntry(string instanceId, string contentId, string propName, string spawnerUsername, Category parentCategory)
    {
        _instanceId = instanceId;
        _propId = contentId;
        _propName = propName;
        _spawnerUsername = spawnerUsername;
        _currentImageUrl = string.Empty;
        _isDestroyed = false;
        _cts = new CancellationTokenSource();

        _element = new CustomElement(
            """{"t": "div", "c": "col-6", "a":{"id":"CVRUI-QMUI-Custom-[UUID]"}}""",
            ElementType.InCategoryElement,
            parentCategory: parentCategory
        );

        _updateFunction = new CustomEngineOnFunction(
            "updatePropListEntry",
            """
            var elem = document.getElementById(elementId);
            if(elem) {
                elem.innerHTML = htmlContent;
            }
            """,
            new Parameter("elementId", typeof(string), true, false),
            new Parameter("htmlContent", typeof(string), true, false)
        );

        _setChildIndexFunction = new CustomEngineOnFunction(
            "setPropListEntryIndex",
            """
            var elem = document.getElementById(elementId2);
            if(elem && elem.parentNode) {
                var parent = elem.parentNode;
                var children = Array.from(parent.children);
                if(index < children.length && children[index] !== elem) {
                    parent.insertBefore(elem, children[index]);
                } else if(index >= children.length) {
                    parent.appendChild(elem);
                }
            }
            """,
            new Parameter("elementId2", typeof(string), true, false),
            new Parameter("index", typeof(int), true, false)
        );

        _element.AddEngineOnFunction(_updateFunction);
        _element.AddEngineOnFunction(_setChildIndexFunction);
        _element.OnElementGenerated += UpdateDisplay;
        parentCategory.AddCustomElement(_element);
        
        FetchImageAsync(contentId);
    }

    private async void FetchImageAsync(string contentId)
    {
        try
        {
            var response = await PedestalInfoBatchProcessor.QueuePedestalInfoRequest(PedestalType.Prop, contentId);
            
            if (_cts.IsCancellationRequested) return;

            string imageUrl = ImageCache.QueueProcessImage(response.ImageUrl, fallback: response.ImageUrl);
            SetImage(imageUrl);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }

    public void SetImage(string imageUrl)
    {
        _currentImageUrl = imageUrl;
        UpdateDisplay();
    }

    public void SetIsDestroyed(bool isDestroyed)
    {
        if (_isDestroyed != isDestroyed)
        {
            _isDestroyed = isDestroyed;
            UpdateDisplay();
        }
    }

    public void SetChildIndexIfNeeded(int index)
    {
        if (_childIndex == index) return;
        _childIndex = index;

        if (!RootLogic.Instance.IsOnMainThread())
            RootLogic.Instance.MainThreadQueue.Enqueue(() => _setChildIndexFunction.TriggerEvent(_element.ElementID, index));
        else
            _setChildIndexFunction.TriggerEvent(_element.ElementID, index);
    }

    public void Destroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _element.Delete();
    }

    private void UpdateDisplay()
    {
        const int rowHeight = 160;
        const int imageSize = rowHeight - 32;
        
        string dimStyle = _isDestroyed ? "opacity: 0.4; filter: grayscale(0.6);" : "";
        string tooltipSuffix = _isDestroyed ? " (Despawned)" : "";

        string imageHtml = $@"
            <img src='{UILibHelper.PlaceholderImageCoui}'
                 style='
                     position: absolute;
                     top: 0; left: 0;
                     width: 100%;
                     height: 100%;
                     object-fit: cover;
                     border-radius: 10px;
                     display: block;
                     background-color: rgba(255,255,255,0.08);
                 '
            />
            <img src='{_currentImageUrl}'
                 style='
                     position: absolute;
                     top: 0; left: 0;
                     width: 100%;
                     height: 100%;
                     object-fit: cover;
                     border-radius: 10px;
                     display: block;
                     opacity: 0;
                     transition: opacity 0.3s ease;
                 '
                 onload='this.style.opacity = 1;'
            />";

        string htmlContent = $@"
    <div class='button'
         data-tooltip='{_propName} - Spawned by {_spawnerUsername}{tooltipSuffix}'
         style='
             display: flex;
             align-items: center;
             padding: 16px;
             cursor: pointer;
             width: 100%;
             height: {rowHeight}px;
             box-sizing: border-box;
             {dimStyle}
         '
         onclick='engine.call(""PropListEntry-Selected"", ""{_instanceId}"", ""{_propId}"", {(_isDestroyed ? "true" : "false")});'>

        <div style='
                width: {imageSize}px;
                height: {imageSize}px;
                position: relative;
                margin-right: 24px;
                flex-shrink: 0;
            '>
            <div style='
                    width: 100%;
                    height: 100%;
                    position: relative;
                '>
                {imageHtml}
            </div>
        </div>

        <div style='
                flex: 1;
                min-width: 0;
                text-align: left;
                display: flex;
                flex-direction: column;
                justify-content: center;
            '>

            <div style='
                    font-size: 34px;
                    font-weight: bold;
                    margin-bottom: 10px;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                '>
                {_propName}
            </div>

            <div style='
                    font-size: 26px;
                    color: rgba(255,255,255,0.8);
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                '>
                Spawned By <span style='color: rgba(255,255,255,0.95);'>{_spawnerUsername}</span>
            </div>

        </div>
    </div>";

        if (!RootLogic.Instance.IsOnMainThread())
            RootLogic.Instance.MainThreadQueue.Enqueue(() => _updateFunction.TriggerEvent(_element.ElementID, htmlContent));
        else
            _updateFunction.TriggerEvent(_element.ElementID, htmlContent);
    }

    public static void ListenForQM()
    {
        CVR_MenuManager.Instance.cohtmlView.View.BindCall("PropListEntry-Selected", OnSelect);
    }

    private static void OnSelect(string instanceId, string propId, bool isDestroyed)
    {
        // If the prop is destroyed, open the details page
        if (isDestroyed)
        {
            ViewManager.Instance.GetPropDetails(propId);
            return;
        }
        
        // Otherwise show the live prop info
        var propData = CVRSyncHelper.Props.Find(prop => prop.InstanceId == instanceId);
        QuickMenuPropSelect.ShowInfo(propData);
    }
}