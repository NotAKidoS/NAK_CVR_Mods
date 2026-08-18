// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NAK.CleanPlates.UI
{
    // DO NOT TOUCH, OTHERWISE MAY BREAK PREFAB
    [RequireComponent(typeof(CanvasRenderer))]
    public class RoundedHexGraphic : MaskableGraphic
    {
        public enum Shape { Hexagonal, Squircle, Circle }

        // Inset is how far the cap cuts in at the top edge as a fraction of height,
        // aspect is the width for that height.
        private readonly struct Profile
        {
            public readonly float Inset;
            public readonly float Aspect;
            public Profile(float inset, float aspect) { Inset = inset; Aspect = aspect; }
        }

        private static readonly Profile[] Profiles =
        {
            new(0.3365f, 99f / 90f), // Hexagon, wider than tall or it reads as a blob
            new(0.25f, 1f),          // Squircle, flat run down the sides
            new(0.5f, 1f),           // Circle, all corner, stadium when stretched
        };

        // Assigning this directly leaves live graphics on the old geometry.
        public static Shape PreferredShape = Shape.Hexagonal;

        private static Profile Current => Profiles[(int)PreferredShape];

        // Text laid out inside one of these pads by CapInset, or a rounder shape clips
        // it.
        public static float CapInset => Current.Inset;

        public static float CapInsetFor(Shape shape) => Profiles[(int)shape].Inset;

        [SerializeField] private bool overrideShape;
        [SerializeField] private Shape shapeOverride = Shape.Squircle;

        public Shape ActiveShape => overrideShape ? shapeOverride : PreferredShape;
        private Profile ActiveProfile => Profiles[(int)ActiveShape];
        public float ActiveCapInset => ActiveProfile.Inset;

        // Text meets the slanted edge across its own height, not the pill's.
        public static float InsetFor(float height, float padding)
            => height * CapInset * 0.5f + padding;
        public static float ShapeAspect => Current.Aspect;

        // A 30 degree slant with a circular arc at either end.
        private const float SlantTangent = 0.5773503f;
        private const float SlantOffset = 0.0321376f;
        private const float TipRadius = 0.2077410f;
        private const float TipEnd = 0.1038705f;
        private const float ShoulderRadius = 0.1847000f;
        private const float ShoulderCenterX = 0.3644097f;
        private const float ShoulderCenterY = 0.3174400f;
        private const float ShoulderStart = 0.4097900f;

        [SerializeField] private bool flatBottom;

        [SerializeField] private float profileScaleHeight;
        [SerializeField, Range(8, 64)] private int capSamples = 36;
        [SerializeField] private Texture texture;
        [SerializeField] private Rect uvRect = new(0f, 0f, 1f, 1f);
        [SerializeField] private Color accentLeft = Color.clear;
        [SerializeField] private Color accentRight = Color.clear;
        [SerializeField] private float accentWidth = 190f;
        // Draws a hollow band of this width, the only option without a stencil.
        [SerializeField] private float outlineThickness;
        // Drops the bottom edge into a point for the chat bubble tail, tapering out
        // before the caps.
        [SerializeField] private float bottomPoint;
        // Width of the tail window, outside which the bottom edge stays flat.
        [SerializeField] private float bottomPointWidth = 24f;

        public void SetBottomPoint(float depth)
        {
            if (Mathf.Approximately(bottomPoint, depth)) return;
            bottomPoint = depth;
            SetVerticesDirty();
        }

        private float TailHalfWidth(Rect r)
            => Mathf.Min(bottomPointWidth, r.width) * 0.5f;

        private float BottomDrop(float x, Rect r)
        {
            if (bottomPoint <= 0f || r.width <= 0f) return 0f;

            float halfW = TailHalfWidth(r);
            float distance = Mathf.Abs(x - r.center.x);
            if (halfW <= 0f || distance >= halfW) return 0f;
            return bottomPoint * (1f - distance / halfW);
        }

        public override Texture mainTexture => texture != null ? texture : s_WhiteTexture;

        public Texture Texture
        {
            get => texture;
            set { texture = value; SetMaterialDirty(); }
        }

        public static void SetPreferredShape(Shape shape)
        {
            if (PreferredShape == shape) return;

            PreferredShape = shape;
            ShapeAspectFitter.ApplyAll();
            foreach (RoundedHexGraphic graphic in live)
                if (!graphic.overrideShape) graphic.SetVerticesDirty();
        }

        public void SetAccents(Color left, Color right)
        {
            accentLeft = left;
            accentRight = right;
            SetVerticesDirty();
        }

        private static readonly List<RoundedHexGraphic> live = new(64);
        private static readonly List<Vector2> columns = new(128); // x, halfHeight

        // List.Remove would run the overloaded Object equality against every element.
        private int liveIndex = -1;

        protected override void OnEnable()
        {
            base.OnEnable();
            liveIndex = live.Count;
            live.Add(this);
        }

        protected override void OnDisable()
        {
            if (liveIndex >= 0)
            {
                int last = live.Count - 1;
                live[liveIndex] = live[last];
                live[liveIndex].liveIndex = liveIndex;
                live.RemoveAt(last);
                liveIndex = -1;
            }

            base.OnDisable();
        }
        private static readonly Vector2[][] insetTables = new Vector2[3][];
        private static readonly int[] insetCounts = new int[3];
        private const int CurveResolution = 512;
        
        private static Vector2[] Insets(int samples, Shape shape)
        {
            int slot = (int)shape;
            if (insetTables[slot] != null && insetCounts[slot] == samples) return insetTables[slot];

            var curve = new Vector2[CurveResolution];
            float highest = 0f;
            for (int i = 0; i < CurveResolution; i++)
            {
                float t = i / (CurveResolution - 1f);
                float inset = InsetAt(t, shape);
                // The spline overshoots where the profile repeats a value, walking a
                // column backwards and flipping its quad.
                if (i > 0 && inset < highest) inset = highest;
                highest = inset;

                // Height is halved against the inset because a cap is drawn at
                // capScale wide and half that tall.
                curve[i] = new Vector2(inset, t * 0.5f);
            }

            var picked = new List<int>(samples) { 0, CurveResolution - 1 };
            while (picked.Count < samples)
            {
                float worst = 0f;
                int take = -1;
                int after = -1;
                for (int i = 0; i < picked.Count - 1; i++)
                {
                    float deviation = Farthest(curve, picked[i], picked[i + 1], out int index);
                    if (deviation <= worst) continue;
                    worst = deviation;
                    take = index;
                    after = i;
                }

                if (take < 0) break;
                picked.Insert(after + 1, take);
            }

            var table = new Vector2[picked.Count];
            for (int i = 0; i < picked.Count; i++)
                table[i] = new Vector2(curve[picked[i]].x, curve[picked[i]].y * 2f);

            insetTables[slot] = table;
            insetCounts[slot] = samples;
            return table;
        }

        private static float Farthest(Vector2[] curve, int from, int to, out int index)
        {
            index = -1;
            if (to - from < 2) return 0f;

            Vector2 start = curve[from];
            Vector2 span = curve[to] - start;
            float length = span.magnitude;
            if (length <= 0f) return 0f;

            float worst = 0f;
            for (int i = from + 1; i < to; i++)
            {
                Vector2 offset = curve[i] - start;
                float deviation = Mathf.Abs(span.x * offset.y - span.y * offset.x) / length;
                if (deviation <= worst) continue;
                worst = deviation;
                index = i;
            }

            return worst;
        }

        private static float InsetAt(float t, Shape shape)
            => shape == Shape.Hexagonal ? HexAt(t) : RoundedAt(t, CapInsetFor(shape));

        // Straight up the side, then an arc into the flat top.
        private static float RoundedAt(float t, float radius)
        {
            float past = 0.5f * Mathf.Clamp01(t) - (0.5f - radius);
            if (past <= 0f) return 0f;
            return radius - Mathf.Sqrt(Mathf.Max(0f, radius * radius - past * past));
        }

        private static float HexAt(float t)
        {
            float y = Mathf.Clamp01(t) * 0.5f;
            if (y <= TipEnd)
                return TipRadius - Mathf.Sqrt(Mathf.Max(0f, TipRadius * TipRadius - y * y));

            if (y < ShoulderStart)
                return SlantTangent * y - SlantOffset;

            float rise = y - ShoulderCenterY;
            return ShoulderCenterX
                   - Mathf.Sqrt(Mathf.Max(0f, ShoulderRadius * ShoulderRadius - rise * rise));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            PrepareUv(r);
            float s = profileScaleHeight > 0f ? profileScaleHeight : r.height;
            float capScale = Mathf.Min(s, r.width / (2f * ActiveCapInset));

            if (flatBottom)
                BuildFlatBottom(vh, r, capScale);
            else
                BuildPlate(vh, r, capScale);
        }

        // A vertical column strip, two vertices per column.
        private void BuildPlate(VertexHelper vh, Rect r, float capScale)
        {
            float half = r.height * 0.5f;
            float midY = r.center.y;
            float capW = ActiveCapInset * capScale;
            Vector2[] cap = Insets(capSamples, ActiveShape);

            // The strip pairs neighboring columns into quads.
            columns.Clear();
            for (int i = 0; i < cap.Length; i++)
                columns.Add(new Vector2(r.xMin + cap[i].x * capScale, cap[i].y * half));

            // Extra full height columns through the accent zones keep the fades smooth
            // past the caps.
            bool accented = accentLeft.a > 0.001f || accentRight.a > 0.001f;
            float leftEnd = Mathf.Min(r.xMin + accentWidth, r.center.x);
            float rightStart = Mathf.Max(r.xMax - accentWidth, r.center.x);

            if (accented)
                for (int i = 1; i <= 10; i++)
                {
                    float x = Mathf.Lerp(r.xMin + capW, leftEnd, i / 10f);
                    if (x > r.xMin + capW) columns.Add(new Vector2(x, half));
                }

            // Without both shoulders the drop interpolates out to the caps and bows the
            // whole edge.
            if (bottomPoint > 0f)
            {
                float halfW = TailHalfWidth(r);
                columns.Add(new Vector2(r.center.x - halfW, half));
                columns.Add(new Vector2(r.center.x, half));
                columns.Add(new Vector2(r.center.x + halfW, half));
            }

            if (accented)
                for (int i = 1; i <= 10; i++)
                {
                    float x = Mathf.Lerp(rightStart, r.xMax - capW, (i - 1) / 10f);
                    if (x < r.xMax - capW) columns.Add(new Vector2(x, half));
                }

            for (int i = cap.Length - 1; i >= 0; i--)
                columns.Add(new Vector2(r.xMax - cap[i].x * capScale, cap[i].y * half));

            UIVertex v = UIVertex.simpleVert;
            if (outlineThickness > 0f)
            {
                BuildPlateOutline(vh, r, half, midY);
                return;
            }

            for (int i = 0; i < columns.Count; i++)
            {
                float x = columns[i].x;
                float h = columns[i].y;
                Color32 c = ColorAt(x, r);
                v.color = c;
                v.position = new Vector3(x, midY + h);
                v.uv0 = UV(v.position);
                vh.AddVert(v);
                v.position = new Vector3(x, midY - h - BottomDrop(x, r));
                v.uv0 = UV(v.position);
                vh.AddVert(v);
            }
            for (int i = 0; i < columns.Count - 1; i++)
            {
                int a = i * 2;
                vh.AddTriangle(a, a + 2, a + 1);
                vh.AddTriangle(a + 1, a + 2, a + 3);
            }
        }

        // Four vertices a column, an inner contour alongside the outer one with the gap
        // filled.
        private void BuildPlateOutline(VertexHelper vh, Rect r, float half, float midY)
        {
            float t = Mathf.Min(outlineThickness, half);
            UIVertex v = UIVertex.simpleVert;

            // A pointed bottom is not a mirror of the top.
            for (int i = 0; i < columns.Count; i++)
            {
                float x = columns[i].x;
                Vector2 top = new(x, midY + columns[i].y);
                Vector2 bottom = new(x, midY - columns[i].y - BottomDrop(x, r));

                Vector2 topInner = top + Inward(i, midY, r, true) * t;
                Vector2 bottomInner = bottom + Inward(i, midY, r, false) * t;

                // The two bands would cross at the tips and fold the quad over.
                if (bottomInner.y > topInner.y)
                {
                    float meet = (topInner.y + bottomInner.y) * 0.5f;
                    topInner.y = meet;
                    bottomInner.y = meet;
                }

                v.color = ColorAt(x, r);
                AddOutlineVert(vh, ref v, top, r);
                AddOutlineVert(vh, ref v, topInner, r);
                AddOutlineVert(vh, ref v, bottomInner, r);
                AddOutlineVert(vh, ref v, bottom, r);
            }

            for (int i = 0; i < columns.Count - 1; i++)
            {
                int a = i * 4;
                int b = (i + 1) * 4;
                vh.AddTriangle(a, b, a + 1);
                vh.AddTriangle(a + 1, b, b + 1);
                vh.AddTriangle(a + 2, b + 2, a + 3);
                vh.AddTriangle(a + 3, b + 2, b + 3);
            }
        }

        // Normal of the edge at this column, taken off the neighbors so the band holds
        // its width wherever the edge tilts.
        private Vector2 Inward(int i, float midY, Rect r, bool top)
        {
            Vector2 At(int index)
            {
                float x = columns[index].x;
                float h = columns[index].y;
                return new Vector2(x, top ? midY + h : midY - h - BottomDrop(x, r));
            }

            Vector2 tangent = At(Mathf.Min(i + 1, columns.Count - 1)) - At(Mathf.Max(i - 1, 0));
            float len = tangent.magnitude;
            if (len <= 0.0001f) return new Vector2(0f, top ? -1f : 1f);

            tangent /= len;
            return top ? new Vector2(tangent.y, -tangent.x) : new Vector2(-tangent.y, tangent.x);
        }

        private void AddOutlineVert(VertexHelper vh, ref UIVertex v, Vector3 position, Rect r)
        {
            v.position = position;
            v.uv0 = UV(position);
            vh.AddVert(v);
        }

        private void BuildFlatBottom(VertexHelper vh, Rect r, float capScale)
        {
            float capH = Mathf.Min(capScale * 0.5f, r.height);
            float yBase = r.yMax - capH;
            // Sampling the curve directly here brings back the overshoot.
            Vector2[] cap = Insets(capSamples, ActiveShape);
            int k = cap.Length;

            UIVertex v = UIVertex.simpleVert;
            v.color = color;
            v.position = r.center;
            v.uv0 = UV(r.center);
            vh.AddVert(v);

            int count = 0;
            void Add(Vector2 p)
            {
                v.position = p;
                v.uv0 = UV(p);
                vh.AddVert(v);
                count++;
            }

            Add(new Vector2(r.xMin, r.yMin));
            for (int i = 0; i < k; i++)
                Add(new Vector2(r.xMin + cap[i].x * capScale, yBase + cap[i].y * capH));
            for (int i = k - 1; i >= 0; i--)
                Add(new Vector2(r.xMax - cap[i].x * capScale, yBase + cap[i].y * capH));
            Add(new Vector2(r.xMax, r.yMin));

            for (int i = 0; i < count; i++)
                vh.AddTriangle(0, 1 + i, 1 + (i + 1) % count);
        }

        // Cover style mapping with the overhang cropped, constant for the whole mesh.
        private Vector2 uvNormScale;
        private Vector2 uvNormOffset;
        private Vector2 uvOutScale;
        private Vector2 uvOutOffset;

        private void PrepareUv(Rect r)
        {
            float invW = r.width > 0f ? 1f / r.width : 0f;
            float invH = r.height > 0f ? 1f / r.height : 0f;
            uvNormScale = new Vector2(invW, invH);
            uvNormOffset = new Vector2(-r.xMin * invW, -r.yMin * invH);

            float uFit = 1f;
            float vFit = 1f;
            if (texture != null && r.height > 0f)
            {
                float texAspect = (float)texture.width / texture.height;
                float rectAspect = r.width / r.height;
                if (texAspect > rectAspect) uFit = rectAspect / texAspect;
                else vFit = texAspect / rectAspect;
            }

            uvOutScale = new Vector2(uFit * uvRect.width, vFit * uvRect.height);
            uvOutOffset = new Vector2(
                uvRect.x + uvRect.width * 0.5f * (1f - uFit),
                uvRect.y + uvRect.height * 0.5f * (1f - vFit));
        }

        private Vector2 UV(Vector2 p)
        {
            float u = Mathf.Clamp01(p.x * uvNormScale.x + uvNormOffset.x);
            float v = Mathf.Clamp01(p.y * uvNormScale.y + uvNormOffset.y);
            return new Vector2(uvOutOffset.x + u * uvOutScale.x, uvOutOffset.y + v * uvOutScale.y);
        }

        private Color32 ColorAt(float x, Rect r)
        {
            Color c = color;
            if (accentLeft.a > 0.001f)
            {
                float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x - r.xMin) / accentWidth));
                c = Color.Lerp(c, new Color(accentLeft.r, accentLeft.g, accentLeft.b, c.a), accentLeft.a * w);
            }
            if (accentRight.a > 0.001f)
            {
                float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r.xMax - x) / accentWidth));
                c = Color.Lerp(c, new Color(accentRight.r, accentRight.g, accentRight.b, c.a), accentRight.a * w);
            }
            return c;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
            SetMaterialDirty();
        }
#endif
    }
}