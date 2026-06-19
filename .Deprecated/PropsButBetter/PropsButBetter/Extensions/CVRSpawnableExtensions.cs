using ABI_RC.Core.Util;
using ABI.CCK.Components;

namespace NAK.PropsButBetter;

public static class CVRSpawnableExtensions
{
    public static bool IsSpawnedByAdmin(this CVRSpawnable spawnable) 
        => spawnable.ownerId is CVRSyncHelper.OWNERID_SYSTEM 
            or CVRSyncHelper.OWNERID_LOCALSERVER;
}