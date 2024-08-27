using ABI_RC.Core;
using ABI_RC.Core.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.Stickers
{
    public class StickerData
    {
        private const float DECAL_SIZE = 0.25f;

        private static readonly int s_EmissionStrengthID = Shader.PropertyToID("_EmissionStrength");

        public float DeathTime; // when a remote player leaves, we need to kill their stickers
        public float LastPlacedTime;
        private Vector3 _lastPlacedPosition = Vector3.zero;

        public readonly bool IsLocal;
        private readonly DecalType _decal;
        private readonly DecalSpawner[] _decalSpawners;
        
        private readonly Guid[] _textureHashes;
        private readonly Material[] _materials;
        private readonly AudioSource _audioSource;

        public StickerData(bool isLocal, int decalSpawnersCount)
        {
            IsLocal = isLocal;

            _decal = ScriptableObject.CreateInstance<DecalType>();
            _decalSpawners = new DecalSpawner[decalSpawnersCount];
            _materials = new Material[decalSpawnersCount];
            _textureHashes = new Guid[decalSpawnersCount];

            for (int i = 0; i < decalSpawnersCount; i++)
            {
                _materials[i] = new Material(StickerMod.DecalSimpleShader);
                _decal.decalSettings = new DecalSpawner.InitData
                {
                    material = _materials[i],
                    useShaderReplacement = false,
                    inheritMaterialProperties = false,
                    inheritMaterialPropertyBlock = false,
                };
                _decalSpawners[i] = DecalManager.GetSpawner(_decal.decalSettings, 4096, 1024);
            }

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
        
        public Guid GetTextureHash(int spawnerIndex = 0)
        {
            if (spawnerIndex < 0 || spawnerIndex >= _decalSpawners.Length)
            {
                StickerMod.Logger.Warning("Invalid spawner index!");
                return Guid.Empty;
            }

            return _textureHashes[spawnerIndex];
        }
        
        public bool CheckHasTextureHash(Guid textureHash)
        {
            foreach (Guid hash in _textureHashes) if (hash == textureHash) return true;
            return false;
        }

        public void SetTexture(Guid textureHash, Texture2D texture, int spawnerIndex = 0)
        {
            if (spawnerIndex < 0 || spawnerIndex >= _decalSpawners.Length)
            {
                StickerMod.Logger.Warning("Invalid spawner index!");
                return;
            }

            if (texture == null)
            {
                StickerMod.Logger.Warning("Assigning null texture to StickerData!");
                return;
            }

            _textureHashes[spawnerIndex] = textureHash;

            texture.wrapMode = TextureWrapMode.Clamp; // prevents white edges
            texture.filterMode = texture.width > 64 || texture.height > 64
                ? FilterMode.Bilinear // smear it cause its fat
                : FilterMode.Point; // my minecraft skin looked shit

            if (IsLocal) StickerMod.Logger.Msg($"Set texture filter mode to: {texture.filterMode}");

            Material material = _materials[spawnerIndex];

            // Destroy the previous texture to avoid memory leaks
            if (material.mainTexture != null) Object.Destroy(material.mainTexture);
            material.mainTexture = texture;
        }

        public void Place(RaycastHit hit, Vector3 forwardDirection, Vector3 upDirection, int spawnerIndex = 0)
        {
            if (spawnerIndex < 0 || spawnerIndex >= _decalSpawners.Length)
            {
                StickerMod.Logger.Warning("Invalid spawner index!");
                return;
            }

            // for performance it is best to let them batch, but we also want movable objects to always
            // be decaled instead of floating in-scene (so avatars/props are always considered movable)
            
            Transform rootObject = null;
            GameObject hitGO = hit.transform.gameObject;
            if (hitGO.TryGetComponent(out Rigidbody _) 
                || hitGO.TryGetComponent(out Animator _)
                || hitGO.scene.buildIndex == 4) // additive (dynamic) content
                rootObject = hitGO.transform;
            
            _lastPlacedPosition = hit.point;
            LastPlacedTime = Time.time;

            // Add decal to the specified spawner
            _decalSpawners[spawnerIndex].AddDecal(
                _lastPlacedPosition, Quaternion.LookRotation(forwardDirection, upDirection),
                hit.collider.gameObject,
                DECAL_SIZE, DECAL_SIZE, 1f, 1f, 0f, rootObject);
        }

        public void Clear()
        {
            foreach (DecalSpawner spawner in _decalSpawners)
            {
                spawner.Release();
                spawner.staticGroups.Clear();
                spawner.movableGroups.Clear();
            }
        }
        
        public void Clear(int spawnerIndex)
        {
            if (spawnerIndex < 0 || spawnerIndex >= _decalSpawners.Length)
            {
                StickerMod.Logger.Warning("Invalid spawner index!");
                return;
            }

            _decalSpawners[spawnerIndex].Release();
            _decalSpawners[spawnerIndex].staticGroups.Clear();
            _decalSpawners[spawnerIndex].movableGroups.Clear();
        }

        public void Cleanup()
        {
            for (int i = 0; i < _decalSpawners.Length; i++)
            {
                _decalSpawners[i].Release();
                _decalSpawners[i].staticGroups.Clear();
                _decalSpawners[i].movableGroups.Clear();

                // Clean up textures and materials
                if (_materials[i].mainTexture != null) Object.Destroy(_materials[i].mainTexture);
                Object.Destroy(_materials[i]);
            }

            Object.Destroy(_decal);
        }

        public void PlayAudio()
        {
            _audioSource.transform.position = _lastPlacedPosition;
            switch (ModSettings.Entry_SelectedSFX.Value)
            {
                case SFXType.SourceEngineSpray:
                    _audioSource.PlayOneShot(StickerMod.SourceSFXPlayerSprayer);
                    break;
                case SFXType.LittleBigPlanetSticker:
                    _audioSource.PlayOneShot(StickerMod.LittleBigPlanetSFXStickerPlace);
                    break;
                case SFXType.FactorioAlertDestroyed:
                    _audioSource.PlayOneShot(StickerMod.FactorioSFXAlertDestroyed);
                    break;
                case SFXType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetAlpha(float alpha)
        {
            foreach (Material material in _materials)
            {
                if (material == null) continue;
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }

        #region shitty identify
        
        public void Identify()
        {
            Color color = Color.HSVToRGB(Time.time % 1f, 1f, 1f);
            foreach (Material material in _materials)
                material.color = color; // cycle rainbow
        }
        
        public void ResetIdentify()
        {
            foreach (Material material in _materials)
                material.color = Color.white;
        }
        
        #endregion shitty identify
    }
}