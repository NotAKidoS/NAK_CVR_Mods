using ABI.CCK.Components;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.PlapPlapForAll;

public enum PlapPlapAudioMode
{
    Ass,
    Mouth,
    Generic,
    Vagina
}

public sealed class PlapPlapTap : MonoBehaviour
{
    private static readonly Rule[] Rules =
    {
        new(PlapPlapAudioMode.Mouth, HumanBodyBones.Head, 3,
            "mouth", "oral", "blow", "bj"),
        new(PlapPlapAudioMode.Vagina, HumanBodyBones.Hips, 3, 
            "pussy", "vagina", "cunt", "v"),
        new(PlapPlapAudioMode.Ass, HumanBodyBones.Hips, 2, "ass", 
            "anus", "butt", "anal", "b"),
        new(PlapPlapAudioMode.Generic, null, 1,
            "thigh", "armpit", "foot", "knee", "paizuri", "buttjob", "breast", "boob", "ear")
    };

    private DPSOrifice _dpsOrifice;
    private Animator _animator;
    private ParentConstraint _constraint;
    private bool _dynamic;
    private bool _lastLightState;
    private int _activeSourceIndex = -1;
    private PlapPlapAudioMode _mode;
    private GameObject _plapPlapObject;
    private RenderTexture _memoryTexture;
    private bool _hasInitialized;

    public static bool IsBuiltInPlapPlapSetup(DPSOrifice dpsOrifice)
    {
        Transform basis = dpsOrifice.basis;
        
        // Check if basis name is plap plap
        if (basis.name == "plap plap") return true;
        
        // Check if there is a texture property parser under the basis
        SkinnedMeshRenderer smr = basis.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr && smr.sharedMaterial && smr.sharedMaterial.name == "Unlit_detect dps")
            return true;
        
