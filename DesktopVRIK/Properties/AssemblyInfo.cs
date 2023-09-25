using MelonLoader;
using NAK.DesktopVRIK.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.DesktopVRIK))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.DesktopVRIK))]

[assembly: MelonInfo(
    typeof(NAK.DesktopVRIK.DesktopVRIK),
    nameof(NAK.DesktopVRIK),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/NAK_CVR_Mods/tree/main/DesktopVRIK"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonOptionalDependencies("BTKUILib", "AvatarMotionTweaker")]
[assembly: MelonColor(255, 155, 89, 182)]
[assembly: MelonAuthorColor(255, 158, 21, 32)]
[assembly: HarmonyDontPatchAll]

namespace NAK.DesktopVRIK.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "4.2.7";
    public const string Author = "NotAKidoS";
}