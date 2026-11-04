using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Kerix
{

    public static class Menu
    {
        private static Rect _rect = new Rect(90f, 70f, 570f, 505f);
        private static int _tab = 1;
        private static int _lastTab = 1;
        private static readonly string[] Tabs = { "Aimbot", "Visuals", "Misc", "Settings" };
        private static bool _drag;
        private static Vector2 _dragOff;

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

            Ui.Tex(new Rect(r.x + 6f, r.y + 10f, r.width, r.height), Ui.Shadow(w, h, 20f, 0.55f));
            Ui.Tex(r, Ui.Rounded(w, h, 16f, Ui.PanelBg, Color.clear, 0f));
            Ui.Tex(r, Ui.Rounded(w, h, 16f, Color.clear, Ui.Royal, 1.5f));

            DrawLogo(new Rect(r.x + 22f, r.y + 13f, 240f, 34f), 24);
            GUIStyle sub = Ui.Style(11, bold: true);
            sub.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(r.x + 22f, r.y + 40f, 240f, 16f), "U N T U R N E D", sub);

            GUIStyle ver = Ui.Style(12, anchor: TextAnchor.MiddleRight);
            ver.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(r.x + r.width - 200f, r.y + 20f, 180f, 20f), "INSERT - menu", ver);

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

                GUIStyle ts = Ui.Style(15, bold: sel, anchor: TextAnchor.MiddleCenter);
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
                case 2: DrawMisc(shifted, e); break;
                case 3: DrawSettings(shifted, e); break;
            }
            Ui.PopFade();
        }

        private static int _aimSub;
        private static int _lastAimSub;
        private static readonly string[] AimSubs = { "Vector", "Silent", "Recoil" };

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
                case 0: DrawVector(bodyShifted, e); break;
                case 1: DrawSilent(bodyShifted, e); break;
                case 2: DrawRecoil(bodyShifted, e); break;
            }
            Ui.PopFade();
        }

        private static void DrawVector(Rect area, Event e)
        {
            Scroll(area, _scrollA0, 280f, a =>
            {
                float y = 2f;
                y = ToggleRow(a, e, "Vector aim", ref Aimbot.VectorEnabled, y);
                y = ToggleRow(a, e, "Only when aiming (RMB)", ref Aimbot.VectorOnlyAiming, y);
                y = ToggleRow(a, e, "Draw FOV circle", ref Aimbot.DrawFovCircle, y);
                y = ChoiceRow(a, e, "Target point", ref Aimbot.VectorTargetPoint, HeadChest, y);
                y = SliderRow(a, e, "FOV", ref Aimbot.VectorFov, 1f, 90f, "0°", y);
                y = SliderRow(a, e, "Smoothing", ref Aimbot.VectorSmoothing, 1f, 20f, "0.0", y);
                y = SliderRow(a, e, "Recoil control", ref Aimbot.VectorRecoil, 0f, 100f, "0%", y);
            });
        }

        private static void DrawSilent(Rect area, Event e)
        {
            Scroll(area, _scrollA1, 150f, a =>
            {
                float y = 2f;
                y = ToggleRow(a, e, "Silent aim", ref Aimbot.SilentEnabled, y);
                y = ToggleRow(a, e, "Skip teammates", ref Aimbot.SkipTeammates, y);
                y = ChoiceRow(a, e, "Target point", ref Aimbot.SilentTargetPoint, HeadChest, y);
                y = SliderRow(a, e, "FOV", ref Aimbot.SilentFov, 1f, 90f, "0°", y);
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

        private static void Scroll(Rect outer, ScrollState st, float contentH, Action<Rect> draw)
        {
            Rect view = new Rect(0f, 0f, outer.width - 14f, Mathf.Max(contentH, outer.height));
            Vector2 s = GUI.BeginScrollView(outer, st.disp, view, GUIStyle.none, GUIStyle.none);
            st.target = s;
            st.disp = Ui.Ease(st.disp, st.target, 14f, Time.unscaledDeltaTime);
            draw(new Rect(0f, 0f, view.width, view.height));
            GUI.EndScrollView();

            if (view.height > outer.height + 1f)
            {
                Rect track = new Rect(outer.x + outer.width - 8f, outer.y, 3f, outer.height);
                Ui.RectFill(track, new Color(1f, 1f, 1f, 0.05f));

                float maxScroll = view.height - outer.height;
                float barH = Mathf.Max(40f, outer.height * (outer.height / view.height));
                float barY = outer.y + (outer.height - barH) * Mathf.Clamp01(st.disp.y / maxScroll);
                Ui.Tex(new Rect(track.x, barY, 3f, barH),
                    Ui.Rounded(3, Mathf.RoundToInt(barH), 1.5f, Ui.Royal, Color.clear, 0f));
            }
        }

        private static float ButtonRow(Rect area, Event e, string label, Action onClick, float y)
        {
            Rect row = new Rect(area.x, y, area.width, 32f);
            bool hover = row.Contains(e.mousePosition);
            float hv = Anim("btn:" + label, hover ? 1f : 0f, 11f);

            Ui.Tex(row, Ui.Rounded((int)row.width, 32, 8f,
                Color.Lerp(new Color(0.10f, 0.105f, 0.15f, 1f), Ui.RoyalDark, hv),
                Color.Lerp(new Color(1f, 1f, 1f, 0.08f), Ui.Royal, hv), 1f));

            GUIStyle bs = Ui.Style(13, bold: true, anchor: TextAnchor.MiddleCenter);
            bs.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, hv);
            GUI.Label(row, label, bs);

            if (hover && e.type == EventType.MouseDown && e.button == 0)
            {
                onClick();
                e.Use();
            }
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

            float hv = Anim("choice:" + label, hover ? 1f : 0f, 11f);
            if (hv > 0.01f)
                Ui.RectFill(new Rect(row.x, row.y, row.width, row.height),
                    new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.10f * hv));

            GUIStyle ls = Ui.Style(14, anchor: TextAnchor.MiddleLeft);
            ls.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, hv * 0.5f);
            GUI.Label(new Rect(row.x + 6f, row.y, row.width - 96f, row.height), label, ls);

            Ui.Tex(btn, Ui.Rounded(80, 24, 7f,
                new Color(0.10f, 0.105f, 0.15f, 1f), new Color(1f, 1f, 1f, 0.08f), 1f));
            GUIStyle bs = Ui.Style(12, bold: true, anchor: TextAnchor.MiddleCenter);
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
                e.Use();
            }
            if (active && e.type == EventType.MouseDrag && e.button == 0)
            {
                float t = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                val = Mathf.Round((min + t * (max - min)) * 10f) / 10f;
                e.Use();
            }
            if (active && e.type == EventType.MouseUp)
            {
                _activeSlider = null;
                e.Use();
            }

            float t01 = Mathf.Clamp01((val - min) / (max - min));
            float d01 = active ? t01 : Anim("sval:" + label, t01, 18f);

            GUIStyle ls = Ui.Style(13, anchor: TextAnchor.MiddleLeft);
            ls.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, Mathf.Max(hv * 0.5f, active ? 1f : 0f));
            GUI.Label(new Rect(row.x + 6f, row.y, row.width - 70f, 20f), label, ls);

            GUIStyle vs = Ui.Style(13, bold: true, anchor: TextAnchor.UpperRight);
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
            Scroll(area, _scrollV, 470f, a =>
            {
                Section(a, 0f, "PLAYERS");

                float y = 24f;
                y = ToggleRow(a, e, "Player ESP", ref Esp.Enabled, y);
                if (Esp.Enabled)
                {
                    y = ToggleRow(a, e, "Box", ref Esp.Box, y);
                    y = ToggleRow(a, e, "Fill", ref Esp.Fill, y);
                    y = ToggleRow(a, e, "Health", ref Esp.Health, y);
                    y = ToggleRow(a, e, "Name", ref Esp.Name, y);
                    y = ToggleRow(a, e, "Distance", ref Esp.Distance, y);
                    y = ToggleRow(a, e, "Snaplines", ref Esp.Snaplines, y);
                }

                y += 6f;
                Section(a, y, "WORLD");
                y += 24f;
                y = ToggleRow(a, e, "Zombies", ref Esp.Zombies, y);
                y = ToggleRow(a, e, "Vehicles", ref Esp.Vehicles, y);
                y = ToggleRow(a, e, "Ground items", ref Esp.Items, y);

                y += 6f;
                Section(a, y, "HUD");
                y += 24f;
                y = ToggleRow(a, e, "FPS counter", ref Misc.ShowFps, y);

                GUIStyle hint = Ui.Style(11, anchor: TextAnchor.LowerLeft);
                hint.normal.textColor = new Color(0.42f, 0.45f, 0.56f);
                GUI.Label(new Rect(a.x, y + 8f, a.width, 18f),
                    "INSERT - menu      END - ESP", hint);
            });
        }

        private static void DrawMisc(Rect area, Event e)
        {
            Scroll(area, _scrollM, 320f, a =>
            {
                Section(a, 0f, "WEAPONS / WORLD");

                float y = 24f;
                y = ToggleRow(a, e, "Fast reload", ref Misc.FastReloadEnabled, y);
                if (Misc.FastReloadEnabled)
                    y = SliderRow(a, e, "Reload speed", ref Misc.FastReload, 10f, 90f, "0%", y);
                y = ToggleRow(a, e, "No vehicle fuel", ref Misc.NoVehicleFuel, y);

                y += 6f;
                Section(a, y, "HUD");
                y += 24f;
                y = ToggleRow(a, e, "Radar", ref Misc.RadarEnabled, y);
                if (Misc.RadarEnabled)
                    y = SliderRow(a, e, "Radar range", ref Misc.RadarRange, 50f, 300f, "0m", y);
                y = ToggleRow(a, e, "Hitmarker", ref Misc.HitmarkerEnabled, y);
                y = ToggleRow(a, e, "Hitmarker sound", ref Misc.HitmarkerSound, y);
            });
        }

        private static void DrawSettings(Rect area, Event e)
        {
            Section(area, area.y, "CONFIG (.krx)");

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
            GUIStyle ab = Ui.Style(12);
            ab.normal.textColor = Ui.TextDim;
            GUI.Label(new Rect(area.x + 6f, y, area.width, 18f), "Kerix.zyx  v1.0", ab);
            y += 18f;
            GUI.Label(new Rect(area.x + 6f, y, area.width, 18f), "INSERT - menu      END - ESP", ab);
        }

        private static float ToggleRow(Rect area, Event e, string label, ref bool val, float y)
        {
            Rect row = new Rect(area.x, y, area.width, 30f);
            Rect track = new Rect(area.x + area.width - 46f, y + 4f, 42f, 22f);
            bool hover = row.Contains(e.mousePosition);

            if (hover && e.type == EventType.MouseDown && e.button == 0)
            {
                val = !val;
                e.Use();
            }

            float on = Anim("tgl:" + label, val ? 1f : 0f, 12f);
            float hv = Anim("row:" + label, hover ? 1f : 0f, 11f);

            if (hv > 0.01f)
                Ui.RectFill(new Rect(row.x, row.y, row.width - 54f, row.height),
                    new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.10f * hv));

            GUIStyle ls = Ui.Style(14, anchor: TextAnchor.MiddleLeft);
            ls.normal.textColor = Color.Lerp(Ui.TextDim, Color.white, Mathf.Max(on, hv * 0.5f));
            GUI.Label(new Rect(row.x + 6f, row.y, row.width - 60f, row.height), label, ls);

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
    }
}
