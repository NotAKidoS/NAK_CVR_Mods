using MelonLoader;
using NAK.Melons.DesktopXRSwitch.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.Melons.DesktopXRSwitch))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.Melons.DesktopXRSwitch))]

[assembly: MelonInfo(
    typeof(NAK.Melons.DesktopXRSwitch.DesktopXRSwitch),
    nameof(NAK.Melons.DesktopXRSwitch),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/DesktopXRSwitch"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: HarmonyDontPatchAll]

namespace NAK.Melons.DesktopXRSwitch.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "4.3.4";
    public const string Author = "NotAKidoS";
}