# RelativeSyncJitterFix

Relative sync jitter fix is the single harmony patch that could not make it into the native release of RelativeSync.
Changes when props apply their incoming sync data to be before the character controller simulation.

## Known Issues
- Movement Parents on remote users will still locally jitter.
  - PuppetMaster/NetIkController applies received position updates in LateUpdate, while character controller updates in FixedUpdate.
- Movement Parents using CVRObjectSync synced by remote users will still locally jitter.
  - CVRObjectSync applies received position updates in LateUpdate, while character controller updates in FixedUpdate. 

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ChilloutVR. 
https://docs.chilloutvr.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation not affiliated with, supported by, or approved by ChilloutVR. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by ChilloutVR.
