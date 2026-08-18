using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NAK.CleanPlates.Build
{
    public static class PrefabTools
    {
        private const string PrefabDir = "Packages/com.nak.cleanplates/Runtime/Prefabs";
        private const int Supersample = 2;

        private struct DemoUser
        {
            public string Name;
            public string Status;
            public string RankTag;
            public bool IsNewUser;
            public bool IsFriend;
            public Color Primary;
            public Color Secondary;
            public Color RankColor;
            public Texture2D Icon;
        }

        private struct Permutation
        {
            public GameObject Prefab;
            public DemoUser User;
            public bool ShowIconSlot;
            public float Opacity;
            public NAK.CleanPlates.UI.RoundedHexGraphic.Shape Shape;
            public string Caption;
            public bool Collapsed;
            public string[] Messages;
            public NAK.CleanPlates.UI.ChatMessageKind MessageKind;
            public string SourceLabel;
            public bool Typing;
            public bool Speaking;
            public bool Tts;
            public float Scale;
        }

        public static void RenderReadmeImagesFromCommandLine()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "-imageOut");
                string outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : "ReadmeImages";
                i = Array.IndexOf(args, "-pfpDir");
                string pfpDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
                Directory.CreateDirectory(outDir);

                var camGo = new GameObject("RenderCam", typeof(Camera));
                Camera cam = camGo.GetComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.transform.position = new Vector3(0f, 0f, -100f);
                cam.nearClipPlane = 1f;
                cam.farClipPlane = 200f;

                var canvasGo = new GameObject("RenderCanvas", typeof(Canvas));
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                GameObject full = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/FullPlate.prefab");
                GameObject simple = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/SimplePlate.prefab");
                GameObject camera = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/CameraPlate.prefab");
                referenceRootScale = full.transform.localScale.x;

                var slime = new DemoUser
                {
                    Name = "Slime", Status = "ApiException: Failed to load Pro",
                    Primary = new Color(0.5098f, 0.7843f, 0.898f), Secondary = new Color(0f, 0.9333f, 1f),
                    Icon = LoadPfp(pfpDir, "slime"),
                };
                var akebono = new DemoUser
                {
                    Name = "DDAkebono", Status = "Cat?",
                    Primary = new Color(0.616f, 0.035f, 0.906f), Secondary = new Color(0.498f, 0f, 0.078f),
                    RankTag = "DEV", RankColor = new Color(0.9412f, 0f, 0.1569f, 0.1961f),
                    Icon = LoadPfp(pfpDir, "ddakebono"),
                };
                var notakid = new DemoUser
                {
                    Name = "NotAKid", Status = "subject to change without notice",
                    Primary = new Color(1f, 0f, 1f), Secondary = new Color(0f, 1f, 1f),
                    Icon = LoadPfp(pfpDir, "notakid"),
                };
                var exterrata = new DemoUser
                {
                    Name = "Exterrata", Status = "She/Her",
                    Primary = new Color(0.3569f, 0.8118f, 0.9804f), Secondary = new Color(0.9608f, 0.6706f, 0.7255f),
                    Icon = LoadPfp(pfpDir, "exterrata"),
                };
                var kjoy = new DemoUser
                {
                    Name = "kjoy", Status = "black/man",
                    Primary = new Color(0f, 0.9333f, 1f), Secondary = new Color(0.6f, 0.4f, 0.8f),
                    RankTag = "MOD", RankColor = new Color(0.8667f, 0f, 0.4627f, 0.1961f),
                    Icon = LoadPfp(pfpDir, "kjoy"),
                };
                var miyoshi = new DemoUser
                {
                    Name = "Miyoshi 0HEf", IsNewUser = true,
                    Primary = new Color(0.894f, 0.212f, 0f), Secondary = new Color(1f, 0.992f, 0.988f),
                    Icon = LoadPfp(pfpDir, "miyoshi"),
                };

                const NAK.CleanPlates.UI.RoundedHexGraphic.Shape hex = NAK.CleanPlates.UI.RoundedHexGraphic.Shape.Hexagonal;
                const NAK.CleanPlates.UI.RoundedHexGraphic.Shape squircle = NAK.CleanPlates.UI.RoundedHexGraphic.Shape.Squircle;
                const NAK.CleanPlates.UI.RoundedHexGraphic.Shape circle = NAK.CleanPlates.UI.RoundedHexGraphic.Shape.Circle;

                Write(outDir, "shapes.png", 3, new[]
                {
                    new Permutation { Prefab = full, User = notakid, ShowIconSlot = true, Opacity = 1f, Shape = hex, Caption = "Hexagonal" },
                    new Permutation { Prefab = full, User = slime, ShowIconSlot = true, Opacity = 1f, Shape = squircle, Caption = "Squircle" },
                    new Permutation { Prefab = full, User = exterrata, ShowIconSlot = true, Opacity = 1f, Shape = circle, Caption = "Circle" },
                }, canvas.transform, cam);

                const float cameraScale = 2.2f;

                DemoUser monogram = notakid;
                monogram.Icon = null;
                DemoUser friend = exterrata;
                friend.IsFriend = true;

                Write(outDir, "styles.png", 4, new[]
                {
                    new Permutation { Prefab = full, User = akebono, ShowIconSlot = true, Opacity = 1f, Shape = hex, Caption = "Full - Hexagonal" },
                    new Permutation { Prefab = simple, User = kjoy, ShowIconSlot = true, Opacity = 1f, Shape = hex, Caption = "Compact - Hexagonal" },
                    new Permutation { Prefab = simple, User = slime, ShowIconSlot = false, Opacity = 1f, Shape = squircle, Caption = "Minimal - Squircle" },
                    new Permutation { Prefab = simple, User = notakid, ShowIconSlot = false, Opacity = 0f, Shape = circle, Caption = "Minimal - 0% opacity" },
                    new Permutation { Prefab = full, User = miyoshi, ShowIconSlot = true, Opacity = 1f, Shape = circle, Caption = "Full - New User" },
                    new Permutation { Prefab = full, User = friend, ShowIconSlot = true, Opacity = 1f, Shape = squircle, Caption = "Full - Friend" },
                    new Permutation { Prefab = full, User = monogram, ShowIconSlot = true, Opacity = 1f, Shape = hex, Caption = "Full - No profile image" },
                    new Permutation { Prefab = simple, User = friend, ShowIconSlot = true, Opacity = 0.5f, Shape = hex, Caption = "Compact - Friend - 50% opacity" },
                }, canvas.transform, cam);

                Write(outDir, "states.png", 3, new[]
                {
                    new Permutation { Prefab = full, User = akebono, ShowIconSlot = true, Opacity = 1f, Shape = hex, Collapsed = true, Caption = "Plate at distance, collapsed" },
                    new Permutation { Prefab = camera, User = kjoy, Opacity = 1f, Shape = hex, Scale = cameraScale, Caption = "Camera indicator" },
                    new Permutation { Prefab = camera, User = kjoy, Opacity = 1f, Shape = hex, Collapsed = true, Scale = cameraScale, Caption = "Camera indicator, at distance" },
                }, canvas.transform, cam);

                Write(outDir, "chat.png", 3, new[]
                {
                    new Permutation { Prefab = full, User = exterrata, ShowIconSlot = true, Opacity = 1f, Shape = hex,
                        Messages = new[] { "the shapes are configurable btw" }, Caption = "Chat message" },
                    new Permutation { Prefab = full, User = akebono, ShowIconSlot = true, Opacity = 1f, Shape = hex,
                        Messages = new[] { "third", "second", "newest one sits at the bottom" }, Caption = "Message history" },
                    new Permutation { Prefab = full, User = notakid, ShowIconSlot = true, Opacity = 1f, Shape = hex,
                        Messages = new[] { "sent from an osc app" }, MessageKind = NAK.CleanPlates.UI.ChatMessageKind.OSC,
                        SourceLabel = "OSC", Caption = "OSC message" },
                    new Permutation { Prefab = full, User = slime, ShowIconSlot = true, Opacity = 1f, Shape = hex,
                        Messages = new[] { "sent by another mod" }, MessageKind = NAK.CleanPlates.UI.ChatMessageKind.Mod,
                        SourceLabel = "Mod", Caption = "Mod message" },
                    new Permutation { Prefab = simple, User = kjoy, ShowIconSlot = true, Opacity = 1f, Shape = hex,
                        Typing = true, Speaking = true, Caption = "Typing and speaking" },
                    new Permutation { Prefab = simple, User = miyoshi, ShowIconSlot = true, Opacity = 1f, Shape = hex,
                        Tts = true, Caption = "Text to speech" },
                }, canvas.transform, cam);
                Debug.Log($"Rendered readme images to {outDir}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"Render failed: {e}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static float referenceRootScale = 1f;

        private static Texture2D LoadPfp(string dir, string name)
        {
            if (dir == null) return null;
            string path = Path.Combine(dir, name + ".png");
            if (!File.Exists(path)) return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        private static void Write(string outDir, string file, int columns,
            Permutation[] permutations, Transform parent, Camera cam, Color backdrop = default)
        {
            var cells = new Texture2D[permutations.Length];
            for (int i = 0; i < permutations.Length; i++)
            {
                Texture2D plate = Crop(RenderPlate(permutations[i], parent, cam), 12 * Supersample);
                Texture2D caption = Crop(RenderCaption(permutations[i].Caption, parent, cam), 2 * Supersample);
                cells[i] = StackVertical(plate, caption, 22 * Supersample);
            }

            Texture2D sheet = Compose(cells, columns, 64 * Supersample, 52 * Supersample);
            if (backdrop.a > 0f) Flatten(sheet, backdrop);
            File.WriteAllBytes(Path.Combine(outDir, file), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        private static void Flatten(Texture2D sheet, Color backdrop)
        {
            Color[] pixels = sheet.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float a = pixels[i].a;
                pixels[i] = new Color(
                    Mathf.Lerp(backdrop.r, pixels[i].r, a),
                    Mathf.Lerp(backdrop.g, pixels[i].g, a),
                    Mathf.Lerp(backdrop.b, pixels[i].b, a),
                    1f);
            }
            sheet.SetPixels(pixels);
            sheet.Apply();
        }

        private static Texture2D StackVertical(Texture2D top, Texture2D bottom, int gap)
        {
            int width = Mathf.Max(top.width, bottom.width);
            int height = top.height + gap + bottom.height;
            var cell = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cell.SetPixels32(new Color32[width * height]);
            cell.SetPixels((width - top.width) / 2, gap + bottom.height, top.width, top.height, top.GetPixels());
            cell.SetPixels((width - bottom.width) / 2, 0, bottom.width, bottom.height, bottom.GetPixels());
            cell.Apply();
            UnityEngine.Object.DestroyImmediate(top);
            UnityEngine.Object.DestroyImmediate(bottom);
            return cell;
        }

        private static void InvokeAwake(Component component)
            => component.GetType()
                .GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(component, null);

        private static void SetupPlate(NAK.CleanPlates.UI.NameplateView view, Permutation permutation)
        {
            DemoUser user = permutation.User;
            NAK.CleanPlates.UI.NameplateView.ShowIconSlot = permutation.ShowIconSlot;
            NAK.CleanPlates.UI.NameplateView.BackgroundOpacity = permutation.Opacity;
            var data = new NAK.CleanPlates.UI.NameplateData
            {
                Username = user.Name,
                Pronouns = string.Empty,
                Status = user.Status,
                StatusKind = NAK.CleanPlates.UI.NameplateStatusKind.None,
                Icon = user.Icon,
                IsNewUser = user.IsNewUser,
                IsFriend = user.IsFriend,
                RankTag = user.RankTag,
                RankColor = user.RankColor,
                PrimaryColor = user.Primary,
                SecondaryColor = user.Secondary,
            };

            InvokeAwake(view);
            view.Bind(data);
            view.SetState(1f, permutation.Collapsed ? 0f : 1f, permutation.Collapsed ? 0f : 1f);

            NAK.CleanPlates.UI.NameplateChat chat = view.Chat;
            bool wantsChat = permutation.Messages != null || permutation.Typing
                             || permutation.Speaking || permutation.Tts;
            if (!wantsChat)
            {
                chat.gameObject.SetActive(false);
                return;
            }

            InvokeAwake(chat);
            chat.SetPlayerColors(user.Primary, user.Secondary);
            chat.SetBackgroundOpacity(permutation.Opacity);
            chat.SetBubbleScale(1f);
            chat.SetOpacity(1f);
            chat.SetDetail(1f);

            if (permutation.Messages != null)
                foreach (string message in permutation.Messages)
                    chat.PushMessage(permutation.MessageKind, message, false, permutation.SourceLabel);

            if (permutation.Typing)
            {
                chat.SetTyping(true);
                Transform typing = chat.transform.Find("Typing");
                foreach (Transform child in typing)
                    if (child.name.StartsWith("Dot")) child.gameObject.SetActive(true);
            }
            if (permutation.Tts) chat.SetPlayingTts(10f);
            if (permutation.Speaking) chat.SetVoiceLevel(1f);
        }

        private static void SetupCameraPlate(NAK.CleanPlates.UI.MiniNameplate mini, Permutation permutation)
        {
            InvokeAwake(mini);
            mini.Bind(permutation.User.Name, permutation.User.Primary, permutation.User.Secondary);
            mini.SetBackgroundOpacity(permutation.Opacity);
            mini.SetState(1f, permutation.Collapsed ? 0f : 1f);
        }

        private static Texture2D RenderPlate(Permutation permutation, Transform parent, Camera cam)
        {
            const int width = 1100, height = 800;
            NAK.CleanPlates.UI.RoundedHexGraphic.SetPreferredShape(permutation.Shape);

            GameObject instance = UnityEngine.Object.Instantiate(permutation.Prefab, parent, false);
            instance.transform.localPosition = Vector3.zero;
            float relative = permutation.Prefab.transform.localScale.x / referenceRootScale;
            instance.transform.localScale =
                Vector3.one * relative * (permutation.Scale <= 0f ? 1f : permutation.Scale);

            var mini = instance.GetComponent<NAK.CleanPlates.UI.MiniNameplate>();
            if (mini != null) SetupCameraPlate(mini, permutation);
            else SetupPlate(instance.GetComponent<NAK.CleanPlates.UI.NameplateView>(), permutation);

            Texture2D tile = Capture(parent, cam, width, height);
            UnityEngine.Object.DestroyImmediate(instance);
            return tile;
        }

        private static Texture2D RenderCaption(string caption, Transform parent, Camera cam)
        {
            var go = new GameObject("Caption", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(1000f, 44f);
            rect.anchoredPosition = Vector2.zero;

            var text = go.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = caption;
            text.fontSize = 26f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = new Color(0.62f, 0.64f, 0.69f);

            Texture2D tile = Capture(parent, cam, 1100, 120);
            UnityEngine.Object.DestroyImmediate(go);
            return tile;
        }

        private static Texture2D Capture(Transform parent, Camera cam, int width, int height)
        {
            foreach (var graphic in parent.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.SetAllDirty();
            foreach (var text in parent.GetComponentsInChildren<TMPro.TMP_Text>(true))
                text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();

            int pixelWidth = width * Supersample;
            int pixelHeight = height * Supersample;

            var rt = RenderTexture.GetTemporary(pixelWidth, pixelHeight, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = (float)width / height;
            cam.orthographicSize = height * 0.5f;
            cam.Render();

            RenderTexture.active = rt;
            var tile = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
            tile.ReadPixels(new Rect(0, 0, pixelWidth, pixelHeight), 0, 0);
            tile.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            return tile;
        }

        private static Texture2D Crop(Texture2D source, int padding)
        {
            Color32[] pixels = source.GetPixels32();
            int minX = source.width, minY = source.height, maxX = -1, maxY = -1;
            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    if (pixels[y * source.width + x].a == 0) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return source;

            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(source.width - 1, maxX + padding);
            maxY = Mathf.Min(source.height - 1, maxY + padding);

            int w = maxX - minX + 1, h = maxY - minY + 1;
            var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cropped.SetPixels(source.GetPixels(minX, minY, w, h));
            cropped.Apply();
            UnityEngine.Object.DestroyImmediate(source);
            return cropped;
        }

        private static Texture2D Compose(Texture2D[] tiles, int columns, int gapX, int gapY)
        {
            int rows = Mathf.CeilToInt(tiles.Length / (float)columns);
            var rowWidths = new int[rows];
            var rowHeights = new int[rows];
            for (int i = 0; i < tiles.Length; i++)
            {
                int row = i / columns;
                rowWidths[row] += tiles[i].width + (i % columns == 0 ? 0 : gapX);
                rowHeights[row] = Mathf.Max(rowHeights[row], tiles[i].height);
            }

            int sheetW = 0, sheetH = 0;
            for (int r = 0; r < rows; r++)
            {
                sheetW = Mathf.Max(sheetW, rowWidths[r]);
                sheetH += rowHeights[r] + (r == 0 ? 0 : gapY);
            }

            var sheet = new Texture2D(sheetW, sheetH, TextureFormat.RGBA32, false);
            var clear = new Color32[sheetW * sheetH];
            sheet.SetPixels32(clear);

            int y = sheetH;
            for (int r = 0; r < rows; r++)
            {
                y -= rowHeights[r] + (r == 0 ? 0 : gapY);
                int x = (sheetW - rowWidths[r]) / 2;
                for (int c = 0; c < columns; c++)
                {
                    int i = r * columns + c;
                    if (i >= tiles.Length) break;
                    Texture2D tile = tiles[i];
                    sheet.SetPixels(x, y, tile.width, tile.height, tile.GetPixels());
                    x += tile.width + gapX;
                    UnityEngine.Object.DestroyImmediate(tile);
                }
            }
            sheet.Apply();
            return sheet;
        }

    }
}