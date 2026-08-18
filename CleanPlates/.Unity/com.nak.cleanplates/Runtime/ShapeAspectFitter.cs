// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using System.Collections.Generic;
using UnityEngine;

namespace NAK.CleanPlates.UI
{
    // For anything whose width comes from the shape.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ShapeAspectFitter : MonoBehaviour
    {
        private static readonly List<ShapeAspectFitter> live = new(32);

        private RectTransform rect;

        // List.Remove would run the overloaded Object equality against every element.
        private int liveIndex = -1;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            liveIndex = live.Count;
            live.Add(this);
            Apply();
        }

        private void OnDisable()
        {
            if (liveIndex < 0) return;

            int last = live.Count - 1;
            live[liveIndex] = live[last];
            live[liveIndex].liveIndex = liveIndex;
            live.RemoveAt(last);
            liveIndex = -1;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (rect != null) Apply();
        }

        public void Apply()
        {
            float width = rect.rect.height * RoundedHexGraphic.ShapeAspect;
            Vector2 size = rect.sizeDelta;
            if (Mathf.Approximately(size.x, width)) return;
            rect.sizeDelta = new Vector2(width, size.y);
        }

        internal static void ApplyAll()
        {
            for (int i = live.Count - 1; i >= 0; i--) live[i].Apply();
        }
    }
}