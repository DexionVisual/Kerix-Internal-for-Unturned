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
        public static bool AntiRubberband = true;
        public static bool FastReloadEnabled;
        public static float FastReload = 50f;
        public static bool NoVehicleFuel;
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
        public static void DrawFps(float fps)
        {
            if (!ShowFps) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            GUIStyle st = Ui.Style(13, bold: true, anchor: TextAnchor.UpperLeft);
            string text = Mathf.RoundToInt(fps) + " FPS";
            Color c = fps > 100 ? new Color(0.4f, 0.9f, 0.5f) : fps > 55 ? Ui.RoyalLight : new Color(0.95f, 0.5f, 0.3f);
            Ui.OutlinedLabel(new Rect(20f, Screen.height - 34f, 100f, 18f), text, text, st, c);
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
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerInput), "ReceiveSimulateMispredictedInputs")]
            static bool MispredictPrefix(uint frameNumber, EPlayerStance stance, Vector3 position,
                                         Vector3 velocity, byte stamina, int lastTireOffset, int lastRestOffset)
            {
                if (Movement.OnCorrection(position, velocity)) return false;
                if (!AntiRubberband) return true;
                return !(SpeedEnabled || JumpEnabled);
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
