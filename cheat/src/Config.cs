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

        private static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;

            Bool("aim.vector.enabled", () => Aimbot.VectorEnabled, v => Aimbot.VectorEnabled = v);
            Bool("aim.vector.onlyAiming", () => Aimbot.VectorOnlyAiming, v => Aimbot.VectorOnlyAiming = v);
            Bool("aim.vector.drawFov", () => Aimbot.DrawFovCircle, v => Aimbot.DrawFovCircle = v);
            Int("aim.vector.target", () => Aimbot.VectorTargetPoint, v => Aimbot.VectorTargetPoint = v);
            Float("aim.vector.fov", () => Aimbot.VectorFov, v => Aimbot.VectorFov = v);
            Float("aim.vector.smoothing", () => Aimbot.VectorSmoothing, v => Aimbot.VectorSmoothing = v);
            Float("aim.vector.recoil", () => Aimbot.VectorRecoil, v => Aimbot.VectorRecoil = v);

            Bool("aim.silent.enabled", () => Aimbot.SilentEnabled, v => Aimbot.SilentEnabled = v);
            Int("aim.silent.target", () => Aimbot.SilentTargetPoint, v => Aimbot.SilentTargetPoint = v);
            Float("aim.silent.fov", () => Aimbot.SilentFov, v => Aimbot.SilentFov = v);

            Bool("aim.recoil.enabled", () => Aimbot.RecoilEnabled, v => Aimbot.RecoilEnabled = v);
            Float("aim.recoil.strength", () => Aimbot.RecoilStrength, v => Aimbot.RecoilStrength = v);

            Bool("aim.skipTeammates", () => Aimbot.SkipTeammates, v => Aimbot.SkipTeammates = v);

            Bool("esp.enabled", () => Esp.Enabled, v => Esp.Enabled = v);
            Bool("esp.box", () => Esp.Box, v => Esp.Box = v);
            Bool("esp.fill", () => Esp.Fill, v => Esp.Fill = v);
            Bool("esp.health", () => Esp.Health, v => Esp.Health = v);
            Bool("esp.name", () => Esp.Name, v => Esp.Name = v);
            Bool("esp.distance", () => Esp.Distance, v => Esp.Distance = v);
            Bool("esp.snaplines", () => Esp.Snaplines, v => Esp.Snaplines = v);

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
            Float("chams.pulse", () => Chams.PulseSpeed, v => Chams.PulseSpeed = v);

            Bool("view.fov.enabled", () => Chams.ViewFovEnabled, v => Chams.ViewFovEnabled = v);
            Float("view.fov.value", () => Chams.ViewFovValue, v => Chams.ViewFovValue = v);

            Bool("misc.speed.enabled", () => Misc.SpeedEnabled, v => Misc.SpeedEnabled = v);
            Float("misc.speed.mult", () => Misc.SpeedMult, v => Misc.SpeedMult = v);
            Bool("misc.jump.enabled", () => Misc.JumpEnabled, v => Misc.JumpEnabled = v);
            Float("misc.jump.mult", () => Misc.JumpMult, v => Misc.JumpMult = v);
            Bool("misc.fastReload.enabled", () => Misc.FastReloadEnabled, v => Misc.FastReloadEnabled = v);
            Float("misc.fastReload.percent", () => Misc.FastReload, v => Misc.FastReload = v);
            Bool("misc.noVehicleFuel", () => Misc.NoVehicleFuel, v => Misc.NoVehicleFuel = v);
            Bool("misc.radar.enabled", () => Misc.RadarEnabled, v => Misc.RadarEnabled = v);
            Float("misc.radar.range", () => Misc.RadarRange, v => Misc.RadarRange = v);
            Bool("misc.fps", () => Misc.ShowFps, v => Misc.ShowFps = v);
            Bool("misc.hitmarker", () => Misc.HitmarkerEnabled, v => Misc.HitmarkerEnabled = v);
            Bool("misc.hitmarkerSound", () => Misc.HitmarkerSound, v => Misc.HitmarkerSound = v);
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
            try
            {
                if (!EnsureDir())
                {
                    Flash("Save failed: cannot create " + ConfigDir);
                    return;
                }
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("KRXCFG1");
                sb.AppendLine("# Kerix.zyx config");
                foreach (Entry e in _entries)
                    sb.AppendLine(e.Key + "=" + e.Get());
                File.WriteAllText(FilePath, sb.ToString());
                Flash("Saved");
            }
            catch (Exception e)
            {
                Flash("Save failed: " + e.Message);
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
