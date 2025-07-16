using MelonLoader;
using NAK.ThirdPerson.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.ThirdPerson))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.ThirdPerson))]

[assembly: MelonInfo(
    typeof(NAK.ThirdPerson.ThirdPerson),
    nameof(NAK.ThirdPerson),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/ThirdPerson"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 246, 25, 97)] // do not change color, originally chosen by Davi
[assembly: MelonAuthorColor(255, 158, 21, 32)] // do not change color, originally chosen by Davi
[assembly: HarmonyDontPatchAll]

namespace NAK.ThirdPerson.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.1.2";
    public const string Author = "Davi & NotAKidoS";
}