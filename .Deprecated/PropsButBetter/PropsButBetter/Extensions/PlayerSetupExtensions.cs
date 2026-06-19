using ABI_RC.Core.Player;

namespace NAK.PropsButBetter;

public static class PlayerSetupExtensions
{
    public static void ToggleDeleteMode(this PlayerSetup playerSetup)
    {
        if (playerSetup.propGuidForSpawn == PlayerSetup.PropModeDeleteString)
            playerSetup.ClearPropToSpawn();
        else
            playerSetup.EnterPropDeleteMode();
    }
}