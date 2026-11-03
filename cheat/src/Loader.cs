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

                Assembly.LoadFrom(Path.Combine(
                    Path.GetDirectoryName(typeof(Loader).Assembly.Location) ?? ".",
                    "0Harmony.dll"));

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
                    Log("hitmarker patch applied");
                }
                else
                {
                    Log("askDamage overload NOT found - hitmarker inactive");
                }

                Log("Harmony patches applied");

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
}
