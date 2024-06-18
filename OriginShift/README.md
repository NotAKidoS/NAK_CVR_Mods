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

## Known Issues
- Player position on a Movement Parent may slightly drift when passing a chunk boundary if being moved by a Force Applicator (unsure how to fix).
- Mod Network is not yet implemented, so Compatibility Mode is required to play with others.
- Portable Camera drone mode is not yet offset by the world origin shift.
- Remote Synced Objects (Spawnables/ObjectSyncs) actively interpolating remote position data will not be offset by the world origin shift.

## Mod Incompatibilities
- PlayerRagdollMod will freak out when you ragdoll between chunk boundaries.

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation not affiliated with, supported by, or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
