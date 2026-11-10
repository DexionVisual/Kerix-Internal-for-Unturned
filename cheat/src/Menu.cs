using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Kerix
{
    public static class Menu
    {
        private static Rect _rect = new Rect(90f, 70f, 720f, 440f);
        private static int _tab = 1;
        private static int _lastTab = 1;
        private static readonly string[] Tabs = { "Aimbot", "Visuals", "Exploit", "Misc", "Settings" };
        private static bool _drag;
        private static Vector2 _dragOff;
        private static string _tipKey;
        private static string _tipText;
        private static float _tipSince;
        private static Vector2 _tipMouse;
        private static bool _tipActive;
        private static GUIStyle _tipStyle;
        private static void TipHover(Event e, string label)
        {
            if (_tipKey != label)
            {
                _tipKey = label;
                _tipText = Tooltips.Get(label);
                _tipSince = Time.unscaledTime;
                return;
            }
            if (_tipText != null && Time.unscaledTime - _tipSince > 0.6f)
            {
                _tipActive = true;
                _tipMouse = e.mousePosition;
            }
        }
        private static readonly Dictionary<string, float> _anim = new Dictionary<string, float>();
        private static float Anim(string key, float target, float speed)
        {
            float cur;
            _anim.TryGetValue(key, out cur);
            cur = Ui.Ease(cur, target, speed, Time.unscaledDeltaTime);
            _anim[key] = cur;
            return cur;
        }
        public static void Draw(float anim)
        {
            Event e = Event.current;
            Vector2 m = e.mousePosition;
            _tipActive = false;
            if (_openColor != null && e.type == EventType.MouseDown &&
                !PopupRect().Contains(e.mousePosition) && !_rect.Contains(e.mousePosition))
            {
                CloseColorPopup();
                e.Use();
            }
            float slide = (1f - Ui.Smooth(anim)) * 34f;
            Rect head = new Rect(_rect.x, _rect.y, _rect.width, 58f);
            if (e.type == EventType.MouseDown && e.button == 0 && head.Contains(m) && anim > 0.9f)
            {
                _drag = true;
                _dragOff = m - new Vector2(_rect.x, _rect.y);
                e.Use();
            }
            if (_drag && e.type == EventType.MouseDrag)
            {
                _rect.x = m.x - _dragOff.x;
                _rect.y = m.y - _dragOff.y;
                e.Use();
            }
            if (e.type == EventType.MouseUp && _drag)
            {
                _drag = false;
                e.Use();
            }
            Rect r = new Rect(_rect.x, _rect.y + slide, _rect.width, _rect.height);
            int w = (int)r.width, h = (int)r.height;
            Ui.Tex(r, Ui.Rounded(w, h, 16f, Ui.PanelBg, Color.clear, 0f));
            Ui.Tex(r, Ui.Rounded(w, h, 16f, Color.clear, Ui.Royal, 1.5f));
            DrawLogo(new Rect(r.x + 22f, r.y + 13f, 240f, 34f), 24);
            GUIStyle sub = Ui.Style(11, bold: true);
            sub.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(r.x + 22f, r.y + 40f, 240f, 16f), "U N T U R N E D", sub);
            Ui.RectFill(new Rect(r.x + 20f, r.y + 62f, r.width - 40f, 1.2f), new Color(1f, 1f, 1f, 0.07f));
            for (int i = 0; i < Tabs.Length; i++)
            {
                Rect tr = new Rect(r.x + 16f, r.y + 76f + i * 52f, 152f, 42f);
                bool hover = tr.Contains(m);
                bool sel = _tab == i;
                float hv = Anim("tab:" + i, hover && !sel ? 1f : 0f, 11f);
                float sv = Anim("tabsel:" + i, sel ? 1f : 0f, 13f);
                Color bgc = Color.Lerp(new Color(0.085f, 0.09f, 0.125f, 1f), Ui.RoyalDark, Mathf.Max(hv, sv));
                Color border = Color.Lerp(new Color(1f, 1f, 1f, 0.06f), Ui.Royal, sv);
                Ui.Tex(tr, Ui.Rounded(152, 42, 10f, bgc, border, sv > 0.02f ? 1.3f : 1f));
                if (sv > 0.02f)
                {
                    Ui.Tex(new Rect(tr.x, tr.y + 10f, 3f * sv, tr.height - 20f),
                        Ui.Rounded(6, 24, 3f, Ui.RoyalLight, Color.clear, 0f));
                }
                GUIStyle ts = Ui.Style(13, bold: sel, anchor: TextAnchor.MiddleCenter);
                ts.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, Mathf.Max(hv * 0.6f, sv));
                GUI.Label(tr, Tabs[i], ts);
                if (hover && e.type == EventType.MouseDown && e.button == 0)
                {
                    _tab = i;
                    e.Use();
                }
            }
            Rect content = new Rect(r.x + 184f, r.y + 76f, r.width - 200f, r.height - 92f);
            if (_tab != _lastTab)
            {
                _lastTab = _tab;
                _anim["tabsw"] = 0f;
            }
            float tsw = Anim("tabsw", 1f, 13f);
            float tse = Ui.Smooth(tsw);
            Ui.PushFade(tse);
            Rect shifted = new Rect(content.x, content.y + (1f - tse) * 16f, content.width, content.height);
            switch (_tab)
            {
                case 0: DrawAimbot(shifted, e); break;
                case 1: DrawVisuals(shifted, e); break;
                case 2: DrawExploit(shifted, e); break;
                case 3: DrawMisc(shifted, e); break;
                case 4: DrawSettings(shifted, e); break;
            }
            Ui.PopFade();
            DrawColorPopup(e);
            DrawBindPopup(e);
            DrawTooltip();
        }
        private static void DrawTooltip()
        {
            Event e = Event.current;
            if (e != null && e.type == EventType.Repaint)
            {
                if (!_tipActive)
                {
                    _tipKey = null;
                    return;
                }
                if (_tipText == null) return;
                if (_tipStyle == null)
                {
                    _tipStyle = new GUIStyle
                    {
                        font = Ui.GetFont(11),
                        fontSize = 11,
                        wordWrap = true,
                        alignment = TextAnchor.UpperLeft,
                    };
                    _tipStyle.normal.textColor = new Color(0.85f, 0.88f, 0.98f);
                }
                const float w = 250f;
                float h = _tipStyle.CalcHeight(new GUIContent(_tipText), w - 20f) + 16f;
                Vector2 p = _tipMouse + new Vector2(16f, 12f);
                if (p.x + w > Screen.width - 8f) p.x = _tipMouse.x - w - 12f;
                if (p.y + h > Screen.height - 8f) p.y = Screen.height - h - 8f;
                Rect r = new Rect(p.x, p.y, w, h);
                Ui.Tex(r, Ui.Rounded((int)w, Mathf.RoundToInt(h), 8f,
                    new Color(0.05f, 0.055f, 0.09f, 0.97f), Ui.Royal, 1.2f));
                GUI.Label(new Rect(r.x + 10f, r.y + 8f, w - 20f, h - 16f), _tipText, _tipStyle);
            }
        }
        private static int _aimSub;
        private static int _lastAimSub;
        private static readonly string[] AimSubs = { "Silent", "Ballistics", "Recoil" };
        private static void DrawAimbot(Rect area, Event e)
        {
            float subW = (area.width - 16f) / 3f;
            for (int i = 0; i < AimSubs.Length; i++)
            {
                Rect tr = new Rect(area.x + i * (subW + 8f), area.y, subW, 30f);
                bool hover = tr.Contains(e.mousePosition);
                bool sel = _aimSub == i;
                float hv = Anim("aimsub:" + i, hover && !sel ? 1f : 0f, 11f);
                float sv = Anim("aimsubsel:" + i, sel ? 1f : 0f, 13f);
                Color bgc = Color.Lerp(new Color(0.085f, 0.09f, 0.125f, 1f), Ui.RoyalDark, Mathf.Max(hv, sv));
                Color border = Color.Lerp(new Color(1f, 1f, 1f, 0.06f), Ui.Royal, sv);
                Ui.Tex(tr, Ui.Rounded((int)subW, 30, 8f, bgc, border, sv > 0.02f ? 1.2f : 1f));
                GUIStyle ts = Ui.Style(13, bold: sel, anchor: TextAnchor.MiddleCenter);
                ts.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, Mathf.Max(hv * 0.6f, sv));
                GUI.Label(tr, AimSubs[i], ts);
                if (hover && e.type == EventType.MouseDown && e.button == 0)
                {
                    _aimSub = i;
                    e.Use();
                }
            }
            Rect body = new Rect(area.x, area.y + 40f, area.width, area.height - 44f);
            if (_aimSub != _lastAimSub)
            {
                _lastAimSub = _aimSub;
                _anim["aimsubsw"] = 0f;
            }
            float asw = Anim("aimsubsw", 1f, 14f);
            float ase = Ui.Smooth(asw);
            Ui.PushFade(ase);
            Rect bodyShifted = new Rect(body.x, body.y + (1f - ase) * 12f, body.width, body.height);
            switch (_aimSub)
            {
                case 0: DrawSilent(bodyShifted, e); break;
                case 1: DrawBallistics(bodyShifted, e); break;
                case 2: DrawRecoil(bodyShifted, e); break;
            }
            Ui.PopFade();
        }
        private static void DrawSilent(Rect area, Event e)
        {
            Scroll(area, _scrollA1, 190f, a =>
            {
                float y = 2f;
                y = ToggleRow(a, e, "Silent aim", ref Aimbot.SilentEnabled, y, "aim.silent");
                y = ToggleRow(a, e, "Draw FOV circle", ref Aimbot.SilentFovCircle, y);
                y = ToggleRow(a, e, "Skip teammates", ref Aimbot.SkipTeammates, y);
                y = ChoiceRow(a, e, "Target point", ref Aimbot.SilentTargetPoint, HeadChest, y);
                y = SliderRow(a, e, "FOV", ref Aimbot.SilentFov, 1f, 90f, "0°", y);
            });
        }
        private static void DrawBallistics(Rect area, Event e)
        {
            Scroll(area, _scrollA3, 120f, a =>
            {
                float y = 2f;
                y = ToggleRow(a, e, "Ballistics", ref Aimbot.BallisticsEnabled, y, "aim.ballistics");
                y = ToggleRow(a, e, "Lead targets", ref Aimbot.BallisticsLead, y);
                y = ToggleRow(a, e, "Weapon info", ref Aimbot.BallisticsInfo, y);
                if (Aimbot.BallisticsInfo)
                {
                    GUIStyle info = Ui.Style(11, bold: true, anchor: TextAnchor.MiddleLeft);
                    info.normal.textColor = Ui.RoyalLight;
                    GUI.Label(new Rect(a.x + 6f, y + 2f, a.width - 12f, 18f),
                        Aimbot.WeaponInfoLine, info);
                }
            });
        }
        private static void DrawRecoil(Rect area, Event e)
        {
            Scroll(area, _scrollA2, 90f, a =>
            {
                float y = 2f;
                y = ToggleRow(a, e, "Recoil control", ref Aimbot.RecoilEnabled, y);
                y = SliderRow(a, e, "Strength", ref Aimbot.RecoilStrength, 0f, 100f, "0%", y);
            });
        }
        private static readonly string[] HeadChest = { "Head", "Chest" };
        private class ScrollState
        {
            public Vector2 target;
            public Vector2 disp;
        }
        private static readonly ScrollState _scrollV = new ScrollState();
        private static readonly ScrollState _scrollM = new ScrollState();
        private static readonly ScrollState _scrollA0 = new ScrollState();
        private static readonly ScrollState _scrollA1 = new ScrollState();
        private static readonly ScrollState _scrollA2 = new ScrollState();
        private static readonly ScrollState _scrollA3 = new ScrollState();
        private static void Scroll(Rect outer, ScrollState st, float contentH, Action<Rect> draw)
        {
            Rect view = new Rect(0f, 0f, outer.width - 14f, Mathf.Max(contentH, outer.height));
            float maxScroll = Mathf.Max(0f, view.height - outer.height);
            Event e = Event.current;
            if (e != null && e.type == EventType.ScrollWheel && outer.Contains(e.mousePosition))
            {
                st.target = new Vector2(0f, Mathf.Clamp(st.target.y + e.delta.y * 24f, 0f, maxScroll));
                e.Use();
            }
            st.target = new Vector2(0f, Mathf.Clamp(st.target.y, 0f, maxScroll));
            GUI.BeginScrollView(outer, st.disp, view, GUIStyle.none, GUIStyle.none);
            st.disp = Ui.Ease(st.disp, st.target, 24f, Time.unscaledDeltaTime);
            draw(new Rect(0f, 0f, view.width, view.height));
            GUI.EndScrollView();
            if (view.height > outer.height + 1f)
            {
                Rect track = new Rect(outer.x + outer.width - 8f, outer.y, 3f, outer.height);
                Ui.RectFill(track, new Color(1f, 1f, 1f, 0.05f));
                float barH = Mathf.Max(40f, outer.height * (outer.height / view.height));
                float barY = outer.y + (outer.height - barH) * Mathf.Clamp01(st.disp.y / maxScroll);
                Ui.Tex(new Rect(track.x, barY, 3f, barH),
                    Ui.Rounded(3, Mathf.RoundToInt(barH), 1.5f, Ui.Royal, Color.clear, 0f));
            }
        }
        private static float ButtonRow(Rect area, Event e, string label, Action onClick, float y, string bindId = null)
        {
            Rect row = new Rect(area.x, y, area.width, 32f);
            bool hover = row.Contains(e.mousePosition);
            Bind bind = bindId != null ? Binds.Find(bindId) : null;
            Rect chip = Rect.zero;
            bool chipHover = false;
            bool chipVisible = false;
            if (bind != null)
            {
                chip = new Rect(area.x + area.width - 40f, y + 5f, 36f, 22f);
                chipHover = chip.Contains(e.mousePosition);
                chipVisible = bind.Key != KeyCode.None || chipHover;
            }
            Ui.Tex(row, Ui.Rounded((int)row.width, 32, 8f,
                new Color(0.10f, 0.105f, 0.15f, 1f),
                new Color(1f, 1f, 1f, 0.08f), 1f));
            GUIStyle bs = Ui.Style(12, bold: true, anchor: TextAnchor.MiddleCenter);
            bs.normal.textColor = Ui.TextDim;
            float labelW = bind != null ? row.width - 60f : row.width;
            GUI.Label(new Rect(row.x, row.y, labelW, row.height), label, bs);
            if (bind != null && chipVisible)
            {
                bool bound = bind.Key != KeyCode.None;
                Color cbg = bound
                    ? new Color(0.08f, 0.10f, 0.20f, 1f)
                    : new Color(0.10f, 0.105f, 0.15f, 0.85f);
                Color cbd = bound
                    ? new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.55f)
                    : new Color(1f, 1f, 1f, 0.12f);
                Ui.Tex(chip, Ui.Rounded(36, 22, 6f, cbg, cbd, 1f));
                GUIStyle cs = Ui.Style(10, bold: true, anchor: TextAnchor.MiddleCenter);
                cs.normal.textColor = bound ? Ui.RoyalLight : new Color(0.5f, 0.54f, 0.66f, 0.8f);
                GUI.Label(chip, bound ? bind.KeyName : "key", cs);
            }
            if (hover && e.type == EventType.MouseDown && e.button == 0)
            {
                if (bind != null && chipHover) OpenBindPopup(bindId);
                else onClick();
                e.Use();
            }
            if (hover) TipHover(e, label);
            return y + 36f;
        }
        private static string _activeSlider;
        private static float ChoiceRow(Rect area, Event e, string label, ref int val, string[] options, float y)
        {
            Rect row = new Rect(area.x, y, area.width, 30f);
            Rect btn = new Rect(area.x + area.width - 86f, y + 3f, 80f, 24f);
            bool hover = row.Contains(e.mousePosition);
            if (btn.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                val = (val + 1) % options.Length;
                e.Use();
            }
            GUIStyle ls = Ui.Style(12, anchor: TextAnchor.MiddleLeft);
            ls.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(row.x + 6f, row.y, row.width - 96f, row.height), label, ls);
            Ui.Tex(btn, Ui.Rounded(80, 24, 7f,
                new Color(0.10f, 0.105f, 0.15f, 1f), new Color(1f, 1f, 1f, 0.08f), 1f));
            GUIStyle bs = Ui.Style(11, bold: true, anchor: TextAnchor.MiddleCenter);
            bs.normal.textColor = Ui.RoyalLight;
            GUI.Label(btn, options[val], bs);
            return y + 32f;
        }
        private static float SliderRow(Rect area, Event e, string label, ref float val,
                                       float min, float max, string fmt, float y)
        {
            Rect row = new Rect(area.x, y, area.width, 42f);
            Rect track = new Rect(area.x + 6f, y + 26f, area.width - 12f, 6f);
            Rect hit = new Rect(area.x, y, area.width, 42f);
            bool hover = hit.Contains(e.mousePosition);
            float hv = Anim("slider:" + label, hover ? 1f : 0f, 11f);
            bool active = _activeSlider == label;
            if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition))
            {
                _activeSlider = label;
                float t0 = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                val = Mathf.Round((min + t0 * (max - min)) * 100f) / 100f;
                e.Use();
            }
            if (active && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove) && e.button == 0)
            {
                float t = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                val = min + t * (max - min);
                e.Use();
            }
            if (active && e.type == EventType.MouseUp)
            {
                _activeSlider = null;
                e.Use();
            }
            float t01 = Mathf.Clamp01((val - min) / (max - min));
            float d01 = active ? t01 : Anim("sval:" + label, t01, 18f);
            GUIStyle ls = Ui.Style(12, anchor: TextAnchor.MiddleLeft);
            ls.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, Mathf.Max(hv * 0.5f, active ? 1f : 0f));
            GUI.Label(new Rect(row.x + 6f, row.y, row.width - 70f, 20f), label, ls);
            GUIStyle vs = Ui.Style(12, bold: true, anchor: TextAnchor.UpperRight);
            vs.normal.textColor = Ui.RoyalLight;
            GUI.Label(new Rect(row.x + row.width - 76f, row.y, 70f, 20f), val.ToString(fmt), vs);
            Ui.RectFill(new Rect(track.x, track.y, track.width, track.height), new Color(0.13f, 0.135f, 0.175f, 1f));
            Ui.RectFill(new Rect(track.x, track.y, track.width * d01, track.height),
                Color.Lerp(Ui.RoyalDark, Ui.Royal, Mathf.Max(d01, hv * 0.3f)));
            float knobX = track.x + track.width * d01;
            Ui.Tex(new Rect(knobX - 7f, track.y - 4f, 14f, 14f),
                Ui.Rounded(14, 14, 7f, active ? Color.white : Ui.RoyalLight, Color.clear, 0f));
            return y + 46f;
        }
        private static void DrawLogo(Rect r, float size, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            const string text = "Kerix.zyx";
            StringBuilder sb = new StringBuilder(text.Length * 32);
            for (int i = 0; i < text.Length; i++)
            {
                float k = i / (float)(text.Length - 1);
                Color c = Color.Lerp(Ui.RoyalLight, Ui.Royal, k);
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>')
                  .Append(text[i]).Append("</color>");
            }
            GUIStyle st = Ui.Style(size, bold: true, rich: true, anchor: anchor);
            Ui.OutlinedLabel(r, text, sb.ToString(), st, Ui.RoyalLight);
        }
        private static void Section(Rect area, float y, string title)
        {
            GUIStyle section = Ui.Style(11, bold: true);
            section.normal.textColor = Ui.RoyalLight;
            GUI.Label(new Rect(area.x, y, area.width, 16f), title, section);
            Ui.RectFill(new Rect(area.x + 78f, y + 8f, area.width - 78f, 1f), new Color(1f, 1f, 1f, 0.05f));
        }
        private static void DrawVisuals(Rect area, Event e)
        {
            Scroll(area, _scrollV, MeasureVisuals(), a =>
            {
                float y = 0f;
                Section(a, y, "PLAYERS");
                y += 24f;
                y = ToggleRow(a, e, "Player ESP", ref Esp.Enabled, y, "esp.players");
                if (Esp.Enabled)
                {
                    y = ToggleRow(a, e, "Box", ref Esp.Box, y);
                    y = ToggleRow(a, e, "Skeleton", ref Esp.Skeleton, y);
                    y = ToggleRow(a, e, "Name", ref Esp.Name, y);
                    y = ToggleRow(a, e, "Snaplines", ref Esp.Snaplines, y);
                    y = ColorRow(a, e, "Color", () => Esp.PlayerColor, v => Esp.PlayerColor = v, y);
                }
                y += 6f;
                Section(a, y, "CHAMS");
                y += 24f;
                y = ToggleRow(a, e, "Players", ref Chams.PlayersEnabled, y, "chams.players");
                if (Chams.PlayersEnabled)
                {
                    y = ChoiceRow(a, e, "Player style", ref Chams.PlayersStyle, Chams.StyleNames, y);
                    y = ColorRow(a, e, "Player color", () => Chams.PlayersColor, v => Chams.PlayersColor = v, y);
                    y = ToggleRow(a, e, "Through walls", ref Chams.PlayersThroughWalls, y);
                }
                y = ToggleRow(a, e, "Hands", ref Chams.HandsEnabled, y, "chams.hands");
                if (Chams.HandsEnabled)
                {
                    y = ChoiceRow(a, e, "Hand style", ref Chams.HandsStyle, Chams.StyleNames, y);
                    y = ColorRow(a, e, "Hand color", () => Chams.HandsColor, v => Chams.HandsColor = v, y);
                }
                y = ToggleRow(a, e, "Weapon", ref Chams.WeaponEnabled, y, "chams.weapon");
                if (Chams.WeaponEnabled)
                {
                    y = ChoiceRow(a, e, "Weapon style", ref Chams.WeaponStyle, Chams.StyleNames, y);
                    y = ColorRow(a, e, "Weapon color", () => Chams.WeaponColor, v => Chams.WeaponColor = v, y);
                }
                if (GlowUsed())
                {
                    y = SliderRow(a, e, "Glow strength", ref Chams.GlowStrength, 0f, 10f, "0.0", y);
                }
                y += 6f;
                Section(a, y, "WORLD");
                y += 24f;
                y = ToggleRow(a, e, "Zombies", ref Esp.Zombies, y, "esp.zombies");
                y = ColorRow(a, e, "Zombie color", () => Esp.ZombieColor, v => Esp.ZombieColor = v, y);
                y = ToggleRow(a, e, "Vehicles", ref Esp.Vehicles, y, "esp.vehicles");
                y = ColorRow(a, e, "Vehicle color", () => Esp.VehicleColor, v => Esp.VehicleColor = v, y);
                y = ToggleRow(a, e, "Ground items", ref Esp.Items, y, "esp.items");
                y += 6f;
                Section(a, y, "LIGHTING");
                y += 24f;
                y = ToggleRow(a, e, "Always day", ref Theme.Enabled, y, "theme.day");
                y += 6f;
                Section(a, y, "MAP");
                y += 24f;
                y = ToggleRow(a, e, "Minimap", ref Minimap.Enabled, y, "visuals.minimap");
                if (Minimap.Enabled)
                {
                    y = ToggleRow(a, e, "Satellite image", ref Minimap.UseSatellite, y);
                    y = ToggleRow(a, e, "Rotate with player", ref Minimap.Rotate, y);
                    y = ToggleRow(a, e, "Players", ref Minimap.ShowPlayers, y);
                    y = ToggleRow(a, e, "Vehicles", ref Minimap.ShowVehicles, y);
                    y = ToggleRow(a, e, "Zombies", ref Minimap.ShowZombies, y);
                    y = SliderRow(a, e, "Size", ref Minimap.Size, 140f, 400f, "0px", y);
                    y = SliderRow(a, e, "Zoom (m across)", ref Minimap.ZoomMeters, 80f, 2400f, "0m", y);
                }
                y += 6f;
                Section(a, y, "CAMERA");
                y += 24f;
                y = ToggleRow(a, e, "Freecam", ref Freecam.Enabled, y, "visuals.freecam");
                if (Freecam.Enabled)
                    y = SliderRow(a, e, "Freecam speed", ref Freecam.Speed, 2f, 60f, "0.0", y);
                y = ToggleRow(a, e, "View FOV", ref Chams.ViewFovEnabled, y);
                if (Chams.ViewFovEnabled)
                    y = SliderRow(a, e, "FOV", ref Chams.ViewFovValue, 60f, 130f, "0", y);
                y += 6f;
                Section(a, y, "HUD");
                y += 24f;
                y = ToggleRow(a, e, "FPS counter", ref Misc.ShowFps, y);
                y = ButtonRow(a, e, "Edit HUD", () =>
                {
                    Hud.EditMode = true;
                    if (KerixMain.Instance != null) KerixMain.Instance.MenuOpen = false;
                }, y + 4f);
            });
        }
        private static bool GlowUsed()
        {
            return (Chams.PlayersEnabled && Chams.PlayersStyle >= 2) ||
                   (Chams.HandsEnabled && Chams.HandsStyle >= 2) ||
                   (Chams.WeaponEnabled && Chams.WeaponStyle >= 2);
        }
        private static float MeasureVisuals()
        {
            float y = 24f;
            y += 32f;
            if (Esp.Enabled) y += 4f * 32f + 32f;
            y += 6f + 24f;
            y = MeasureChamBlock(y, Chams.PlayersEnabled, 2f);
            y = MeasureChamBlock(y, Chams.HandsEnabled, 1f);
            y = MeasureChamBlock(y, Chams.WeaponEnabled, 1f);
            if (GlowUsed()) y += 2f * 46f;
            y += 6f + 24f;
            y += 3f * 32f;
            y += 2f * 32f;
            y += 6f + 24f;
            y += 32f;
            if (Theme.Enabled) y += 0f;
            y += 6f + 24f;
            y += 32f;
            if (Minimap.Enabled) y += 5f * 32f + 2f * 46f;
            y += 6f + 24f;
            y += 32f;
            if (Freecam.Enabled) y += 46f;
            y += 32f;
            if (Chams.ViewFovEnabled) y += 46f;
            y += 6f + 24f;
            y += 3f * 32f;
            y += 40f;
            return y;
        }
        private static float MeasureChamBlock(float y, bool on, float extraRows)
        {
            y += 32f;
            if (on) y += (1f + extraRows) * 32f;
            return y;
        }
        private static string _openColor;
        private static Func<Color> _getColor;
        private static Action<Color> _setColor;
        private static void OpenColorPopup(string label, Func<Color> get, Action<Color> set)
        {
            _openColor = label;
            _getColor = get;
            _setColor = set;
            CloseBindPopup();
        }
        private static void CloseColorPopup()
        {
            _openColor = null;
            _getColor = null;
            _setColor = null;
        }
        private static Rect PopupRect()
        {
            const float pw = 224f, ph = 292f;
            float x = _rect.x + _rect.width + 12f;
            if (x + pw > Screen.width - 8f) x = _rect.x - pw - 12f;
            float y = Mathf.Clamp(_rect.y + 66f, 8f, Mathf.Max(8f, Screen.height - ph - 8f));
            return new Rect(x, y, pw, ph);
        }
        private static float ColorRow(Rect area, Event e, string label,
                                      Func<Color> get, Action<Color> set, float y)
        {
            Rect row = new Rect(area.x, y, area.width, 30f);
            Rect swatch = new Rect(area.x + area.width - 86f, y + 4f, 80f, 22f);
            bool open = _openColor == label;
            bool hover = row.Contains(e.mousePosition);
            Color color = get();
            if (swatch.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                if (open) CloseColorPopup();
                else OpenColorPopup(label, get, set);
                e.Use();
            }
            float hv = Anim("color:" + label, hover ? 1f : 0f, 11f);
            if (hv > 0.01f)
                Ui.RectFill(new Rect(row.x, row.y, row.width, row.height),
                    new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.10f * hv));
            GUIStyle ls = Ui.Style(12, anchor: TextAnchor.MiddleLeft);
            ls.normal.textColor = Color.Lerp(Ui.TextDim, Color.white,
                Mathf.Max(hv * 0.5f, open ? 0.5f : 0f));
            GUI.Label(new Rect(row.x + 6f, row.y, row.width - 96f, row.height), label, ls);
            Ui.RectFill(swatch, color);
            Ui.RectOutline(swatch, open ? Ui.RoyalLight : new Color(1f, 1f, 1f, 0.2f), 1.5f);
            return y + 32f;
        }
        private static Texture2D _wheelTex;
        private static Texture2D _svTex;
        private static int _svHueBuilt = -1;
        private static Texture2D BuildHueWheel(int size, float rOuter, float rInner)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            Color32[] px = new Color32[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((rOuter - d) / 1.5f) * Mathf.Clamp01((d - rInner) / 1.5f);
                    Color col = a > 0f
                        ? Color.HSVToRGB(Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI), 1f), 1f, 1f)
                        : Color.clear;
                    col.a = a;
                    px[y * size + x] = col;
                }
            }
            t.SetPixels32(px);
            t.Apply(false);
            return t;
        }
        private static Texture2D GetSvTexture(int hueDeg, int w, int h)
        {
            if (_svTex == null)
            {
                _svTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                _svTex.hideFlags = HideFlags.HideAndDontSave;
            }
            if (_svTex.width != w || _svTex.height != h)
            {
                _svTex.Reinitialize(w, h, TextureFormat.RGBA32, false);
                _svHueBuilt = -1;
            }
            if (_svHueBuilt == hueDeg) return _svTex;
            Color32[] px = new Color32[w * h];
            float hue = hueDeg / 360f;
            for (int y = 0; y < h; y++)
            {
                float v = 1f - y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float s = x / (float)(w - 1);
                    Color col = Color.HSVToRGB(hue, s, v);
                    px[y * w + x] = col;
                }
            }
            _svTex.SetPixels32(px);
            _svTex.Apply(false);
            _svHueBuilt = hueDeg;
            return _svTex;
        }
        private static void DrawColorPopup(Event e)
        {
            if (_openColor == null || _getColor == null || _setColor == null) return;
            Rect p = PopupRect();
            int pw = (int)p.width, ph = (int)p.height;
            Ui.Tex(p, Ui.Rounded(pw, ph, 12f, Ui.PanelBg, Ui.Royal, 1.5f));
            GUIStyle hs = Ui.Style(11, bold: true);
            hs.normal.textColor = Ui.RoyalLight;
            GUI.Label(new Rect(p.x + 14f, p.y + 10f, 100f, 16f), "COLOR", hs);
            Color col = _getColor();
            float hue, sat, val;
            Color.RGBToHSV(col, out hue, out sat, out val);
            int hueDeg = Mathf.RoundToInt(hue * 360f) % 360;
            GUIStyle hexs = Ui.Style(12, bold: true, anchor: TextAnchor.UpperRight);
            hexs.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(p.x + p.width - 108f, p.y + 10f, 94f, 16f),
                "#" + ColorUtility.ToHtmlStringRGB(col), hexs);
            const int wheelSize = 148;
            Rect wheel = new Rect(p.x + (p.width - wheelSize) * 0.5f, p.y + 30f, wheelSize, wheelSize);
            if (_wheelTex == null)
                _wheelTex = BuildHueWheel(wheelSize, wheelSize * 0.5f - 1f, wheelSize * 0.5f - 24f);
            Ui.Tex(wheel, _wheelTex);
            float rOuter = wheelSize * 0.5f - 1f, rInner = wheelSize * 0.5f - 24f;
            float rMid = (rOuter + rInner) * 0.5f;
            Rect wheelHit = new Rect(wheel.x - 6f, wheel.y - 6f, wheel.width + 12f, wheel.height + 12f);
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
                e.button == 0 && wheelHit.Contains(e.mousePosition))
            {
                Vector2 d = e.mousePosition - wheel.center;
                hue = Mathf.Repeat(Mathf.Atan2(d.y, d.x) / (2f * Mathf.PI), 1f);
                _setColor(Color.HSVToRGB(hue, sat, val));
                hueDeg = Mathf.RoundToInt(hue * 360f) % 360;
                e.Use();
            }
            float ang = hue * 2f * Mathf.PI;
            Vector2 mark = wheel.center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rMid;
            Ui.Tex(new Rect(mark.x - 8f, mark.y - 8f, 16f, 16f),
                Ui.Rounded(16, 16, 8f, Color.clear, Color.white, 2f));
            Rect sv = new Rect(p.x + 18f, p.y + 190f, p.width - 36f, 56f);
            Ui.Tex(sv, GetSvTexture(hueDeg, (int)sv.width, (int)sv.height));
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
                e.button == 0 && sv.Contains(e.mousePosition))
            {
                sat = Mathf.Clamp01((e.mousePosition.x - sv.x) / sv.width);
                val = 1f - Mathf.Clamp01((e.mousePosition.y - sv.y) / sv.height);
                _setColor(Color.HSVToRGB(hue, sat, val));
                e.Use();
            }
            Vector2 svm = new Vector2(sv.x + sat * sv.width, sv.y + (1f - val) * sv.height);
            Ui.Tex(new Rect(svm.x - 6f, svm.y - 6f, 12f, 12f),
                Ui.Rounded(12, 12, 6f, Color.clear, Color.white, 1.6f));
            Rect prev = new Rect(p.x + 18f, p.y + p.height - 34f, p.width - 36f, 22f);
            Ui.RectFill(prev, col);
            Ui.RectOutline(prev, new Color(1f, 1f, 1f, 0.2f), 1.5f);
            GUIStyle ps = Ui.Style(12, bold: true, anchor: TextAnchor.MiddleCenter);
            float lum = col.r * 0.3f + col.g * 0.6f + col.b * 0.1f;
            ps.normal.textColor = lum > 0.55f ? new Color(0.06f, 0.06f, 0.09f, 0.95f) : Color.white;
            GUI.Label(prev, "#" + ColorUtility.ToHtmlStringRGB(col), ps);
        }
        private static readonly ScrollState _scrollE = new ScrollState();
        private static void DrawExploit(Rect area, Event e)
        {
            Scroll(area, _scrollE, 700f, a =>
            {
                Section(a, 0f, "MOVEMENT");
                float y = 24f;
                y = ToggleRow(a, e, "Speed", ref Misc.SpeedEnabled, y, "misc.speed");
                if (Misc.SpeedEnabled)
                    y = SliderRow(a, e, "Speed x", ref Misc.SpeedMult, 1.1f, 5f, "0.0x", y);
                y = ToggleRow(a, e, "Jump", ref Misc.JumpEnabled, y, "misc.jump");
                if (Misc.JumpEnabled)
                    y = SliderRow(a, e, "Jump x", ref Misc.JumpMult, 1.1f, 8f, "0.0x", y);
                y = ToggleRow(a, e, "Climb boost", ref Movement.ClimbBoostEnabled, y, "movement.climb");
                y += 6f;
                Section(a, y, "VEHICLES");
                y += 24f;
                y = ToggleRow(a, e, "Steering overflow", ref VehicleSteer.SteerEnabled, y, "exploit.vsteer");
                if (Misc.SpeedEnabled || Misc.JumpEnabled)
                {
                    y += 6f;
                    Section(a, y, "SYNC");
                    y += 24f;
                    y = ToggleRow(a, e, "Anti rubberband", ref Misc.AntiRubberband, y);
                    y = ToggleRow(a, e, "Sync on disable", ref Movement.AutoSyncOnDisable, y);
                    y = ToggleRow(a, e, "Leash", ref Movement.AutoSyncLeash, y);
                    if (Movement.AutoSyncLeash)
                        y = SliderRow(a, e, "Leash x", ref Movement.AutoSyncDistance, 10f, 200f, "0m", y);
                    y = SliderRow(a, e, "Glide", ref Movement.GlideSpeed, 4f, 80f, "0m/s", y);
                }
            });
        }
        private static void DrawMisc(Rect area, Event e)
        {
            Scroll(area, _scrollM, 300f, a =>
            {
                Section(a, 0f, "WEAPONS / WORLD");
                float y = 24f;
                y = ToggleRow(a, e, "Fast reload", ref Misc.FastReloadEnabled, y, "misc.fastreload");
                if (Misc.FastReloadEnabled)
                    y = SliderRow(a, e, "Reload speed", ref Misc.FastReload, 10f, 90f, "0%", y);
                y = ToggleRow(a, e, "No vehicle fuel", ref Misc.NoVehicleFuel, y, "misc.nofuel");
                y += 6f;
                Section(a, y, "HUD");
                y += 24f;
                y = ToggleRow(a, e, "Hitmarker", ref Misc.HitmarkerEnabled, y);
                y = ToggleRow(a, e, "Hitmarker sound", ref Misc.HitmarkerSound, y);
            });
        }
        private static void DrawSettings(Rect area, Event e)
        {
            Section(area, area.y, "CONFIG");
            float y = area.y + 24f;
            y = ButtonRow(area, e, "Save config", Config.Save, y);
            y = ButtonRow(area, e, "Load config", Config.Load, y);
            y = ButtonRow(area, e, "Reset defaults", Config.Reset, y);
            string status = Config.StatusNow;
            if (status.Length > 0)
            {
                GUIStyle ss = Ui.Style(12, bold: true, anchor: TextAnchor.UpperRight);
                ss.normal.textColor = Ui.RoyalLight;
                GUI.Label(new Rect(area.x, area.y + 24f, area.width - 6f, 90f), status, ss);
            }
            y += 10f;
            Section(area, y, "ABOUT");
            y += 24f;
            GUIStyle ab = Ui.Style(12, rich: true);
            ab.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(area.x + 6f, y, area.width, 18f), "Kerix.xyz  v2.0", ab);
            y += 18f;
            GUI.Label(new Rect(area.x + 6f, y, area.width, 18f),
                "ins - menu      <color=#6B94FF>tg - @KerixUnturned</color>", ab);
        }
        private static float ToggleRow(Rect area, Event e, string label, ref bool val, float y, string bindId = null)
        {
            Rect row = new Rect(area.x, y, area.width, 30f);
            Rect track = new Rect(area.x + area.width - 46f, y + 4f, 42f, 22f);
            bool hover = row.Contains(e.mousePosition);
            Bind bind = bindId != null ? Binds.Find(bindId) : null;
            Rect chip = Rect.zero;
            bool chipHover = false;
            bool chipVisible = false;
            if (bind != null)
            {
                chip = new Rect(area.x + area.width - 46f - 40f, y + 4f, 36f, 22f);
                chipHover = chip.Contains(e.mousePosition);
                chipVisible = bind.Key != KeyCode.None || chipHover;
            }
            if (hover && e.type == EventType.MouseDown && e.button == 0)
            {
                if (bind != null && chipHover)
                {
                    OpenBindPopup(bindId);
                }
                else
                {
                    val = !val;
                }
                e.Use();
            }
            if (hover) TipHover(e, label);
            float on = Anim("tgl:" + label, val ? 1f : 0f, 12f);
            GUIStyle ls = Ui.Style(12, anchor: TextAnchor.MiddleLeft);
            ls.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, on);
            float labelW = bind != null ? row.width - 104f : row.width - 60f;
            GUI.Label(new Rect(row.x + 6f, row.y, labelW, row.height), label, ls);
            if (bind != null && chipVisible)
            {
                bool bound = bind.Key != KeyCode.None;
                Color cbg = bound
                    ? Color.Lerp(new Color(0.08f, 0.10f, 0.20f, 1f), Ui.RoyalDark, on * 0.5f)
                    : new Color(0.10f, 0.105f, 0.15f, 0.85f);
                Color cbd = bound
                    ? new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.55f)
                    : new Color(1f, 1f, 1f, 0.12f);
                Ui.Tex(chip, Ui.Rounded(36, 22, 6f, cbg, cbd, 1f));
                GUIStyle cs = Ui.Style(10, bold: true, anchor: TextAnchor.MiddleCenter);
                cs.normal.textColor = bound
                    ? Ui.RoyalLight
                    : new Color(0.5f, 0.54f, 0.66f, 0.8f);
                GUI.Label(chip, bound ? bind.KeyName : "key", cs);
            }
            Color trackBg = Color.Lerp(new Color(0.13f, 0.135f, 0.175f, 1f), Ui.RoyalDark, on);
            Color trackBd = Color.Lerp(new Color(1f, 1f, 1f, 0.08f), Ui.Royal, on);
            Ui.Tex(track, Ui.Rounded(42, 22, 11f, trackBg, trackBd, 1f));
            float ksize = Mathf.Lerp(13f, 16f, on);
            float cx = Mathf.Lerp(track.x + 2f + 8f, track.x + track.width - 2f - 8f, on);
            Rect knob = new Rect(cx - ksize * 0.5f, track.y + 3f + (16f - ksize) * 0.5f, ksize, ksize);
            Ui.Tex(knob, Ui.Rounded(16, 16, 8f,
                Color.Lerp(new Color(0.55f, 0.57f, 0.66f, 1f), Ui.RoyalLight, on), Color.clear, 0f));
            return y + 32f;
        }
        private static string _openBindId;
        private static void OpenBindPopup(string id)
        {
            _openBindId = id;
            Binds.CaptureFor = null;
            CloseColorPopup();
        }
        private static void CloseBindPopup()
        {
            _openBindId = null;
            Binds.CaptureFor = null;
        }
        private static Rect BindPopupRect()
        {
            const float pw = 240f, ph = 250f;
            float x = _rect.x + _rect.width + 12f;
            if (x + pw > Screen.width - 8f) x = _rect.x - pw - 12f;
            float y = Mathf.Clamp(_rect.y + 66f, 8f, Mathf.Max(8f, Screen.height - ph - 8f));
            return new Rect(x, y, pw, ph);
        }
        private static void DrawBindPopup(Event e)
        {
            if (_openBindId == null) return;
            Bind bind = Binds.Find(_openBindId);
            if (bind == null) { CloseBindPopup(); return; }
            bool capturing = Binds.CaptureFor == _openBindId;
            Rect p = BindPopupRect();
            int pw = (int)p.width, ph = (int)p.height;
            Ui.Tex(p, Ui.Rounded(pw, ph, 12f, Ui.PanelBg, Ui.Royal, 1.5f));
            GUIStyle hs = Ui.Style(11, bold: true);
            hs.normal.textColor = Ui.RoyalLight;
            GUI.Label(new Rect(p.x + 14f, p.y + 10f, p.width - 28f, 16f), "HOTKEY", hs);
            GUIStyle nm = Ui.Style(13, bold: true, anchor: TextAnchor.MiddleLeft);
            nm.normal.textColor = Color.white;
            GUI.Label(new Rect(p.x + 14f, p.y + 30f, p.width - 28f, 20f), bind.Label, nm);
            Rect cap = new Rect(p.x + 14f, p.y + 58f, p.width - 28f, 64f);
            bool capHover = cap.Contains(e.mousePosition);
            float cav = Anim("bindcap", capturing ? 1f : capHover ? 0.4f : 0f, 12f);
            Ui.Tex(cap, Ui.Rounded((int)cap.width, 64, 9f,
                Color.Lerp(new Color(0.085f, 0.09f, 0.125f, 1f), Ui.RoyalDark, cav * 0.6f),
                new Color(1f, 1f, 1f, Mathf.Lerp(0.08f, 0.45f, cav)), 1f));
            GUIStyle capSt = Ui.Style(12, bold: true, anchor: TextAnchor.MiddleCenter);
            capSt.normal.textColor = capturing ? Ui.RoyalLight : Color.white;
            GUI.Label(new Rect(cap.x, cap.y + 8f, cap.width, 24f),
                capturing ? "press any key..." : bind.Key == KeyCode.None ? "not bound" : bind.KeyName, capSt);
            GUIStyle capSub = Ui.Style(10, anchor: TextAnchor.MiddleCenter);
            capSub.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(cap.x, cap.y + 34f, cap.width, 16f),
                capturing ? "esc to cancel" : "click to bind", capSub);
            if (!capturing && capHover && e.type == EventType.MouseDown && e.button == 0)
            {
                Binds.CaptureFor = _openBindId;
                e.Use();
            }
            GUIStyle seg = Ui.Style(11, bold: true, anchor: TextAnchor.MiddleCenter);
            const float segW = 103f;
            for (int i = 0; i < 2; i++)
            {
                bool isHold = i == 0;
                Rect sr = new Rect(p.x + 14f + i * (segW + 6f), p.y + 134f, segW, 28f);
                bool sel = bind.HoldMode == isHold;
                bool sHover = sr.Contains(e.mousePosition);
                float sv = Anim("bindseg:" + i, sel ? 1f : sHover ? 0.4f : 0f, 12f);
                Ui.Tex(sr, Ui.Rounded((int)segW, 28, 8f,
                    Color.Lerp(new Color(0.085f, 0.09f, 0.125f, 1f), Ui.RoyalDark, sv),
                    sel ? Ui.Royal : new Color(1f, 1f, 1f, 0.08f), sel ? 1.3f : 1f));
                seg.normal.textColor = sel ? Color.white : Ui.TextDim;
                GUI.Label(sr, isHold ? "Hold" : "Toggle", seg);
                if (sHover && e.type == EventType.MouseDown && e.button == 0 && bind.HoldMode != isHold)
                {
                    bind.HoldMode = isHold;
                    bind.HoldActive = false;
                    e.Use();
                }
            }
            GUIStyle modeHint = Ui.Style(10, anchor: TextAnchor.MiddleLeft);
            modeHint.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(p.x + 14f, p.y + 170f, p.width - 28f, 16f),
                bind.HoldMode ? "hold" : "toggle", modeHint);
            GUIStyle btn = Ui.Style(11, bold: true, anchor: TextAnchor.MiddleCenter);
            Rect ub = new Rect(p.x + 14f, p.y + p.height - 40f, 103f, 28f);
            bool ubHover = ub.Contains(e.mousePosition);
            float uv = Anim("bindunbind", ubHover ? 1f : 0f, 12f);
            Ui.Tex(ub, Ui.Rounded(103, 28, 8f,
                Color.Lerp(new Color(0.085f, 0.09f, 0.125f, 1f), new Color(0.45f, 0.14f, 0.16f, 1f), uv),
                new Color(1f, 1f, 1f, 0.08f), 1f));
            btn.normal.textColor = new Color(0.95f, 0.55f, 0.55f);
            GUI.Label(ub, "Unbind", btn);
            if (ubHover && e.type == EventType.MouseDown && e.button == 0)
            {
                Binds.ClearKey(_openBindId);
                e.Use();
            }
            Rect cl = new Rect(p.x + 14f + segW + 6f, p.y + p.height - 40f, 103f, 28f);
            bool clHover = cl.Contains(e.mousePosition);
            float cv = Anim("bindclose", clHover ? 1f : 0f, 12f);
            Ui.Tex(cl, Ui.Rounded(103, 28, 8f,
                Color.Lerp(new Color(0.085f, 0.09f, 0.125f, 1f), Ui.RoyalDark, cv),
                new Color(1f, 1f, 1f, 0.08f), 1f));
            btn.normal.textColor = Color.white;
            GUI.Label(cl, "Close", btn);
            if (clHover && e.type == EventType.MouseDown && e.button == 0)
            {
                CloseBindPopup();
                e.Use();
            }
            if (capturing)
            {
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Insert)
                    {
                        Binds.CaptureFor = null;
                    }
                    else if (e.keyCode != KeyCode.None)
                    {
                        Binds.TryCapture(e.keyCode);
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseDown &&
                         !p.Contains(e.mousePosition) && !_rect.Contains(e.mousePosition))
                {
                    if (e.button >= 0 && e.button <= 6)
                        Binds.TryCapture(KeyCode.Mouse0 + e.button);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDown &&
                     !p.Contains(e.mousePosition) && !_rect.Contains(e.mousePosition))
            {
                CloseBindPopup();
                e.Use();
            }
        }
    }
}
