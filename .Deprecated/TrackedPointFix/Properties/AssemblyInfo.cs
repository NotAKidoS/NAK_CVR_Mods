using MelonLoader;
using NAK.TrackedPointFix.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.TrackedPointFix))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.TrackedPointFix))]

[assembly: MelonInfo(
    typeof(NAK.TrackedPointFix.TrackedPointFix),
    nameof(NAK.TrackedPointFix),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/TrackedPointFix"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: HarmonyDontPatchAll]

namespace NAK.TrackedPointFix.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.3";
    public const string Author = "NotAKidoS";
}