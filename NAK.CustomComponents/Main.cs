using ABI_RC.Core.Util.AssetFiltering;
using MelonLoader;
using NAK.CCK.CustomComponents;

namespace NAK.Melons.CustomComponents;

public class CustomComponents : MelonMod
{
    public override void OnInitializeMelon()
    {
        // Add our CCK component to the prop whitelist
        var propWhitelist = SharedFilter._spawnableWhitelist;
        propWhitelist.Add(typeof(NAKPointerTracker));
    }
}