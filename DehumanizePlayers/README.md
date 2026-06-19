# DehumanizePlayers

Humanoid animators are needlessly more expensive than generic ones due to scheduling twist resolving for each layer, even when empty or unnecessary.

You can see some profiler data gathered by a VRChat community member here:
https://docs.google.com/document/d/1SpG7O30O0Cb5tQCEgRro8BixO0lRkrlV2o9Cbq-rzJU/edit?tab=t.0

This was my attempt at addressing the issue when it was first linked to me. It works, albeit really jank. Remote avatars do not need to be humanoid as muscle data is streamed in over the network (unless playing an emote).

VRChat has recently addressed this issue as well, but with an engine modification to skip this when IK Pass is disabled on the evaluated layer, which we are unable to do via mod or native. I have no way to test if the two fixes are comparable, but their fix likely applies to all animators, unlike this fix which only applies to remote player avatars.

Surprisingly, Unity lets you nest animators. This mod creates a dummy disabled animator on the parent object of the avatar and directs the netikcontroller to apply muscles through that (which is very similar to how Basis drives muscles). The avatars original animator is then **dehumanized** to avoid the humanoid layer cost.

This setup also would allow CVR to utilize Playable Animation Jobs on existing avatars to drive muscles, which makes netik cost free when the avatar is occlusion culled:
https://bsky.app/profile/nak.koneko.cat/post/3mlgptv7ttu2o

(playable netik was dropped native due to breaking existing animator setups)

Alternatively, this problem can also be addressed by driving the transforms of remote avatars directly, like Zettai's FastNetIK mod:
https://github.com/ZettaiVR/CVR-Mods/tree/main/FastNetIK

The two approaches to addressing this low-hanging perf-issue are likely to be considered and tested after the GS2 update.

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ChilloutVR.
https://docs.chilloutvr.net/official/legal/tos/#6-modding-our-game

> This mod is an independent creation not affiliated with, supported by, or approved by ChilloutVR. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by ChilloutVR.
