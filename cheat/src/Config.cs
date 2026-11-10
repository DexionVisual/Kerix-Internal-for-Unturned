using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
namespace Kerix
{
    public static class Config
    {
        private class Entry
        {
            public string Key;
            public Func<string> Get;
            public Action<string> Set;
            public string Default;
        }
        private static readonly List<Entry> _entries = new List<Entry>();
        private static bool _registered;
        public static string Status = "";
        private static float _statusUntil;
        private static void Add(string key, Func<string> get, Action<string> set)
        {
            Entry e = new Entry { Key = key, Get = get, Set = set };
            e.Default = get();
            _entries.Add(e);
        }
        private static void Bool(string key, Func<bool> get, Action<bool> set)
        {
            Add(key, () => get().ToString(), v => { bool r; if (bool.TryParse(v, out r)) set(r); });
        }
        private static void Float(string key, Func<float> get, Action<float> set)
        {
            Add(key, () => get().ToString("R", CultureInfo.InvariantCulture),
                v => { float r; if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out r)) set(r); });
        }
        private static void Int(string key, Func<int> get, Action<int> set)
        {
            Add(key, () => get().ToString(CultureInfo.InvariantCulture),
                v => { int r; if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)) set(r); });
        }
        private static void Hex(string key, Func<Color> get, Action<Color> set)
        {
            Add(key, () => "#" + ColorUtility.ToHtmlStringRGB(get()),
                v =>
                {
                    Color c;
                    if (ColorUtility.TryParseHtmlString(v.StartsWith("#") ? v : "#" + v, out c)) set(c);
                });
        }
        private static void String(string key, Func<string> get, Action<string> set)
        {
            Add(key, get, set);
        }
        private static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;
            Bool("aim.silent.enabled", () => Aimbot.SilentEnabled, v => Aimbot.SilentEnabled = v);
            Bool("aim.silent.fovCircle", () => Aimbot.SilentFovCircle, v => Aimbot.SilentFovCircle = v);
            Int("aim.silent.target", () => Aimbot.SilentTargetPoint, v => Aimbot.SilentTargetPoint = v);
            Float("aim.silent.fov", () => Aimbot.SilentFov, v => Aimbot.SilentFov = v);
            Bool("aim.recoil.enabled", () => Aimbot.RecoilEnabled, v => Aimbot.RecoilEnabled = v);
            Float("aim.recoil.strength", () => Aimbot.RecoilStrength, v => Aimbot.RecoilStrength = v);
            Bool("aim.ballistics.enabled", () => Aimbot.BallisticsEnabled, v => Aimbot.BallisticsEnabled = v);
            Bool("aim.ballistics.lead", () => Aimbot.BallisticsLead, v => Aimbot.BallisticsLead = v);
            Bool("aim.ballistics.info", () => Aimbot.BallisticsInfo, v => Aimbot.BallisticsInfo = v);
            Bool("aim.skipTeammates", () => Aimbot.SkipTeammates, v => Aimbot.SkipTeammates = v);
            Bool("esp.enabled", () => Esp.Enabled, v => Esp.Enabled = v);
            Bool("esp.box", () => Esp.Box, v => Esp.Box = v);
            Bool("esp.skeleton", () => Esp.Skeleton, v => Esp.Skeleton = v);
            Bool("esp.name", () => Esp.Name, v => Esp.Name = v);
            Bool("esp.snaplines", () => Esp.Snaplines, v => Esp.Snaplines = v);
            Hex("esp.playerColor", () => Esp.PlayerColor, v => Esp.PlayerColor = v);
            Hex("esp.zombieColor", () => Esp.ZombieColor, v => Esp.ZombieColor = v);
            Hex("esp.vehicleColor", () => Esp.VehicleColor, v => Esp.VehicleColor = v);
            Bool("esp.zombies", () => Esp.Zombies, v => Esp.Zombies = v);
            Bool("esp.vehicles", () => Esp.Vehicles, v => Esp.Vehicles = v);
            Bool("esp.items", () => Esp.Items, v => Esp.Items = v);
            Bool("chams.players", () => Chams.PlayersEnabled, v => Chams.PlayersEnabled = v);
            Int("chams.players.style", () => Chams.PlayersStyle, v => Chams.PlayersStyle = v);
            Hex("chams.players.color", () => Chams.PlayersColor, v => Chams.PlayersColor = v);
            Bool("chams.players.walls", () => Chams.PlayersThroughWalls, v => Chams.PlayersThroughWalls = v);
            Bool("chams.hands", () => Chams.HandsEnabled, v => Chams.HandsEnabled = v);
            Int("chams.hands.style", () => Chams.HandsStyle, v => Chams.HandsStyle = v);
            Hex("chams.hands.color", () => Chams.HandsColor, v => Chams.HandsColor = v);
            Bool("chams.weapon", () => Chams.WeaponEnabled, v => Chams.WeaponEnabled = v);
            Int("chams.weapon.style", () => Chams.WeaponStyle, v => Chams.WeaponStyle = v);
            Hex("chams.weapon.color", () => Chams.WeaponColor, v => Chams.WeaponColor = v);
            Float("chams.glow", () => Chams.GlowStrength, v => Chams.GlowStrength = v);
            Bool("view.fov.enabled", () => Chams.ViewFovEnabled, v => Chams.ViewFovEnabled = v);
            Float("view.fov.value", () => Chams.ViewFovValue, v => Chams.ViewFovValue = v);
            Bool("misc.speed.enabled", () => Misc.SpeedEnabled, v => Misc.SpeedEnabled = v);
            Float("misc.speed.mult", () => Misc.SpeedMult, v => Misc.SpeedMult = v);
            Bool("misc.jump.enabled", () => Misc.JumpEnabled, v => Misc.JumpEnabled = v);
            Float("misc.jump.mult", () => Misc.JumpMult, v => Misc.JumpMult = v);
            Bool("misc.antiRubberband", () => Misc.AntiRubberband, v => Misc.AntiRubberband = v);
            Bool("movement.climb", () => Movement.ClimbBoostEnabled, v => Movement.ClimbBoostEnabled = v);
            Bool("exploit.instantEquip.enabled", () => InstantEquip.Enabled, v => InstantEquip.Enabled = v);
            Bool("exploit.vsteer.enabled", () => VehicleSteer.SteerEnabled, v => VehicleSteer.SteerEnabled = v);
            Bool("movement.autoSyncOnDisable", () => Movement.AutoSyncOnDisable, v => Movement.AutoSyncOnDisable = v);
            Bool("movement.autoSyncLeash", () => Movement.AutoSyncLeash, v => Movement.AutoSyncLeash = v);
            Float("movement.leashDistance", () => Movement.AutoSyncDistance, v => Movement.AutoSyncDistance = v);
            Float("movement.glideSpeed", () => Movement.GlideSpeed, v => Movement.GlideSpeed = v);
            Bool("misc.fastReload.enabled", () => Misc.FastReloadEnabled, v => Misc.FastReloadEnabled = v);
            Float("misc.fastReload.percent", () => Misc.FastReload, v => Misc.FastReload = v);
            Bool("misc.noVehicleFuel", () => Misc.NoVehicleFuel, v => Misc.NoVehicleFuel = v);
            Bool("misc.fps", () => Misc.ShowFps, v => Misc.ShowFps = v);
            Bool("misc.hitmarker", () => Misc.HitmarkerEnabled, v => Misc.HitmarkerEnabled = v);
            Bool("misc.hitmarkerSound", () => Misc.HitmarkerSound, v => Misc.HitmarkerSound = v);
            Bool("minimap.enabled", () => Minimap.Enabled, v => Minimap.Enabled = v);
            Float("minimap.size", () => Minimap.Size, v => Minimap.Size = v);
            Float("minimap.zoom", () => Minimap.ZoomMeters, v => Minimap.ZoomMeters = v);
            Bool("minimap.rotate", () => Minimap.Rotate, v => Minimap.Rotate = v);
            Bool("minimap.players", () => Minimap.ShowPlayers, v => Minimap.ShowPlayers = v);
            Bool("minimap.vehicles", () => Minimap.ShowVehicles, v => Minimap.ShowVehicles = v);
            Bool("minimap.zombies", () => Minimap.ShowZombies, v => Minimap.ShowZombies = v);
            Bool("minimap.satellite", () => Minimap.UseSatellite,
                v => { Minimap.UseSatellite = v; Minimap.Invalidate(); });
            Bool("theme.day", () => Theme.Enabled, v => Theme.Enabled = v);
            Float("freecam.speed", () => Freecam.Speed, v => Freecam.Speed = v);
            String("binds", Binds.Serialize, Binds.Deserialize);
            String("hud", Hud.Serialize, Hud.Deserialize);
        }
        private const string ConfigDir = "C:\\DexionDeveloper\\KerixUnturn3d";
        private static string FilePath
        {
            get { return Path.Combine(ConfigDir, "kerix.krx"); }
        }
        private static bool EnsureDir()
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                    Directory.CreateDirectory(ConfigDir);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static void Save()
        {
            RegisterAll();
            if (WriteFile()) Flash("Saved");
            _snapshot = Snapshot();
            _dirty = false;
        }
        private static void SilentSave()
        {
            RegisterAll();
            WriteFile();
            _snapshot = Snapshot();
            _dirty = false;
        }
        private static bool WriteFile()
        {
            try
            {
                if (!EnsureDir())
                {
                    Flash("Save failed: cannot create " + ConfigDir);
                    return false;
                }
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("KRXCFG1");
                sb.AppendLine("# Kerix.zyx config");
                foreach (Entry e in _entries)
                    sb.AppendLine(e.Key + "=" + e.Get());
                File.WriteAllText(FilePath, sb.ToString());
                return true;
            }
            catch (Exception e)
            {
                Flash("Save failed: " + e.Message);
                return false;
            }
        }
        private static string _snapshot;
        private static bool _dirty;
        private static float _autosaveAt;
        private static float _nextCheck;
        private static string Snapshot()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Entry e in _entries)
                sb.Append(e.Key).Append('=').Append(e.Get()).Append('\n');
            return sb.ToString();
        }
        public static void UpdateTick()
        {
            float t = Time.realtimeSinceStartup;
            if (_dirty && t >= _autosaveAt)
                SilentSave();
            if (t < _nextCheck) return;
            _nextCheck = t + 0.25f;
            RegisterAll();
            string now = Snapshot();
            if (_snapshot == null)
            {
                _snapshot = now;
                return;
            }
            if (now != _snapshot)
            {
                _snapshot = now;
                _dirty = true;
                _autosaveAt = t + 2f;
            }
        }
        public static void Load()
        {
            RegisterAll();
            try
            {
                if (!File.Exists(FilePath))
                {
                    Flash("No config file");
                    _snapshot = Snapshot();
                    return;
                }
                foreach (string raw in File.ReadAllLines(FilePath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("KRXCFG")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    foreach (Entry e in _entries)
                        if (e.Key == key) { e.Set(val); break; }
                }
                Flash("Loaded");
                _snapshot = Snapshot();
                _dirty = false;
            }
            catch (Exception e)
            {
                Flash("Load failed: " + e.Message);
            }
        }
        public static void Reset()
        {
            RegisterAll();
            foreach (Entry e in _entries)
                e.Set(e.Default);
            Flash("Reset to defaults");
            _snapshot = Snapshot();
            _dirty = false;
        }
        private static void Flash(string msg)
        {
            Status = msg;
            _statusUntil = Time.realtimeSinceStartup + 2f;
        }
        public static string StatusNow
        {
            get { return Time.realtimeSinceStartup < _statusUntil ? Status : ""; }
        }
    }
}
