using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using cohtml;
using HarmonyLib;
using MelonLoader;
using UnityEngine;


namespace MenuScalePatch;

public class MenuScalePatch : MelonMod
{

    private static MelonPreferences_Category m_categoryMenuScalePatch;
    private static MelonPreferences_Entry<bool> m_entryScaleCollision;
    private static MelonPreferences_Entry<bool> m_entrySkinWidthLimit;

    private static float defaultSkinWidth = 0f;

    public override void OnApplicationStart()
    {
        m_categoryMenuScalePatch = MelonPreferences.CreateCategory(nameof(MenuScalePatch));
        m_entryScaleCollision = m_categoryMenuScalePatch.CreateEntry<bool>("Scale Collision", false, description: "Should we scale player collision alongside avatar?");
        //m_entrySmallHeightLimit = m_categoryMenuScalePatch.CreateEntry<bool>("Small Height Limit", true, description: "Prevents avatar collision height from going below 0.3f. Disabling this can lead to errors if avatar is too small.");
        m_entrySkinWidthLimit = m_categoryMenuScalePatch.CreateEntry<bool>("No Skin Width", false, description: "Enabling this allows your feet to touch the ground properly, but may also make it easier for you to get stuck on collision.");

        m_categoryMenuScalePatch.SaveToFile(false);
        m_entryScaleCollision.OnValueChangedUntyped += UpdateSettings;
        m_entrySkinWidthLimit.OnValueChangedUntyped += UpdateSettings;

        UpdateSettings();
    }

    private static void UpdateSettings()
    {
        HarmonyPatches._controllerScaleCollision = m_entryScaleCollision.Value;

        if (!MovementSystem.Instance) return;

        CharacterController controller = Traverse.Create(MovementSystem.Instance).Field("controller").GetValue() as CharacterController;
        
        if (!m_entryScaleCollision.Value)
        {
            controller.skinWidth = defaultSkinWidth;
            MovementSystem.Instance.UpdateAvatarHeightFactor(1f);
        }
        else
        {
            float _avatarHeight = Traverse.Create(PlayerSetup.Instance).Field("_avatarHeight").GetValue<float>();
            MovementSystem.Instance.UpdateAvatarHeightFactor(_avatarHeight);
        }
    }

    [HarmonyPatch]
    private class HarmonyPatches
    {

        static public bool _controllerScaleCollision = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MovementSystem), "UpdateCollider")]
        private static void SetScale(ref float ____minimumColliderRadius, ref float ____avatarHeightFactor, ref CharacterController ___controller, ref float ___groundDistance, ref CapsuleCollider ___proxyCollider, ref GameObject ___forceObject, ref Transform ___groundCheck, ref Vector3 ____colliderCenter)
        {
            if (!_controllerScaleCollision) return;
            //avatar height = viewpoint height
            //heightfactor = viewpoint height * scale difference

            //unity docs say to not put skinwidth too low, or you chance getting stuck often
            //but removing skinWidth allows your character to completely touch the floor

            //grab the original skinWidth if it wasn't already logged
            if (defaultSkinWidth == 0f) defaultSkinWidth = ___controller.skinWidth;

            float skinWidth = (m_entrySkinWidthLimit.Value ? 0.001f : defaultSkinWidth);
            //to prevent falling anims when smol- take skinWidth into maths
            ___controller.skinWidth = skinWidth;
            ___groundDistance = ___controller.radius + skinWidth;
            ___groundCheck.localPosition = ____colliderCenter + Vector3.up * skinWidth;

            //scale charactercontroller collision (take allow small player collider setting into account)
            ___controller.height = Mathf.Max(____avatarHeightFactor, ____minimumColliderRadius);
            ___controller.radius = Mathf.Max(____avatarHeightFactor / 6f, ____minimumColliderRadius);
            ___controller.center = ____colliderCenter + Vector3.up * (___controller.height * 0.5f);

            //match the proxy and force colliders to the scaled charactercontroller 
            ___proxyCollider.height = ___controller.height;
            ___proxyCollider.radius = ___controller.radius;
            ___proxyCollider.center = ___controller.center;
            ___forceObject.transform.localScale = new Vector3(___controller.radius + 0.1f, ___controller.height, ___controller.radius + 0.1f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVR_MenuManager), "SetScale")]
        private static void SetQMScale(ref CohtmlView ___quickMenu, ref float ____scaleFactor, float avatarHeight)
        {
            if (!MetaPort.Instance.isUsingVr)
            {
                //correct quickmenu - pretty much needsQuickmenuPositionUpdate()
                Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
                ___quickMenu.transform.eulerAngles = new Vector3(rotationPivot.eulerAngles.x, rotationPivot.eulerAngles.y, rotationPivot.eulerAngles.z);
                ___quickMenu.transform.position = rotationPivot.position + rotationPivot.forward * 1f * ____scaleFactor;
            }

            //update avatar height while we are here
            if (!_controllerScaleCollision) return;
            MovementSystem.Instance.UpdateAvatarHeightFactor(avatarHeight);
        }

        //ViewManager.SetScale runs once a second when it should only run when aspect ratio changes- CVR bug
        //assuming its caused by cast from int to float getting the screen size, something floating point bleh
        //attempting to ignore that call if there wasnt actually a change

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewManager), "SetScale")]
        private static void CheckLegit(float avatarHeight, ref float ___cachedAvatarHeight, out bool __state)
        {
            if (___cachedAvatarHeight == avatarHeight)
            {
                __state = false;
                return;
            }
            __state = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewManager), "SetScale")]
        private static void SetMMScale(ref ViewManager __instance, ref bool ___needsMenuPositionUpdate, ref float ___scaleFactor, bool __state)
        {
            if (!__state) return;

            //correct main menu - pretty much UpdateMenuPosition()
            Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
            __instance.gameObject.transform.position = rotationPivot.position + __instance.gameObject.transform.forward * 1f * ___scaleFactor;
            ___needsMenuPositionUpdate = false;
        }
    }
}
