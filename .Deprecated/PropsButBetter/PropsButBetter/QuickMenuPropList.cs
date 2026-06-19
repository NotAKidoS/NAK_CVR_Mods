using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using UnityEngine;

namespace NAK.PropsButBetter;

public static class QuickMenuPropList
{
    public const int TotalPropListModes = 3;
    public enum PropListMode
    {
        AllProps,
        MyProps,
        History,
    }
    
    private static PropListMode _currentPropListMode = PropListMode.AllProps;
    
    private static string _propListCategoryName;
    private static string _propsListPopulatedText;
    private static string _propListEmptyText;
    
    private static Page _page;
    private static Category _actionsCategory;
    private static Category _propListCategory;

    private static Button _enterDeleteModeButton;
    private static Button _deleteMyPropsButton;
    private static Button _deleteOthersPropsButton;
    private static Button _cyclePropListModeButton;
    private static TextBlock _propListTextBlock;

    private static UndoRedoButtons _undoRedoButtons;
    private static ScheduledJob _updateJob;
    
    private static DateTime _lastTabChangedTime = DateTime.Now;
    private static string _rootPageElementID;
    private static bool _isOurTabOpened;

    public static void BuildUI()
    {
        QuickMenuAPI.OnTabChange += OnTabChange;
        
        _page = Page.GetOrCreatePage(nameof(PropsButBetter), nameof(QuickMenuPropList), isRootPage: true, "PropsButBetter-rubiks-cube");
        _page.MenuTitle = "Prop List";
        _page.MenuSubtitle = "You can see all Props currently spawned in here!";
        _page.OnPageOpen += OnPageOpened;
        _page.OnPageClosed += OnPageClosed;

        _rootPageElementID = _page.ElementID;
        
        _actionsCategory = _page.AddCategory("Quick Actions", false, false);

        _undoRedoButtons = new UndoRedoButtons();
        _undoRedoButtons.OnUndo += OnUndo;
        _undoRedoButtons.OnRedo += OnRedo;
        
        _enterDeleteModeButton = _actionsCategory.AddButton("Delete Mode", "PropsButBetter-wand", "Enters Prop delete mode.");
        _enterDeleteModeButton.OnPress += OnDeleteMode;
        
        _deleteMyPropsButton = _actionsCategory.AddButton("Delete My Props", "PropsButBetter-remove", "Deletes all Props spawned by you. This will remove them for everyone.");
        _deleteMyPropsButton.OnPress += OnDeleteMyProps;
        
        _deleteOthersPropsButton = _actionsCategory.AddButton("Remove Others Props", "PropsButBetter-remove", "Removes all Props spawned by other players only for you. This does not delete them for everyone.");
        _deleteOthersPropsButton.OnPress += OnRemoveOthersProps;
        
        _cyclePropListModeButton = _actionsCategory.AddButton("Cycle Prop List Mode", "PropsButBetter-rubiks-cube", "Cycles the Prop List display mode.");
        _cyclePropListModeButton.OnPress += OnCyclePropListMode;
        
        _propListCategory = _page.AddCategory("All Props", true, true);
        _propListTextBlock = _propListCategory.AddTextBlock(string.Empty);

        SetPropListMode(ModSettings.HiddenPropListMode.Value);
    }

    private static void OnPageOpened()
    {
        _undoRedoButtons.SetUndoHidden(false);
        _undoRedoButtons.SetRedoHidden(false);
    }

    private static void OnPageClosed()
    {
        _undoRedoButtons.SetUndoHidden(true);
        _undoRedoButtons.SetRedoHidden(true);
    }
    
    private static void OnPageUpdate()
    {
        // Don't run while the menu is closed, it's needless
        if (!CVR_MenuManager.Instance.IsViewShown)
            return;

        ForceUndoRedoButtonUpdate();
        ForcePropListUpdate();
    }

    private static void ForceUndoRedoButtonUpdate()
    {
        _undoRedoButtons.SetUndoDisabled(!PropHelper.CanUndo());
        _undoRedoButtons.SetRedoDisabled(!PropHelper.CanRedo());
    }
    
    private static void ForcePropListUpdate()
    {
        if (_currentPropListMode == PropListMode.History)
            UpdateHistoryPropList();
        else
            UpdateLivePropList();
    }
    
    private static readonly Dictionary<string, PropListEntry> _entries = new();
    private static readonly Stack<string> _removalBuffer = new();
    
    private static int[] _entrySiblingIndices;
    private static int _currentLiveUpdateCycle;
    private static int _lastPropCount;
    
