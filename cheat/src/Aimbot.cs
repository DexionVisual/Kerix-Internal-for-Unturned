using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kerix
{
    public static class Aimbot
    {

        public static bool VectorEnabled;
        public static bool VectorOnlyAiming = true;
        public static float VectorFov = 15f;
        public static float VectorSmoothing = 5f;
        public static int VectorTargetPoint;
        public static float VectorRecoil;
        public static bool DrawFovCircle = true;

        public static bool SilentEnabled;
        public static float SilentFov = 25f;
        public static int SilentTargetPoint;

        public static bool RecoilEnabled;
        public static float RecoilStrength = 60f;

        public static bool SkipTeammates = true;

        public static bool VectorAimingNow;

        private static FieldInfo _pitchField;
        private static FieldInfo _yawField;
        private static FieldInfo _bulletsField;
        private static PropertyInfo _velocityProp;
        private static PropertyInfo _positionProp;
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

        private static void InitReflection()
        {
            if (_pitchField != null) return;
            Type look = typeof(PlayerLook);
            _pitchField = look.GetField("_pitch", BindingFlags.NonPublic | BindingFlags.Instance);
            _yawField = look.GetField("_yaw", BindingFlags.NonPublic | BindingFlags.Instance);
            _bulletsField = typeof(UseableGun).GetField("bullets", BindingFlags.NonPublic | BindingFlags.Instance);
            _velocityProp = typeof(BulletInfo).GetProperty("velocity");
            _positionProp = typeof(BulletInfo).GetProperty("position");
            Loader.Log("Aimbot reflection: pitch=" + (_pitchField != null) + " yaw=" + (_yawField != null)
                       + " bullets=" + (_bulletsField != null) + " velocity=" + (_velocityProp != null));
        }

        private static float StanceHeight(Player p)
        {
            try
            {
                switch (p.stance.stance)
                {
                    case EPlayerStance.PRONE: return 0.8f;
                    case EPlayerStance.CROUCH: return 1.5f;
                    case EPlayerStance.SITTING: return 1.4f;
                    case EPlayerStance.DRIVING: return 1.5f;
                    default: return 1.95f;
                }
            }
            catch { return 1.95f; }
        }

        private static float PointHeightFraction(int pointIndex)
        {
            return pointIndex == 0 ? 0.93f : 0.60f;
        }

        private static bool IsTeammate(Player local, SteamPlayer sp)
        {
            try
            {
                if (!SkipTeammates) return false;
                SteamPlayer localSp = null;
                List<SteamPlayer> clients = Provider.clients;
                if (clients == null) return false;
                for (int i = 0; i < clients.Count; i++)
                    if (clients[i] != null && clients[i].player == local) { localSp = clients[i]; break; }
                if (localSp == null) return false;
                return sp.playerID.group != CSteamID.Nil && sp.playerID.group == localSp.playerID.group;
            }
            catch { return false; }
        }

        public static bool FindTargetPoint(float fovDeg, int pointIndex, out Vector3 point)
        {
            point = Vector3.zero;
            Camera cam = Camera.main;
            Player local = Player.LocalPlayer;
            if (cam == null || local == null) return false;

            List<SteamPlayer> clients = Provider.clients;
            if (clients == null) return false;

            float frac = PointHeightFraction(pointIndex);
            float best = fovDeg;

            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == local) continue;
                try
                {
                    Player p = sp.player;
                    if (p.life == null || p.life.isDead) continue;
                    if (IsTeammate(local, sp)) continue;

                    Vector3 target = p.transform.position + Vector3.up * (StanceHeight(p) * frac);
                    Vector3 dir = target - cam.transform.position;
                    float dist = dir.magnitude;
                    if (dist < 0.5f) continue;
                    dir /= dist;

                    float angle = Vector3.Angle(cam.transform.forward, dir);
                    if (angle < best)
                    {
                        best = angle;
                        point = target;
                    }
                }
                catch { }
            }
            return best < fovDeg;
        }

        private static bool IsAimingGun(Player local)
        {
            try
            {
                UseableGun gun = local.equipment != null ? local.equipment.useable as UseableGun : null;
                return gun != null && gun.isAiming;
            }
            catch { return false; }
        }

        private static Vector3 _targetPoint;
        private static bool _hasTarget;

        private static float _lastYaw;
        private static float _lastPitch;
        private static bool _hasLast;
        private static float _lastRealtime;

        public static void UpdateTick()
        {
            _hasTarget = false;
            VectorAimingNow = false;
            if (!VectorEnabled) return;
            if (KerixMain.Instance != null && KerixMain.Instance.MenuOpen) return;

            Player local = Player.LocalPlayer;
            if (local == null || local.look == null || !local.life.IsAlive) return;
            if (VectorOnlyAiming && !IsAimingGun(local)) return;

            Vector3 point;
            if (!FindTargetPoint(VectorFov, VectorTargetPoint, out point)) return;

            _targetPoint = point;
            _hasTarget = true;
            VectorAimingNow = true;
        }

        private static void AnglesTo(Vector3 point, out float yaw, out float pitch)
        {
            Camera cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            Vector3 dir = point - eye;
            float dist = dir.magnitude;
            dir = dist > 0.001f ? dir / dist : Vector3.forward;

            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            pitch = 90f + Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static void SteerLook(ref float lookX, ref float lookY)
        {
            if (!_hasTarget)
            {
                _hasLast = false;
                return;
            }

            float targetYaw, targetPitch;
            AnglesTo(_targetPoint, out targetYaw, out targetPitch);

            if (!_hasLast)
            {

                Player local = Player.LocalPlayer;
                if (local == null || local.look == null) return;
                _lastYaw = local.look.yaw;
                _lastPitch = local.look.pitch;
                _hasLast = true;
                _lastRealtime = Time.realtimeSinceStartup;
            }

            float dt = Mathf.Clamp(Time.realtimeSinceStartup - _lastRealtime, 0.001f, 0.1f);
            _lastRealtime = Time.realtimeSinceStartup;

            float dy = Mathf.Repeat(targetYaw - _lastYaw + 180f, 360f) - 180f;
            float dp = targetPitch - _lastPitch;

            float smooth = Mathf.Max(1f, VectorSmoothing);
            float k = Mathf.Clamp01(dt * (30f / smooth));
            if (smooth <= 1.01f) k = 1f;

            _lastYaw += dy * k;
            _lastPitch += dp * k;

            lookX = _lastYaw;
            lookY = _lastPitch;
        }

        public static void DrawFovOverlay()
        {
            if (!VectorEnabled || !DrawFovCircle) return;
            if (VectorOnlyAiming && KerixMain.Instance != null && KerixMain.Instance.MenuOpen) { }
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Material m = LineMat;
            if (m == null) return;

            float camFov = cam.fieldOfView;
            float radiusPx = Mathf.Tan(VectorFov * Mathf.Deg2Rad) /
                             (2f * Mathf.Tan(camFov * 0.5f * Mathf.Deg2Rad)) * Screen.height;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            const int segs = 64;

            GL.PushMatrix();
            m.SetPass(0);
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
            GL.Begin(GL.LINES);
            Color c = new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.55f);
            GL.Color(c);
            for (int i = 0; i < segs; i++)
            {
                float a0 = i / (float)segs * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segs * Mathf.PI * 2f;
                GL.Vertex3(center.x + Mathf.Cos(a0) * radiusPx, center.y + Mathf.Sin(a0) * radiusPx, 0f);
                GL.Vertex3(center.x + Mathf.Cos(a1) * radiusPx, center.y + Mathf.Sin(a1) * radiusPx, 0f);
            }
            GL.End();
            GL.PopMatrix();
        }

        [HarmonyPatch]
        internal static class Patches
        {
            private static int _bulletsBefore;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerLook), "simulate")]
            static void SimulatePrefix(PlayerLook __instance, ref float look_x, ref float look_y)
            {
                if (!VectorEnabled || !VectorAimingNow) return;
                try
                {
                    if (__instance.player != Player.LocalPlayer) return;
                    SteerLook(ref look_x, ref look_y);
                }
                catch { }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerLook), "recoil")]
            static void RecoilPrefix(ref float x, ref float y)
            {
                float comp = 0f;

                if (RecoilEnabled)
                    comp = Mathf.Max(comp, Mathf.Clamp01(RecoilStrength / 100f));
                if (VectorEnabled && VectorAimingNow && VectorRecoil > 0f)
                    comp = Mathf.Max(comp, Mathf.Clamp01(VectorRecoil / 100f));

                y *= 1f - comp;
                x *= 1f - comp;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(UseableGun), "fire")]
            static void FirePrefix(UseableGun __instance)
            {
                _bulletsBefore = CountBullets(__instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UseableGun), "fire")]
            static void FirePostfix(UseableGun __instance)
            {
                if (!SilentEnabled) return;
                try
                {
                    Vector3 point;
                    if (!FindTargetPoint(SilentFov, SilentTargetPoint, out point)) return;

                    InitReflection();
                    List<BulletInfo> bullets = (List<BulletInfo>)_bulletsField.GetValue(__instance);
                    if (bullets == null || bullets.Count <= _bulletsBefore) return;

                    for (int i = _bulletsBefore; i < bullets.Count; i++)
                    {
                        BulletInfo b = bullets[i];
                        if (b == null) continue;
                        Vector3 origin = b.origin;
                        Vector3 dir = point - origin;
                        float dist = dir.magnitude;
                        if (dist < 0.1f) continue;
                        dir /= dist;

                        Vector3 vel = (Vector3)_velocityProp.GetValue(b, null);
                        float speed = vel.magnitude;
                        if (speed < 0.1f) speed = 1f;
                        _velocityProp.SetValue(b, dir * speed, null);
                        _positionProp.SetValue(b, origin, null);
                    }
                }
                catch (Exception e)
                {
                    Loader.Log("silent aim error: " + e.Message);
                }
            }

            private static int CountBullets(UseableGun gun)
            {
                try
                {
                    InitReflection();
                    List<BulletInfo> bullets = (List<BulletInfo>)_bulletsField.GetValue(gun);
                    return bullets != null ? bullets.Count : 0;
                }
                catch { return 0; }
            }
        }
    }
}
