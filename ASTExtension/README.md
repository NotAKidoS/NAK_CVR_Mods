# ASTExtension

Extension mod for [Avatar Scale Tool](https://github.com/NotAKidoS/AvatarScaleTool):
- VR Gesture to scale
- Persistent height
- Copy height from others

Best used with Avatar Scale Tool, but will attempt to work with found scaling setups.
Requires already having Avatar Scaling on the avatar. This is **not** Universal Scaling.

## Supported Setups

ASTExtension will attempt to work with the following setups:

**Parameter Names:**
- AvatarScale
- Scale
- Scaler
- Scale/Scale
- Height
- LoliModifier

Assuming the parameter is a float, ASTExtension will attempt to use it as the height parameter. Will automatically calibrate to the height range of the found parameter, assuming the scaling animation is in a blend tree / state using motion time & is linear. The scaling animation state **must be active** at time of avatar load.

The max value ASTExtension will drive the parameter to is 100. As the mod is having to guess the max height, it may not be accurate if the max height is not capped at a multiple of 10. 

Examples:
- `AvatarScale` - 0 to 1 (slider)
  - This is the default setup for Avatar Scale Tool and will work perfectly.
- `Scale` - 0 to 100 (input single)
  - This will also work perfectly as the max height is a multiple of 10.
- `Height` - 0 to 2 (input single)
  - This will not work properly. The max value to drive the parameter to is not a multiple of 10, and as such ASTExtension will believe the parameter range is 0 to 1.
- `BurntToast` - 0 to 10 (input single)
  - This will not work properly. The parameter name is not recognized by ASTExtension.

If your setup is theoretically supported but not working, it is likely the scaling animation is not linear. In this case, you will need to fix your animation clip curves / blend tree to be linear, or use Avatar Scale Tool to generate a new scaling animation.

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation not affiliated with, supported by, or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