    private static void UpdateLivePropList()
    {
        List<CVRSyncHelper.PropData> props;
        switch (_currentPropListMode)
        {
            case PropListMode.AllProps: props = CVRSyncHelper.Props; break;
            case PropListMode.MyProps: props = PropHelper.MyProps; break;
            case PropListMode.History: 
            default: return;
        }
        
        int propCount = props.Count;
        
        bool hasPropCountChanged = propCount != _lastPropCount;
        bool needsRunRemovalPass = _lastPropCount > propCount;
        bool noProps = propCount == 0;
        
        if (hasPropCountChanged)
        {
            _lastPropCount = propCount;
            _propListCategory.CategoryName = $"{_propListCategoryName} ({propCount})";
            
            if (noProps)
            {
                ClearPropList();
                _propListTextBlock.Text = _propListEmptyText;
            }
            else
            {
                // Resize our arrays
                Array.Resize(ref _entrySiblingIndices, propCount);
                for (int i = 0; i < propCount; i++) _entrySiblingIndices[i] = i; // Reset order for sort
                _propListTextBlock.Text = _propsListPopulatedText;
            }
        }

        if (noProps)
        {
            // No need to continue update
            return;
        }
        
        // Sort props by distance
        if (propCount > 1)
        {
            Vector3 playerPos = PlayerSetup.Instance.activeCam.transform.position;
            DistanceComparerStruct comparer = new(playerPos, props);
            Array.Sort(_entrySiblingIndices, 0, propCount, comparer);
        }
        
        // Increment the live update cycle count
        _currentLiveUpdateCycle += 1;
        
        // Sort or create the menu entries we need
        int index = 1; // Leave the no props text area as the first child
        for (int i = 0; i < propCount; i++)
        {
            var prop = props[_entrySiblingIndices[i]];
            string id = prop.InstanceId;

            if (!_entries.TryGetValue(id, out var entry))
            {
                string username = CVRPlayerManager.Instance.TryGetPlayerName(prop.SpawnedBy);
                entry = new PropListEntry(
                    id,
                    prop.ObjectId,
                    prop.ContentMetadata.AssetName,
                    username,
                    _propListCategory
                );
                _entries[id] = entry;
            }

            // Set last updated cycle and sort the menu entry
            entry.LastUpdatedCycle = _currentLiveUpdateCycle;
            entry.SetChildIndexIfNeeded(index++);
        }

        if (needsRunRemovalPass)
        {
            // Iterate all entries now and remove all which did not get fishy flipped
            foreach ((string instanceId, PropListEntry entry) in _entries)
            {
                if (entry.LastUpdatedCycle != _currentLiveUpdateCycle)
                {
                    _removalBuffer.Push(instanceId);
                    entry.Destroy();
                }
            }
            
            // Remove all which have been scheduled for death
            int toRemoveCount = _removalBuffer.Count;
            for (int i = 0; i < toRemoveCount; i++)
            {
                string toRemove = _removalBuffer.Pop();
                _entries.Remove(toRemove);
            }
        }
    }
    
    private static void UpdateHistoryPropList()
    {
        int historyCount = PropHelper.SpawnedThisSession.Count;
        
        bool hasPropCountChanged = historyCount != _lastPropCount;
        bool noProps = historyCount == 0;
        
        if (hasPropCountChanged)
        {
            _lastPropCount = historyCount;
            _propListCategory.CategoryName = $"{_propListCategoryName} ({historyCount})";
            
            if (noProps)
            {
                ClearPropList();
                _propListTextBlock.Text = _propListEmptyText;
            }
            else
            {
                _propListTextBlock.Text = _propsListPopulatedText;

            }
        }
        
        if (noProps)
        {
            // No need to continue update
            return;
        }
        
        // Increment the live update cycle count
        _currentLiveUpdateCycle += 1;
        
        // Process history entries in order (newest first since list is already ordered that way)
        int index = 1; // Leave the no props text area as the first child
        for (int i = 0; i < historyCount; i++)
        {
            var historyData = PropHelper.SpawnedThisSession[i];
            string id = historyData.InstanceId;

            if (!_entries.TryGetValue(id, out var entry))
            {
                entry = new PropListEntry(
                    historyData.InstanceId,
                    historyData.PropId,
                    historyData.PropName,
                    historyData.SpawnerName,
                    _propListCategory
                );
                _entries[id] = entry;
            }
            
            // Update destroyed state
            entry.SetIsDestroyed(historyData.IsDestroyed);

            // Set last updated cycle and sort the menu entry
            entry.LastUpdatedCycle = _currentLiveUpdateCycle;
            entry.SetChildIndexIfNeeded(index++);
        }

        // Remove entries that are no longer in history
        foreach ((string instanceId, PropListEntry entry) in _entries)
        {
            if (entry.LastUpdatedCycle != _currentLiveUpdateCycle)
            {
                _removalBuffer.Push(instanceId);
                entry.Destroy();
            }
        }
        
        // Remove all which have been scheduled for death
        int toRemoveCount = _removalBuffer.Count;
        for (int i = 0; i < toRemoveCount; i++)
        {
            string toRemove = _removalBuffer.Pop();
            _entries.Remove(toRemove);
        }
    }

