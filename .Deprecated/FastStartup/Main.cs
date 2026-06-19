using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using ABI_RC.Core.Base;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Savior.SceneManagers;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.UI;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAK.FastStartup;

public class FastStartupMod : MelonMod
{
    private const string APIAddress = "https://api.chilloutvr.net";
    private const string APIVersion = "1";
    
    private static MelonLogger.Instance Logger;
    private static LoginProfile _profile;
    private static Task<BaseResponse<UserAuthResponse>> _pendingAuth;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        LoginRoom.IsPlayerIn = true;
        
        // Load profile and fire auth request as early as possible
        _profile = LoadFirstProfile();
        if (_profile != null)
        {
            Logger.Msg($"Firing early auth for {_profile.Username}...");
            _pendingAuth = AuthenticateAsync(_profile.Username, _profile.AccessKey);
        }
        else
        {
            Logger.Error("No valid profile found.");
        }

        HarmonyInstance.Patch(
            typeof(Preparation).GetMethod(nameof(Preparation.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(FastStartupMod).GetMethod(nameof(OnPrePreparationStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(Init).GetMethod(nameof(Init.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(FastStartupMod).GetMethod(nameof(OnMethodWeDontCareAboutAndWantToFuckOff),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch(
            typeof(IntroManager).GetMethod(nameof(IntroManager.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(FastStartupMod).GetMethod(nameof(OnMethodWeDontCareAboutAndWantToFuckOff),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch(
            typeof(IntroManager).GetMethod(nameof(IntroManager.OnDestroy),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(FastStartupMod).GetMethod(nameof(OnMethodWeDontCareAboutAndWantToFuckOff),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(LoginRoom).GetMethod(nameof(LoginRoom.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(FastStartupMod).GetMethod(nameof(OnLoginRoomStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        Logger.Msg("FastStartup initialized");
    }
    
    private static bool OnPrePreparationStart()
    {
        MelonCoroutines.Start(WaitForAuthAndContinue());
        return false;
    }

    private static bool OnMethodWeDontCareAboutAndWantToFuckOff() => false;

    private static void OnLoginRoomStart() => WorldTransitionSystem.Instance.ContinueTransition();

    private static IEnumerator WaitForAuthAndContinue()
    {
        Logger.Msg("Waiting for early auth to complete...");
        
        // Load init scene
        SceneManager.LoadScene("Init");
        yield return null;

        PlayerSetup.Instance.ToggleCameras(true, true);
        WorldTransitionSystem.Instance.StartTransition(true);
        CursorLockManager.Instance.UpdateCursorState();
        
        if (_pendingAuth == null)
        {
            MelonCoroutines.Start(LoginRoom.LoadScene());
            yield break;
        }
        
        while (!_pendingAuth.IsCompleted)
            yield return null;

        if (_pendingAuth.IsFaulted || _pendingAuth.Result?.Data == null)
        {
            Logger.Error($"Early auth failed: {_pendingAuth.Exception?.Message ?? "null response"}");
            MelonCoroutines.Start(LoginRoom.LoadScene());
            yield break;
        }

        var authData = _pendingAuth.Result.Data;
        Logger.Msg($"Early auth succeeded: {authData.Username} ({authData.UserId})");
        
        MelonCoroutines.Start(AuthManager.AuthenticationSucceeded(_pendingAuth.Result, AuthManager.AuthEntity.ABIProfile));
        MelonCoroutines.Start(LoadOurContent());
    }

    private static IEnumerator LoadOurContent()
    {
        yield return null;
        Content.LoadIntoWorld(MetaPort.Instance.homeWorldGuid, true);
        AssetManagement.Instance.LoadLocalAvatar(MetaPort.Instance.currentAvatarGuid);
    }

    #region Profile Loading

    private static LoginProfile LoadFirstProfile()
    {
        try
        { 
            /*var profileFiles = Directory.GetFiles(Application.dataPath, "*.profile");
            if (profileFiles.Length == 0)
            {
                Logger.Warning("No .profile files found");
                return null;
            }

            // Load first valid profile found
            foreach (var file in profileFiles)
            {*/
            string file = Path.Combine(Application.dataPath, "autologin.profile");
            if (File.Exists(file))
            {
                try
                {
                    using var reader = new StreamReader(file);
                    var serializer = new XmlSerializer(typeof(LoginProfile));
                    var profile = (LoginProfile)serializer.Deserialize(reader);

                    if (!string.IsNullOrEmpty(profile?.Username) && !string.IsNullOrEmpty(profile?.AccessKey))
                    {
                        Logger.Msg($"Loaded profile: {profile.Username}");
                        return profile;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to load {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            // }
        }
        catch (Exception ex)
        {
            Logger.Error($"Profile loading failed: {ex.Message}");
        }

        return null;
    }

    #endregion Profile Loading

    #region Auth Request

    private static async Task<BaseResponse<UserAuthResponse>> AuthenticateAsync(string username, string accessKey)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        client.DefaultRequestHeaders.Add(ApiConnection.HeaderUsername, username);
        client.DefaultRequestHeaders.Add(ApiConnection.HeaderAccessKey, accessKey);
        client.DefaultRequestHeaders.Add(ApiConnection.HeaderUserAgent, $"ChilloutVR/{Application.version} (fuck you)");
        client.DefaultRequestHeaders.Add(ApiConnection.HeaderPlatform, MetaPort.Platform);
        client.DefaultRequestHeaders.Add(ApiConnection.HeaderCompatibleVersions, MetaPort.CompatibleVersions);

        var payload = JsonConvert.SerializeObject(new
        {
            Username = username,
            Password = accessKey,
            AuthType = 1,
            get = "?acceptTOS=true"
        });

        var url = $"{APIAddress}/{APIVersion}/users/auth?acceptTOS=true";

        try
        {
            var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error($"Auth failed: {response.StatusCode}");
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<BaseResponse<UserAuthResponse>>(body);
        }
        catch (Exception ex)
        {
            Logger.Error($"Auth request failed: {ex.Message}");
            return null;
        }
    }

    #endregion Auth Request
}

[XmlRoot("LoginProfile")]
public class LoginProfile
{
    [XmlElement("Username")] public string Username { get; set; }
    [XmlElement("AccessKey")] public string AccessKey { get; set; }
}