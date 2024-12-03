using UnityEngine;

namespace NAK.ShareBubbles.UI
{
    public class HexagonSpinner : MonoBehaviour
    {
        [Tooltip("Base distance from the center point to each child")]
        [SerializeField] private float radius = 1f;
        
        [Tooltip("How much the radius pulses in/out")]
        [SerializeField] private float radiusPulseAmount = 0.2f;
        
        [Tooltip("Speed of the animation")]
        [SerializeField] private float animationSpeed = 3f;
        
        private Transform[] children;
        private Vector3[] hexagonPoints;
        
        private void Start()
        {
            children = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                if (i < transform.childCount)
                {
                    children[i] = transform.GetChild(i);
                }
                else
                {
                    Debug.LogError("HexagonSpinner requires exactly 6 child objects!");
                    enabled = false;
                    return;
                }
            }
            
            // Calculate base hexagon points (XY plane, Z-up)
            hexagonPoints = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                hexagonPoints[i] = new Vector3(
                    Mathf.Sin(angle),
                    Mathf.Cos(angle),
                    0f
                );
            }
        }
        
        private void Update()
        {
            for (int i = 0; i < 6; i++)
            {
                float phaseOffset = (i * 60f * Mathf.Deg2Rad);
                float currentPhase = (Time.time * animationSpeed) + phaseOffset;
                
                // Calculate radius variation
                float currentRadius = radius + (Mathf.Sin(currentPhase) * radiusPulseAmount);
                
                // Position each child
                Vector3 basePosition = hexagonPoints[i] * currentRadius;
                children[i].localPosition = basePosition;
                
                // Calculate scale based on sine wave, but only show points on the "visible" half
                // Remap sine wave from [-1,1] to [0,1] for cleaner scaling
                float scaleMultiplier = Mathf.Sin(currentPhase);
                scaleMultiplier = Mathf.Max(0f, scaleMultiplier); // Only positive values
                children[i].localScale = Vector3.one * scaleMultiplier;
            }
        }
    }
}