using MelonLoader;
using NAK.PhysicsGunMod.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.PhysicsGunMod))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.PhysicsGunMod))]

[assembly: MelonInfo(
    typeof(NAK.PhysicsGunMod.PhysicsGunMod),
    nameof(NAK.PhysicsGunMod),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidOnSteam/NAK_CVR_Mods/tree/main/PhysicsGunMod"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 241, 200, 82)]
[assembly: MelonAuthorColor(255, 114, 17, 25)]
[assembly: HarmonyDontPatchAll]

namespace NAK.PhysicsGunMod.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.0";
    public const string Author = "NotAKidoS";
}