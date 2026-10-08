# VRCFTHeadTracking

Applies head rotation and optional position sent by VRCFT (`v2/Head/Yaw|Pitch|Roll|PosX|PosY|PosZ`) to your avatar head when in Desktop Mode via OSC.

ChilloutVR only advertises face tracking parameters to VRCFT over OSCQuery, so this mod adds the six head parameters to the advertised list. VRCFT then sends them whenever a module (for example [VRC_iFacialMocap](https://github.com/Shuisho10/VRC_iFacialMocap) with head tracking) fills `UnifiedTracking.Data.Head`.

- Requires OSC to be enabled.
- Rotation maps -1 to 1 onto -90 to 90 degrees per axis, position maps 1 to 0.5 meters, as [defined by VRCFT](https://github.com/benaclejames/VRCFaceTracking/blob/6432e6a8d85fa7ec5115fc725c6abcb6dbdd4f35/VRCFaceTracking.Core/Params/Data/UnifiedData.cs#L109-L118).
- Position is off by default and can be enabled in melon preferences.

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ChilloutVR.
https://docs.chilloutvr.net/official/legal/tos/#6-modding-our-game

> This mod is an independent creation not affiliated with, supported by, or approved by ChilloutVR.

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by ChilloutVR.
