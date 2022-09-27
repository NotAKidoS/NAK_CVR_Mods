# DesktopVRSwitch
Allows you to switch between Desktop and VR with a keybind.

Press Control + F6 to switch.

While this mod is a nice convienence feature to have access to, not every chillout system or mod is built to support it. Keep in mind there **will be issues**, so don't get mad if you do end up having to restart. **:stare:**

Please take a look at the table below for what may not function as intended after switch. A lot of them can be remedied by rejoining your current instance.

| Function  | Working | Note |
| ------------- | ------------- | ------------- |
| Discord/Steam RPC  | None  | Rich Presence will report the first mode you launched in. (might correct on world change)  |
| CVRPickupObject  | None  | Pickups will use the grip origin set during spawn. The game overrides the default VR origin with [Desktop] origin if available, so I cannot revert to VR origins when switching from Desktop. |
| CVRWorld UpdatePostProcessing | Not Implemented  | GraphicsAO and other specific post processing settings are not currently set on switch. |
| SceneLoaded worldCamera | Not Implemented  | World-set camera settings are not currently set for both Desktop & VR cameras. |
| ControllerRay vrActive  | Not Sure | I haven't had any issues with this, so I haven't bothered looking into it.  |
| CVRWorld CopyRefCamValues | Not Implemented  | This may not cause issues, but farclip is not set on the opposite modes transition effects when you load a world.  |
| CameraFacingObject | Maybe | All CameraFacingObject behaviors have their camera changed to the active main camera on switch. Not sure if this is a good solution, but required for nameplates to face your viewpoint. |
| CoHtmlHud | Yes | CoHtmlHud is parented to the active camera on switch. |
| HudOperations  | Kind Of | HudOperations is set to use the correct gameobjects to show loading info on the bottom right of the hud, but it seems to not be placed correctly. |
| CheckVR | No | Not needed. We initialize SteamVR ourselves and MetaPort only checks it on Start. |
| MetaPort  | Yes | This is the most important as everything checks here for VRMode. Some systems will cache VRMode on start though, which is where issues with switching occur. |
| PlayerSetup | Yes | Also highly important. PlayerSetup caches VRMode from MetaPort to run correct calibration. To prevent errors, a dummy VRIK component is created before running a quick calibration to prevent PlayerSetup from erroring when switching to VR. |
| MovementSystem | Yes | MovementSystem constantly checks MetaPort for VRMode each frame, so while it isn't directly needed I still set the cached VRMode to true. |
| CVRInputManager | Yes..?  | We set CVRInputManager.reload to have input & menus reload next frame, but also set independentHeadToggle and others to false to prevent the game from locking head control. |
| GesturePlaneTest | Testing | GestureReconizer will use the original launch camera for reference, which means you cannot pull the camera out with gestures when switched. It is the only part of the game that makes a cached result from VRMode private, so I have to dick around with Traverse/Harmony. :stare: |

(an old clip from the first few tests, no longer using melonprefs as uiexpansionkit kinda gets nuked on menu reload)

https://user-images.githubusercontent.com/37721153/192128515-5630f47b-63ed-45c5-b0eb-0aac46d23731.mp4

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
