using UnityEngine;
using NAK.CleanPlates.UI;

public class MiniNameplateDemo : MonoBehaviour
{
    [SerializeField] private MiniNameplate nameplate;

    [Header("Demo")]
    [SerializeField] private string username = "Player";
    [SerializeField] private Color primary = Color.cyan;
    [SerializeField] private Color secondary = Color.magenta;

    [SerializeField] private bool animate = true;
    [SerializeField] private float speed = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float blend = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float alpha = 1f;

    private void Awake()
    {
        if (nameplate != null)
            nameplate.Bind(username, primary, secondary);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || nameplate == null)
            return;

        nameplate.Bind(username, primary, secondary);
        nameplate.SetState(alpha, blend);
    }

    private void Update()
    {
        if (nameplate == null)
            return;

        if (animate)
            blend = Mathf.PingPong(Time.time * speed, 1f);

        nameplate.SetState(alpha, blend);
    }

    [ContextMenu("Rebind")]
    private void Rebind()
    {
        if (nameplate != null)
            nameplate.Bind(username, primary, secondary);
    }
}