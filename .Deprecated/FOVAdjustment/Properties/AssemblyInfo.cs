using MelonLoader;
using NAK.FOVAdjustment.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.FOVAdjustment))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.FOVAdjustment))]

[assembly: MelonInfo(
    typeof(NAK.FOVAdjustment.FOVAdjustment),
    nameof(NAK.FOVAdjustment),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/FOVAdjustment"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonOptionalDependencies("UIExpansionKit")]
[assembly: MelonColor(255, 125, 125, 125)]
[assembly: MelonAuthorColor(255, 158, 21, 32)]
[assembly: HarmonyDontPatchAll]

namespace NAK.FOVAdjustment.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.1";
    public const string Author = "NotAKidoS";
}