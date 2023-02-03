using MelonLoader;
using NAK.Melons.GestureLock.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.Melons.GestureLock))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.Melons.GestureLock))]

[assembly: MelonInfo(
    typeof(NAK.Melons.GestureLock.GestureLockMod),
    nameof(NAK.Melons.GestureLock),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/GestureLock"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]

namespace NAK.Melons.GestureLock.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "2.0.0";
    public const string Author = "NotAKidoS";
}