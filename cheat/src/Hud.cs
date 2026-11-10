using System.Collections.Generic;
using UnityEngine;
namespace Kerix
{
    public static class Hud
    {
        public static bool EditMode;
        private static readonly Dictionary<string, Vector2> _pos = new Dictionary<string, Vector2>();
        private static readonly Dictionary<string, Rect> _reported = new Dictionary<string, Rect>();
        private static readonly string[] Known = { "minimap" };
        private static string _dragging;
        private static Vector2 _dragOff;
        public static void Report(string id, Rect r)
        {
            _reported[id] = r;
        }
        public static bool HasCustom(string id)
        {
            return _pos.ContainsKey(id);
        }
        public static Vector2 GetPos(string id, Vector2 fallback)
        {
            Vector2 p;
            return _pos.TryGetValue(id, out p) ? p : fallback;
        }
        public static void SetPos(string id, Vector2 p)
        {
            _pos[id] = p;
        }
        public static void ResetAll()
        {
            _pos.Clear();
        }
        public static string Serialize()
        {
            var sb = new System.Text.StringBuilder();
            foreach (string id in Known)
            {
                Vector2 p;
                if (!_pos.TryGetValue(id, out p)) continue;
                if (sb.Length > 0) sb.Append(';');
                sb.Append(id).Append('=').Append(p.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(',').Append(p.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
        public static void Deserialize(string data)
        {
            _pos.Clear();
            if (string.IsNullOrEmpty(data)) return;
            try
            {
                foreach (string part in data.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq <= 0) continue;
                    string id = part.Substring(0, eq);
                    string[] xy = part.Substring(eq + 1).Split(',');
                    float x, y;
                    if (xy.Length == 2 &&
                        float.TryParse(xy[0], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out x) &&
                        float.TryParse(xy[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out y))
                        _pos[id] = new Vector2(x, y);
                }
            }
            catch { }
        }
        public static void DrawEditOverlay()
        {
            if (!EditMode) { if (_reported.Count > 0) _reported.Clear(); return; }
            if (Event.current == null) return;
            Event e = Event.current;
            Ui.RectFill(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0f, 0f, 0f, 0.55f));
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                for (int i = Known.Length - 1; i >= 0; i--)
                {
                    Rect r;
                    if (!_reported.TryGetValue(Known[i], out r)) continue;
                    if (r.Contains(e.mousePosition))
                    {
                        _dragging = Known[i];
                        _dragOff = e.mousePosition - r.position;
                        if (!HasCustom(Known[i])) SetPos(Known[i], r.position);
                        e.Use();
                        break;
                    }
                }
            }
            else if (e.type == EventType.MouseDrag && _dragging != null && e.button == 0)
            {
                Vector2 p = e.mousePosition - _dragOff;
                p.x = Mathf.Clamp(p.x, 0f, Screen.width - 40f);
                p.y = Mathf.Clamp(p.y, 0f, Screen.height - 20f);
                SetPos(_dragging, p);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _dragging != null)
            {
                _dragging = null;
                e.Use();
            }
            for (int i = 0; i < Known.Length; i++)
            {
                Rect r;
                if (!_reported.TryGetValue(Known[i], out r)) continue;
                bool hot = _dragging == Known[i] || r.Contains(e.mousePosition);
                Ui.RectOutline(r, hot ? Ui.RoyalLight : new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.55f), 1.5f);
                GUIStyle ls = Ui.Style(10, bold: true, anchor: TextAnchor.UpperLeft);
                ls.normal.textColor = hot ? Ui.RoyalLight : Ui.TextDim;
                string label = Known[i];
                Ui.OutlinedLabel(new Rect(r.x + 4f, r.y + 2f, 120f, 14f), label, label, ls, ls.normal.textColor);
            }
            const float w = 220f, h = 34f;
            Rect bar = new Rect((Screen.width - w) * 0.5f, 10f, w, h);
            Ui.Tex(bar, Ui.Rounded((int)w, (int)h, 9f, Ui.PanelBg, Ui.Royal, 1.3f));
            GUIStyle hs = Ui.Style(11, bold: true, anchor: TextAnchor.MiddleCenter);
            hs.normal.textColor = Ui.RoyalLight;
            GUI.Label(new Rect(bar.x + 8f, bar.y, 74f, h), "HUD EDIT", hs);
            Rect done = new Rect(bar.x + 86f, bar.y + 5f, 62f, h - 10f);
            bool dHover = done.Contains(e.mousePosition);
            Ui.Tex(done, Ui.Rounded(62, (int)(h - 10f), 6f,
                Color.Lerp(new Color(0.1f, 0.105f, 0.15f, 1f), Ui.RoyalDark, dHover ? 1f : 0f),
                new Color(1f, 1f, 1f, 0.1f), 1f));
            GUIStyle bs = Ui.Style(11, bold: true, anchor: TextAnchor.MiddleCenter);
            bs.normal.textColor = Color.white;
            GUI.Label(done, "Done", bs);
            if (dHover && e.type == EventType.MouseDown && e.button == 0)
            {
                EditMode = false;
                e.Use();
            }
            Rect reset = new Rect(bar.x + 152f, bar.y + 5f, 62f, h - 10f);
            bool rHover = reset.Contains(e.mousePosition);
            Ui.Tex(reset, Ui.Rounded(62, (int)(h - 10f), 6f,
                Color.Lerp(new Color(0.1f, 0.105f, 0.15f, 1f), new Color(0.45f, 0.14f, 0.16f, 1f), rHover ? 1f : 0f),
                new Color(1f, 1f, 1f, 0.1f), 1f));
            bs.normal.textColor = new Color(0.95f, 0.55f, 0.55f);
            GUI.Label(reset, "Reset", bs);
            if (rHover && e.type == EventType.MouseDown && e.button == 0)
            {
                ResetAll();
                e.Use();
            }
        }
    }
}
