using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kerix
{
    public static class Misc
    {

        public static bool SpeedEnabled;
        public static float SpeedMult = 1.5f;
        public static bool JumpEnabled;
        public static float JumpMult = 1.5f;

        public static bool FastReloadEnabled;
        public static float FastReload = 50f;
        public static bool NoVehicleFuel;

        public static bool RadarEnabled = true;
        public static float RadarRange = 150f;
        public static bool ShowFps = true;
        public static bool HitmarkerEnabled = true;
        public static bool HitmarkerSound;

        public static event System.Action SoundHook;

        private static float _lastHit = -10f;
        private static FieldInfo _reloadTimeField;

        public static void UpdateTick()
        {
            try
            {
                Player local = Player.LocalPlayer;
                if (local == null || local.movement == null) return;

                local.movement.pluginSpeedMultiplier = SpeedEnabled ? Mathf.Max(0.1f, SpeedMult) : 1f;
                local.movement.pluginJumpMultiplier = JumpEnabled ? Mathf.Max(0.1f, JumpMult) : 1f;
            }
            catch { }
        }

        private static void OnHit()
        {
            _lastHit = Time.realtimeSinceStartup;
            if (HitmarkerSound)
            {
                try { SoundHook?.Invoke(); } catch { }
            }
        }

        public static void DrawHitmarker()
        {
            if (!HitmarkerEnabled) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            float age = Time.realtimeSinceStartup - _lastHit;
            if (age > 0.4f) return;

            float a = 1f - age / 0.4f;
            Material m = LineMat;
            if (m == null) return;

            Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float r0 = 9f, r1 = 20f;

            GL.PushMatrix();
            m.SetPass(0);
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0.95f, 0.9f, a));
            for (int i = 0; i < 4; i++)
            {
                float ang = Mathf.PI / 4f + i * Mathf.PI / 2f;
                Vector2 d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                GL.Vertex3(c.x + d.x * r0, c.y + d.y * r0, 0f);
                GL.Vertex3(c.x + d.x * r1, c.y + d.y * r1, 0f);
            }
            GL.End();
            GL.PopMatrix();
        }

        private static Material _lineMat;
        private static Material LineMat
        {
            get
            {
                if (_lineMat == null)
                {
                    Shader sh = Shader.Find("Sprites/Default");
                    if (sh != null) _lineMat = new Material(sh);
                }
                return _lineMat;
            }
        }

        public static void DrawRadar()
        {
            if (!RadarEnabled) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            Player local = Player.LocalPlayer;
            List<SteamPlayer> clients = Provider.clients;
            if (local == null || clients == null) return;

            float size = 168f;
            Rect panel = new Rect(Screen.width - size - 20f, 70f, size, size);
            Vector2 center = new Vector2(panel.x + size * 0.5f, panel.y + size * 0.5f);
            float scale = size * 0.5f / Mathf.Max(20f, RadarRange);

            Ui.Tex(new Rect(panel.x + 4f, panel.y + 6f, size, size), Ui.Shadow((int)size, (int)size, 14f, 0.5f));
            Ui.Tex(panel, Ui.Rounded((int)size, (int)size, 14f, new Color(0.05f, 0.055f, 0.085f, 0.85f), Ui.Royal, 1.3f));

            Vector3 lp = local.transform.position;

            float yawRad = local.look.yaw * Mathf.Deg2Rad;
            Vector2 fwd = new Vector2(Mathf.Sin(yawRad), Mathf.Cos(yawRad));
            Material m = LineMat;
            if (m != null)
            {
                GL.PushMatrix();
                m.SetPass(0);
                GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
                GL.Begin(GL.LINES);
                GL.Color(Color.white);
                GL.Vertex3(center.x, center.y, 0f);
                GL.Vertex3(center.x + fwd.x * 8f, center.y - fwd.y * 8f, 0f);
                GL.End();
                GL.PopMatrix();
            }

            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == local) continue;
                try
                {
                    if (sp.player.life == null || sp.player.life.isDead) continue;
                    Vector3 d = sp.player.transform.position - lp;
                    if (d.magnitude > RadarRange) continue;
                    Vector2 p = center + new Vector2(d.x * scale, -d.z * scale);
                    bool mate = sp.playerID.group != CSteamID.Nil && sp.playerID.group == GroupOf(local);
                    Color c = mate ? new Color(0.3f, 0.9f, 0.4f) : new Color(1f, 0.3f, 0.3f);
                    Ui.RectFill(new Rect(p.x - 3f, p.y - 3f, 6f, 6f), c);
                }
                catch { }
            }

            try
            {
                List<InteractableVehicle> vehicles = VehicleManager.vehicles;
                if (vehicles != null)
                {
                    for (int i = 0; i < vehicles.Count; i++)
                    {
                        InteractableVehicle v = vehicles[i];
                        if (v == null || v.isDead || v.transform == null) continue;
                        Vector3 d = v.transform.position - lp;
                        if (d.magnitude > RadarRange) continue;
                        Vector2 p = center + new Vector2(d.x * scale, -d.z * scale);
                        Ui.RectFill(new Rect(p.x - 2.5f, p.y - 2.5f, 5f, 5f), new Color(0.3f, 0.88f, 1f));
                    }
                }
            }
            catch { }
        }

        private static CSteamID GroupOf(Player local)
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

        public static void DrawFps(float fps)
        {
            if (!ShowFps) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            GUIStyle st = Ui.Style(13, bold: true, anchor: TextAnchor.UpperRight);
            string text = Mathf.RoundToInt(fps) + " FPS";
            Color c = fps > 100 ? new Color(0.4f, 0.9f, 0.5f) : fps > 55 ? Ui.RoyalLight : new Color(0.95f, 0.5f, 0.3f);
            Ui.OutlinedLabel(new Rect(Screen.width - 120f, Screen.height - 34f, 100f, 18f), text, text, st, c);
        }

        [HarmonyPatch]
        internal static class Patches
        {

            [HarmonyPrefix]
            [HarmonyPatch(typeof(InteractableVehicle), "simulateBurnFuel")]
            static bool NoFuelPrefix()
            {
                return !NoVehicleFuel;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UseableGun), "ReceivePlayReload")]
            static void FastReloadPostfix(UseableGun __instance)
            {
                if (!FastReloadEnabled || FastReload <= 0f) return;
                try
                {
                    if (_reloadTimeField == null)
                        _reloadTimeField = typeof(UseableGun).GetField("reloadTime",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_reloadTimeField == null) return;

                    float t = (float)_reloadTimeField.GetValue(__instance);
                    t *= 1f - Mathf.Clamp01(FastReload / 100f);
                    _reloadTimeField.SetValue(__instance, t);
                }
                catch { }
            }

            internal static void HitmarkerPostfix(CSteamID newKiller)
            {
                try
                {
                    if (newKiller == CSteamID.Nil || newKiller != Provider.client) return;
                    OnHit();
                }
                catch { }
            }
        }
    }
}
