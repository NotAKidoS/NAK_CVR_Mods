using ABI_RC.Core.IO;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.Communications;
using NAK.FuckVivox;

namespace FuckMLA;

public static class VivoxHelpers
{
    public static void AttemptLogin()
    {
        if (!AuthManager.IsAuthenticated)
        {
            FuckVivox.Logger.Msg("Attempted to log in without being authenticated!");
            return;
        }
        VivoxServiceManager.Instance.Login(MetaPort.Instance.ownerId, MetaPort.Instance.blockedUserIds);
    }
    
    public static void AttemptLogout()
    {
        if (!VivoxServiceManager.Instance.IsLoggedIn())
        {
            FuckVivox.Logger.Msg("Attempted to log out when not logged in.");
            return;
        }
        VivoxServiceManager.Instance.Logout();
    }

    public static void PleaseReLoginThankYou()
    {
        FuckVivox.Logger.Msg("PleaseReLoginThankYou!!!");
        
        AttemptLogout();
        SchedulerSystem.AddJob(AttemptLogin, 3f, 1, 1);
    }
}