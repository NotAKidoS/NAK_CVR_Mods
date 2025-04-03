using System.Reflection;
using MelonLoader;
using NAK.RCCVirtualSteeringWheel;
using NAK.RCCVirtualSteeringWheel.Properties;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.RCCVirtualSteeringWheel))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.RCCVirtualSteeringWheel))]

[assembly: MelonInfo(
    typeof(RCCVirtualSteeringWheelMod),
    nameof(NAK.RCCVirtualSteeringWheel),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/RCCVirtualSteeringWheel"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 246, 25, 99)] // red-pink
[assembly: MelonAuthorColor(255, 158, 21, 32)] // red
[assembly: HarmonyDontPatchAll]

namespace NAK.RCCVirtualSteeringWheel.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.4";
    public const string Author = "NotAKidoS";
}