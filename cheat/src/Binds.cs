using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Kerix
{
    public class Bind
    {
        public string Id;
        public string Label;
        public KeyCode Key;        
        public bool HoldMode;      
        public Func<bool> Get;
        public Action<bool> Set;
        public bool WasDown;       
        public bool HoldRestore;   
        public bool HoldActive;
        public string KeyName
        {
            get
            {
                if (Key == KeyCode.None) return "-";
                if (Key >= KeyCode.Mouse0 && Key <= KeyCode.Mouse6)
                    return "M" + (Key - KeyCode.Mouse0);
                switch (Key)
                {
                    case KeyCode.RightBracket: return "]";
                    case KeyCode.LeftBracket: return "[";
                    case KeyCode.Space: return "Spc";
                    case KeyCode.Return: return "Ent";
                    case KeyCode.KeypadEnter: return "NEnt";
                    case KeyCode.Escape: return "Esc";
                    case KeyCode.LeftShift: return "LSft";
                    case KeyCode.RightShift: return "RSft";
                    case KeyCode.LeftControl: return "LCtl";
                    case KeyCode.RightControl: return "RCtl";
                    case KeyCode.LeftAlt: return "LAlt";
                    case KeyCode.RightAlt: return "RAlt";
                    case KeyCode.LeftCommand: return "LCmd";
                    case KeyCode.RightCommand: return "RCmd";
                    case KeyCode.CapsLock: return "Cap";
                    case KeyCode.BackQuote: return "`";
                    case KeyCode.Minus: return "-";
                    case KeyCode.Equals: return "=";
                    case KeyCode.Semicolon: return ";";
                    case KeyCode.Quote: return "'";
                    case KeyCode.Comma: return ",";
                    case KeyCode.Period: return ".";
                    case KeyCode.Slash: return "/";
                    case KeyCode.Backslash: return "\\";
                    case KeyCode.ScrollLock: return "Scl";
                    case KeyCode.Insert: return "Ins";
                    case KeyCode.Delete: return "Del";
                    case KeyCode.Home: return "Hom";
                    case KeyCode.End: return "End";
                    case KeyCode.PageUp: return "PUp";
                    case KeyCode.PageDown: return "PDn";
                }
                string n = Key.ToString();
                if (n.StartsWith("Alpha")) return n.Substring(5);
                if (n.StartsWith("NumPad")) return "N" + n.Substring(6);
                if (n.Length > 5) return n.Substring(0, 5);
                return n;
            }
        }
    }
    public static class Binds
    {
        private static readonly List<Bind> _binds = new List<Bind>();
        private static bool _registered;
        public static string CaptureFor;
        public static IList<Bind> All
        {
            get { return _binds; }
        }
        public static Bind Find(string id)
        {
            for (int i = 0; i < _binds.Count; i++)
                if (_binds[i].Id == id) return _binds[i];
            return null;
        }
        public static void Register(string id, string label, Func<bool> get, Action<bool> set)
        {
            if (Find(id) != null) return;
            _binds.Add(new Bind { Id = id, Label = label, Key = KeyCode.None, HoldMode = false, Get = get, Set = set });
        }
        public static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;
            Register("aim.silent", "Silent Aim", () => Aimbot.SilentEnabled, v => Aimbot.SilentEnabled = v);
            Register("aim.ballistics", "Ballistics", () => Aimbot.BallisticsEnabled, v => Aimbot.BallisticsEnabled = v);
            Register("aim.recoil", "Recoil Control", () => Aimbot.RecoilEnabled, v => Aimbot.RecoilEnabled = v);
            Register("esp.players", "Player ESP", () => Esp.Enabled, v => Esp.Enabled = v);
            Register("chams.players", "Chams Players", () => Chams.PlayersEnabled, v => Chams.PlayersEnabled = v);
            Register("chams.hands", "Chams Hands", () => Chams.HandsEnabled, v => Chams.HandsEnabled = v);
            Register("chams.weapon", "Chams Weapon", () => Chams.WeaponEnabled, v => Chams.WeaponEnabled = v);
            Register("esp.zombies", "Zombie ESP", () => Esp.Zombies, v => Esp.Zombies = v);
            Register("esp.vehicles", "Vehicle ESP", () => Esp.Vehicles, v => Esp.Vehicles = v);
            Register("esp.items", "Item ESP", () => Esp.Items, v => Esp.Items = v);
            Register("misc.speed", "Speed", () => Misc.SpeedEnabled, v => Misc.SpeedEnabled = v);
            Register("misc.jump", "Jump Power", () => Misc.JumpEnabled, v => Misc.JumpEnabled = v);
            Register("misc.sync", "Sync Position",
                () => false,
                v =>
                {
                    if (v)
                    {
                        Misc.SpeedEnabled = false;
                        Misc.JumpEnabled = false;
                        Movement.RequestSync();
                    }
                });
            Register("movement.climb", "Climb Boost", () => Movement.ClimbBoostEnabled, v => Movement.ClimbBoostEnabled = v);
            Register("exploit.vsteer", "Steering Overflow", () => VehicleSteer.SteerEnabled, v => VehicleSteer.SteerEnabled = v);
            Register("misc.fastreload", "Fast Reload", () => Misc.FastReloadEnabled, v => Misc.FastReloadEnabled = v);
            Register("misc.nofuel", "No Vehicle Fuel", () => Misc.NoVehicleFuel, v => Misc.NoVehicleFuel = v);
            Register("visuals.minimap", "Minimap", () => Minimap.Enabled, v => Minimap.Enabled = v);
            Register("visuals.freecam", "Freecam", () => Freecam.Enabled, v => Freecam.Enabled = v);
        }
        public static void Update()
        {
            if (KerixMain.Instance != null && KerixMain.Instance.MenuOpen)
            {
                for (int i = 0; i < _binds.Count; i++) _binds[i].WasDown = false;
                return;
            }
            for (int i = 0; i < _binds.Count; i++)
            {
                Bind b = _binds[i];
                if (b.Key == KeyCode.None) continue;
                try
                {
                    bool down = Input.GetKey(b.Key);
                    bool pressed = down && !b.WasDown;
                    b.WasDown = down;
                    if (b.HoldMode)
                    {
                        if (down && !b.HoldActive)
                        {
                            b.HoldActive = true;
                            b.HoldRestore = b.Get();
                            b.Set(true);
                        }
                        else if (!down && b.HoldActive)
                        {
                            b.HoldActive = false;
                            b.Set(b.HoldRestore);
                        }
                    }
                    else if (pressed)
                    {
                        b.Set(!b.Get());
                    }
                }
                catch { }
            }
        }
        public static bool TryCapture(KeyCode key)
        {
            Bind b = Find(CaptureFor);
            if (b == null) return false;
            b.Key = key == KeyCode.None ? KeyCode.None : key;
            CaptureFor = null;
            return true;
        }
        public static void ClearKey(string id)
        {
            Bind b = Find(id);
            if (b != null)
            {
                b.Key = KeyCode.None;
                b.HoldActive = false;
            }
        }
        public static string Serialize()
        {
            RegisterAll();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _binds.Count; i++)
            {
                Bind b = _binds[i];
                if (sb.Length > 0) sb.Append(';');
                sb.Append(b.Id).Append('=').Append((int)b.Key).Append(':').Append(b.HoldMode ? 'h' : 't');
            }
            return sb.ToString();
        }
        public static void Deserialize(string data)
        {
            RegisterAll();
            if (string.IsNullOrEmpty(data)) return;
            try
            {
                foreach (string part in data.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq <= 0) continue;
                    string id = part.Substring(0, eq);
                    string rest = part.Substring(eq + 1);
                    Bind b = Find(id);
                    if (b == null) continue;
                    int colon = rest.IndexOf(':');
                    int keyInt = 0;
                    string keyPart = colon >= 0 ? rest.Substring(0, colon) : rest;
                    string modePart = colon >= 0 ? rest.Substring(colon + 1) : "";
                    if (int.TryParse(keyPart, out keyInt) && keyInt > 0)
                        b.Key = (KeyCode)keyInt;
                    b.HoldMode = modePart == "h";
                }
            }
            catch { }
        }
    }
}
