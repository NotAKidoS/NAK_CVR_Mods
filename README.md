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
# ClearHudNotifications

Simple mod to clear hud notifications when joining an online instance. Can also manually clear notifications by pressing F4.

There is no native method to clear notifications, so I force an immediate notification to clear the buffer.

# Blackout

    Functionality heavily inspired by VRSleeper on Booth: https://booth.pm/ja/items/2151940

    There are three states of "blackout":

    0 - Awake (no effect)
    1 - Drowsy (partial effect)
    2 - Sleep (full effect)

    After staying still for DrowsyModeTimer (minutes), you enter DrowsyMode.
    This mode dims the screen to your selected dimming strength.
    After continuing to stay still for SleepModeTimer (seconds), you enter SleepMode.
    This mode over renders mostly everything with black.

    Slight movement while in SleepMode will place you in DrowsyMode until SleepModeTimer is reached again.
    Hard movement once entering DrowsyMode will fully wake you and return complete vision.
    
    Auto state changing can be disabled. This allows you to use UIExpansionKit to manually change Blackout states.
    
    Supports DesktopVRSwitch~ if that releases.
    
**Settings**

* Hud Messages - Sends hud notification on state change.
* Lower FPS While Sleep - Caps FPS to 5 while in Sleep State.
* Drowsy Dim Strength - How strong of a dimming effect should drowsy mode have.

//Automatic State Change related stuff
* Automatic State Change - Dim screen when there is no movement for a while.
* Drowsy Threshold - Degrees of movement to return partial vision.
* Awake Threshold - Degrees of movement to return full vision.
* Enter Drowsy Time - How many minutes without movement until enter drowsy mode.
* Enter Sleep Time - How many seconds without movement until enter sleep mode.
    
---

Here is the block of text where I tell you this mod is not affiliated or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation and is not affiliated with, supported by or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.

