#if !UNITY_EDITOR
using NAK.OriginShift.Properties;
using MelonLoader;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.OriginShift))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.OriginShift))]

[assembly: MelonInfo(
    typeof(NAK.OriginShift.OriginShiftMod),
    nameof(NAK.OriginShift),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/NAK_CVR_Mods/tree/main/OriginShift"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 125, 126, 129)]
[assembly: MelonAuthorColor(255, 158, 21, 32)]
[assembly: HarmonyDontPatchAll]

namespace NAK.OriginShift.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.0";
    public const string Author = "NotAKidoS";
}
#endif