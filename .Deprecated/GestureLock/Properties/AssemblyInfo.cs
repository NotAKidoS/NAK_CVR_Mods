using MelonLoader;
using NAK.GestureLock.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.GestureLock))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.GestureLock))]

[assembly: MelonInfo(
    typeof(NAK.GestureLock.GestureLock),
    nameof(NAK.GestureLock),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/GestureLock"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 128, 66, 33)]
[assembly: MelonAuthorColor(255, 158, 21, 32)]
[assembly: HarmonyDontPatchAll]

namespace NAK.GestureLock.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "2.0.1";
    public const string Author = "NotAKidoS";
}