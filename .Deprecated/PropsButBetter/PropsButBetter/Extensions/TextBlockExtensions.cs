using ABI_RC.Systems.UI.UILib.UIObjects.Components;

namespace NAK.PropsButBetter;

public static class TextBlockExtensions
{
    public static void SetHiddenIfNeeded(this TextBlock textBlock, bool hidden)
    {
        // Don't invoke a view trigger event needlessly
        if (textBlock.Hidden != hidden) textBlock.Hidden = hidden;
    }
}