using NAK.SwitchToDesktopOnSteamVRExit.Properties;
using MelonLoader;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.SwitchToDesktopOnSteamVRExit))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.SwitchToDesktopOnSteamVRExit))]

[assembly: MelonInfo(
    typeof(NAK.SwitchToDesktopOnSteamVRExit.SwitchToDesktopOnSteamVRExit),
    nameof(NAK.SwitchToDesktopOnSteamVRExit),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/SwitchToDesktopOnSteamVRExit"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 52, 152, 219)]
[assembly: MelonAuthorColor(255, 158, 21, 32)]
[assembly: HarmonyDontPatchAll]

namespace NAK.SwitchToDesktopOnSteamVRExit.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.0";
    public const string Author = "NotAKidoS";
}