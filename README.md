<<<<<<< HEAD
# AASBufferFix
 
Fixes two issues with the Avatar Advanced Settings buffers when loading remote avatars:

https://feedback.abinteractive.net/p/aas-is-still-synced-while-loading-an-avatar

https://feedback.abinteractive.net/p/aas-buffer-is-nuked-on-remote-load

Avatars will no longer load in naked or transition to the wrong state on load. 

AAS will also not be updated unless the expected data matches what is received.

The avatar will stay in the default animator state until AAS data is received that is deemed correct.

You will no longer sync garbage AAS while switching avatar.
    
=======
# BadAnimatorFix
 A bad fix for a niche issue. Is it really even a fix?

This mod occasionally rewinds animation states that have loop enabled.

Unity seems to have a weird quirk where animations with loop cause performance issues after running for a long long time.
You'll only start to notice this after a few hours to a few days of idling.

Disable loop on your 2-frame toggle clips! They cycle insanely fast and heavily contribute to this issue.

This mod also indirectly fixes your locomotion animations or other animations locking up after being AFK/Idle for days at a time.

## Note

I haven't figured out exactly what's causing the performance drops over time, but I think it might be due to animation clips that have loop enabled for no reason. Unity's loop setting for animation clips is inconsistent, so clips created from the Project tab don't have loop, while those created from the Animation tab do. Depending on how creators make these clips for their avatars, they might unintentionally be more prone to this issue.

Poking around existing communities and searching around, this issue or a similar one has been noted before, with the cause being short animations with loop enabled.

Unity is weird. It is hard to debug this issue as it is avatar dependent and sometimes just does not occur without actually idling for hours to days. I can speed up the game or use EvaluateController() to attempt to force the issue sooner, but even so, it sometimes just does not occur.

>>>>>>> BadAnimatorFix/main
---

Here is the block of text where I tell you this mod is not affiliated or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation and is not affiliated with, supported by or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
<<<<<<< HEAD

=======
>>>>>>> BadAnimatorFix/main
