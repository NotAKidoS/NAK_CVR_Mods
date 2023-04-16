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
    
# DesktopVRIK
Adds VRIK to Desktop ChilloutVR avatars. No longer will you be a liveless sliding statue~!

(adds the small feet stepping when looking around on Desktop)

https://user-images.githubusercontent.com/37721153/221870123-fbe4f5e8-8d6e-4a43-aa5e-f2188e6491a9.mp4

## Configuration:
* Configurable body lean weight. Set to 0 to disable.

* Configurable max root heading angle with chest/pelvis weight settings. Set to 0 to disable.

* Options to disable VRIK from using mapped toes &/or find unmapped (non-human) toe bones.

* Autofixes for avatars without fingers & incorrect chest/spine bone mapping (might not play well with netik).

## Relevant Feedback Posts:
https://feedback.abinteractive.net/p/desktop-feet-ik-for-avatars

https://feedback.abinteractive.net/p/pivot-desktop-camera-with-head
# FuckMetrics

This mod limits UpdateMetrics & SendCoreUpdate while the menus are closed. This helps to alleviate hitching and performance issues, particularly with FPS drops while unmuted in online instances and VRIK tapping in place.

## Settings

* Disable CohtmlView On Idle 
  - Disables CohtmlView on the menus when idle. Takes up to 6 seconds after menu exit. This can give a huge performance boost, but is disabled by default as Cohtml can be unpredictable.
* Menu Metrics
  - Menu metrics settings (FPS & Ping). Always, Menu Only, or Disabled. Updates once on menu open if disabled.
* Menu Core Updates
  - Menu core update settings (Gamerule icons & download debug status). Always, Menu Only, or Disabled. Updates once on menu open if disabled.
* Metrics Update Rate
  - Sets the update rate for the menu metrics. CVR default is 0.5f. Recommended to be 1f or higher.
* Core Update Rate
  - Sets the update rate for the menu core updates. CVR default is 0.1f. Recommended to be 2f or higher as it is intensive.

In general, keeping Menu Metrics & Menu Core Updates to Menu Only with a high Update Rate (in seconds), should be enough for smooth gameplay. Only turn on Disable CohtmlView On Idle if you really wanna squeeze performance, as Cohtml can sometimes freak out when disabled.

## Examples

The following clips demonstrate the difference in performance with and without the FuckMetrics mod. While not a scientifically rigorous comparison, it is clear that there is a significant performance hit when unmuted, causing Dynamic Bones to jitter, in the clip without the mod:

https://user-images.githubusercontent.com/37721153/225494880-7e06195c-6f0d-4a21-aaa8-5f9f4ba5e9dd.mp4

However, with the FuckMetrics mod enabled, the performance hit when unmuted is almost negligible, as shown in the clip below:

https://user-images.githubusercontent.com/37721153/225495141-7abcb17b-60c7-487d-9de8-ef9818cbd6eb.mp4

While this mod is not directly fixing the performance hit while unmuted, it is likely freeing enough resources that unmuting does not cause a noticable performance hit while in online instances. This comes at the cost of Cohtml being a bit more fragile, as it is more likely to randomly error while disabled.

## Relevant Feedback Posts:

https://feedback.abinteractive.net/p/fps-drop-while-unmuted
# FuckToes
Prevents VRIK from autodetecting toes in HalfbodyIK.

Optionally can be applied in FBT, but toes in FBT are nice so you are a monster if so.

![fuckthetoes](https://user-images.githubusercontent.com/37721153/216518012-ae3b1dde-17ea-419a-a875-48d57e13f3dd.png)
# GestureLock
 Simple GestureLock for CVR.

Uses ChilloutVR's "Controller Toggle Gestures" binding in SteamVR to lock GestureLeft & GestureRight input. 

Does nothing on Knuckles controllers as the bind is used for finger tracking.

![VirtualDesktop Android-20220907-172923](https://user-images.githubusercontent.com/37721153/188999382-7663a863-49be-4b9b-8839-8b6e8c32783b.jpg)

# IKFixes
A few small fixes to IK.

**FBT Fixes** - 
* Knee tracking.
* Running animations..
* Emotes playing in wrong direction.
* Forced to calibrate if all IK Tracking Settings are disabled.

**Halfbody Fixes** - 
* Locomotion footsteps while on Movement Parents.
* Root Angle Offset while looking around. Fixes feet only pointing in direction of head.

## Relevant Feedback Posts:
https://feedback.abinteractive.net/p/ik-knee-tracking

https://feedback.abinteractive.net/p/2022r170-ex3-knee-ik-weirdness-when-using-knee-trackers

https://feedback.abinteractive.net/p/disabling-all-tracked-points-makes-game-assume-fbt

https://feedback.abinteractive.net/p/about-ik-behaviour

https://feedback.abinteractive.net/p/vrik-addplatformmotion-for-movement-parents

https://feedback.abinteractive.net/p/halfbodyik-feet-will-only-point-in-direction-of-head

---

Here is the block of text where I tell you this mod is not affiliated or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation and is not affiliated with, supported by or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
