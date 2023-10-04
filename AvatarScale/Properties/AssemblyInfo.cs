using MelonLoader;
using NAK.AvatarScaleMod.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.AvatarScaleMod))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.AvatarScaleMod))]

[assembly: MelonInfo(
    typeof(NAK.AvatarScaleMod.AvatarScaleMod),
    nameof(NAK.AvatarScaleMod),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/NAK_CVR_Mods/tree/main/AvatarScale"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonOptionalDependencies("Action Menu")]
[assembly: MelonColor(255, 241, 200, 82)]
[assembly: MelonAuthorColor(255, 114, 17, 25)]
[assembly: HarmonyDontPatchAll]

namespace NAK.AvatarScaleMod.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.0";
    public const string Author = "NotAKidoS";
}