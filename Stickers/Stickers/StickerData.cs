using ABI_RC.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.Stickers;

public class StickerData
{
    private const float DECAL_SIZE = 0.25f;

    public float DeathTime; // when a remote player leaves, we need to kill their stickers
    
    public Guid TextureHash;
    public float LastPlacedTime;
    
    private Vector3 _lastPlacedPosition = Vector3.zero;

    public readonly bool IsLocal;
    private readonly DecalType _decal;
    private readonly DecalSpawner _spawner;
    private readonly AudioSource _audioSource;

    public StickerData(bool isLocal)
    {
        IsLocal = isLocal;
        
        _decal = ScriptableObject.CreateInstance<DecalType>();
        _decal.decalSettings = new DecalSpawner.InitData
        {
            material = new Material(StickerMod.DecalSimpleShader)
            {
                //color = new Color(Random.value, Random.value, Random.value, 1f),
            },
            useShaderReplacement = false,
            inheritMaterialProperties = false,
            inheritMaterialPropertyBlock = false,
        };
        
        _spawner = DecalManager.GetSpawner(_decal.decalSettings, 4096, 1024);
        
        _audioSource = new GameObject("StickerAudioSource").AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.volume = 0.5f;
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        _audioSource.maxDistance = 5f;
        _audioSource.minDistance = 1f;
        _audioSource.outputAudioMixerGroup = RootLogic.Instance.propSfx; // props are close enough to stickers
        
        if (isLocal) Object.DontDestroyOnLoad(_audioSource.gameObject); // keep audio source through world transitions
    }
    
    public void SetTexture(Guid textureHash, Texture2D texture)
    {
        if (texture == null) StickerMod.Logger.Warning("Assigning null texture to StickerData!");
        
        TextureHash = textureHash;
        
        texture.wrapMode = TextureWrapMode.Clamp; // noachi said to do, prevents white edges
        texture.filterMode = texture.width > 64 || texture.height > 64 
            ? FilterMode.Bilinear // smear it cause its fat
            : FilterMode.Point; // my minecraft skin looked shit

        if (IsLocal) StickerMod.Logger.Msg($"Set texture filter mode to: {texture.filterMode}");
        
        Material material = _decal.decalSettings.material;
        
        // TODO: fix
        if (material.mainTexture != null) Object.Destroy(material.mainTexture);
        material.mainTexture = texture;
    }
    
    public void Place(RaycastHit hit, Vector3 forwardDirection, Vector3 upDirection)
    {
        Transform rootObject = null;
        if (hit.rigidbody != null) rootObject = hit.rigidbody.transform;
        
        _lastPlacedPosition = hit.point;
        LastPlacedTime = Time.time;
        
        // todo: add decal to queue 
        _spawner.AddDecal(
            _lastPlacedPosition, Quaternion.LookRotation(forwardDirection, upDirection), 
            hit.collider.gameObject,
            DECAL_SIZE, DECAL_SIZE, 1f, 1f, 0f, rootObject);
    }
    
    public void Clear()
    {
        // BUG?: Release does not clear dictionary's, clearing them myself so we can hit same objects again
        // maybe it was intended to recycle groups- but rn no work like that
        _spawner.Release();
        _spawner.staticGroups.Clear();
        _spawner.movableGroups.Clear();
    }
    
    public void Cleanup()
    {
        Clear();
        
        // no leaking textures or mats
        Material material = _decal.decalSettings.material;
        Object.Destroy(material.mainTexture);
        Object.Destroy(material);
        Object.Destroy(_decal);
    }
    
    public void PlayAudio()
    {
        _audioSource.transform.position = _lastPlacedPosition;
        switch (ModSettings.Entry_SelectedSFX.Value)
        {
            case ModSettings.SFXType.Source:
                _audioSource.PlayOneShot(StickerMod.SourceSFXPlayerSprayer);
                break;
            case ModSettings.SFXType.LBP:
                _audioSource.PlayOneShot(StickerMod.LittleBigPlanetStickerPlace);
                break;
            case ModSettings.SFXType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public void SetAlpha(float alpha)
    {
        Material material = _decal.decalSettings.material;
        material.color = new Color(material.color.r, material.color.g, material.color.b, alpha);
    }
}