using MelonLoader;
using NAK.Melons.DesktopVRSwitch.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.Melons.DesktopVRSwitch))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.Melons.DesktopVRSwitch))]

[assembly: MelonInfo(
    typeof(NAK.Melons.DesktopVRSwitch.DesktopVRSwitchMod),
    nameof(NAK.Melons.DesktopVRSwitch),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/DesktopVRSwitch"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(ConsoleColor.DarkCyan)]
[assembly: HarmonyDontPatchAll]

namespace NAK.Melons.DesktopVRSwitch.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "4.3.5";
    public const string Author = "NotAKidoS";
}