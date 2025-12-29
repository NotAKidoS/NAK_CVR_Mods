using UnityEngine;

namespace NAK.OriginShift.Components;

public class OriginShiftParticleSystemReceiver : MonoBehaviour
{
#if !UNITY_EDITOR
    
    // max particles count cause i said so 2
    private static readonly ParticleSystem.Particle[] _tempParticles = new ParticleSystem.Particle[10000];
    
    private ParticleSystem[] _particleSystems;

    #region Unity Events

    private void Start()
    {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (_particleSystems.Length == 0)
            {
                // OriginShiftMod.Logger.Error("OriginShiftParticleSystemReceiver: No ParticleSystems found on GameObject: " + gameObject.name, this);
                enabled = false;
            }
        }

    private void OnEnable()
    {
            OriginShiftManager.OnOriginShifted += OnOriginShifted;
        }

    private void OnDisable()
    {
            OriginShiftManager.OnOriginShifted -= OnOriginShifted;
        }

    #endregion Unity Events

    #region Origin Shift Events

    private void OnOriginShifted(Vector3 offset)
    {
            foreach (ParticleSystem particleSystem in _particleSystems)
                ShiftParticleSystem(particleSystem, offset);
        }

    private static void ShiftParticleSystem(ParticleSystem particleSystem, Vector3 offset)
    {
            int particleCount = particleSystem.GetParticles(_tempParticles);
            for (int i = 0; i < particleCount; i++) _tempParticles[i].position += offset;
            particleSystem.SetParticles(_tempParticles, particleCount);
        }

    #endregion Origin Shift Events
    
#endif
}