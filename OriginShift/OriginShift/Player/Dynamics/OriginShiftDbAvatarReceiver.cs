using Unity.Mathematics;
using UnityEngine;
using Zettai;

namespace NAK.OriginShift;

public class OriginShiftDbAvatarReceiver : MonoBehaviour
{
    private DbJobsAvatarManager _avatarManager;
    
    private void OnEnable()
    {
        OriginShiftManager.OnOriginShifted += OnOriginShifted;
        _avatarManager = GetComponent<DbJobsAvatarManager>();
    }

    private void OnDisable()
    {
        OriginShiftManager.OnOriginShifted -= OnOriginShifted;
    }

    private void OnOriginShifted(Vector3 shift)
    {
        // get all particles
        var particles = _avatarManager.particlesArray;
        for (var index = 0; index < particles.Length; index++)
        {
            ParticleStruct particle = particles[index];
            
            float3 position = particle.m_Position;
            position.x += shift.x;
            position.y += shift.y;
            position.z += shift.z;
            particle.m_Position = position;
            
            position = particle.m_PrevPosition;
            position.x += shift.x;
            position.y += shift.y;
            position.z += shift.z;
            particle.m_PrevPosition = position;
        
            particles[index] = particle;
        }
        _avatarManager.particlesArray = particles;
        
        // get all transforminfo
        var transformInfos = _avatarManager.transformInfoArray;
        for (var index = 0; index < transformInfos.Length; index++)
        {
            TransformInfo transformInfo = transformInfos[index];
            
            float3 position = transformInfo.position;
            position.x += shift.x;
            position.y += shift.y;
            position.z += shift.z;
            transformInfo.position = position;
            
            position = transformInfo.prevPosition;
            position.x += shift.x;
            position.y += shift.y;
            position.z += shift.z;
            transformInfo.prevPosition = position;
            
            transformInfos[index] = transformInfo;
        }
        _avatarManager.transformInfoArray = transformInfos;
    }
}