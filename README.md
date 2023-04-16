# AASBufferFix
 
Fixes two issues with the Avatar Advanced Settings buffers when loading remote avatars:

https://feedback.abinteractive.net/p/aas-is-still-synced-while-loading-an-avatar

https://feedback.abinteractive.net/p/aas-buffer-is-nuked-on-remote-load

Avatars will no longer load in naked or transition to the wrong state on load. 

AAS will also not be updated unless the expected data matches what is received.

The avatar will stay in the default animator state until AAS data is received that is deemed correct.

You will no longer sync garbage AAS while switching avatar.
    
# CVRGizmos
 Adds in-game gizmos to CCK components.

Current implementation may be a bit ugly, but at least it doesn't tank FPS.

Uses modified version of Popcron.Gizmos:

https://github.com/popcron/gizmos

![ChilloutVR_vQAWKRkt73](https://user-images.githubusercontent.com/37721153/190173732-368dec7a-d56e-47a0-bc38-3c7f38caa0bc.png)

---

Here is the block of text where I tell you this mod is not affiliated or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation and is not affiliated with, supported by or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.

