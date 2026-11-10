using System;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
namespace Kerix
{
    public static class Movement
    {
        public static bool ClimbBoostEnabled;
        public static bool AutoSyncOnDisable = true;
        public static bool AutoSyncLeash;
        public static float AutoSyncDistance = 40f;
        public static float GlideSpeed = 35f;
        public static bool SyncActive;   
        public static bool Gliding;
        public static float DesyncEstimate;
        private static bool _syncPending;
        private static float _syncRequestTime;
        private static Vector3 _glideFrom;
        private static Vector3 _glideTo;
        private static float _glideT;
        private static float _glideDuration = 1f;
        private static bool _prevSpeed;
        private static bool _prevJump;
        private static Vector3 _lastCorrectionPos;
        private static float _lastCorrectionTime = -10f;
        private static FieldInfo _jumpField;
        private static FieldInfo _horizontalField;
        private static FieldInfo _verticalField;
        private static void InitReflection()
        {
            if (_jumpField != null) return;
            Type t = typeof(PlayerMovement);
            _jumpField = t.GetField("_jump", BindingFlags.NonPublic | BindingFlags.Instance);
            _horizontalField = t.GetField("_horizontal", BindingFlags.NonPublic | BindingFlags.Instance);
            _verticalField = t.GetField("_vertical", BindingFlags.NonPublic | BindingFlags.Instance);
            Loader.Log("Movement reflection: jump=" + (_jumpField != null)
                       + " h=" + (_horizontalField != null) + " v=" + (_verticalField != null));
        }
        public static FieldInfo GetHorizontalField()
        {
            InitReflection();
            return _horizontalField;
        }
        public static void RequestSync()
        {
            if (Gliding || _syncPending) return;
            Player local = Player.LocalPlayer;
            if (local == null) return;
            try
            {
                if (local.life == null || local.life.isDead) return;
                if (local.movement != null && local.movement.getVehicle() != null) return;
            }
            catch { return; }
            _syncPending = true;
            SyncActive = true;
            _syncRequestTime = Time.realtimeSinceStartup;
        }
        public static void UpdateTick()
        {
            Player local = Player.LocalPlayer;
            if (local == null) { ResetState(); return; }
            InitReflection();
            if (AutoSyncOnDisable)
            {
                if (_prevSpeed && !Misc.SpeedEnabled) RequestSync();
                if (_prevJump && !Misc.JumpEnabled) RequestSync();
            }
            _prevSpeed = Misc.SpeedEnabled;
            _prevJump = Misc.JumpEnabled;
            bool desynced = (Misc.SpeedEnabled || Misc.JumpEnabled) && !SyncActive;
            if (desynced)
            {
                UpdateDesyncEstimate(local);
                if (AutoSyncLeash && AutoSyncDistance > 1f && DesyncEstimate > AutoSyncDistance)
                {
                    Misc.SpeedEnabled = false;
                    Misc.JumpEnabled = false;
                    RequestSync();
                }
            }
            else if (!SyncActive)
            {
                DesyncEstimate = 0f;
            }
            if (_syncPending && Time.realtimeSinceStartup - _syncRequestTime > 1.5f)
            {
                _syncPending = false;
                SyncActive = false;
            }
            if (SyncActive)
            {
                try
                {
                    if (local.life == null || local.life.isDead ||
                        (local.movement != null && local.movement.getVehicle() != null))
                        ResetState();
                }
                catch { }
            }
        }
        private static void UpdateDesyncEstimate(Player local)
        {
            if (_lastCorrectionTime < 0f) { DesyncEstimate = 0f; return; }
            DesyncEstimate = Vector3.Distance(local.transform.position, _lastCorrectionPos);
        }
        private static void ResetState()
        {
            _syncPending = false;
            Gliding = false;
            SyncActive = false;
        }
        public static void LateUpdate()
        {
            if (!Gliding) return;
            Player local = Player.LocalPlayer;
            if (local == null || local.movement == null) { ResetState(); return; }
            try
            {
                _glideT += Time.unscaledDeltaTime / Mathf.Max(0.05f, _glideDuration);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_glideT));
                CharacterController ctrl = local.movement.controller;
                if (ctrl != null && ctrl.enabled) ctrl.enabled = false;
                local.transform.position = Vector3.Lerp(_glideFrom, _glideTo, k);
                if (_glideT >= 1f)
                {
                    local.transform.position = _glideTo;
                    if (ctrl != null) ctrl.enabled = true;
                    Gliding = false;
                    SyncActive = false;
                }
            }
            catch { ResetState(); }
        }
        public static bool OnCorrection(Vector3 position, Vector3 velocity)
        {
            _lastCorrectionPos = position;
            _lastCorrectionTime = Time.realtimeSinceStartup;
            if (_syncPending && !Gliding)
            {
                _syncPending = false;
                Gliding = true;
                SyncActive = true;
                Player local = Player.LocalPlayer;
                _glideFrom = local != null ? local.transform.position : position;
                _glideTo = position + velocity * 0.12f;
                _glideT = 0f;
                float dist = Vector3.Distance(_glideFrom, _glideTo);
                _glideDuration = Mathf.Clamp(dist / Mathf.Max(4f, GlideSpeed), 0.25f, 3f);
                return true;
            }
            return Gliding;
        }
        public static void DrawDesyncHud()
        {
            if (Gliding) return;
            if (!(Misc.SpeedEnabled || Misc.JumpEnabled)) return;
            if (DesyncEstimate < 1f) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            GUIStyle st = Ui.Style(12, bold: true, anchor: TextAnchor.LowerCenter);
            st.normal.textColor = new Color(0.95f, 0.72f, 0.35f, 0.9f);
            string text = "desync ~" + Mathf.RoundToInt(DesyncEstimate) + "m";
            Ui.OutlinedLabel(new Rect(Screen.width * 0.5f - 100f, Screen.height - 74f, 200f, 20f),
                text, text, st, st.normal.textColor);
        }
        [HarmonyPatch]
        internal static class Patches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerMovement), "Update")]
            static void InputOverridePostfix(PlayerMovement __instance)
            {
                if (__instance.player != Player.LocalPlayer) return;
                InitReflection();
                if (_jumpField == null || _horizontalField == null || _verticalField == null) return;
                try
                {
                    if (Gliding)
                    {
                        _jumpField.SetValue(__instance, false);
                        _horizontalField.SetValue(__instance, (byte)1);
                        _verticalField.SetValue(__instance, (byte)1);
                        return;
                    }
                    if (ClimbBoostEnabled && !KerixMain.Instance.MenuOpen &&
                        __instance.player.stance.stance == EPlayerStance.CLIMB &&
                        (byte)_verticalField.GetValue(__instance) == 2)
                    {
                        _verticalField.SetValue(__instance, (byte)15);
                        return;
                    }
                    if (__instance.player.stance.stance == EPlayerStance.DRIVING)
                        VehicleSteer.OverrideDrivingInputs(__instance, __instance.player.movement.getVehicle());
                }
                catch { }
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerMovement), "simulate",
                new[] { typeof(uint), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float),
                        typeof(bool), typeof(bool), typeof(float) })]
            static bool GlideSimulatePrefix(PlayerMovement __instance)
            {
                if (Gliding)
                    return __instance.player != Player.LocalPlayer;
                return true;
            }
        }
    }
}
