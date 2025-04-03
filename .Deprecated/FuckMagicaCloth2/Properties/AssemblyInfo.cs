using MelonLoader;
using NAK.FuckOffMagicaCloth2.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.FuckOffMagicaCloth2))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.FuckOffMagicaCloth2))]

[assembly: MelonInfo(
    typeof(NAK.FuckOffMagicaCloth2.FuckOffMagicaCloth2Mod),
    nameof(NAK.FuckOffMagicaCloth2),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/FuckOffMagicaCloth2"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: HarmonyDontPatchAll]

namespace NAK.FuckOffMagicaCloth2.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.1";
    public const string Author = "NotAKidoS";
}