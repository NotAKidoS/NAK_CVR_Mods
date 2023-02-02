using NAK.Melons.Blackout.Properties;
using MelonLoader;
using System.Reflection;


[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.Melons.Blackout))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.Melons.Blackout))]

[assembly: MelonInfo(
    typeof(NAK.Melons.Blackout.Blackout),
    nameof(NAK.Melons.Blackout),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/Blackout"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonOptionalDependencies("UIExpansionKit", "BTKUILib")]

namespace NAK.Melons.Blackout.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "2.0.0";
    public const string Author = "NotAKidoS";
}