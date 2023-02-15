using MelonLoader;
using NAK.Melons.MenuScalePatch.Properties;
using System.Reflection;


[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.Melons.MenuScalePatch))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.Melons.MenuScalePatch))]

[assembly: MelonInfo(
    typeof(NAK.Melons.MenuScalePatch.MenuScalePatch),
    nameof(NAK.Melons.MenuScalePatch),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/MenuScalePatch"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]

namespace NAK.Melons.MenuScalePatch.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "4.2.3";
    public const string Author = "NotAKidoS";
}