    private static void ClearPropList()
    {
        _lastPropCount = -1; // Forces rebuild of prop list
        foreach (PropListEntry entry in _entries.Values) entry.Destroy();
        _entries.Clear();
    }

    private static void RebuildPropList()
    {
        ClearPropList();
        ForcePropListUpdate();
    }
    
    private readonly struct DistanceComparerStruct(Vector3 playerPos, IReadOnlyList<CVRSyncHelper.PropData> props) : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            var pa = props[a];
            var pb = props[b];

            float dx = pa.PositionX - playerPos.x;
            float dy = pa.PositionY - playerPos.y;
            float dz = pa.PositionZ - playerPos.z;
            float da = dx * dx + dy * dy + dz * dz;

            dx = pb.PositionX - playerPos.x;
            dy = pb.PositionY - playerPos.y;
            dz = pb.PositionZ - playerPos.z;
            float db = dx * dx + dy * dy + dz * dz;

            return da.CompareTo(db);
        }
    }
    
    private static void OnTabChange(string newTab, string previousTab)
    {
        bool isOurTabOpened = newTab == _rootPageElementID;
        
        // Check for change
        if (isOurTabOpened != _isOurTabOpened)
        {
            _isOurTabOpened = isOurTabOpened;
            if (_isOurTabOpened)
            {
                if (_updateJob == null) _updateJob = BetterScheduleSystem.AddJob(OnPageUpdate, 0f, 1f);
                OnPageUpdate();
            }
            else
            {
                if (_updateJob != null) BetterScheduleSystem.RemoveJob(_updateJob);
                _updateJob = null;
            }
        }
        
        if (!_isOurTabOpened) return;
        
        TimeSpan timeDifference = DateTime.Now - _lastTabChangedTime;
        if (timeDifference.TotalSeconds <= 0.5)
        {
            OnDeleteMode();
            return;
        }
        _lastTabChangedTime = DateTime.Now;
    }

    private static void OnUndo()
    {
        PropHelper.UndoProp();
        ForceUndoRedoButtonUpdate();
    }

    private static void OnRedo()
    {
        PropHelper.RedoProp();
        ForceUndoRedoButtonUpdate();
    }

    private static void OnDeleteMode()
    {
        PlayerSetup.Instance.ToggleDeleteMode();
    }
    
    private static void OnDeleteMyProps()
    {
        PropHelper.RemoveMyProps();
        
        // Force a page update
        ForcePropListUpdate();
    }

    private static void OnRemoveOthersProps()
    {
        PropHelper.RemoveOthersProps();
        
        // Force a page update
        ForcePropListUpdate();
    }
    
    private static void OnCyclePropListMode() 
        => CyclePropListMode();

    public static void CyclePropListMode()
    {
        int nextMode = (int)_currentPropListMode + 1;
        SetPropListMode((PropListMode)(nextMode < 0 ? TotalPropListModes - 1 : nextMode % TotalPropListModes));
    }
    
    public static void SetPropListMode(PropListMode mode)
    {
        _currentPropListMode = mode;
        ModSettings.HiddenPropListMode.Value = mode;
        
        switch (_currentPropListMode)
        {
            case PropListMode.AllProps:
                _propListCategoryName = "All Props";
                _propsListPopulatedText = "These Props are sorted by distance to you.";
                _propListEmptyText = "No Props are spawned in the World.";
                _cyclePropListModeButton.ButtonIcon = "PropsButBetter-rubiks-cube-eye";
                break;
            case PropListMode.MyProps:
                _propListCategoryName = "My Props";
                _propsListPopulatedText = "These Props are sorted by distance to you.";
                _propListEmptyText = "You have not spawned any Props.";
                _cyclePropListModeButton.ButtonIcon = "PropsButBetter-rubiks-cube-star";
                break;
            case PropListMode.History:
                _propListCategoryName = "Spawned This Session";
                _propsListPopulatedText = "Showing last 30 Props which have been spawned this session.";
                _propListEmptyText = "No Props have been spawned this session.";
                _cyclePropListModeButton.ButtonIcon = "PropsButBetter-rubiks-cube-clock";
                break;
        }
        
        // Force rebuild of the props list
        RebuildPropList();
    }
}