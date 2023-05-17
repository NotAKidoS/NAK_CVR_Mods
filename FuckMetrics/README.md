# FuckMetrics **(Depricated)**

  # This fix/a better alternative is now built into ChilloutVR.

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

---

Here is the block of text where I tell you this mod is not affiliated or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation and is not affiliated with, supported by or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.

