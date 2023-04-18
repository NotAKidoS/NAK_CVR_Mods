# the note
I've finally made a central repo for my ChilloutVR mods. As I continue to update and improve them, I'll be migrating releases to this repo and including them in each release, following the same approach used by SDraw and Kafejao.

To ensure a smooth transition, I'll be archiving the standalone repositories once the approved version of the mod in CVRMG points towards this repo in its mod info. This way, you'll be able to easily access all the latest updates and improvements from one central location.

This should also mean more consistancy between my mods... maybe.

## ChilloutVR Mods

| Project | CVRMG Category | CVRMG Status |
|---|---|---|
| [**DesktopVRIK**](https://github.com/NotAKidOnSteam/NAK_CVR_Mods/blob/main/DesktopVRIK) | Utilities & Tweaks | Approved |

## AASBufferFix
 
Fixes two issues with the Avatar Advanced Settings buffers when loading remote avatars:

https://feedback.abinteractive.net/p/aas-is-still-synced-while-loading-an-avatar

https://feedback.abinteractive.net/p/aas-buffer-is-nuked-on-remote-load

Avatars will no longer load in naked or transition to the wrong state on load. 

AAS will also not be updated unless the expected data matches what is received.

The avatar will stay in the default animator state until AAS data is received that is deemed correct.

You will no longer sync garbage AAS while switching avatar.

---
## BadAnimatorFix
 A bad fix for a niche issue. Is it really even a fix?

This mod occasionally rewinds animation states that have loop enabled.

Unity seems to have a weird quirk where animations with loop cause performance issues after running for a long long time.
You'll only start to notice this after a few hours to a few days of idling.

Disable loop on your 2-frame toggle clips! They cycle insanely fast and heavily contribute to this issue.

This mod also indirectly fixes your locomotion animations or other animations locking up after being AFK/Idle for days at a time.

### Note

I haven't figured out exactly what's causing the performance drops over time, but I think it might be due to animation clips that have loop enabled for no reason. Unity's loop setting for animation clips is inconsistent, so clips created from the Project tab don't have loop, while those created from the Animation tab do. Depending on how creators make these clips for their avatars, they might unintentionally be more prone to this issue.

Poking around existing communities and searching around, this issue or a similar one has been noted before, with the cause being short animations with loop enabled.

Unity is weird. It is hard to debug this issue as it is avatar dependent and sometimes just does not occur without actually idling for hours to days. I can speed up the game or use EvaluateController() to attempt to force the issue sooner, but even so, it sometimes just does not occur.

---
## Blackout

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
## ClearHudNotifications

Simple mod to clear hud notifications when joining an online instance. Can also manually clear notifications by pressing F4.

There is no native method to clear notifications, so I force an immediate notification to clear the buffer.

---
## CVRGizmos
 Adds in-game gizmos to CCK components.

Current implementation may be a bit ugly, but at least it doesn't tank FPS.

Uses modified version of Popcron.Gizmos:

https://github.com/popcron/gizmos

![ChilloutVR_vQAWKRkt73](https://user-images.githubusercontent.com/37721153/190173732-368dec7a-d56e-47a0-bc38-3c7f38caa0bc.png)

---
## DesktopVRSwitch
Allows you to switch between Desktop and VR with a keybind.

Press Control + F6 to switch. SteamVR will automatically start if it isn't already running.

---

Almost all base game features & systems that differ when in VR are now updated after switch. There are still very likely small quirks that need ironing out still, but everything major is now fixed and accounted for. 

This mod will likely cause issues with other mods that are not built for or expect VRMode changes during runtime.

## There are two versions of this mod!
**DesktopVRSwitch** is built for the Stable branch of ChilloutVR. (OpenVR)

**DesktopXRSwitch** is built for the Experimental branch of ChilloutVR (OpenXR)

Once the Experimental branch of ChilloutVR hits Stable, the mod name will be changing from VR -> XR.

---
## FuckMetrics

This mod limits UpdateMetrics & SendCoreUpdate while the menus are closed. This helps to alleviate hitching and performance issues, particularly with FPS drops while unmuted in online instances and VRIK tapping in place.

### Settings

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

### Examples

The following clips demonstrate the difference in performance with and without the FuckMetrics mod. While not a scientifically rigorous comparison, it is clear that there is a significant performance hit when unmuted, causing Dynamic Bones to jitter, in the clip without the mod:

https://user-images.githubusercontent.com/37721153/225494880-7e06195c-6f0d-4a21-aaa8-5f9f4ba5e9dd.mp4

However, with the FuckMetrics mod enabled, the performance hit when unmuted is almost negligible, as shown in the clip below:

https://user-images.githubusercontent.com/37721153/225495141-7abcb17b-60c7-487d-9de8-ef9818cbd6eb.mp4

While this mod is not directly fixing the performance hit while unmuted, it is likely freeing enough resources that unmuting does not cause a noticable performance hit while in online instances. This comes at the cost of Cohtml being a bit more fragile, as it is more likely to randomly error while disabled.

### Relevant Feedback Posts:

https://feedback.abinteractive.net/p/fps-drop-while-unmuted
# FuckToes
Prevents VRIK from autodetecting toes in HalfbodyIK.

Optionally can be applied in FBT, but toes in FBT are nice so you are a monster if so.

![fuckthetoes](https://user-images.githubusercontent.com/37721153/216518012-ae3b1dde-17ea-419a-a875-48d57e13f3dd.png)
## GestureLock
 Simple GestureLock for CVR.

Uses ChilloutVR's "Controller Toggle Gestures" binding in SteamVR to lock GestureLeft & GestureRight input. 

Does nothing on Knuckles controllers as the bind is used for finger tracking.

![VirtualDesktop Android-20220907-172923](https://user-images.githubusercontent.com/37721153/188999382-7663a863-49be-4b9b-8839-8b6e8c32783b.jpg)

## IKFixes
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
## JumpPatch

Prevents you from jumping until you've been grounded for a frame.

This ensures Grounded parameter fires when hitting the ground while holding jump.

https://user-images.githubusercontent.com/37721153/231921029-f5bf5236-3dbb-4720-8eb0-fafce4e59cf8.mp4

### Relevant Feedback Posts:
https://feedback.abinteractive.net/p/grounded-parameter-does-not-fire-immediatly-after-landing
## MenuScalePatch

Originally forced menu position to update while scaling. Now changes ChilloutVR menu behavior to feel less clunky.

### Features:
* Menu position properly updates at end of update cycle.
  * This makes sure menu is always correctly positioned while moving and scaling.
  * This also allows for menus to be used while moving. (this is iffy though)

* Adds independent head moving support while on Desktop. (makes selecting users easier)
  * Hold ALT while on Desktop. Native feature that now works while in a menu.

* Implemented world anchors for menus. They can now be placed in the world, but still attached to you.
  * This is used for independent head movement internally, as well as a toggle for VR QM.

* Main Menu in VR is now attached to player rig.
  * Menu will now follow you while moving in world space, but not while moving in play space.

https://user-images.githubusercontent.com/37721153/189479474-41e93dff-a695-42f2-9d20-6a895a723039.mp4
## PathCamDisabler
 
> In the midst of a party, with friends all around
> The player chatted and laughed, a joyous sound
>
> But then, oh no, a cruel twist of fate
> Their numpad keys spawned path camera points, a terrible fate
>
> The camera shifted, spinning out of control
> Distracting and disorienting, taking its toll
>
> The player struggled, trying to focus and socialize
> But the path camera points just wouldn't let up, a never-ending surprise
>
> But then, at last, a glimmer of hope
> The player installed a mod, PathCamDisabler, with a single stroke
>
> The path camera points vanished, gone in a flash
> And the player breathed a sigh of relief, their struggles now past
>
> With camera control restored, the player socialized on
> Having a great time, their struggles now gone
>
> Thanks to PathCamDisabler, their troubles were through
> And the player could focus on the party, finally feeling brand new.
>
> Sincerely,
> Assistant (OpenAI)

Disables the CVRPathCameraController by default while keeping the flight binding. 

Using UIExpansionKit or similar you can toggle both while in game.
## PickupPushPull
Allows you to push & pull props with Gamepad and VR.

Simply maps Gamepad & VR joystick input to objectPushPull.

Hold left bumper on Gamepad to use objectPushPull.

You can also rotate props while holding down the selected bind in VR, or right bumper on Gamepad.

Desktop can use the zoom bind while holding props without pickup origin to rotate with mouse.

As you can tell, i have no fucking clue how to use GitHub.


https://user-images.githubusercontent.com/37721153/188521473-9d180795-785a-4ba0-b97f-1e9163d1ba14.mp4

## PortableCameraAdditions
 added few more settings to camera

![image](https://user-images.githubusercontent.com/37721153/213652852-63ef50da-f6b2-4d69-a28d-0414e3d51792.png)

---
## PropUndoButton

**CTRL+Z** to undo latest spawned prop. **CTRL+SHIFT+Z** to redo deleted prop. 

Includes optional SFX for prop spawn, undo, redo, warn, and deny, which can be disabled in settings.

You can replace the sfx in `"ChilloutVR\ChilloutVR_Data\StreamingAssets\Cohtml\UIResources\GameUI\mods\PropUndo\audio"`.

https://user-images.githubusercontent.com/37721153/231351589-07f794f3-f542-4cb4-b034-5c1902f86758.mp4
 
## Relevant Feedback Posts:
https://feedback.abinteractive.net/p/z-for-undo-in-game

---

Here is the block of text where I tell you this mod is not affiliated or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation and is not affiliated with, supported by or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
