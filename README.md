# MenuScalePatch

Originally forced menu position to update while scaling. Now changes ChilloutVR menu behavior to feel less clunky.

## Features:
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

---

Here is the block of text where I tell you this mod is not affiliated or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation and is not affiliated with, supported by or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.

