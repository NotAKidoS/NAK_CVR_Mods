using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem.Base;
using UnityEngine;

namespace NAK.ShareBubbles.UI;

public class ShakeSoundController : MonoBehaviour
{
    [Header("Shake Detection")]
    [SerializeField] private float minimumVelocityForShake = 5f;
    [SerializeField] private float directionChangeThreshold = 0.8f;
    [SerializeField] private float velocitySmoothing = 0.1f;
    [SerializeField] private float minTimeBetweenShakes = 0.15f;
        
    [Header("Sound")]
    [SerializeField] private AudioClip bellSound;
    [SerializeField] [Range(0f, 1f)] private float minVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float maxVolume = 1f;
    [SerializeField] private float velocityToVolumeMultiplier = 0.1f;

    private AudioSource audioSource;
    private Vector3 lastVelocity;
    private Vector3 currentVelocity;
    private Vector3 smoothedVelocity;
    private float shakeTimer;
    
    private Pickupable _pickupable;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialize = true;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 5f;
        
        _pickupable = GetComponent<Pickupable>();
        
        // Add source to prop mixer group so it can be muted
        audioSource.outputAudioMixerGroup = RootLogic.Instance.propSfx;
        
        // TODO: Make jingle velocity scale with player playspace
        // Make jingle velocity only sample local space, so OriginShift doesn't affect it
        // Just make all this not bad...
    }

    private void Update()
    {
        shakeTimer -= Time.deltaTime;

        if (!_pickupable.IsGrabbedByMe)
            return;

        // Calculate raw velocity
        currentVelocity = (transform.position - lastVelocity) / Time.deltaTime;
            
        // Smooth the velocity
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, currentVelocity, velocitySmoothing);
            
        // Only check for direction change if moving fast enough and cooldown has elapsed
        if (smoothedVelocity.magnitude > minimumVelocityForShake && shakeTimer <= 0)
        {
            float directionChange = Vector3.Dot(smoothedVelocity.normalized, lastVelocity.normalized);
                
            if (directionChange < -directionChangeThreshold)
            {
                float shakePower = smoothedVelocity.magnitude * Mathf.Abs(directionChange);
                PlayBellSound(shakePower);
                shakeTimer = minTimeBetweenShakes;
            }
        }

        lastVelocity = transform.position;
    }

    private void PlayBellSound(float intensity)
    {
        if (bellSound == null) return;

        float volume = Mathf.Clamp(intensity * velocityToVolumeMultiplier, minVolume, maxVolume);
        audioSource.volume = volume;
        audioSource.PlayOneShot(bellSound);
    }
}