<<<<<<< Updated upstream
﻿using DesktopVRSwitch.Properties;
using MelonLoader;
=======
﻿using MelonLoader;
using NAK.Melons.DesktopVRSwitch.Properties;
>>>>>>> Stashed changes
using System.Reflection;


[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
<<<<<<< Updated upstream
[assembly: AssemblyTitle(nameof(DesktopVRSwitch))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(DesktopVRSwitch))]

[assembly: MelonInfo(
    typeof(DesktopVRSwitch.DesktopVRSwitch),
    nameof(DesktopVRSwitch),
=======
[assembly: AssemblyTitle(nameof(NAK.Melons.DesktopVRSwitch))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.Melons.DesktopVRSwitch))]

[assembly: MelonInfo(
    typeof(NAK.Melons.DesktopVRSwitch.DesktopVRSwitchMod),
    nameof(NAK.Melons.DesktopVRSwitch),
>>>>>>> Stashed changes
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/DesktopVRSwitch"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]

<<<<<<< Updated upstream
namespace DesktopVRSwitch.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "3.0.5";
=======
namespace NAK.Melons.DesktopVRSwitch.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "4.2.4";
>>>>>>> Stashed changes
    public const string Author = "NotAKidoS";
}