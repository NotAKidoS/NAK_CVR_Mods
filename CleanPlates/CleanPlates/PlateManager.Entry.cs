using ABI_RC.Core.Player;
using ABI_RC.Systems.Communications.Audio.Components;
using NAK.CleanPlates.UI;
using UnityEngine;

namespace NAK.CleanPlates;

public static partial class PlateManager
{
    // This is just to forward the active state to the overhead controller
    // shipped by the game. Somewhat of a hack, but we cannot modify our Nameplate!
    public class OverheadHandle : IOverhead
    {
        internal Entry Entry;
        public bool IsActive => Entry.Alpha > 0.001f;
    }

    public class ChatOverheadHandle : IOverhead
    {
        internal Entry Entry;
        public bool IsActive => Entry.Chat.IsActive;
    }

    internal class Entry
    {
        public Transform Parent;
        public NameplateView Plate;
        public MiniNameplate Mini;
        public NameplateChat Chat;
        public NameplateData Data;
        public OverheadHandle Handle;
        public ChatOverheadHandle ChatHandle;
        public float Distance = float.MaxValue;
        public float Alpha;
        public float LodBlend;
        public float DetailBlend;
        public float TalkLevel;
        public float AppliedTalk = -1f;
        public float RevealTime;
        public float Scale = -1f;
        public bool Dirty;
        public bool AlphaDirty;

        public bool NearHidden;
        // Starts collapsed so the first tick past the threshold reports it.
        public bool Collapsed = true;
        public bool IsLocal;
        public bool IsIndicator;
        // Only remote nameplates leave it
        public bool InNearBand = true;

        public PlayerBase Player;
        public OverheadController OverheadController;
        // Overhead for nameplates, plate itself for indicators
        public Transform Anchor;
        public Vector3 BaseScale = Vector3.one;
        public PuppetMaster Puppet;

        public IPlateView View => IsIndicator ? Mini : Plate;

        public float GetCommsSmoothAmplitude()
        {
            if (IsLocal)
            {
                Comms_CapturePipeline capture = Comms_CapturePipeline.Instance;
                return capture != null ? capture.SmoothAmplitude : 0f;
            }
            Comms_ParticipantPipeline pipeline = Puppet.CommsPipeline;
            return pipeline != null ? pipeline.SmoothAmplitude : 0f;
        }
    }
}