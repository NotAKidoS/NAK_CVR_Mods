using UnityEngine;

namespace NAK.PlapPlapForAll;

public sealed class SkinnedDickFixRoot : MonoBehaviour
{
    private struct Entry
    {
        public Transform root;
        public GameObject lightObject;
        public Renderer[] renderers;
        public int rendererCount;
        public bool lastState;
    }

    private Entry[] _entries;
    private int _entryCount;

    private void Awake()
    {
        _entries = new Entry[4];
        _entryCount = 0;
    }

    public void Register(Renderer renderer, Transform dpsRoot, float length)
    {
        int idx = FindEntry(dpsRoot);
        if (idx < 0)
        {
            idx = _entryCount;
            if (idx == _entries.Length)
            {
                Entry[] old = _entries;
                _entries = new Entry[old.Length << 1];
                Array.Copy(old, _entries, old.Length);
            }

            // TODO: Noachi said light should be at base, but i am too lazy to test it now
            GameObject lightObj = new("[PlapPlapForAllMod] Auto DPS Tip Light");
            lightObj.transform.SetParent(dpsRoot, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, length * 0.5f);
            lightObj.SetActive(false); // Initially off

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 0.49f;
            l.intensity = 0.354f;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForceVertex;
            l.color = new Color(0.003921569f, 0.003921569f, 0.003921569f);

            Entry e;
            e.root = dpsRoot;
            e.lightObject = lightObj;
            e.renderers = new Renderer[2];
            e.rendererCount = 0;
            e.lastState = false;

            _entries[idx] = e;
            _entryCount++;
        }

        ref Entry entry = ref _entries[idx];
        Renderer[] list = entry.renderers;

        for (int i = 0; i < entry.rendererCount; i++)
            if (list[i] == renderer)
                return;

        if (entry.rendererCount == list.Length)
        {
            Renderer[] old = list;
            list = new Renderer[old.Length << 1];
            Array.Copy(old, list, old.Length);
            entry.renderers = list;
        }

        list[entry.rendererCount++] = renderer;
    }

    private int FindEntry(Transform root)
    {
        for (int i = 0; i < _entryCount; i++)
            if (_entries[i].root == root)
                return i;
        return -1;
    }

    private void Update()
    {
        for (int i = 0; i < _entryCount; i++)
        {
            ref Entry entry = ref _entries[i];

            bool active = false;
            Renderer[] list = entry.renderers;
            int count = entry.rendererCount;

            for (int r = 0; r < count; r++)
            {
                Renderer ren = list[r];
                if (!ren) continue;
                if (ren.enabled && ren.gameObject.activeInHierarchy)
                {
                    active = true;
                    break;
                }
            }

            if (active != entry.lastState)
            {
                entry.lastState = active;
                entry.lightObject.SetActive(active);
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < _entryCount; i++)
        {
            ref Entry entry = ref _entries[i];
            entry.lightObject.SetActive(false);
            entry.lastState = false;
        }
    }
}