The mod compiles `com.nak.cleanplates/Runtime` directly, see the csproj.

Unity gets at it with a local package dependency. In the Unity project's
`Packages/manifest.json`:

    "com.nak.cleanplates": "file:C:/workspaces/NAK_CVR_Mods/CleanPlates/.Unity/com.nak.cleanplates"