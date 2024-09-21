using ABI_RC.Core;
using ABI_RC.Core.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.Stickers
{
    public class StickerData
    {
        private const float DECAL_SIZE = 0.25f;
        
        public readonly string PlayerId;
        public float LastPlacedTime;
        public float DeathTime = -1f;
        public float IdentifyTime = -1f;
        
        private Vector3 _lastPlacedPosition = Vector3.zero;

        private readonly DecalSpawner[] _decalSpawners;
        
        private readonly Guid[] _textureHashes;
        private readonly Material[] _materials;
        private readonly AudioSource _audioSource;

        public StickerData(string playerId, int decalSpawnersCount)
        {
            PlayerId = playerId;

            _decalSpawners = new DecalSpawner[decalSpawnersCount];
            _materials = new Material[decalSpawnersCount];
            _textureHashes = new Guid[decalSpawnersCount];

            for (int i = 0; i < decalSpawnersCount; i++)
            {
                _materials[i] = new Material(StickerMod.DecalSimpleShader);
                DecalSpawner.InitData decalSettings = new()
                {
                    material = _materials[i],
                    useShaderReplacement = false,
                    inheritMaterialProperties = false,
                    inheritMaterialPropertyBlock = false,
                };
                _decalSpawners[i] = DecalManager.GetSpawner(decalSettings, 4096, 1024);
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
            
            // this is a hack so judge and fuck off lol
            if (PlayerId == StickerSystem.PlayerLocalId)
            {
                Object.DontDestroyOnLoad(_audioSource.gameObject); // keep audio source through world transitions

                _previewMaterial = new Material(StickerMod.DecalSimpleShader);
                _previewDecalSpawner = DecalManager.GetSpawner(new DecalSpawner.InitData
                {
                    material = _previewMaterial, // default material
                    useShaderReplacement = false,
                    inheritMaterialProperties = false,
                    inheritMaterialPropertyBlock = false,
                }, 4096, 1024);
            }
        }
        
        // public Guid GetTextureHash(int spawnerIndex = 0)
        // {
        //     if (spawnerIndex < 0 || spawnerIndex >= _decalSpawners.Length)
        //     {
        //         StickerMod.Logger.Warning("Invalid spawner index!");
        //         return Guid.Empty;
        //     }
        //
        //     return _textureHashes[spawnerIndex];
        // }
        
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

            Material material = _materials[spawnerIndex];

            // destroy the previous texture to avoid memory leaks
            if (material.mainTexture != null) Object.Destroy(material.mainTexture);
            material.mainTexture = texture;

            // update the preview as well i guess cause lame
            if (_previewMaterial != null && _previewSpawnerIndex != spawnerIndex)
                _previewMaterial.mainTexture = texture;
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
            if (hitGO.scene.buildIndex == 4 // additive (dynamic) content
                || hitGO.GetComponentInParent<Animator>() != null // potentially movable
                || hitGO.GetComponentInParent<Rigidbody>() != null) // movable
                rootObject = hitGO.transform;
            
            _lastPlacedPosition = hit.point;
            LastPlacedTime = Time.time;

            // Add decal to the specified spawner
            _decalSpawners[spawnerIndex].AddDecal(
                _lastPlacedPosition, Quaternion.LookRotation(forwardDirection, upDirection),
                hitGO,
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
                if (_materials[i] == null) continue;
                if (_materials[i].mainTexture != null) Object.Destroy(_materials[i].mainTexture);
                Object.Destroy(_materials[i]);
            }
            
            if (_audioSource != null) Object.Destroy(_audioSource.gameObject);
            if (_previewDecalSpawner != null) // local player only
            {
                _previewDecalSpawner.Release();
                _previewDecalSpawner.staticGroups.Clear();
                _previewDecalSpawner.movableGroups.Clear();
                Object.Destroy(_previewMaterial);
            }
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
        
        #region Sticker Preview
        
        private const float FLASH_FREQUENCY = 2f;
        private readonly DecalSpawner _previewDecalSpawner;
        private readonly Material _previewMaterial;
        private int _previewSpawnerIndex = -1;
        private float _flashTime;
        
        public void PlacePreview(RaycastHit hit, Vector3 forwardDirection, Vector3 upDirection, int spawnerIndex = 0)
        {
            if (spawnerIndex < 0 || spawnerIndex >= _decalSpawners.Length)
            {
                StickerMod.Logger.Warning("Invalid spawner index for preview!");
                return;
            }
            
            if (_previewDecalSpawner == null)
                return; // uh fuck

            // place at hit pos
            Transform rootObject = null;
            GameObject hitGO = hit.transform.gameObject;
            if (hitGO.scene.buildIndex == 4 // additive (dynamic) content
                || hitGO.GetComponentInParent<Animator>() != null // potentially movable
                || hitGO.GetComponentInParent<Rigidbody>() != null) // movable
                rootObject = hitGO.transform;

            _previewDecalSpawner.AddDecal(
                hit.point, Quaternion.LookRotation(forwardDirection, upDirection),
                hitGO,
                DECAL_SIZE, DECAL_SIZE, 1f, 1f, 0f, rootObject);
        }

        public void UpdatePreview(int spawnerIndex)
        {
            if (_previewSpawnerIndex != spawnerIndex)
            {
                _previewSpawnerIndex = spawnerIndex; // update the preview image
                _previewMaterial.mainTexture = _materials[spawnerIndex].mainTexture;
            }

            _flashTime += Time.deltaTime;
            float baseAlpha = (Mathf.Sin(_flashTime * 2f * Mathf.PI / FLASH_FREQUENCY) + 1f) / 2f;
            float alpha = Mathf.Lerp(0.5f, 0.8f, baseAlpha); // 50% to 80% alpha

            Color color = _previewMaterial.color;
            color.a = alpha;
            _previewMaterial.color = color;
        }

        public void ClearPreview()
        {
            if (_previewDecalSpawner == null) 
                return;
            
            _previewDecalSpawner.Release();
            _previewDecalSpawner.staticGroups.Clear();
            _previewDecalSpawner.movableGroups.Clear();
        }
    
        #endregion Sticker Preview
    }
}