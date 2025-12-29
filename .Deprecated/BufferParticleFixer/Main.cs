using System.Reflection;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Util.AssetFiltering;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.BufferParticleFixer;

public class BufferParticleFixerMod : MelonMod
{
    private static MelonLogger.Instance Logger;
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(BufferParticleFixer));

    private static readonly MelonPreferences_Entry<bool> EntryFixBufferParticles =
        Category.CreateEntry(
            identifier: "fix_buffer_particles",
            true,
            display_name: "Fix Buffer Particles",
            description: "Should the mod attempt to fix buffer particles by modifying their lifetime and sub-emitter settings?");
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        HarmonyInstance.Patch(
            typeof(SharedFilter).GetMethod(nameof(SharedFilter.ProcessParticleComponent),
                BindingFlags.Public | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(BufferParticleFixerMod).GetMethod(nameof(OnProcessParticleComponent),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnProcessParticleComponent(
        string collectionId, 
        Component particleComponent, 
        bool physicsCollision, 
        CompatibilityVersions compatibilityVersion)
    {
        if (particleComponent is not ParticleSystem particleSystem) 
            return;
        
        if (!EntryFixBufferParticles.Value)
            return;
        
        // Logger.Msg($"Processing particle system on collection '{collectionId}'.");
        
        if (!IsLikelyBufferParticle(particleSystem))
            return;
        
        Logger.Msg($"Detected likely buffer particle system '{particleSystem.name}' on collection '{collectionId}'. Applying fix...");
        
        // Set start lifetime to 1000
        // All sub-emitters to "Spawn on Birth"
        // https://x.com/hfcRedddd/status/1696914565919813679
        
        ParticleSystem.MainModule mainModule = particleSystem.main;
        
        mainModule.startLifetime = 1f;
        
        for (int i = 0; i < particleSystem.subEmitters.subEmittersCount; i++)
        {
            ParticleSystem subEmitter = particleSystem.subEmitters.GetSubEmitterSystem(i);
            if (subEmitter) particleSystem.subEmitters.SetSubEmitterType(i, ParticleSystemSubEmitterType.Birth);
        }
    }
    
    // https://x.com/hfcRedddd/status/1696913727415537807
    private static bool IsLikelyBufferParticle(ParticleSystem ps)
    {
        // Check if the sub-emitters are children of the particle system
        Transform psTransform = ps.transform;
        
        bool hasSubEmitterNotChild = false;
        
        ParticleSystem.SubEmittersModule subEmitters = ps.subEmitters;
        int subEmitterCount = subEmitters.subEmittersCount;
        
        for (int i = 0; i < subEmitterCount; i++)
        {
            ParticleSystem subEmitter = subEmitters.GetSubEmitterSystem(i);
            
            // Skip null sub-emitters
            if (!subEmitter)
            {
                Logger.Warning($"Particle system '{ps.name}' has a null sub-emitter at index {i}.");
                continue;
            }
            
            // If any sub-emitter is not a child of the particle system, it's likely a buffer particle.
            // This setup is also what shits into our logs...
            if (!subEmitter.transform.IsChildOf(psTransform))
                hasSubEmitterNotChild = true;
        }

        if (hasSubEmitterNotChild)
        {
            // Buffer particles have very short lifetimes
            if (!(ps.main.startLifetime.constant > 0.05f)) 
                return true;
            
            Logger.Msg($"A potential buffer particle system '{ps.name}' has a start lifetime of {ps.main.startLifetime.constant}, which is longer than expected.");
        }
        
        return false;
    }
}