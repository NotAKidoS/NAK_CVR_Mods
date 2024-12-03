using UnityEngine;
using System.Collections;

namespace NAK.ShareBubbles.UI
{
    public class BubbleAnimController : MonoBehaviour
    {
        [Header("Transform References")]
        [SerializeField] private Transform centerPoint;
        [SerializeField] private Transform hubPivot;
        [SerializeField] private Transform base1;
        [SerializeField] private Transform base2;

        [Header("Animation Settings")]
        [SerializeField] private float spawnDuration = 1.2f;
        [SerializeField] private float hubScaleDuration = 0.5f;
        [SerializeField] private float centerHeightOffset = 0.2f;
        [SerializeField] private AnimationCurve spawnBounceCurve;
        [SerializeField] private AnimationCurve hubScaleCurve;
        
        [Header("Movement Settings")]
        [SerializeField] private float positionSmoothTime = 0.1f;
        [SerializeField] private float rotationSmoothTime = 0.15f;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float floatHeight = 0.05f;
        [SerializeField] private float baseRotationSpeed = 15f;
        [SerializeField] private float spawnRotationSpeed = 180f;

        [Header("Inner Rotation Settings")]
        [SerializeField] private float innerRotationSpeed = 0.8f;
        [SerializeField] private float innerRotationRange = 10f;
        [SerializeField] private AnimationCurve innerRotationGradient = AnimationCurve.Linear(0, 0.4f, 1, 1f);

        private SkinnedMeshRenderer base1Renderer;
        private SkinnedMeshRenderer base2Renderer;
        
        private Transform[] base1Inners;
        private Transform[] base2Inners;
        private Vector3 centerTargetPos;
        private Vector3 centerVelocity;
        private float base1Rotation;
        private float base2Rotation;
        private float base1RotationSpeed;
        private float base2RotationSpeed;
        private float targetBase1RotationSpeed;
        private float targetBase2RotationSpeed;
        private float rotationSpeedVelocity1;
        private float rotationSpeedVelocity2;
        private float animationTime;
        private bool isSpawning = true;

        private Quaternion[] base1InnerStartRots;
        private Quaternion[] base2InnerStartRots;

        private void Start()
        {
            if (spawnBounceCurve.length == 0)
            {
                spawnBounceCurve = new AnimationCurve(
                    new Keyframe(0, 0, 0, 2),
                    new Keyframe(0.6f, 1.15f, 0, 0),
                    new Keyframe(0.8f, 0.95f, 0, 0),
                    new Keyframe(1, 1, 0, 0)
                );
            }
            
            if (hubScaleCurve.length == 0)
            {
                hubScaleCurve = new AnimationCurve(
                    new Keyframe(0, 0, 0, 2),
                    new Keyframe(1, 1, 0, 0)
                );
            }

            base1Inners = new Transform[3];
            base2Inners = new Transform[3];
            base1InnerStartRots = new Quaternion[3];
            base2InnerStartRots = new Quaternion[3];

            Transform currentBase1 = base1;
            Transform currentBase2 = base2;
            for (int i = 0; i < 3; i++)
            {
                base1Inners[i] = currentBase1.GetChild(0);
                base2Inners[i] = currentBase2.GetChild(0);
                base1InnerStartRots[i] = base1Inners[i].localRotation;
                base2InnerStartRots[i] = base2Inners[i].localRotation;
                currentBase1 = base1Inners[i];
                currentBase2 = base2Inners[i];
            }

            base1RotationSpeed = spawnRotationSpeed;
            base2RotationSpeed = -spawnRotationSpeed;
            targetBase1RotationSpeed = spawnRotationSpeed;
            targetBase2RotationSpeed = -spawnRotationSpeed;
            
            // hack
            base1Renderer = base1.GetComponentInChildren<SkinnedMeshRenderer>();
            base2Renderer = base2.GetComponentInChildren<SkinnedMeshRenderer>();

            ResetTransforms();
            StartCoroutine(SpawnAnimation());
        }

