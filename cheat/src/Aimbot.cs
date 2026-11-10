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
        public static bool SilentEnabled;
        public static float SilentFov = 25f;
        public static int SilentTargetPoint;
        public static bool SilentFovCircle = true;
        public static bool RecoilEnabled;
        public static float RecoilStrength = 60f;
        public static bool BallisticsEnabled = true;
        public static bool BallisticsLead = true;
        public static bool BallisticsInfo;
        public static bool SkipTeammates = true;
        private static FieldInfo _pitchField;
        private static FieldInfo _yawField;
        private static FieldInfo _bulletsField;
        private static PropertyInfo _velocityProp;
        private static PropertyInfo _positionProp;
        private static MethodInfo _calcGravityMethod;
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
            _calcGravityMethod = typeof(UseableGun).GetMethod("CalculateBulletGravityMultiplier",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }
        internal const float BALLISTICS_DELTA_TIME = 0.02f;
        private const float GravityMag = 9.81f;
        public static string WeaponInfoLine
        {
            get
            {
                Player local = Player.LocalPlayer;
                if (local == null || local.equipment == null) return "";
                ItemGunAsset ga = local.equipment.asset as ItemGunAsset;
                if (ga == null) return "no gun";
                float mv, gm;
                if (!GetGunParams(local, out mv, out gm)) return "no gun";
                float t = 100f / mv;
                float drop = 0.5f * GravityMag * gm * t * Mathf.Max(0f, t - BALLISTICS_DELTA_TIME);
                return ga.itemName + "  " + Mathf.RoundToInt(mv) + " m/s  drop@100m " + drop.ToString("F2") + "m";
            }
        }
        private static bool GetGunParams(Player local, out float muzzleVelocity, out float gravityMult)
        {
            muzzleVelocity = 0f;
            gravityMult = 1f;
            try
            {
                UseableGun gun = local.equipment != null ? local.equipment.useable as UseableGun : null;
                ItemGunAsset ga = local.equipment != null ? local.equipment.asset as ItemGunAsset : null;
                if (gun == null || ga == null) return false;
                muzzleVelocity = ga.muzzleVelocity;
                if (muzzleVelocity < 1f) return false;
                if (_calcGravityMethod != null)
                {
                    object r = _calcGravityMethod.Invoke(gun, null);
                    if (r is float) gravityMult = (float)r;
                }
                else
                {
                    gravityMult = ga.bulletGravityMultiplier;
                }
                return true;
            }
            catch { return false; }
        }
        private static Vector3 SolveBallistic(Vector3 origin, Vector3 target, Vector3 targetVel,
                                              float speed, float gravityMult)
        {
            float g = GravityMag * gravityMult;
            if (g <= 0f) return target;
            float t = Vector3.Distance(origin, target) / speed;
            Vector3 lead = target;
            for (int i = 0; i < 3; i++)
            {
                Vector3 future = BallisticsLead ? target + targetVel * t : target;
                float drop = 0.5f * g * t * Mathf.Max(0f, t - BALLISTICS_DELTA_TIME);
                lead = future + Vector3.up * drop;
                t = Vector3.Distance(origin, lead) / speed;
            }
            return lead;
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
        private class VelTrack
        {
            public Vector3 Pos;
            public float Time;
            public Vector3 Vel;
        }
        private static readonly Dictionary<SteamPlayer, VelTrack> _velTracks =
            new Dictionary<SteamPlayer, VelTrack>();
        private static void TrackVelocities()
        {
            List<SteamPlayer> clients = Provider.clients;
            if (clients == null) return;
            if (_velTracks.Count > 64) _velTracks.Clear();
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null) continue;
                try
                {
                    Vector3 pos = sp.player.transform.position;
                    VelTrack t;
                    if (!_velTracks.TryGetValue(sp, out t))
                    {
                        _velTracks[sp] = new VelTrack { Pos = pos, Time = now, Vel = Vector3.zero };
                        continue;
                    }
                    float dt = now - t.Time;
                    if (dt > 1f)
                    {
                        t.Pos = pos;
                        t.Time = now;
                        t.Vel = Vector3.zero;
                        continue;
                    }
                    if (dt < 0.02f) continue;
                    Vector3 v = (pos - t.Pos) / dt;
                    t.Vel = Vector3.Lerp(t.Vel, v, 0.45f);
                    t.Pos = pos;
                    t.Time = now;
                }
                catch { }
            }
        }
        private static Vector3 VelocityOf(SteamPlayer sp)
        {
            VelTrack t;
            if (sp != null && _velTracks.TryGetValue(sp, out t)) return t.Vel;
            return Vector3.zero;
        }
        public static bool FindTargetPoint(float fovDeg, int pointIndex, out Vector3 point)
        {
            Vector3 vel;
            return FindTargetPoint(fovDeg, pointIndex, out point, out vel);
        }
        public static bool FindTargetPoint(float fovDeg, int pointIndex, out Vector3 point, out Vector3 targetVel)
        {
            point = Vector3.zero;
            targetVel = Vector3.zero;
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
                        targetVel = VelocityOf(sp);
                    }
                }
                catch { }
            }
            return best < fovDeg;
        }
        public static void UpdateTick()
        {
            TrackVelocities();
        }
        private static int _bulletsBefore;
        [HarmonyPatch]
        internal static class Patches
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerLook), "recoil")]
            static void RecoilPrefix(ref float x, ref float y)
            {
                if (RecoilEnabled)
                {
                    float comp = Mathf.Clamp01(RecoilStrength / 100f);
                    y *= 1f - comp;
                    x *= 1f - comp;
                }
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
                    Player local = Player.LocalPlayer;
                    Vector3 point, vel;
                    if (!FindTargetPoint(SilentFov, SilentTargetPoint, out point, out vel)) return;
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
                        Vector3 velB = (Vector3)_velocityProp.GetValue(b, null);
                        float speed = velB.magnitude;
                        if (speed < 0.1f) speed = 1f;
                        Vector3 launch = dir;
                        if (BallisticsEnabled && local != null)
                        {
                            float mv, gm;
                            if (GetGunParams(local, out mv, out gm) && mv > 1f)
                            {
                                Vector3 solved = SolveBallistic(origin, point, vel, speed, gm);
                                Vector3 sd = solved - origin;
                                float sdMag = sd.magnitude;
                                if (sdMag > 0.1f) launch = sd / sdMag;
                            }
                        }
                        _velocityProp.SetValue(b, launch * speed, null);
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
        public static bool DrawSilentLine()
        {
            if (!SilentEnabled) return false;
            if (Event.current == null || Event.current.type != EventType.Repaint) return false;
            if (KerixMain.Instance != null && KerixMain.Instance.MenuOpen) return false;
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 point;
            if (!FindTargetPoint(SilentFov, SilentTargetPoint, out point)) return false;
            Vector3 s = cam.WorldToScreenPoint(point);
            if (s.z <= 0.05f) return false;
            Material m = LineMat;
            if (m == null) return false;
            float sh = Screen.height;
            GL.PushMatrix();
            m.SetPass(0);
            GL.LoadPixelMatrix(0f, Screen.width, sh, 0f);
            GL.Begin(GL.LINES);
            GL.Color(new Color(Ui.RoyalLight.r, Ui.RoyalLight.g, Ui.RoyalLight.b, 0.55f));
            GL.Vertex3(Screen.width * 0.5f, sh * 0.5f, 0f);
            GL.Vertex3(s.x, sh - s.y, 0f);
            GL.End();
            GL.PopMatrix();
            return true;
        }
        public static void DrawSilentFovCircle()
        {
            if (!SilentEnabled || !SilentFovCircle) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            if (KerixMain.Instance != null && KerixMain.Instance.MenuOpen) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Material m = LineMat;
            if (m == null) return;
            float camFov = cam.fieldOfView;
            float radiusPx = Mathf.Tan(SilentFov * Mathf.Deg2Rad) /
                             (2f * Mathf.Tan(camFov * 0.5f * Mathf.Deg2Rad)) * Screen.height;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            const int segs = 64;
            GL.PushMatrix();
            m.SetPass(0);
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
            GL.Begin(GL.LINES);
            GL.Color(new Color(Ui.Royal.r, Ui.Royal.g, Ui.Royal.b, 0.4f));
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
    }
}
