# RelativeSync

Relative sync for Movement Parent & Chairs. Requires both users to have the mod installed. Synced over Mod Network.

https://github.com/NotAKidOnSteam/NAK_CVR_Mods/assets/37721153/ae6c6e4b-7529-42e2-bd2c-afa050849906

## Mod Settings
- **Debug Network Inbound**: Log network messages received from other players.
- **Debug Network Outbound**: Log network messages sent to other players.
- **Exp Spawnable Sync Hack**: Forces CVRSpawnable to update position in FixedUpdate. This can help with local jitter while on a remote synced movement parent.
- **Exp Disable Interpolation on BBCC**: Disables interpolation on BetterBetterCharacterController. This can help with local jitter while on any movement parent.

## Known Issues
- Movement Parents on remote users will still locally jitter.
  - PuppetMaster/NetIkController applies received position updates in LateUpdate, while character controller updates in FixedUpdate.
- Movement Parents using CVRObjectSync synced by remote users will still locally jitter.
  - CVRObjectSync applies received position updates in LateUpdate, while character controller updates in FixedUpdate. 
- Slight interpolation issue with humanoid avatar hips while standing on a Movement Parent.
    - Requires further investigation. I believe it to be because hips are not synced properly, requiring me to relative sync the hips as well.

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation not affiliated with, supported by, or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