        private void ResetTransforms()
        {
            centerPoint.localScale = Vector3.zero;
            centerPoint.localPosition = Vector3.zero;
            hubPivot.localScale = Vector3.zero;
            
            base1.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            base1.localRotation = Quaternion.Euler(0, 180, 0);
            
            base2.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            base2.localRotation = Quaternion.Euler(0, 180, 0);

            centerTargetPos = Vector3.zero;
            base1Rotation = 180f;
            base2Rotation = 180f;
        }

        private IEnumerator SpawnAnimation()
        {
            float elapsed = 0f;

            while (elapsed < spawnDuration)
            {
                float t = elapsed / spawnDuration;
                float bounceT = spawnBounceCurve.Evaluate(t);

                // Center point and hub pivot animation with earlier start
                float centerT = Mathf.Max(0, (t - 0.3f) * 1.43f); // Adjusted timing
                centerPoint.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, centerT);
                centerTargetPos = Vector3.up * (bounceT * centerHeightOffset);

                // Base animations with inverted rotation
                base1.localScale = Vector3.Lerp(new Vector3(0.04f, 0.04f, 0.04f), new Vector3(0.1f, 0.1f, 0.1f), bounceT);
                base2.localScale = Vector3.Lerp(new Vector3(0.02f, 0.02f, 0.02f), new Vector3(0.06f, 0.06f, 0.06f), bounceT);
                
                base1Rotation += base1RotationSpeed * Time.deltaTime;
                base2Rotation += base2RotationSpeed * Time.deltaTime;

                elapsed += Time.deltaTime;
                yield return null;
            }

            targetBase1RotationSpeed = baseRotationSpeed;
            targetBase2RotationSpeed = -baseRotationSpeed;
            isSpawning = false;
        }

        public void ShowHubPivot()
        {
            StartCoroutine(ScaleHubPivot());
        }
        
        public void SetLifetimeVisual(float timeLeftNormalized)
        {
            float value = 100f - (timeLeftNormalized * 100f);
            base1Renderer.SetBlendShapeWeight(0, value);
            base2Renderer.SetBlendShapeWeight(0, value);
        }
        
        private IEnumerator ScaleHubPivot()
        {
            float elapsed = 0f;
            
            while (elapsed < hubScaleDuration)
            {
                float t = elapsed / hubScaleDuration;
                float scaleT = hubScaleCurve.Evaluate(t);
                
                hubPivot.localScale = Vector3.one * scaleT;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            hubPivot.localScale = Vector3.one;
        }
        
        private void Update()
        {
            animationTime += Time.deltaTime;

            if (!isSpawning)
            {
                float floatOffset = Mathf.Sin(animationTime * floatSpeed) * floatHeight;
                centerTargetPos = Vector3.up * (centerHeightOffset + floatOffset);
            }
            
            centerPoint.localPosition = Vector3.SmoothDamp(
                centerPoint.localPosition, 
                centerTargetPos, 
                ref centerVelocity, 
                positionSmoothTime
            );

            base1RotationSpeed = Mathf.SmoothDamp(
                base1RotationSpeed,
                targetBase1RotationSpeed,
                ref rotationSpeedVelocity1,
                rotationSmoothTime
            );

            base2RotationSpeed = Mathf.SmoothDamp(
                base2RotationSpeed,
                targetBase2RotationSpeed,
                ref rotationSpeedVelocity2,
                rotationSmoothTime
            );

            base1Rotation += base1RotationSpeed * Time.deltaTime;
            base2Rotation += base2RotationSpeed * Time.deltaTime;
            
            base1.localRotation = Quaternion.Euler(0, base1Rotation, 0);
            base2.localRotation = Quaternion.Euler(0, base2Rotation, 0);

            for (int i = 0; i < 3; i++)
            {
                float phase = (animationTime * innerRotationSpeed) * Mathf.Deg2Rad;
                float gradientMultiplier = innerRotationGradient.Evaluate(i / 2f);
                float rotationAmount = Mathf.Sin(phase) * innerRotationRange * gradientMultiplier;
                
                base1Inners[i].localRotation = base1InnerStartRots[i] * Quaternion.Euler(0, rotationAmount, 0);
                base2Inners[i].localRotation = base2InnerStartRots[i] * Quaternion.Euler(0, -rotationAmount, 0);
            }
        }
    }
}