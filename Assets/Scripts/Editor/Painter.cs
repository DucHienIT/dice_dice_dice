using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// CPU pixel painter for the procedural art. Editor-only: every sprite it produces is
    /// baked to a .png asset by <see cref="SpriteBaker"/> at build time and wired into
    /// prefabs — nothing here ever runs in a player build.
    /// </summary>
    public class Painter
    {
        private readonly int _w;
        private readonly int _h;
        private readonly Color32[] _px;

        public int Width => _w;
        public int Height => _h;

        public Painter(int width, int height)
        {
            _w = width;
            _h = height;
            _px = new Color32[width * height];
        }

        public void Clear(Color32 c)
        {
            for (int i = 0; i < _px.Length; i++) _px[i] = c;
        }

        public void GradientV(Color top, Color bottom, int y0, int y1)
        {
            if (y1 < y0) { (y0, y1) = (y1, y0); }
            y0 = Mathf.Max(0, y0);
            y1 = Mathf.Min(_h - 1, y1);
            float span = Mathf.Max(1, y1 - y0);
            for (int y = y0; y <= y1; y++)
            {
                // y=0 is bottom; "top" color at y1
                Color c = Color.Lerp(bottom, top, (y - y0) / span);
                Color32 c32 = c;
                int row = y * _w;
                for (int x = 0; x < _w; x++) Blend(row + x, c32);
            }
        }

        public void FillRect(float x, float y, float w, float h, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(x + w));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(y));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(y + h));
            Color32 c32 = color;
            for (int py = y0; py <= y1; py++)
            {
                int row = py * _w;
                for (int px = x0; px <= x1; px++) Blend(row + px, c32);
            }
        }

        public void FillCircle(float cx, float cy, float r, Color color) =>
            FillEllipse(cx, cy, r, r, color);

        public void FillEllipse(float cx, float cy, float rx, float ry, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 2));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(cx + rx + 2));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 2));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(cy + ry + 2));
            float edge = Mathf.Min(rx, ry);
            for (int py = y0; py <= y1; py++)
            {
                int row = py * _w;
                for (int px = x0; px <= x1; px++)
                {
                    float dx = (px - cx) / rx;
                    float dy = (py - cy) / ry;
                    float d = (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * edge;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) continue;
                    Color c = color;
                    c.a *= a;
                    Blend(row + px, c);
                }
            }
        }

        /// <summary>Ellipse with vertical two-tone shading (light on top).</summary>
        public void FillEllipseShaded(float cx, float cy, float rx, float ry, Color top, Color bottom)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 2));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(cx + rx + 2));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 2));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(cy + ry + 2));
            float edge = Mathf.Min(rx, ry);
            for (int py = y0; py <= y1; py++)
            {
                int row = py * _w;
                float t = Mathf.Clamp01((py - (cy - ry)) / (2f * ry));
                Color rowColor = Color.Lerp(bottom, top, t);
                for (int px = x0; px <= x1; px++)
                {
                    float dx = (px - cx) / rx;
                    float dy = (py - cy) / ry;
                    float d = (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * edge;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) continue;
                    Color c = rowColor;
                    c.a *= a;
                    Blend(row + px, c);
                }
            }
        }

        public void OutlineEllipse(float cx, float cy, float rx, float ry, float thickness, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - thickness - 2));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(cx + rx + thickness + 2));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - thickness - 2));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(cy + ry + thickness + 2));
            float edge = Mathf.Min(rx, ry);
            for (int py = y0; py <= y1; py++)
            {
                int row = py * _w;
                for (int px = x0; px <= x1; px++)
                {
                    float dx = (px - cx) / rx;
                    float dy = (py - cy) / ry;
                    float d = (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * edge;
                    float a = Mathf.Clamp01(0.5f - Mathf.Abs(d) + thickness * 0.5f);
                    if (a <= 0f) continue;
                    Color c = color;
                    c.a *= a;
                    Blend(row + px, c);
                }
            }
        }

        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            float d = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(d) < 0.0001f) return;
            Color32 c32 = color;
            for (int py = y0; py <= y1; py++)
            {
                int row = py * _w;
                for (int px = x0; px <= x1; px++)
                {
                    float w1 = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (py - c.y)) / d;
                    float w2 = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / d;
                    float w3 = 1f - w1 - w2;
                    if (w1 < 0f || w2 < 0f || w3 < 0f) continue;
                    Blend(row + px, c32);
                }
            }
        }

        /// <summary>Additive radial glow (soft light halo).</summary>
        public void Glow(float cx, float cy, float r, Color color, float intensity)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(cy + r));
            float r2 = r * r;
            for (int py = y0; py <= y1; py++)
            {
                int row = py * _w;
                for (int px = x0; px <= x1; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float d2 = dx * dx + dy * dy;
                    if (d2 > r2) continue;
                    float fall = 1f - Mathf.Sqrt(d2) / r;
                    float amt = fall * fall * intensity;
                    int i = row + px;
                    Color32 dst = _px[i];
                    dst.r = (byte)Mathf.Min(255, dst.r + color.r * 255f * amt);
                    dst.g = (byte)Mathf.Min(255, dst.g + color.g * 255f * amt);
                    dst.b = (byte)Mathf.Min(255, dst.b + color.b * 255f * amt);
                    dst.a = (byte)Mathf.Min(255, dst.a + color.a * 255f * amt);
                    _px[i] = dst;
                }
            }
        }

        public void FillRoundRect(float x, float y, float w, float h, float radius, Color color)
        {
            radius = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x - 1));
            int x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(x + w + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(y - 1));
            int y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(y + h + 1));
            float cx0 = x + radius, cx1 = x + w - radius;
            float cy0 = y + radius, cy1 = y + h - radius;
            for (int py = y0; py <= y1; py++)
            {
                int row = py * _w;
                for (int px = x0; px <= x1; px++)
                {
                    float qx = Mathf.Max(cx0 - px, Mathf.Max(0f, px - cx1));
                    float qy = Mathf.Max(cy0 - py, Mathf.Max(0f, py - cy1));
                    float d = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) continue;
                    Color c = color;
                    c.a *= a;
                    Blend(row + px, c);
                }
            }
        }

        /// <summary>Thick arc from angle a0 to a1 (radians), drawn as sampled discs.</summary>
        public void Arc(float cx, float cy, float r, float a0, float a1, float thickness, Color color)
        {
            int steps = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(a1 - a0) * r));
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)steps);
                FillCircle(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, thickness * 0.5f, color);
            }
        }

        /// <summary>Thick line drawn as sampled discs.</summary>
        public void Line(Vector2 from, Vector2 to, float thickness, Color color)
        {
            float len = Vector2.Distance(from, to);
            int steps = Mathf.Max(2, Mathf.CeilToInt(len));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, i / (float)steps);
                FillCircle(p.x, p.y, thickness * 0.5f, color);
            }
        }

        private void Blend(int index, Color32 src)
        {
            if (src.a == 255) { _px[index] = src; return; }
            if (src.a == 0) return;
            Color32 dst = _px[index];
            float sa = src.a / 255f;
            float da = dst.a / 255f * (1f - sa);
            float outA = sa + da;
            if (outA <= 0f) { _px[index] = new Color32(0, 0, 0, 0); return; }
            _px[index] = new Color32(
                (byte)((src.r * sa + dst.r * da) / outA),
                (byte)((src.g * sa + dst.g * da) / outA),
                (byte)((src.b * sa + dst.b * da) / outA),
                (byte)(outA * 255f));
        }

        /// <summary>Encodes the canvas to PNG bytes ready to be written as a project asset.</summary>
        public byte[] EncodeToPng()
        {
            var tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
            tex.SetPixels32(_px);
            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }
    }
}
