using System;
using UnityEngine;

namespace ZeepkistPartSearch.UI
{
    /// <summary>
    /// A rounded-rectangle plate drawn at runtime in Zeepkist's tile colours.
    ///
    /// The inspector's thumbnail sprite is a small tile texture, not a 9-slice plate: using it
    /// across a full-width field stretches the art into a smear (seen in the 17:11 screenshot).
    /// Drawing the plate here gives the same rounded corners at any width, with no borrowed art.
    ///
    /// The texture is built once per session, is never written to disk, and is 9-sliced with a
    /// border of nine pixels, so a bar of 18 pixels or taller keeps its corners intact.
    /// </summary>
    internal static class TilePlateSprite
    {
        private const int Radius = 7;
        private const int BorderWidth = 1;
        private const int Size = Radius * 2 + 6;
        private const int SliceBorder = Radius + 2;
        private const int Samples = 4;

        private static readonly Color FillColour = new Color32(255, 232, 115, 255);
        private static readonly Color EdgeColour = new Color32(224, 194, 94, 255);

        private static Sprite cached;
        private static bool failed;

        public static Sprite Get()
        {
            if (cached != null || failed)
            {
                return cached;
            }

            try
            {
                cached = Build();
            }
            catch (Exception exception)
            {
                failed = true;
                Plugin.LogError("Parts Search: could not build the tile plate sprite: " + exception.Message);
            }

            return cached;
        }

        private static Sprite Build()
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = "Parts Search Plate";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color32[] pixels = new Color32[Size * Size];
            float half = Size * 0.5f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float outer = Coverage(x, y, half, Radius);
                    float inner = Coverage(x, y, half - BorderWidth, Mathf.Max(Radius - BorderWidth, 0));

                    float fill = Mathf.Clamp01(inner);
                    float edge = Mathf.Clamp01(outer - inner);
                    float alpha = Mathf.Clamp01(outer);

                    Color colour = alpha > 0f
                        ? (FillColour * fill + EdgeColour * edge) / alpha
                        : Color.clear;

                    pixels[y * Size + x] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(colour.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(colour.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(colour.b) * 255f),
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Vector4 border = new Vector4(SliceBorder, SliceBorder, SliceBorder, SliceBorder);
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>How much of one pixel is inside a rounded box, sampled 4x4 for smooth edges.</summary>
        private static float Coverage(int x, int y, float half, float radius)
        {
            float hit = 0f;

            for (int sampleY = 0; sampleY < Samples; sampleY++)
            {
                for (int sampleX = 0; sampleX < Samples; sampleX++)
                {
                    float px = x + (sampleX + 0.5f) / Samples - half;
                    float py = y + (sampleY + 0.5f) / Samples - half;

                    if (RoundedBox(px, py, half, radius) <= 0f)
                    {
                        hit += 1f;
                    }
                }
            }

            return hit / (Samples * Samples);
        }

        /// <summary>Signed distance to a rounded box; negative inside.</summary>
        private static float RoundedBox(float x, float y, float half, float radius)
        {
            float qx = Mathf.Abs(x) - (half - radius);
            float qy = Mathf.Abs(y) - (half - radius);
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);

            return Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY)
                   + Mathf.Min(Mathf.Max(qx, qy), 0f)
                   - radius;
        }
    }
}
