/*using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Systems.RuntimeDebug;
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.PropsButBetter;

public static class PropDistanceHider
{
    public enum HiderMode
    {
        Disabled,
        DisableRendering,
    }

    private class PropPart
    {
        public Transform Transform;
        public Transform BoundsCenter; // Child transform at local bounds center
        public float BoundsRadius;     // Local extents magnitude
    }

    private class PropCache
    {
        public CVRSyncHelper.PropData PropData;

        public PropPart[] Parts;
        public int PartCount;

        // Renderers (not Behaviours, have enabled)
        public Renderer[] Renderers;
        public int RendererCount;
        public bool[] RendererEnabled;

        // Colliders (not Behaviours, have enabled)
        public Collider[] Colliders;
        public int ColliderCount;
        public bool[] ColliderEnabled;

        // Rigidbodies (no enabled, use isKinematic)
        public Rigidbody[] Rigidbodies;
        public int RigidbodyCount;
        public bool[] RigidbodyWasKinematic;

        // ParticleSystems (not Behaviours, no enabled, use Play/Stop)
        public ParticleSystem[] ParticleSystems;
        public int ParticleSystemCount;
        public bool[] PsWasPlaying;
        public bool[] PsPlayOnAwake;
        public float[] PsTime;
        public double[] PsStartTime;
        public uint[] PsRandomSeed;
        public bool[] PsUseAutoSeed;

        // CVRPickupObject - must drop before disabling
        public CVRPickupObject[] Pickups;
        public int PickupCount;

        // CVRAttachment - must deattach before disabling
        public CVRAttachment[] Attachments;
        public int AttachmentCount;

        // CVRInteractable - must disable before other behaviours
        public CVRInteractable[] Interactables;
        public int InteractableCount;
        public bool[] InteractableEnabled;

        // Behaviour state tracking
        public Behaviour[] Behaviours;
        public int BehaviourCount;
        public bool[] BehaviourEnabled;

        // Animator state
        public bool[] AnimWasEnabled;
        public double[] AnimStartTime;

        // AudioSource state
        public bool[] AudioWasPlaying;
        public bool[] AudioLoop;
        public bool[] AudioPlayOnAwake;
        public double[] AudioStartDsp;
        public float[] AudioStartTime;

        public bool IsHidden;
    }

    public static float Hysteresis = 2f;

    private static readonly Dictionary<CVRSyncHelper.PropData, PropCache> _propCaches = new();
    private static readonly List<PropCache> _propCacheList = new();
    private static readonly List<CVRSyncHelper.PropData> _toRemove = new();

    // Reusable collections for spawn processing (avoid allocations)
    private static readonly Dictionary<Transform, int> _tempPartLookup = new();
    private static readonly List<Bounds> _tempBoundsList = new();
    private static readonly List<Renderer> _tempRenderers = new();
    private static readonly List<Collider> _tempColliders = new();
    private static readonly List<Rigidbody> _tempRigidbodies = new();
    private static readonly List<ParticleSystem> _tempParticleSystems = new();
    private static readonly List<CVRPickupObject> _tempPickups = new();
    private static readonly List<CVRAttachment> _tempAttachments = new();
    private static readonly List<CVRInteractable> _tempInteractables = new();
    private static readonly List<Behaviour> _tempBehaviours = new();
    private static readonly List<int> _tempRendererPartIndices = new();

    public static void OnPropSpawned(CVRSyncHelper.PropData propData)
    {
        GameObject propObject = propData.Wrapper;
        if (!propObject)
            return;

        CVRSpawnable spawnable = propData.Spawnable;
        if (!spawnable)
            return;

        Transform spawnableTransform = spawnable.transform;
        var subSyncs = spawnable.subSyncs;
        int subSyncCount = subSyncs?.Count ?? 0;

        // Build parts array (root + subsyncs)
        int partCount = 1 + subSyncCount;
        PropPart[] parts = new PropPart[partCount];

        // Clear temp collections
        _tempPartLookup.Clear();
        _tempRenderers.Clear();
        _tempColliders.Clear();
        _tempRigidbodies.Clear();
        _tempParticleSystems.Clear();
        _tempPickups.Clear();
        _tempAttachments.Clear();
        _tempInteractables.Clear();
        _tempBehaviours.Clear();
        _tempRendererPartIndices.Clear();

        // Root spawnable part
        parts[0] = new PropPart { Transform = spawnableTransform, BoundsCenter = null, BoundsRadius = 0f };
        _tempPartLookup[spawnableTransform] = 0;

        // Subsync parts
        for (int i = 0; i < subSyncCount; i++)
        {
            CVRSpawnableSubSync subSync = subSyncs?[i];
            if (subSync == null) continue;

            Transform subSyncTransform = subSync.transform;
            parts[i + 1] = new PropPart { Transform = subSyncTransform, BoundsCenter = null, BoundsRadius = 0f };
            _tempPartLookup[subSyncTransform] = i + 1;
        }

        // Get all components once and categorize
        Component[] allComponents = propObject.GetComponentsInChildren<Component>(true);
        int componentCount = allComponents.Length;

        for (int i = 0; i < componentCount; i++)
        {
            Component comp = allComponents[i];
            if (!comp) continue;

            if (comp is Renderer renderer)
            {
                _tempRenderers.Add(renderer);
                _tempRendererPartIndices.Add(FindOwningPartIndex(renderer.transform));
            }
            else if (comp is Collider collider)
            {
                _tempColliders.Add(collider);
            }
            else if (comp is Rigidbody rigidbody)
            {
                _tempRigidbodies.Add(rigidbody);
            }
            else if (comp is ParticleSystem particleSystem)
            {
                _tempParticleSystems.Add(particleSystem);
            }
            else if (comp is CVRPickupObject pickup)
            {
                _tempPickups.Add(pickup);
            }
            else if (comp is CVRAttachment attachment)
            {
                _tempAttachments.Add(attachment);
            }
            else if (comp is CVRInteractable interactable)
            {
                _tempInteractables.Add(interactable);
            }
            else if (comp is Behaviour behaviour)
            {
                // Skip the spawnable as it needs to be enabled to sync moving
                if (behaviour is CVRSpawnable)
                    continue;
                
                _tempBehaviours.Add(behaviour);
            }
        }

        // Copy to final arrays
        int rendererCount = _tempRenderers.Count;
        int colliderCount = _tempColliders.Count;
        int rigidbodyCount = _tempRigidbodies.Count;
        int particleSystemCount = _tempParticleSystems.Count;
        int pickupCount = _tempPickups.Count;
        int attachmentCount = _tempAttachments.Count;
        int interactableCount = _tempInteractables.Count;
        int behaviourCount = _tempBehaviours.Count;

        Renderer[] renderers = new Renderer[rendererCount];
        int[] rendererPartIndices = new int[rendererCount];
        Collider[] colliders = new Collider[colliderCount];
        Rigidbody[] rigidbodies = new Rigidbody[rigidbodyCount];
        ParticleSystem[] particleSystems = new ParticleSystem[particleSystemCount];
        CVRPickupObject[] pickups = new CVRPickupObject[pickupCount];
        CVRAttachment[] attachments = new CVRAttachment[attachmentCount];
        CVRInteractable[] interactables = new CVRInteractable[interactableCount];
        Behaviour[] behaviours = new Behaviour[behaviourCount];

        for (int i = 0; i < rendererCount; i++)
        {
            renderers[i] = _tempRenderers[i];
            rendererPartIndices[i] = _tempRendererPartIndices[i];
        }

        for (int i = 0; i < colliderCount; i++)
            colliders[i] = _tempColliders[i];

        for (int i = 0; i < rigidbodyCount; i++)
            rigidbodies[i] = _tempRigidbodies[i];

        for (int i = 0; i < particleSystemCount; i++)
            particleSystems[i] = _tempParticleSystems[i];

        for (int i = 0; i < pickupCount; i++)
            pickups[i] = _tempPickups[i];

        for (int i = 0; i < attachmentCount; i++)
            attachments[i] = _tempAttachments[i];

        for (int i = 0; i < interactableCount; i++)
            interactables[i] = _tempInteractables[i];

        for (int i = 0; i < behaviourCount; i++)
            behaviours[i] = _tempBehaviours[i];

        // Calculate bounds per part in local space
        for (int p = 0; p < partCount; p++)
        {
            PropPart part = parts[p];
            Transform partTransform = part.Transform;
            if (!partTransform)
                continue;

            _tempBoundsList.Clear();

            for (int i = 0; i < rendererCount; i++)
            {
                if (rendererPartIndices[i] != p)
                    continue;

                // We are ignoring particle systems because their bounds are cooked.
                // Their initial bounds seem leftover from whatever was last in-editor.
                Renderer renderer = renderers[i];
                if (!renderer || renderer is ParticleSystemRenderer)
                    continue;
                
                Bounds worldBounds = renderer.bounds;

                // Convert bounds to part's local space
                Vector3 localCenter = partTransform.InverseTransformPoint(worldBounds.center);
                Vector3 localExtents = partTransform.InverseTransformVector(worldBounds.extents);
                localExtents = new Vector3(
                    Mathf.Abs(localExtents.x),
                    Mathf.Abs(localExtents.y),
                    Mathf.Abs(localExtents.z)
                );

                Bounds localBounds = new Bounds(localCenter, localExtents * 2f);
                _tempBoundsList.Add(localBounds);
            }

            int boundsCount = _tempBoundsList.Count;
            if (boundsCount > 0)
            {
                // Combine all renderer local bounds into one
                Bounds combined = _tempBoundsList[0];
                for (int i = 1; i < boundsCount; i++)
                    combined.Encapsulate(_tempBoundsList[i]);

                // Include the prop root as an enforced bounds point.
                // This makes some props which have their contents really far away at least visible on spawn
                // even with an aggressive distance hider configuration. Ex: the big fish ship
                if (p == 0 && ModSettings.EntryIncludePropRootInBounds.Value)
                    combined.Encapsulate(-partTransform.localPosition);
                
                // Create the BoundsCenter object
                GameObject boundsCenterObj = new GameObject("_BoundsCenter");
                Transform boundsCenterObjTransform = boundsCenterObj.transform;
                boundsCenterObjTransform.SetParent(partTransform, false);
                boundsCenterObjTransform.localPosition = combined.center;
                boundsCenterObjTransform.localScale = Vector3.one;

                float radius = 0f;
                Vector3 c = combined.center;
                Vector3 e = combined.extents;
                radius = Mathf.Max(radius, (c + new Vector3( e.x,  e.y,  e.z) - c).magnitude);
                radius = Mathf.Max(radius, (c + new Vector3( e.x,  e.y, -e.z) - c).magnitude);
                radius = Mathf.Max(radius, (c + new Vector3( e.x, -e.y,  e.z) - c).magnitude);
                radius = Mathf.Max(radius, (c + new Vector3( e.x, -e.y, -e.z) - c).magnitude);
                radius = Mathf.Max(radius, (c + new Vector3(-e.x,  e.y,  e.z) - c).magnitude);
                radius = Mathf.Max(radius, (c + new Vector3(-e.x,  e.y, -e.z) - c).magnitude);
                radius = Mathf.Max(radius, (c + new Vector3(-e.x, -e.y,  e.z) - c).magnitude);
                radius = Mathf.Max(radius, (c + new Vector3(-e.x, -e.y, -e.z) - c).magnitude);
                
                part.BoundsRadius = radius;
                part.BoundsCenter = boundsCenterObjTransform;
            }
        }

        // Clear temp collections
        _tempPartLookup.Clear();
        _tempBoundsList.Clear();
        _tempRenderers.Clear();
        _tempColliders.Clear();
        _tempRigidbodies.Clear();
        _tempParticleSystems.Clear();
        _tempPickups.Clear();
        _tempAttachments.Clear();
        _tempInteractables.Clear();
        _tempBehaviours.Clear();
        _tempRendererPartIndices.Clear();

        var cache = new PropCache
        {
            PropData = propData,
            Parts = parts,
            PartCount = partCount,
            Renderers = renderers,
            RendererCount = rendererCount,
            RendererEnabled = new bool[rendererCount],
            Colliders = colliders,
            ColliderCount = colliderCount,
            ColliderEnabled = new bool[colliderCount],
            Rigidbodies = rigidbodies,
            RigidbodyCount = rigidbodyCount,
            RigidbodyWasKinematic = new bool[rigidbodyCount],
            ParticleSystems = particleSystems,
            ParticleSystemCount = particleSystemCount,
            PsWasPlaying = new bool[particleSystemCount],
            PsPlayOnAwake = new bool[particleSystemCount],
            PsTime = new float[particleSystemCount],
            PsStartTime = new double[particleSystemCount],
            PsRandomSeed = new uint[particleSystemCount],
            PsUseAutoSeed = new bool[particleSystemCount],
            Pickups = pickups,
            PickupCount = pickupCount,
            Attachments = attachments,
            AttachmentCount = attachmentCount,
            Interactables = interactables,
            InteractableCount = interactableCount,
            InteractableEnabled = new bool[interactableCount],
            Behaviours = behaviours,
            BehaviourCount = behaviourCount,
            BehaviourEnabled = new bool[behaviourCount],
            AnimWasEnabled = new bool[behaviourCount],
            AnimStartTime = new double[behaviourCount],
            AudioWasPlaying = new bool[behaviourCount],
            AudioLoop = new bool[behaviourCount],
            AudioPlayOnAwake = new bool[behaviourCount],
            AudioStartDsp = new double[behaviourCount],
            AudioStartTime = new float[behaviourCount],
            IsHidden = false
        };

        _propCaches[propData] = cache;
        _propCacheList.Add(cache);
    }

    private static int FindOwningPartIndex(Transform transform)
    {
        Transform current = transform;
        while (current)
        {
            if (_tempPartLookup.TryGetValue(current, out int partIndex))
                return partIndex;
            current = current.parent;
        }
        return 0; // Default to root
    }

    public static void OnPropDestroyed(CVRSyncHelper.PropData propData)
    {
        if (!_propCaches.TryGetValue(propData, out PropCache cache))
            return;

        if (cache.IsHidden)
            ShowProp(cache);

        _propCaches.Remove(propData);
        _propCacheList.Remove(cache);
    }

    public static void Tick()
    {
        HiderMode mode = ModSettings.EntryPropDistanceHiderMode.Value;
        if (mode == HiderMode.Disabled)
            return;

        PlayerSetup playerSetup = PlayerSetup.Instance;
        if (!playerSetup)
            return;

        Camera activeCam = playerSetup.activeCam;
        if (!activeCam)
            return;

        Vector3 playerPos = activeCam.transform.position;
        float hideDistance = ModSettings.EntryPropDistanceHiderDistance.Value;
        float hysteresis = Hysteresis;
        float hideDistSqr = hideDistance * hideDistance;
        float showDist = hideDistance - hysteresis;
        float showDistSqr = showDist * showDist;

        int cacheCount = _propCacheList.Count;
        for (int i = 0; i < cacheCount; i++)
        {
            PropCache cache = _propCacheList[i];
            PropPart[] parts = cache.Parts;
            int partCount = cache.PartCount;

            bool isHidden = cache.IsHidden;
            float threshold = isHidden ? showDistSqr : hideDistSqr;

            bool anyPartValid = false;
            bool anyPartWithinThreshold = false;
            
            // Debug gizmos for all parts
            if (ModSettings.DebugPropMeasuredBounds.Value)
            {
                Quaternion playerRot = activeCam.transform.rotation;
                Vector3 viewOffset = new Vector3(0f, -0.15f, 0f);
                Vector3 debugLineStartPos = playerPos + playerRot * viewOffset;
                for (int p = 0; p < partCount; p++)
                {
                    PropPart part = parts[p];
                    Transform boundsCenter = part.BoundsCenter;

                    if (!boundsCenter)
                        continue;

                    Vector3 boundsCenterPos = boundsCenter.position;

                    // Compute world-space radius based on runtime scale (largest axis)
                    Vector3 lossyScale = boundsCenter.lossyScale;
                    float radius = part.BoundsRadius * Mathf.Max(lossyScale.x, Mathf.Max(lossyScale.y, lossyScale.z));

                    float distToCenter = Vector3.Distance(playerPos, boundsCenterPos);
                    float distToEdge = distToCenter - radius;

                    // RuntimeGizmos DrawSphere is off by 2x
                    Color boundsColor = isHidden ? Color.red : Color.green;
                    RuntimeGizmos.DrawSphere(boundsCenterPos, radius * 2f, boundsColor, opacity: 0.2f);
                    RuntimeGizmos.DrawLineFromTo(debugLineStartPos, boundsCenterPos, 0.01f, boundsColor, opacity: 0.1f);
                    RuntimeGizmos.DrawText(boundsCenterPos, $"{distToEdge:F1}m {(isHidden ? "[Hidden]" : "[Visible]")}", 0.1f, boundsColor, opacity: 1f);
                }
            }

            for (int p = 0; p < partCount; p++)
            {
                PropPart part = parts[p];
                Transform boundsCenter = part.BoundsCenter;

                if (!boundsCenter)
                    continue;

                anyPartValid = true;

                // World-space radius with runtime scale
                Vector3 lossyScale = boundsCenter.lossyScale;
                float radius = part.BoundsRadius * Mathf.Max(lossyScale.x, Mathf.Max(lossyScale.y, lossyScale.z));

                float distToCenter = Vector3.Distance(playerPos, boundsCenter.position);
                float distToEdge = distToCenter - radius;
                float distToEdgeSqr = distToEdge * distToEdge;

                if (distToEdge < 0f)
                    distToEdgeSqr = -distToEdgeSqr;

                if (distToEdgeSqr < threshold)
                {
                    anyPartWithinThreshold = true;
                    break;
                }
            }

            if (!anyPartValid)
            {
                _toRemove.Add(cache.PropData);
                continue;
            }

            if (!isHidden && !anyPartWithinThreshold)
            {
                HideProp(cache);
            }
            else if (isHidden && anyPartWithinThreshold)
            {
                ShowProp(cache);
            }
        }

        int removeCount = _toRemove.Count;
        if (removeCount > 0)
        {
            for (int i = 0; i < removeCount; i++)
            {
                CVRSyncHelper.PropData propData = _toRemove[i];
                if (_propCaches.TryGetValue(propData, out PropCache cache))
                {
                    _propCaches.Remove(propData);
                    _propCacheList.Remove(cache);
                }
            }
            _toRemove.Clear();
        }
    }

    private static void HideProp(PropCache cache)
    {
        cache.IsHidden = true;

        double dsp = AudioSettings.dspTime;
        double time = Time.timeAsDouble;

        // Drop all pickups first to avoid race conditions
        CVRPickupObject[] pickups = cache.Pickups;
        int pickupCount = cache.PickupCount;
        for (int i = 0; i < pickupCount; i++)
        {
            CVRPickupObject pickup = pickups[i];
            if (!pickup) continue;
            pickup.ControllerRay = null;
        }

        // Deattach all attachments before disabling
        CVRAttachment[] attachments = cache.Attachments;
        int attachmentCount = cache.AttachmentCount;
        for (int i = 0; i < attachmentCount; i++)
        {
            CVRAttachment attachment = attachments[i];
            if (!attachment) continue;
            attachment.DeAttach();
        }

        // Disable interactables before other behaviours
        CVRInteractable[] interactables = cache.Interactables;
        bool[] interactableEnabled = cache.InteractableEnabled;
        int interactableCount = cache.InteractableCount;
        for (int i = 0; i < interactableCount; i++)
        {
            CVRInteractable interactable = interactables[i];
            if (!interactable) continue;
            interactableEnabled[i] = interactable.enabled;
            interactable.enabled = false;
        }

        // Snapshot and disable renderers
        Renderer[] renderers = cache.Renderers;
        bool[] rendererEnabled = cache.RendererEnabled;
        int rendererCount = cache.RendererCount;
        for (int i = 0; i < rendererCount; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer) continue;
            rendererEnabled[i] = renderer.enabled;
            renderer.enabled = false;
        }

        // Snapshot and disable colliders
        Collider[] colliders = cache.Colliders;
        bool[] colliderEnabled = cache.ColliderEnabled;
        int colliderCount = cache.ColliderCount;
        for (int i = 0; i < colliderCount; i++)
        {
            Collider collider = colliders[i];
            if (!collider) continue;
            colliderEnabled[i] = collider.enabled;
            collider.enabled = false;
        }

        // Snapshot and set rigidbodies to kinematic
        Rigidbody[] rigidbodies = cache.Rigidbodies;
        bool[] rigidbodyWasKinematic = cache.RigidbodyWasKinematic;
        int rigidbodyCount = cache.RigidbodyCount;
        for (int i = 0; i < rigidbodyCount; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (!rb) continue;
            rigidbodyWasKinematic[i] = rb.isKinematic;
            rb.isKinematic = true;
        }

        // Snapshot and stop particle systems
        ParticleSystem[] particleSystems = cache.ParticleSystems;
        bool[] psWasPlaying = cache.PsWasPlaying;
        bool[] psPlayOnAwake = cache.PsPlayOnAwake;
        float[] psTime = cache.PsTime;
        double[] psStartTime = cache.PsStartTime;
        uint[] psRandomSeed = cache.PsRandomSeed;
        bool[] psUseAutoSeed = cache.PsUseAutoSeed;
        int particleSystemCount = cache.ParticleSystemCount;
        for (int i = 0; i < particleSystemCount; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (!ps) continue;

            var main = ps.main;
            psPlayOnAwake[i] = main.playOnAwake;

            if (ps.isPlaying)
            {
                psWasPlaying[i] = true;
                psTime[i] = ps.time;
                psStartTime[i] = time;
            }
            else
            {
                psWasPlaying[i] = false;
            }

            psUseAutoSeed[i] = ps.useAutoRandomSeed;
            psRandomSeed[i] = ps.randomSeed;

            main.playOnAwake = false;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Snapshot and disable behaviours
        Behaviour[] behaviours = cache.Behaviours;
        bool[] behaviourEnabled = cache.BehaviourEnabled;
        bool[] animWasEnabled = cache.AnimWasEnabled;
        double[] animStartTime = cache.AnimStartTime;
        bool[] audioWasPlaying = cache.AudioWasPlaying;
        bool[] audioLoop = cache.AudioLoop;
        bool[] audioPlayOnAwake = cache.AudioPlayOnAwake;
        double[] audioStartDsp = cache.AudioStartDsp;
        float[] audioStartTime = cache.AudioStartTime;
        int behaviourCount = cache.BehaviourCount;

        for (int i = 0; i < behaviourCount; i++)
        {
            Behaviour b = behaviours[i];
            if (!b) continue;

            behaviourEnabled[i] = b.enabled;

            if (!b.enabled) continue;

            if (b is Animator animator)
            {
                animWasEnabled[i] = true;
                animStartTime[i] = time;
                animator.enabled = false;
                continue;
            }

            if (b is AudioSource audio)
            {
                audioLoop[i] = audio.loop;
                audioPlayOnAwake[i] = audio.playOnAwake;

                if (audio.isPlaying)
                {
                    audioWasPlaying[i] = true;
                    audioStartTime[i] = audio.time;
                    audioStartDsp[i] = dsp - audio.time;
                }
                else
                {
                    audioWasPlaying[i] = false;
                }

                audio.Stop();
                audio.playOnAwake = false;
                audio.enabled = false;
                continue;
            }

            b.enabled = false;
        }
    }

    private static void ShowProp(PropCache cache)
    {
        double dsp = AudioSettings.dspTime;
        double time = Time.timeAsDouble;

        cache.IsHidden = false;

        // Restore renderers
        Renderer[] renderers = cache.Renderers;
        bool[] rendererEnabled = cache.RendererEnabled;
        int rendererCount = cache.RendererCount;
        for (int i = 0; i < rendererCount; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer) continue;
            renderer.enabled = rendererEnabled[i];
        }

        // Restore colliders
        Collider[] colliders = cache.Colliders;
        bool[] colliderEnabled = cache.ColliderEnabled;
        int colliderCount = cache.ColliderCount;
        for (int i = 0; i < colliderCount; i++)
        {
            Collider collider = colliders[i];
            if (!collider) continue;
            collider.enabled = colliderEnabled[i];
        }

        // Restore rigidbodies
        Rigidbody[] rigidbodies = cache.Rigidbodies;
        bool[] rigidbodyWasKinematic = cache.RigidbodyWasKinematic;
        int rigidbodyCount = cache.RigidbodyCount;
        for (int i = 0; i < rigidbodyCount; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (!rb) continue;
            rb.isKinematic = rigidbodyWasKinematic[i];
        }

        // Restore particle systems
        ParticleSystem[] particleSystems = cache.ParticleSystems;
        bool[] psWasPlaying = cache.PsWasPlaying;
        bool[] psPlayOnAwake = cache.PsPlayOnAwake;
        float[] psTime = cache.PsTime;
        double[] psStartTime = cache.PsStartTime;
        uint[] psRandomSeed = cache.PsRandomSeed;
        bool[] psUseAutoSeed = cache.PsUseAutoSeed;
        int particleSystemCount = cache.ParticleSystemCount;
        for (int i = 0; i < particleSystemCount; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (!ps) continue;

            if (psWasPlaying[i])
            {
                ps.useAutoRandomSeed = false;
                ps.randomSeed = psRandomSeed[i];

                float originalTime = psTime[i];
                float elapsed = (float)(time - psStartTime[i]);
                float simulateTime = originalTime + elapsed;

                ps.Simulate(
                    simulateTime,
                    withChildren: true,
                    restart: true,
                    fixedTimeStep: false
                );

                ps.Play(true);

                ps.useAutoRandomSeed = psUseAutoSeed[i];
            }

            var main = ps.main;
            main.playOnAwake = psPlayOnAwake[i];
        }

        // Restore behaviours
        Behaviour[] behaviours = cache.Behaviours;
        bool[] behaviourEnabled = cache.BehaviourEnabled;
        bool[] animWasEnabled = cache.AnimWasEnabled;
        double[] animStartTime = cache.AnimStartTime;
        bool[] audioWasPlaying = cache.AudioWasPlaying;
        bool[] audioLoop = cache.AudioLoop;
        bool[] audioPlayOnAwake = cache.AudioPlayOnAwake;
        double[] audioStartDsp = cache.AudioStartDsp;
        int behaviourCount = cache.BehaviourCount;

        for (int i = 0; i < behaviourCount; i++)
        {
            Behaviour b = behaviours[i];
            if (!b) continue;

            bool wasEnabled = behaviourEnabled[i];

            if (b is Animator animator)
            {
                animator.enabled = wasEnabled;

                if (wasEnabled && animWasEnabled[i])
                {
                    float delta = (float)(time - animStartTime[i]);
                    if (delta > 0f)
                        animator.Update(delta);
                }

                continue;
            }

            if (b is AudioSource audio)
            {
                audio.enabled = wasEnabled;

                if (wasEnabled && audioWasPlaying[i] && audio.clip)
                {
                    double elapsed = dsp - audioStartDsp[i];
                    float clipLen = audio.clip.length;

                    float t = audioLoop[i] ? (float)(elapsed % clipLen) : (float)elapsed;
                    if (t < clipLen)
                    {
                        audio.time = t;
                        audio.Play();
                    }
                }

                audio.playOnAwake = audioPlayOnAwake[i];
                continue;
            }

            b.enabled = wasEnabled;
        }

        // Restore interactables after other behaviours
        CVRInteractable[] interactables = cache.Interactables;
        bool[] interactableEnabled = cache.InteractableEnabled;
        int interactableCount = cache.InteractableCount;
        for (int i = 0; i < interactableCount; i++)
        {
            CVRInteractable interactable = interactables[i];
            if (!interactable) continue;
            interactable.enabled = interactableEnabled[i];
        }
    }

    public static void RefreshAll()
    {
        int count = _propCacheList.Count;
        for (int i = 0; i < count; i++)
        {
            PropCache cache = _propCacheList[i];
            if (cache.IsHidden)
                ShowProp(cache);
        }
    }

    public static void Clear()
    {
        RefreshAll();
        _propCaches.Clear();
        _propCacheList.Clear();
    }
}*/