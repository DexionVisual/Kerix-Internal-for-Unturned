using System.Collections.Generic;
using UnityEngine;

namespace Kerix
{

    public static class Ui
    {

        public static readonly Color Royal = new Color(0.255f, 0.412f, 0.882f);
        public static readonly Color RoyalLight = new Color(0.42f, 0.58f, 1.00f);
        public static readonly Color RoyalDark = new Color(0.10f, 0.16f, 0.38f);
        public static readonly Color PanelBg = new Color(0.052f, 0.058f, 0.090f, 0.97f);
        public static readonly Color TextDim = new Color(0.58f, 0.62f, 0.74f);

        private static Texture2D _white;
        private static readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private static readonly Dictionary<string, GUIStyle> _styles = new Dictionary<string, GUIStyle>();

        private static float _fade = 1f;
        private static Color _savedGuiColor;

        public static void PushFade(float fade)
        {
            _savedGuiColor = GUI.color;
            _fade = Mathf.Clamp01(_savedGuiColor.a * Mathf.Clamp01(fade));
            GUI.color = new Color(1f, 1f, 1f, _fade);
        }

        public static float Ease(float cur, float target, float speed, float dt)
        {
            cur = Mathf.Lerp(cur, target, 1f - Mathf.Exp(-speed * dt));
            if (Mathf.Abs(cur - target) < 0.001f) cur = target;
            return cur;
        }

        public static Vector2 Ease(Vector2 cur, Vector2 target, float speed, float dt)
        {
            cur = Vector2.Lerp(cur, target, 1f - Mathf.Exp(-speed * dt));
            if ((cur - target).sqrMagnitude < 0.0005f) cur = target;
            return cur;
        }

        public static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        public static void PopFade()
        {
            GUI.color = _savedGuiColor;
            _fade = 1f;
        }

        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply(false);
                }
                return _white;
            }
        }

        public static void RectFill(Rect r, Color c)
        {
            Color old = GUI.color;
            c.a *= old.a;
            GUI.color = c;
            GUI.DrawTexture(r, White, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        public static void Tex(Rect r, Texture2D t)
        {
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
        }

        public static void RectOutline(Rect r, Color c, float t = 1.5f)
        {
            RectFill(new Rect(r.x, r.y, r.width, t), c);
            RectFill(new Rect(r.x, r.yMax - t, r.width, t), c);
            RectFill(new Rect(r.x, r.y, t, r.height), c);
            RectFill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        public static Font GetFont(float size)
        {
            int s = Mathf.RoundToInt(size);
            Font f;
            if (_fonts.TryGetValue(s.ToString(), out f) && f != null) return f;
            try { f = UnityEngine.Font.CreateDynamicFontFromOSFont("Segoe UI", s); } catch { f = null; }
            if (f == null)
                try { f = UnityEngine.Font.CreateDynamicFontFromOSFont("Arial", s); } catch { f = null; }
            _fonts[s.ToString()] = f;
            return f;
        }

        public static GUIStyle Style(float size, bool bold = false, bool rich = false,
                                     TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            string key = size + "|" + bold + "|" + rich + "|" + anchor;
            GUIStyle st;
            if (_styles.TryGetValue(key, out st) && st != null) return st;
            st = new GUIStyle
            {
                font = GetFont(size),
                fontSize = Mathf.RoundToInt(size),
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                richText = rich,
                alignment = anchor,
                wordWrap = false,
                clipping = TextClipping.Overflow,
            };
            st.normal.textColor = Color.white;
            _styles[key] = st;
            return st;
        }

        private static float SdRounded(float px, float py, float w, float h, float r)
        {
            float qx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - r);
            float qy = Mathf.Abs(py - h * 0.5f) - (h * 0.5f - r);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        public static Texture2D Rounded(int w, int h, float radius, Color fill, Color border, float borderW,
                                        Color glow = default, float glowW = 0f)
        {
            string key = w + ":" + h + ":" + radius + ":" + fill + ":" + border + ":" + borderW
                       + ":" + glow + ":" + glowW;
            Texture2D t;
            if (_tex.TryGetValue(key, out t) && t != null) return t;

            t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sd = SdRounded(x + 0.5f, y + 0.5f, w, h, radius);
                    float cov = Mathf.Clamp01(0.5f - sd);
                    float a = fill.a * cov;
                    float r = fill.r, g = fill.g, b = fill.b;

                    if (borderW > 0f)
                    {
                        float bc = Mathf.Clamp01(0.5f - (Mathf.Abs(sd) - borderW * 0.5f));
                        if (bc > 0f)
                        {
                            float sa = border.a * bc;
                            float na = sa + a * (1f - sa);
                            if (na > 0f)
                            {
                                r = (border.r * sa + r * a * (1f - sa)) / na;
                                g = (border.g * sa + g * a * (1f - sa)) / na;
                                b = (border.b * sa + b * a * (1f - sa)) / na;
                            }
                            a = na;
                        }
                    }

                    if (glowW > 0f && glow.a > 0f && sd > 0.5f && sd < glowW)
                    {
                        float gk = 1f - (sd - 0.5f) / (glowW - 0.5f);
                        float ga = gk * gk * glow.a;
                        float na = ga + a * (1f - ga);
                        if (na > 0f)
                        {
                            r = (glow.r * ga + r * a * (1f - ga)) / na;
                            g = (glow.g * ga + g * a * (1f - ga)) / na;
                            b = (glow.b * ga + b * a * (1f - ga)) / na;
                        }
                        a = na;
                    }

                    px[y * w + x] = new Color32(
                        (byte)(Mathf.Clamp01(r) * 255f),
                        (byte)(Mathf.Clamp01(g) * 255f),
                        (byte)(Mathf.Clamp01(b) * 255f),
                        (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(false);
            _tex[key] = t;
            return t;
        }

        public static Texture2D Shadow(int w, int h, float radius, float alpha)
        {
            return Rounded(w, h, radius, new Color(0f, 0f, 0f, alpha), Color.clear, 0f);
        }

        public static void OutlinedLabel(Rect r, string plain, string rich, GUIStyle st, Color core)
        {
            Color save = st.normal.textColor;
            st.normal.textColor = new Color(0.01f, 0.02f, 0.05f, 0.92f * _fade);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                Vector2 o = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.7f;
                GUI.Label(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), plain, st);
            }
            core.a *= _fade;
            st.normal.textColor = core;
            GUI.Label(r, rich, st);
            st.normal.textColor = save;
        }

        private static bool _prepared;

        public static void Prepare()
        {
            if (_prepared) return;
            _prepared = true;
            Texture2D w = White;
            GetFont(11); GetFont(12); GetFont(13); GetFont(15); GetFont(18); GetFont(24);
        }
    }
}
