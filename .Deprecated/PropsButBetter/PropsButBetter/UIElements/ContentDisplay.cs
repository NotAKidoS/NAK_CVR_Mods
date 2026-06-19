using System.Text;
using ABI_RC.Core;
using ABI_RC.Core.EventSystem;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using ABI_RC.Systems.UI.UILib.UIObjects.Objects;

namespace NAK.PropsButBetter;

public class ContentDisplay
{
    private readonly CustomElement _element;
    private readonly CustomEngineOnFunction _updateFunction;
    
    public ContentDisplay(Category parentCategory)
    {
        _element = new CustomElement(
            """{"t": "div", "c": "col-12", "a":{"id":"CVRUI-QMUI-Custom-[UUID]"}}""",
            ElementType.InCategoryElement,
            parentCategory: parentCategory
        );
        parentCategory.AddCustomElement(_element);
        
        _updateFunction = new CustomEngineOnFunction(
            "updateContentDisplay",
            """
            var elem = document.getElementById(elementId);
            if(elem) {
                elem.innerHTML = htmlContent;
            }
            """,
            new Parameter("elementId", typeof(string), true, false),
            new Parameter("htmlContent", typeof(string), true, false)
        );
        
        _element.AddEngineOnFunction(_updateFunction);
    }
    
    public void SetContent(AssetManagement.UgcMetadata metadata, string imageUrl = "")
    {
        StringBuilder tagsBuilder = new StringBuilder();

        void AddTag(bool condition, string tagName)
        {
            if (condition)
            {
                tagsBuilder.Append(
                    $"<span style=\"background-color: rgba(255,255,255,0.15);" +
                    $"padding: 6px 14px;" +
                    $"border-radius: 8px;" +
                    $"font-size: 22px;" +
                    $"line-height: 1;" +
                    $"white-space: nowrap;" +
                    $"flex-shrink: 0;" +
                    $"margin-right: 8px;\">" +
                    $"{tagName}</span>"
                );
            }
        }

        AddTag(metadata.TagsData.Gore, "Gore");
        AddTag(metadata.TagsData.Horror, "Horror");
        AddTag(metadata.TagsData.Jumpscare, "Jumpscare");
        AddTag(metadata.TagsData.Explicit, "Explicit");
        AddTag(metadata.TagsData.Suggestive, "Suggestive");
        AddTag(metadata.TagsData.Violence, "Violence");
        AddTag(metadata.TagsData.FlashingEffects, "Flashing Effects");
        AddTag(metadata.TagsData.LoudAudio, "Loud Audio");
        AddTag(metadata.TagsData.ScreenEffects, "Screen Effects");
        AddTag(metadata.TagsData.LongRangeAudio, "Long Range Audio");

        if (tagsBuilder.Length == 0)
        {
            tagsBuilder.Append(
                "<span style=\"color: rgba(255,255,255,0.5); font-style: italic; font-size: 22px;\">No Tags</span>"
            );
        }

        string htmlContent = $@"
    <div style='display: flex; align-items: center;'>
        <div class='button'
             data-tooltip='Open details page in Main Menu.'
             style='width: 300px; height: 300px; margin: 0 48px 0 0; cursor: pointer; position: relative;'
             onclick='engine.trigger(""QuickMenuPropSelect-OpenDetails"");'>
            <img src='{UILibHelper.PlaceholderImageCoui}'
                 class='shit' />
            <img src='{imageUrl}'
                 class='shit shit2'
                 onload='this.style.opacity = ""1"";' />
        </div>

        <div style='flex: 1; font-size: 36px; line-height: 1.6;'>
            <div style='margin-bottom: 24px;'>
                <b>Tags:</b>
                <div style='
                    margin-top: 8px;
                    display: flex;
                    flex-wrap: nowrap;
                    overflow: hidden;
                '>
                    {tagsBuilder}
                </div>
            </div>

            <div>
                <b>File Size:</b>
                <span style='font-size: 32px;'>
                    {CVRTools.HumanReadableFilesize(metadata.FileSize)}
                </span>
            </div>
        </div>
    </div>";

        if (!RootLogic.Instance.IsOnMainThread())
            RootLogic.Instance.MainThreadQueue.Enqueue(() => _updateFunction.TriggerEvent(_element.ElementID, htmlContent));
        else
            _updateFunction.TriggerEvent(_element.ElementID, htmlContent);
    }
}