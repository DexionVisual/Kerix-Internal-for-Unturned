using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Kerix
{
    public static class Loader
    {
        public static void Init()
        {
            try
            {
                string dir = Path.GetDirectoryName(typeof(Loader).Assembly.Location) ?? ".";
                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    if (e.Name != null && e.Name.StartsWith("0Harmony"))
                        return Assembly.LoadFrom(Path.Combine(dir, "Vault.dll"));
                    return null;
                };
                Patcher.Apply();
                GameObject go = new GameObject("Kerix");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<KerixMain>();
                Log("Kerix loaded");
            }
            catch (Exception e)
            {
                Log("Init failed: " + e);
            }
        }
        public static void Log(string msg)
        {
            try
            {
                File.AppendAllText("kerix_cheat.log",
                    DateTime.Now.ToString("HH:mm:ss") + "  " + msg + Environment.NewLine);
            }
            catch { }
        }
    }
    internal static class Patcher
    {
        public static void Apply()
        {
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("kerix.unturned");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            MethodInfo askDamage = HarmonyLib.AccessTools.Method(typeof(SDG.Unturned.PlayerLife),
                "askDamage", new[]
                {
                    typeof(byte), typeof(Vector3), typeof(SDG.Unturned.EDeathCause),
                    typeof(SDG.Unturned.ELimb), typeof(Steamworks.CSteamID),
                    typeof(SDG.Unturned.EPlayerKill).MakeByRefType(),
                    typeof(bool), typeof(SDG.Unturned.ERagdollEffect), typeof(bool), typeof(bool),
                });
            if (askDamage != null)
            {
                harmony.Patch(askDamage, postfix: new HarmonyLib.HarmonyMethod(
                    typeof(Misc.Patches), nameof(Misc.Patches.HitmarkerPostfix)));
                Loader.Log("hitmarker patch applied");
            }
            else
            {
                Loader.Log("askDamage overload NOT found - hitmarker inactive");
            }
            Loader.Log("Harmony patches applied");
        }
    }
}
