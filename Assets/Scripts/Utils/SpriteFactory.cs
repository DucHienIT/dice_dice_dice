using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>
    /// Generates every sprite at runtime (no external art). Sprites are drawn white and tinted
    /// via SpriteRenderer.color / Image.color, cached forever. 64px = 1 world unit.
    /// </summary>
    public static class SpriteFactory
    {
        private const int Size = 64;
        private const float PixelsPerUnit = 64f;

        private static Sprite _white;
        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _softCircle;
        private static Sprite _uiRounded;
        private static Sprite _slash;
        private static Sprite _healCross;
        private static readonly Sprite[] DiceFaces = new Sprite[6];
        private static readonly Dictionary<ItemIcon, Sprite> Icons = new Dictionary<ItemIcon, Sprite>(11);
        private static readonly Dictionary<ProjectileVisual, Sprite> Projectiles = new Dictionary<ProjectileVisual, Sprite>(5);

        // ---------- Public accessors ----------

        public static Sprite White => _white != null ? _white : (_white = BuildWhite());

        public static Sprite Circle => _circle != null ? _circle : (_circle = Build(c => c.FillCircle(32f, 32f, 30f, White1)));

        public static Sprite Ring => _ring != null ? _ring : (_ring = Build(c => c.Ring(32f, 32f, 29f, 4f, White1)));

        public static Sprite SoftCircle => _softCircle != null ? _softCircle : (_softCircle = BuildSoftCircle());

        /// <summary>9-sliced rounded rect for UI panels/buttons (tint via Image.color).</summary>
        public static Sprite UiRounded => _uiRounded != null ? _uiRounded : (_uiRounded = BuildUiRounded());

        public static Sprite Slash => _slash != null ? _slash : (_slash = Build(c =>
        {
            c.Line(10f, 10f, 54f, 54f, 4f, White1);
            c.Line(52f, 12f, 12f, 52f, 4f, White1);
        }));

        public static Sprite HealCross => _healCross != null ? _healCross : (_healCross = Build(c =>
        {
            c.FillRect(26f, 12f, 12f, 40f, White1);
            c.FillRect(12f, 26f, 40f, 12f, White1);
        }));

        public static Sprite DiceFace(int face)
        {
            int index = Mathf.Clamp(face, 1, 6) - 1;
            if (DiceFaces[index] == null)
            {
                DiceFaces[index] = BuildDiceFace(index + 1);
            }
            return DiceFaces[index];
        }

        public static Sprite Icon(ItemIcon icon)
        {
            Sprite sprite;
            if (!Icons.TryGetValue(icon, out sprite))
            {
                sprite = BuildIcon(icon);
                Icons[icon] = sprite;
            }
            return sprite;
        }

        public static Sprite Projectile(ProjectileVisual visual)
        {
            Sprite sprite;
            if (!Projectiles.TryGetValue(visual, out sprite))
            {
                sprite = BuildProjectile(visual);
                Projectiles[visual] = sprite;
            }
            return sprite;
        }

        // ---------- Builders ----------

        private static readonly Color32 White1 = new Color32(255, 255, 255, 255);
        private static readonly Color32 Dark1 = new Color32(35, 38, 48, 255);

        private static Sprite BuildWhite()
        {
            var canvas = new PixelCanvas(4);
            canvas.FillRect(0f, 0f, 4f, 4f, White1);
            return canvas.ToSprite(4f);
        }

        private static Sprite Build(System.Action<PixelCanvas> draw)
        {
            var canvas = new PixelCanvas(Size);
            draw(canvas);
            return canvas.ToSprite(PixelsPerUnit);
        }

        private static Sprite BuildSoftCircle()
        {
            var canvas = new PixelCanvas(Size);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - 31.5f, dy = y - 31.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / 30f;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha;
                    canvas.SetPixel(x, y, new Color32(255, 255, 255, (byte)(alpha * 255f)));
                }
            }
            return canvas.ToSprite(PixelsPerUnit);
        }

        private static Sprite BuildUiRounded()
        {
            var canvas = new PixelCanvas(Size);
            canvas.RoundedRect(0f, 0f, Size, Size, 14f, White1);
            return canvas.ToSprite(PixelsPerUnit, new Vector4(20f, 20f, 20f, 20f));
        }

        private static Sprite BuildDiceFace(int face)
        {
            var canvas = new PixelCanvas(Size);
            canvas.RoundedRect(4f, 4f, 56f, 56f, 12f, White1);
            const float lo = 19f, mid = 32f, hi = 45f, r = 5f;
            if (face == 1) { canvas.FillCircle(mid, mid, r + 1f, Dark1); }
            if (face == 2) { canvas.FillCircle(lo, hi, r, Dark1); canvas.FillCircle(hi, lo, r, Dark1); }
            if (face == 3) { canvas.FillCircle(lo, hi, r, Dark1); canvas.FillCircle(mid, mid, r, Dark1); canvas.FillCircle(hi, lo, r, Dark1); }
            if (face >= 4) { canvas.FillCircle(lo, lo, r, Dark1); canvas.FillCircle(lo, hi, r, Dark1); canvas.FillCircle(hi, lo, r, Dark1); canvas.FillCircle(hi, hi, r, Dark1); }
            if (face == 5) { canvas.FillCircle(mid, mid, r, Dark1); }
            if (face == 6) { canvas.FillCircle(lo, mid, r, Dark1); canvas.FillCircle(hi, mid, r, Dark1); }
            return canvas.ToSprite(PixelsPerUnit);
        }

        private static Sprite BuildIcon(ItemIcon icon)
        {
            return Build(c =>
            {
                switch (icon)
                {
                    case ItemIcon.Dice:
                        c.RoundedRect(8f, 8f, 48f, 48f, 10f, White1);
                        c.FillCircle(22f, 42f, 4f, Dark1);
                        c.FillCircle(32f, 32f, 4f, Dark1);
                        c.FillCircle(42f, 22f, 4f, Dark1);
                        break;
                    case ItemIcon.Bow:
                        c.Ring(20f, 32f, 22f, 4f, White1, -70f, 70f);
                        c.Line(20f, 10f, 20f, 54f, 2f, White1);
                        c.Line(14f, 32f, 52f, 32f, 3f, White1);
                        c.FillTriangle(58f, 32f, 48f, 27f, 48f, 37f, White1);
                        break;
                    case ItemIcon.Sword:
                        c.Line(16f, 16f, 46f, 46f, 6f, White1);
                        c.FillTriangle(54f, 54f, 42f, 48f, 48f, 42f, White1);
                        c.Line(24f, 36f, 36f, 24f, 4f, White1);
                        c.FillRect(12f, 12f, 10f, 10f, White1);
                        break;
                    case ItemIcon.Crossbow:
                        c.Ring(32f, 24f, 20f, 4f, White1, 180f, 360f);
                        c.Line(32f, 8f, 32f, 56f, 4f, White1);
                        c.FillTriangle(32f, 2f, 26f, 12f, 38f, 12f, White1);
                        c.Line(18f, 48f, 46f, 48f, 3f, White1);
                        break;
                    case ItemIcon.Cannon:
                        c.FillCircle(24f, 22f, 13f, White1);
                        c.Line(30f, 30f, 52f, 48f, 9f, White1);
                        c.FillCircle(12f, 12f, 4f, White1);
                        break;
                    case ItemIcon.FireBook:
                        c.FillRect(12f, 8f, 40f, 34f, White1);
                        c.FillRect(12f, 8f, 40f, 6f, Dark1);
                        c.FillTriangle(32f, 60f, 22f, 40f, 42f, 40f, White1);
                        c.FillTriangle(32f, 54f, 27f, 42f, 37f, 42f, Dark1);
                        break;
                    case ItemIcon.FrostStone:
                        c.Line(32f, 8f, 32f, 56f, 3f, White1);
                        c.Line(11f, 20f, 53f, 44f, 3f, White1);
                        c.Line(11f, 44f, 53f, 20f, 3f, White1);
                        c.FillCircle(32f, 32f, 6f, White1);
                        break;
                    case ItemIcon.LightningOrb:
                        c.FillTriangle(38f, 58f, 20f, 30f, 34f, 30f, White1);
                        c.FillTriangle(26f, 6f, 44f, 34f, 30f, 34f, White1);
                        break;
                    case ItemIcon.Anvil:
                        c.FillRect(10f, 36f, 44f, 10f, White1);
                        c.FillTriangle(20f, 36f, 44f, 36f, 32f, 18f, White1);
                        c.FillRect(24f, 12f, 16f, 8f, White1);
                        break;
                    case ItemIcon.Hourglass:
                        c.FillTriangle(14f, 52f, 50f, 52f, 32f, 32f, White1);
                        c.FillTriangle(14f, 12f, 50f, 12f, 32f, 32f, White1);
                        c.FillRect(12f, 8f, 40f, 5f, White1);
                        c.FillRect(12f, 51f, 40f, 5f, White1);
                        break;
                    case ItemIcon.Shield:
                        c.FillRect(14f, 30f, 36f, 22f, White1);
                        c.FillTriangle(14f, 32f, 50f, 32f, 32f, 8f, White1);
                        c.FillCircle(32f, 34f, 6f, Dark1);
                        break;
                }
            });
        }

        private static Sprite BuildProjectile(ProjectileVisual visual)
        {
            return Build(c =>
            {
                switch (visual)
                {
                    case ProjectileVisual.Arrow:
                        c.Line(8f, 32f, 48f, 32f, 3f, White1);
                        c.FillTriangle(58f, 32f, 46f, 26f, 46f, 38f, White1);
                        break;
                    case ProjectileVisual.Bolt:
                        c.Line(6f, 32f, 50f, 32f, 5f, White1);
                        c.FillTriangle(60f, 32f, 48f, 25f, 48f, 39f, White1);
                        break;
                    case ProjectileVisual.Shell:
                        c.FillCircle(32f, 32f, 12f, White1);
                        break;
                    case ProjectileVisual.Meteor:
                        c.FillCircle(32f, 24f, 13f, White1);
                        c.FillTriangle(32f, 62f, 22f, 34f, 42f, 34f, White1);
                        break;
                    case ProjectileVisual.Frost:
                        c.FillCircle(32f, 32f, 9f, White1);
                        c.Line(32f, 14f, 32f, 50f, 2f, White1);
                        c.Line(16f, 23f, 48f, 41f, 2f, White1);
                        c.Line(16f, 41f, 48f, 23f, 2f, White1);
                        break;
                }
            });
        }

        // ---------- Pixel drawing ----------

        private class PixelCanvas
        {
            private readonly int _size;
            private readonly Color32[] _pixels;

            public PixelCanvas(int size)
            {
                _size = size;
                _pixels = new Color32[size * size];
            }

            public void SetPixel(int x, int y, Color32 color)
            {
                if (x < 0 || y < 0 || x >= _size || y >= _size) return;
                _pixels[y * _size + x] = color;
            }

            public void FillRect(float x, float y, float w, float h, Color32 color)
            {
                for (int py = (int)y; py < y + h; py++)
                {
                    for (int px = (int)x; px < x + w; px++)
                    {
                        SetPixel(px, py, color);
                    }
                }
            }

            public void FillCircle(float cx, float cy, float r, Color32 color)
            {
                int min = Mathf.Max(0, (int)(cx - r - 1f));
                int max = Mathf.Min(_size - 1, (int)(cx + r + 1f));
                int minY = Mathf.Max(0, (int)(cy - r - 1f));
                int maxY = Mathf.Min(_size - 1, (int)(cy + r + 1f));
                float r2 = r * r;
                for (int py = minY; py <= maxY; py++)
                {
                    for (int px = min; px <= max; px++)
                    {
                        float dx = px - cx, dy = py - cy;
                        if (dx * dx + dy * dy <= r2) SetPixel(px, py, color);
                    }
                }
            }

            public void Ring(float cx, float cy, float r, float thickness, Color32 color, float angleFrom = 0f, float angleTo = 360f)
            {
                float rOut2 = r * r;
                float rIn = r - thickness;
                float rIn2 = rIn * rIn;
                bool fullCircle = angleTo - angleFrom >= 360f;
                for (int py = 0; py < _size; py++)
                {
                    for (int px = 0; px < _size; px++)
                    {
                        float dx = px - cx, dy = py - cy;
                        float d2 = dx * dx + dy * dy;
                        if (d2 > rOut2 || d2 < rIn2) continue;
                        if (!fullCircle)
                        {
                            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                            if (angle < angleFrom || angle > angleTo) continue;
                        }
                        SetPixel(px, py, color);
                    }
                }
            }

            public void RoundedRect(float x, float y, float w, float h, float radius, Color32 color)
            {
                for (int py = (int)y; py < y + h; py++)
                {
                    for (int px = (int)x; px < x + w; px++)
                    {
                        float lx = Mathf.Max(x + radius - px, px - (x + w - 1f - radius));
                        float ly = Mathf.Max(y + radius - py, py - (y + h - 1f - radius));
                        if (lx > 0f && ly > 0f && lx * lx + ly * ly > radius * radius) continue;
                        SetPixel(px, py, color);
                    }
                }
            }

            public void Line(float x1, float y1, float x2, float y2, float thickness, Color32 color)
            {
                float dx = x2 - x1, dy = y2 - y1;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                int steps = Mathf.CeilToInt(length * 2f);
                float half = thickness * 0.5f;
                for (int i = 0; i <= steps; i++)
                {
                    float t = steps == 0 ? 0f : (float)i / steps;
                    FillCircle(x1 + dx * t, y1 + dy * t, half, color);
                }
            }

            public void FillTriangle(float x1, float y1, float x2, float y2, float x3, float y3, Color32 color)
            {
                int minX = Mathf.Max(0, (int)Mathf.Min(x1, Mathf.Min(x2, x3)));
                int maxX = Mathf.Min(_size - 1, (int)Mathf.Max(x1, Mathf.Max(x2, x3)));
                int minY = Mathf.Max(0, (int)Mathf.Min(y1, Mathf.Min(y2, y3)));
                int maxY = Mathf.Min(_size - 1, (int)Mathf.Max(y1, Mathf.Max(y2, y3)));
                for (int py = minY; py <= maxY; py++)
                {
                    for (int px = minX; px <= maxX; px++)
                    {
                        float d1 = Cross(px - x2, py - y2, x1 - x2, y1 - y2);
                        float d2 = Cross(px - x3, py - y3, x2 - x3, y2 - y3);
                        float d3 = Cross(px - x1, py - y1, x3 - x1, y3 - y1);
                        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
                        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
                        if (!(hasNeg && hasPos)) SetPixel(px, py, color);
                    }
                }
            }

            private static float Cross(float ax, float ay, float bx, float by)
            {
                return ax * by - ay * bx;
            }

            public Sprite ToSprite(float pixelsPerUnit, Vector4 border = default)
            {
                var texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixels32(_pixels);
                texture.Apply(false, true);
                return Sprite.Create(texture, new Rect(0f, 0f, _size, _size), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            }
        }
    }
}
