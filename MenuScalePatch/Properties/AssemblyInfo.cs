using MelonLoader;
using MenuScalePatch.Properties;
using System.Reflection;


[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(MenuScalePatch))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(MenuScalePatch))]

[assembly: MelonInfo(
    typeof(NAK.Melons.MenuScalePatch.MenuScalePatch),
    nameof(MenuScalePatch),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/MenuScalePatch"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]

namespace MenuScalePatch.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "4.0.0";
    public const string Author = "NotAKidoS";
}