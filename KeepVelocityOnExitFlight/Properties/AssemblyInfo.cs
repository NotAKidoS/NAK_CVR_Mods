﻿using MelonLoader;
using NAK.KeepVelocityOnExitFlight.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.KeepVelocityOnExitFlight))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.KeepVelocityOnExitFlight))]

[assembly: MelonInfo(
    typeof(NAK.KeepVelocityOnExitFlight.KeepVelocityOnExitFlightMod),
    nameof(NAK.KeepVelocityOnExitFlight),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/KeepVelocityOnExitFlight"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 246, 25, 99)] // red-pink
[assembly: MelonAuthorColor(255, 158, 21, 32)] // red
[assembly: HarmonyDontPatchAll]

namespace NAK.KeepVelocityOnExitFlight.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.1";
    public const string Author = "NotAKidoS";
}