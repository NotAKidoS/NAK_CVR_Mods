using ABI_RC.Core.UI;
using UnityEngine;

namespace NAK.FuckOffUICamera;

[RequireComponent(typeof(Camera))]
public class CohtmlRenderForwarder : MonoBehaviour
{
    #region Private Variables
    
    private CohtmlControlledView[] controlledViews;
    
    #endregion Private Variables

    #region Unity Events
    
    private void OnPreRender()
    {
        if (controlledViews == null) return;
        foreach (CohtmlControlledView view in controlledViews)
            if (view) view.OnPreRender();
    }
    
    #endregion Unity Events

    #region Public Methods
    
    public static void Setup(Camera camera, params CohtmlControlledView[] views)
    {
        CohtmlRenderForwarder forwarder = camera.gameObject.AddComponent<CohtmlRenderForwarder>();
        forwarder.controlledViews = views;
    }
    
    #endregion Public Methods
}