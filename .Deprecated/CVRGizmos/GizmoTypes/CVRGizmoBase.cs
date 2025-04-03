using UnityEngine;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmoBase : MonoBehaviour
{
    public Component[] GetLocalOnly(Component[] input)
    {
        return input.Where(c => c.gameObject.scene.name == "DontDestroyOnLoad").ToArray();
    }

    public virtual void OnEnable()
    {
        CacheGizmos();
        // register the callback when enabling object
        Camera.onPreRender += RenderGizmos;
    }
    public virtual void OnDisable()
    {
        // remove the callback when disabling object
        Camera.onPreRender -= RenderGizmos;
    }

    public virtual void RenderGizmos(Camera cam)
    {
        DrawGizmos();
    }

    public virtual void CacheGizmos()
    {
    }

    public virtual void DrawGizmos()
    {
    }
}