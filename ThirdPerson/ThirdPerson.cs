using ABI_RC.Systems.GameEventSystem;
using MelonLoader;
using UnityEngine;
using static NAK.ThirdPerson.CameraLogic;

namespace NAK.ThirdPerson;

public class ThirdPerson : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        Patches.Apply(HarmonyInstance);
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(SetupCamera);
    }
    
    public override void OnUpdate()
    {
        // Prevents scrolling while using Effector/BetterInteractDesktop
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            // Prevents scrolling while in Menus or UnityExplorer
            if (!State || Cursor.lockState != CursorLockMode.Locked) 
                return;
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f) ScrollDist(Mathf.Sign(scroll));
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.T)) State = !State;
        if (!State || !Input.GetKeyDown(KeyCode.Y)) return;
        RelocateCam((CameraLocation)(((int)CurrentLocation + (Input.GetKey(KeyCode.LeftShift) ? -1 : 1) + Enum.GetValues(typeof(CameraLocation)).Length) % Enum.GetValues(typeof(CameraLocation)).Length), true);
    }
}