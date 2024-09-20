# Stickers

Stickers! Allows you to place small images on any surface. Requires both users to have the mod installed. Synced over Mod Network.

### How to use
Any image placed in the `UserData/Stickers/` folder will be available to choose from within the BTKUI tab. Once you’ve selected an image, enter Sticker Mode or use the Desktop Binding to start placing the sticker. Remote clients running the mod will automatically request the image data if they do not have it stored locally.

### Please read the tooltips
- You have 4 "Sticker Slots".
- You double-click or hold a "Sticker Slot" to select an image from the `UserData/Stickers/` folder.

### Limitations
- Only PNG, JPG, & JPEG images are supported.
  - While it would be cool to send gifs, I don't want to abuse Mod Network that much lol.
- Target surface must have a renderer on the same GameObject as the collider.
  - Terrain is not supported.
- Image should be under 256KB in size.
- Image dimensions should be a power of 2 (e.g. 512x512, 1024x1024).
  - If the image exceeds the size limit or is not a power of 2 the mod will automatically resize it.
  - The automatic resizing may result in loss of quality (or may just fail), so it is recommended to resize the image yourself before placing it in the `UserData/Stickers/` folder.
- Only 512 max images per folder (otherwise Cohtml gets very upset).
- Requires the experimental Shader Safety Settings to be disabled as it will cause crashes when decals attempt to generate on GPU.
  - The mod will automatically disable this setting when it is enabled on startup.

### Restrictions
- Full Restriction.
  - To disable Stickers for the whole world, name an empty GameObject "**[DisableStickers]**".
- Partial Restriction.
  - To keep stickers enabled but not allowing it on certain objects, add the "**[NoSticker]**" tag to the GameObject name.

## Attributions
- All icons used are by [Gohsantosadrive](<https://www.flaticon.com/authors/gohsantosadrive>) on Flaticon.
- Decal generation system by [Mr F](<https://assetstore.unity.com/publishers/37453>) on the Unity Asset Store.

## Notice of partial source-code
This mod is built around a modified version of [Decalery](<https://assetstore.unity.com/packages/tools/level-design/decalery-293468>) from the Unity Asset Store. As such, only partial source code is available in this repository.

If you are looking for a similar open-source asset to generate decals at runtime, I recommend [Driven Decals (MIT)](<https://github.com/Anatta336/driven-decals>) as it is what the mod was built around originally.

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation not affiliated with, supported by, or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
