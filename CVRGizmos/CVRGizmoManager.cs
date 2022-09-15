using CVRGizmos.GismoTypes;
using Gizmos = Popcron.Gizmos;
using UnityEngine;

namespace CVRGizmos
{
    public class CVRGizmoManager : MonoBehaviour
    {
        public static CVRGizmoManager Instance;

        public bool g_enabled = false;
        public bool g_localOnly = false;

        public MonoBehaviour[] managed;

        public System.Type[] GizmoTypes = {
            typeof(CVRGizmos_Pointer),
            typeof(CVRGizmos_AdvancedAvatarSettingsTrigger),
            typeof(CVRGizmos_SpawnableTrigger),
            typeof(CVRGizmos_AdvancedAvatarSettingsPointer),
            typeof(CVRGizmos_DistanceLod),
            typeof(CVRGizmos_HapticZone),
            typeof(CVRGizmos_HapticAreaChest),
            typeof(CVRGizmos_ToggleStateTrigger),
            typeof(CVRGizmos_Avatar),
            typeof(CVRGizmos_AvatarPickupMarker),
            typeof(CVRGizmos_DistanceConstrain),
        };

        void Start()
        {
            CVRGizmoManager.Instance = this;
            managed = new MonoBehaviour[GizmoTypes.Count()];
            for (int i = 0; i < GizmoTypes.Count(); i++)
            {
                managed[i] = gameObject.AddComponent(GizmoTypes[i]) as MonoBehaviour;
            }
        }

        public void EnableGizmos(bool able)
        {
            for (int i = 0; i < GizmoTypes.Count(); i++)
            {
                managed[i].enabled = able;
                Gizmos.Enabled = able;
            }
            RefreshGizmos();
        }

        public void RefreshGizmos()
        {
            for (int i = 0; i < GizmoTypes.Count(); i++)
            {
                managed[i].Invoke("CacheGizmos", 0f);
            }
        }
    }
}