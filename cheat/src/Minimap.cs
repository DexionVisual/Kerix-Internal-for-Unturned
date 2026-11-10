using System;
using System.Collections.Generic;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
namespace Kerix
{
    public static class Minimap
    {
        public static bool Enabled = true;
        public static float Size = 250f;
        public static float ZoomMeters = 420f;   
        public static bool Rotate;
        public static bool ShowPlayers = true;
        public static bool ShowVehicles = true;
        public static bool ShowZombies;
        public static bool UseSatellite;         
        private const int MAX_ZOMBIE_DOTS = 300;
        private static Texture2D _map;
        private static string _loadedPath;
        private static bool _triedThisPath;
        private static bool _lastSatellite;
        public static void UpdateTick()
        {
            string path = Level.info != null ? Level.info.path : null;
            if (path == _loadedPath && _triedThisPath && UseSatellite == _lastSatellite) return;
            _lastSatellite = UseSatellite;
            _loadedPath = path;
            _triedThisPath = true;
            _map = null;
            if (path != null)
            {
                try
                {
                    string file = path + (UseSatellite ? "/Map.png" : "/Chart.png");
                    if (System.IO.File.Exists(file))
                        _map = ReadWrite.readTextureFromFile(file);
                }
                catch { }
            }
            if (_map == null)
                try { _map = Resources.Load<Texture2D>("Level/Map"); } catch { }
        }
        public static void Invalidate()
        {
            _loadedPath = null;
            _triedThisPath = false;
        }
        private static float LevelSize
        {
            get
            {
                float s = Level.size - Level.border * 2f;
                return s > 1f ? s : (Level.size > 1f ? Level.size : 1024f);
            }
        }
        private static Vector2 WorldToNorm(Vector3 w)
        {
            float s = LevelSize;
            return new Vector2(w.x / s + 0.5f, 0.5f - w.z / s);
        }
        public static void Draw()
        {
            if (!Enabled && !Hud.EditMode) return;
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            UpdateTick();
            float size = Mathf.Round(Mathf.Clamp(Size, 140f, 400f) / 20f) * 20f;
            Vector2 pos = Hud.GetPos("minimap", new Vector2(Screen.width - size - 20f, Screen.height - size - 20f));
            Rect panel = new Rect(pos.x, pos.y, size, size);
            Hud.Report("minimap", panel);
            float ghost = Hud.EditMode && !Enabled ? 0.45f : 1f;
            Ui.PushFade(ghost);
            Ui.Tex(new Rect(panel.x + 4f, panel.y + 6f, size, size), Ui.Shadow((int)size, (int)size, 14f, 0.5f));
            Ui.Tex(panel, Ui.Rounded((int)size, (int)size, 14f, new Color(0.05f, 0.055f, 0.085f, 0.92f), Ui.Royal, 1.3f));
            const float inset = 8f;
            Rect map = new Rect(panel.x + inset, panel.y + inset, size - inset * 2f, size - inset * 2f);
            if (_map == null)
            {
                GUIStyle st = Ui.Style(12, bold: true, anchor: TextAnchor.MiddleCenter);
                st.normal.textColor = Ui.TextDim;
                GUI.Label(panel, "no map", st);
                Ui.PopFade();
                return;
            }
            Player local = Player.LocalPlayer;
            if (local == null)
            {
                Ui.PopFade();
                return;
            }
            float levelSize = LevelSize;
            float zoom = Mathf.Clamp(ZoomMeters, 80f, levelSize);
            float du = zoom / levelSize;          
            float dv = du;
            Vector2 cNorm = WorldToNorm(local.transform.position);
            float u0 = Mathf.Clamp(cNorm.x - du * 0.5f, 0f, Mathf.Max(0f, 1f - du));
            float vTop0 = Mathf.Clamp(cNorm.y - dv * 0.5f, 0f, Mathf.Max(0f, 1f - dv));
            Rect uv = new Rect(u0, 1f - (vTop0 + dv), du, dv);
            GUI.BeginGroup(map);
            Rect local0 = new Rect(0f, 0f, map.width, map.height);
            if (Rotate)
            {
                Vector2 pivot = new Vector2(map.width * 0.5f, map.height * 0.5f);
                GUIUtility.RotateAroundPivot(-local.look.yaw, pivot);
            }
            GUI.DrawTextureWithTexCoords(local0, _map, uv, true);
            float mapW = map.width, mapH = map.height;
            Func<Vector3, Vector2?> toLocal = w =>
            {
                Vector2 n = WorldToNorm(w);
                Vector2 p = new Vector2((n.x - u0) / du * mapW, (n.y - vTop0) / dv * mapH);
                if (p.x < -6f || p.y < -6f || p.x > mapW + 6f || p.y > mapH + 6f) return null;
                return p;
            };
            if (ShowZombies) DrawZombieDots(toLocal);
            if (ShowVehicles) DrawVehicleDots(toLocal);
            if (ShowPlayers) DrawPlayerDots(local, toLocal);
            Vector2? cLocal = toLocal(local.transform.position);
            if (cLocal.HasValue)
                DrawLocalArrow(cLocal.Value, local.look.yaw);
            GUI.matrix = Matrix4x4.identity;
            GUI.EndGroup();
            Ui.RectOutline(map, new Color(1f, 1f, 1f, 0.10f), 1f);
            HandleWheel(panel);
            Ui.PopFade();
        }
        private static void HandleWheel(Rect panel)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.ScrollWheel) return;
            if (!panel.Contains(e.mousePosition)) return;
            ZoomMeters = Mathf.Clamp(ZoomMeters + e.delta.y * 30f, 80f, LevelSize);
            e.Use();
        }
        private static Texture2D _arrowTex;
        private static Texture2D ArrowTex
        {
            get
            {
                if (_arrowTex != null) return _arrowTex;
                const int s = 32;
                _arrowTex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                _arrowTex.hideFlags = HideFlags.HideAndDontSave;
                Color32[] px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                {
                    float t = Mathf.Clamp01((s - 3 - y) / 26f);
                    float half = Mathf.Lerp(1.2f, 12.5f, t);
                    for (int x = 0; x < s; x++)
                    {
                        bool inside = y >= 3f && y <= s - 3 && Mathf.Abs(x - (s * 0.5f - 0.5f)) <= half;
                        px[y * s + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                }
                _arrowTex.SetPixels32(px);
                _arrowTex.Apply(false);
                return _arrowTex;
            }
        }
        private static void DrawLocalArrow(Vector2 p, float yawDeg)
        {
            Matrix4x4 save = GUI.matrix;
            GUIUtility.RotateAroundPivot(yawDeg, p);
            GUI.DrawTexture(new Rect(p.x - 9f, p.y - 9f, 18f, 18f), ArrowTex, ScaleMode.StretchToFill, true);
            GUI.matrix = save;
            Ui.RectFill(new Rect(p.x - 1.6f, p.y - 1.6f, 3.2f, 3.2f),
                new Color(1f, 1f, 1f, 0.9f));
        }
        private static void DrawDot(Vector2 p, float r, Color c)
        {
            Ui.RectFill(new Rect(p.x - r, p.y - r, r * 2f, r * 2f), c);
        }
        private static void DrawPlayerDots(Player local, Func<Vector3, Vector2?> toLocal)
        {
            List<SteamPlayer> clients = Provider.clients;
            if (clients == null) return;
            CSteamID myGroup = MiscGroupOf(local);
            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == local) continue;
                try
                {
                    if (sp.player.life == null || sp.player.life.isDead) continue;
                    Vector2? pp = toLocal(sp.player.transform.position);
                    if (!pp.HasValue) continue;
                    bool mate = sp.playerID.group != CSteamID.Nil && sp.playerID.group == myGroup;
                    DrawDot(pp.Value, 4f, mate ? new Color(0.3f, 0.9f, 0.4f) : new Color(1f, 0.3f, 0.3f));
                }
                catch { }
            }
        }
        private static void DrawVehicleDots(Func<Vector3, Vector2?> toLocal)
        {
            List<InteractableVehicle> vehicles = VehicleManager.vehicles;
            if (vehicles == null) return;
            for (int i = 0; i < vehicles.Count; i++)
            {
                InteractableVehicle v = vehicles[i];
                if (v == null || v.isDead || v.IsChildOfVehicle || v.transform == null) continue;
                try
                {
                    Vector2? pp = toLocal(v.transform.position);
                    if (!pp.HasValue) continue;
                    DrawDot(pp.Value, 3.2f, new Color(0.3f, 0.88f, 1f));
                }
                catch { }
            }
        }
        private static void DrawZombieDots(Func<Vector3, Vector2?> toLocal)
        {
            ZombieRegion[] regions = ZombieManager.regions;
            if (regions == null) return;
            int drawn = 0;
            for (int r = 0; r < regions.Length && drawn < MAX_ZOMBIE_DOTS; r++)
            {
                ZombieRegion region = regions[r];
                if (region == null || region.zombies == null) continue;
                for (int i = 0; i < region.zombies.Count && drawn < MAX_ZOMBIE_DOTS; i++)
                {
                    Zombie z = region.zombies[i];
                    if (z == null || z.isDead || z.transform == null) continue;
                    try
                    {
                        Vector2? pp = toLocal(z.transform.position);
                        if (!pp.HasValue) continue;
                        Color c = z.isBoss ? new Color(1f, 0.72f, 0.2f)
                                : z.isMega ? new Color(1f, 0.45f, 0.1f)
                                : new Color(1f, 0.55f, 0.55f);
                        DrawDot(pp.Value, z.isBoss || z.isMega ? 4f : 2.6f, c);
                        drawn++;
                    }
                    catch { }
                }
            }
        }
        private static CSteamID MiscGroupOf(Player local)
        {
            try
            {
                List<SteamPlayer> clients = Provider.clients;
                for (int i = 0; i < clients.Count; i++)
                    if (clients[i] != null && clients[i].player == local)
                        return clients[i].playerID.group;
            }
            catch { }
            return CSteamID.Nil;
        }
    }
}
