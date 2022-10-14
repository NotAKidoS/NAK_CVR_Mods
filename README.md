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

> I am not affiliated with ABI in any official capacity, these mods are not endorsed or outright permitted by ABI and are subject to scrutiny.

> Neither I nor these mods are in any way affiliated with Alpha Blend Interactive and/or ChilloutVR. Using these modifications might cause issues with the performance, security or stability of the game. Use at your own risk.

> Any modifications that are not approved can get your ABI account terminated and such this modification is following the "modding guidelines" at the best it could be.
> They reserve the right to punish users using my mod.
> If you are scared of using modifications in your game do not install mods.

> I do not affiliate ABI and the mod is not supported by ABI.

> Me and this modification are in no affiliation with ABI and not supported by ABI.

> This mod is not affiliated with Alpha Blend Interactive. The mod comes with no warranty. Use at your own risk, as I am not responsible for any misuse.

> I'm not affiliated with Alpha Blend Interactive and this mod is not officially supported by the game.

> When releasing mods to the public, it is required to state, that the mod authors and modification are in no affiliation with ABI and not supported by ABI. :trollface:

> i ran out of places to steal disclaimers from
