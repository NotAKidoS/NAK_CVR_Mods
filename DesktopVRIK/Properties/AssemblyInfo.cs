using MelonLoader;
using NAK.Melons.DesktopVRIK.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.Melons.DesktopVRIK))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.Melons.DesktopVRIK))]

[assembly: MelonInfo(
    typeof(NAK.Melons.DesktopVRIK.DesktopVRIKMod),
    nameof(NAK.Melons.DesktopVRIK),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/DesktopVRIK"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonOptionalDependencies("BTKUILib")]
[assembly: HarmonyDontPatchAll]

namespace NAK.Melons.DesktopVRIK.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "4.0.2";
    public const string Author = "NotAKidoS";
}