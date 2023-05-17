using ABI.CCK.Components;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using HarmonyLib;
using RootMotion.FinalIK;
using System.Reflection;
using UnityEngine;

namespace NAK.Melons.DesktopXRSwitch.Patches;

public class PlayerSetupTracker : MonoBehaviour
{
    public static PlayerSetupTracker Instance;
    public PlayerSetup playerSetup;

    public Traverse _initialCameraPosTraverse;
    public Traverse _lookIKTraverse;
    public Traverse _lastScaleTraverse;

    public HumanPose avatarInitialHumanPose;
    public HumanPoseHandler avatarHumanPoseHandler;

    public GameObject avatarObject;
    public CVRAvatar avatarDescriptor;
    public Animator avatarAnimator;
    public Vector3 initialPosition;
    public Quaternion initialRotation;

    void Start()
    {
        Instance = this;
        playerSetup = GetComponent<PlayerSetup>();
        _initialCameraPosTraverse = Traverse.Create(playerSetup).Field("initialCameraPos");
        _lookIKTraverse = Traverse.Create(playerSetup).Field("lookIK");
        _lastScaleTraverse = Traverse.Create(playerSetup).Field("lastScale");
    }

    public void OnSetupAvatar(GameObject avatar)
    {
        avatarObject = avatar;
        avatarDescriptor = avatarObject.GetComponent<CVRAvatar>();
        avatarAnimator = avatarObject.GetComponent<Animator>();
        initialPosition = avatarObject.transform.localPosition;
        initialRotation = avatarObject.transform.localRotation;
        if (avatarHumanPoseHandler == null)
        {
            avatarHumanPoseHandler = new HumanPoseHandler(avatarAnimator.avatar, avatarAnimator.transform);
        }
        avatarHumanPoseHandler.GetHumanPose(ref avatarInitialHumanPose);
    }

    public void OnClearAvatar()
    {
        avatarObject = null;
        avatarDescriptor = null;
        avatarAnimator = null;
        initialPosition = Vector3.one;
        initialRotation = Quaternion.identity;
        avatarHumanPoseHandler = null;
    }

    public void ConfigureAvatarIK(bool isVR)
    {
        bool StateOnDisable = avatarAnimator.keepAnimatorStateOnDisable;
        avatarAnimator.keepAnimatorStateOnDisable = true;
        avatarAnimator.enabled = false;

        //reset avatar offsets
        avatarObject.transform.localPosition = initialPosition;
        avatarObject.transform.localRotation = initialRotation;
        avatarHumanPoseHandler.SetHumanPose(ref avatarInitialHumanPose);

        if (isVR)
        {
            SwitchAvatarVr();
        }
        else
        {
            SwitchAvatarDesktop();
        }

        //lazy fix
        _lastScaleTraverse.SetValue(Vector3.zero);
        MethodInfo setPlaySpaceScaleMethod = playerSetup.GetType().GetMethod("CheckUpdateAvatarScaleToPlaySpaceRelation", BindingFlags.NonPublic | BindingFlags.Instance);
        setPlaySpaceScaleMethod.Invoke(playerSetup, null);

        avatarAnimator.keepAnimatorStateOnDisable = StateOnDisable;
        avatarAnimator.enabled = true;
    }

    private void SwitchAvatarVr()
    {
        if (avatarDescriptor == null) return;

        //remove LookAtIK if found
        LookAtIK avatarLookAtIK = avatarObject.GetComponent<LookAtIK>();
        if (avatarLookAtIK != null) DestroyImmediate(avatarLookAtIK);

        //have IKSystem set up avatar for VR
        //a good chunk of IKSystem initialization checks for existing
        //stuff & if it is set up, so game is ready to handle it
        if (avatarAnimator != null && avatarAnimator.isHuman)
        {
            IKSystem.Instance.InitializeAvatar(avatarDescriptor);
        }
    }

    private void SwitchAvatarDesktop()
    {
        if (avatarDescriptor == null) return;

        //remove VRIK & VRIKRootController if found
        VRIK avatarVRIK = avatarObject.GetComponent<VRIK>();
        VRIKRootController avatarVRIKRootController = avatarObject.GetComponent<VRIKRootController>();
        if (avatarVRIK != null) DestroyImmediate(avatarVRIK);
        if (avatarVRIKRootController != null) DestroyImmediate(avatarVRIKRootController);

        //remove all TwistRelaxer components
        TwistRelaxer[] avatarTwistRelaxers = avatarObject.GetComponentsInChildren<TwistRelaxer>();
        for (int i = 0; i < avatarTwistRelaxers.Length; i++)
        {
            DestroyImmediate(avatarTwistRelaxers[i]);
        }

        Transform headTransform = avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
        if (headTransform != null)
        {
            //set DesktopCameraRig to head bone pos
            playerSetup.desktopCameraRig.transform.position = headTransform.position;
            //add LookAtIK & Configure
            LookAtIK lookAtIK = avatarObject.AddComponentIfMissing<LookAtIK>();
            lookAtIK.solver.head = new IKSolverLookAt.LookAtBone(headTransform);
            lookAtIK.solver.headWeight = 0.75f;
            lookAtIK.solver.target = playerSetup.desktopCameraTarget.transform;
            _lookIKTraverse.SetValue(lookAtIK);
        }

        //set camera position & initial position for headbob (both modes end up with same number)
        playerSetup.desktopCamera.transform.localPosition = (Vector3)_initialCameraPosTraverse.GetValue();
    }
}