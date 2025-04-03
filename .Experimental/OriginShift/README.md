# OriginShift

Experimental mod that allows world origin to be shifted to prevent floating point precision issues.

## Compromises
- Steam Audio data cannot be shifted.
- NavMesh data cannot be shifted.
- Light Probe data cannot be shifted (until [unity 2022](https://docs.unity3d.com/2022.3/Documentation/Manual/LightProbes-Moving.html)).
- Occlusion Culling data cannot be shifted.
  - When using "Forced" mode, occlusion culling is disabled.
- Only 10k trail positions can be shifted per Trail Renderer (artificial limit).
- Only 10k particle positions can be shifted per Particle System (artificial limit).
  - Potentially can fix by changing Particle System to Custom Simulation Space ? (untested)
- World Constraints are not shifted.

## Known Issues
- Mod Network is not yet implemented, so Compatibility Mode is required to play with others.
- Portable Camera drone mode is not yet offset by the world origin shift.
- Chunk threshold past 10 units will break Voice Chat with remote players in some cases (without Compatibility Mode).
  - This is because the voice server dictates who can hear who based on distance from each other and the world origin shift messes with that.
- Teleports past 50k units will not work.
  - BetterBetterCharacterController prevents teleports past 50k units.
- Magica Cloth.

## Mod Incompatibilities
- PlayerRagdollMod will freak out when you ragdoll between chunk boundaries.

## Provided Components
- `OriginShiftController` - World script to configure origin shift.
- `OriginShiftEventReceiver` - Event receiver for OriginShift events.
- `OriginShiftTransformReceiver` - Shifts the transform of the GameObject it is attached to.
- `OriginShiftRigidbodyReceiver` - Shifts the rigidbody of the GameObject it is attached to.
- `OriginShiftTrailRendererReceiver` - Shifts the positions of the Trail Renderer of the GameObject it is attached to.
- `OriginShiftParticleSystemReceiver` - Shifts the positions of the Particle System of the GameObject it is attached to.

The provided receiver components are automatically added to Props, Players, and Object Syncs.

## Provided Shader Globals
- `_OriginShiftChunkOffset` - The current amount of chunks offset from the origin.
- `_OriginShiftChunkThreshold` - The size of a chunk in world units.
- `_OriginShiftChunkPosition` - The chunk offset multiplied by the chunk threshold.

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation not affiliated with, supported by, or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
