# Mowods

Adds a Mòwóds page to the Main Menu home page to view all mod settings in-game. Spiritual replacement for UI Expansion Kit.

**Features:**
- Mod setting search
- Displays author, version, assembly name, source URL
- Categories color-coded by defined mod/author colors
- Tooltips & settings reset buttons

Everything is parsed from the MelonLoader [attributes](<https://melonwiki.xyz/#/modders/attributes?id=attributes-in-melonloader>) and [preferences](<https://melonwiki.xyz/#/modders/preferences>).

## Custom pages

Mods can author their own pages if they want:

```cs
Page page = ModsMenu.AddPage("My Mod");

Section general = page.AddSection("General");
general.Add(myToggleEntry);
general.Add(new Number("Speed", Binding.Of(mySpeedEntry), 0, 10, 0.5));
general.Add(new Choice("Mode", Binding.Of(myModeEntry)));

Section debug = page.AddSection("Debug", "Only for testing");
debug.Add(new Toggle("Draw Gizmos", Binding.Of(() => drawGizmos, v => drawGizmos = v, false)));
debug.Add(new Button("Cache", "Clear", ClearCache));
```

`Binding.Of(get, set, default)` binds plain fields. Call `NotifyChanged()` on the binding when the value changes elsewhere.

## Attributions

The watermelon emoji is from [Twemoji](https://github.com/jdecked/twemoji) ([CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/)).

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ChilloutVR.
https://docs.chilloutvr.net/official/legal/tos/#6-modding-our-game

> This mod is an independent creation not affiliated with, supported by, or approved by ChilloutVR.

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by ChilloutVR.