        return false;
    }
    
    public static PlapPlapTap CreateFromOrifice(DPSOrifice dpsOrifice, Animator animator, GameObject plapPlapPrefab)
    {
        Light light = dpsOrifice.dpsLight;
        PlapPlapTap tap = light.gameObject.AddComponent<PlapPlapTap>();
        tap._dpsOrifice = dpsOrifice;
        tap._animator = animator;
        
        GameObject plapPlap = Instantiate(plapPlapPrefab, light.transform, false);
        tap._plapPlapObject = plapPlap;
        
        // Duplicate memory texture
        CVRTexturePropertyParser parser = plapPlap.GetComponentInChildren<CVRTexturePropertyParser>(true);
        Camera camera = plapPlap.GetComponentInChildren<Camera>(true);

        RenderTexture instancedTexture = new(parser.texture);
        instancedTexture.name += $"_Copy_{instancedTexture.GetHashCode()}";
        parser.texture = instancedTexture;
        camera.targetTexture = instancedTexture;
        tap._memoryTexture = instancedTexture;
        
        ParentConstraint pc = tap.GetComponentInParent<ParentConstraint>(true);
        if (pc && pc.sourceCount > 0)
        {
            tap._constraint = pc;
            tap._dynamic = true;
        }

        tap.SetOrificeMode(dpsOrifice.type);
        tap.RecomputeMode();
        tap.SyncLightState();

        PlapPlapForAllMod.Logger.Msg(
            $"PlapPlapTap created for orifice '{dpsOrifice.type}' using light '{dpsOrifice.basis.name}'. " +
            $"Dynamic: {tap._dynamic}, Initial Mode: {tap._mode}");

        tap._hasInitialized = true;
        
        return tap;
    }

    private void OnEnable()
    {
        if (!_hasInitialized) return;
        RecomputeMode();
        SyncLightState();
    }

    private void OnDisable()
    {
        if (!_hasInitialized) return;
        SyncLightState();
    }

    private void OnDestroy()
    {
        if (_memoryTexture)
        {
            Destroy(_memoryTexture);
            _memoryTexture = null;
        }
    }
        
    private void Update()
    {
        if (!_dynamic || !_constraint) return;

        int top = -1;
        float best = -1f;

        int count = _constraint.sourceCount;
        for (int i = 0; i < count; i++)
        {
            ConstraintSource src = _constraint.GetSource(i);
            if (!src.sourceTransform) continue;

            float w = src.weight;
            if (w > best)
            {
                best = w;
                top = i;
            }
        }

        if (top != _activeSourceIndex)
        {
            _activeSourceIndex = top;
            RecomputeMode();
        }
    }

    private void SyncLightState()
    {
        Light light = _dpsOrifice.dpsLight;
        if (!light) return;

        bool on = light.isActiveAndEnabled;
        if (_lastLightState == on) return;
        _lastLightState = on;
        
        _plapPlapObject.SetActive(on);
    }

    private void RecomputeMode()
    {
        Transform basis = _dpsOrifice.dpsLight.transform;

        if (_dynamic && _constraint && _activeSourceIndex >= 0)
        {
            ConstraintSource src = _constraint.GetSource(_activeSourceIndex);
            if (src.sourceTransform)
                basis = src.sourceTransform;
        }

        string basisName = basis.name;
        int bestScore = int.MinValue;
        PlapPlapAudioMode bestMode = PlapPlapAudioMode.Generic;

        for (int r = 0; r < Rules.Length; r++)
        {
            ref readonly Rule rule = ref Rules[r];
            int score = 0;

            if (rule.HintBone.HasValue && _animator)
            {
                Transform bone = _animator.GetBoneTransform(rule.HintBone.Value);
                if (bone && basis.IsChildOf(bone))
                    score += rule.BoneWeight;
            }

            string lowerName = basisName.ToLowerInvariant();

            for (int k = 0; k < rule.Keywords.Length; k++)
            {
                string kw = rule.Keywords[k];
                string lowerKw = kw.ToLowerInvariant();

                if (lowerName == lowerKw)
                {
                    score += 8;
                    continue;
                }

                bool tokenMatch = false;
                int start = 0;
                for (int i = 0; i <= lowerName.Length; i++)
                {
                    if (i == lowerName.Length || !char.IsLetter(lowerName[i]))
                    {
                        int len = i - start;
                        if (len == lowerKw.Length)
                        {
                            bool equal = true;
                            for (int c = 0; c < len; c++)
                            {
                                if (lowerName[start + c] != lowerKw[c])
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            if (equal)
                            {
                                tokenMatch = true;
                                break;
                            }
                        }
                        start = i + 1;
                    }
                }

                if (tokenMatch)
                {
                    score += lowerKw.Length == 1 ? 6 : 5;
                    continue;
                }

                if (lowerKw.Length >= 4 && lowerName.Contains(lowerKw))
                    score += 3;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMode = rule.Mode;
            }
        }

        if (bestMode == _mode) return;
        _mode = bestMode;

        SetAudioMode(_mode);
        
        PlapPlapForAllMod.Logger.Msg($"PlapPlapTap applying mode {_mode}");
    }

    private readonly struct Rule(PlapPlapAudioMode mode, HumanBodyBones? bone, int boneWeight, params string[] keywords)
    {
        public readonly PlapPlapAudioMode Mode = mode;
        public readonly HumanBodyBones? HintBone = bone;
        public readonly int BoneWeight = boneWeight;
        public readonly string[] Keywords = keywords;
    }
    
    /* Interacting with Plap Plap */
    
    public void SetAudioMode(PlapPlapAudioMode mode)
    {
        CVRAnimatorDriver animatorDriver  = _plapPlapObject.GetComponent<CVRAnimatorDriver>();
        animatorDriver.animatorParameter01 = (float)mode;
    }

    public void SetOrificeMode(DPSLightType mode)
    {
        CVRAnimatorDriver animatorDriver = _plapPlapObject.GetComponent<CVRAnimatorDriver>();
        animatorDriver.animatorParameter02 = (float)mode;
    }
}