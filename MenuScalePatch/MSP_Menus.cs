using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ABI_RC.Core.Savior;

namespace NAK.Melons.MenuScalePatch.Helpers;

public class MSP_MenuInfo
{
    //Shared Info
    public static float ScaleFactor = 1f;
    public static float AspectRatio = 1f;
    public static Transform CameraTransform;

    //Settings...?
    public static bool WorldAnchorQM;

    //if other mods need to disable?
    public static bool DisableQMHelper;
    public static bool DisableQMHelper_VR;
    public static bool DisableMMHelper;
    public static bool DisableMMHelper_VR;
}