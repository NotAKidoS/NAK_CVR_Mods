using MelonLoader;
using NAK.RemoteAvatarDisablingCameraOnFirstFrameFix.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.RemoteAvatarDisablingCameraOnFirstFrameFix))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.RemoteAvatarDisablingCameraOnFirstFrameFix))]

[assembly: MelonInfo(
    typeof(NAK.RemoteAvatarDisablingCameraOnFirstFrameFix.RemoteAvatarDisablingCameraOnFirstFrameFixMod),
    nameof(NAK.RemoteAvatarDisablingCameraOnFirstFrameFix),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/RemoteAvatarDisablingCameraOnFirstFrameFix"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: HarmonyDontPatchAll]

namespace NAK.RemoteAvatarDisablingCameraOnFirstFrameFix.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.0";
    public const string Author = "NotAKidoS";
}