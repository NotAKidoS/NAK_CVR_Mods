using UnityEngine;
using UnityEngine.EventSystems;

namespace NAK.SuperAwesomeMod.Interaction;

public class CustomInputModule : StandaloneInputModule
{
    bool meow = false;

    #region Unity Events

    protected override void Start()
    {
        base.Start();
        
        m_InputOverride = gameObject.AddComponent<CustomBaseInput>();
        
        // Disable other event systems in the scene
        DisableOtherEventSystems();
    }

    #endregion
    
    #region Overrides

    public override void Process()
    {
        CursorLockMode currentLockState = Cursor.lockState;

        Cursor.lockState = CursorLockMode.None;
            
        base.Process();

        Cursor.lockState = currentLockState;
    }

    protected override MouseState GetMousePointerEventData(int id)
    {
        MouseState pointerEventData = base.GetMousePointerEventData(id);
        MouseButtonEventData leftEventData = pointerEventData.GetButtonState(PointerEventData.InputButton.Left).eventData;
        RaycastResult pointerRaycast = leftEventData.buttonData.pointerCurrentRaycast;
        
        if (meow) leftEventData.buttonData.pointerCurrentRaycast = new RaycastResult();
        
        return pointerEventData;
    }

    #endregion Overrides

    #region Private Methods
        
    private void DisableOtherEventSystems()
    {
        EventSystem thisEventSystem = GetComponent<EventSystem>();
        EventSystem[] systems = FindObjectsOfType<EventSystem>();
        foreach (EventSystem system in systems)
        {
            if (system.gameObject.name == "UniverseLibCanvas") continue;
            if (system != thisEventSystem)
            {
                system.enabled = false;
            }
        }
    }

    #endregion Private Methods
}