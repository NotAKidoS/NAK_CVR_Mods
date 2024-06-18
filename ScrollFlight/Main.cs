using System.Globalization;
using ABI_RC.Core.UI;
using ABI_RC.Systems.Movement;
using MelonLoader;
using UnityEngine;

namespace NAK.ScrollFlight;

public class ScrollFlight : MelonMod
{
    // stole from LucMod lol
    public override void OnUpdate()
    {
        if (BetterBetterCharacterController.Instance == null
            || !BetterBetterCharacterController.Instance.IsFlying()
            || Input.GetKey(KeyCode.Mouse2)
            || Cursor.lockState != CursorLockMode.Locked)
            return;

        BetterBetterCharacterController.Instance.worldFlightSpeedMultiplier = Math.Max(0f,
            BetterBetterCharacterController.Instance.worldFlightSpeedMultiplier + Input.mouseScrollDelta.y);
        if (Input.mouseScrollDelta.y != 0f)
            CohtmlHud.Instance.ViewDropTextImmediate("(Local) ScrollFlight",
                BetterBetterCharacterController.Instance.worldFlightSpeedMultiplier.ToString(CultureInfo
                    .InvariantCulture), "Speed multiplier");
    }
}