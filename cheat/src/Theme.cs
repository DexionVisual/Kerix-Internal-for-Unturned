using System;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
namespace Kerix
{
    public static class Theme
    {
        public static bool Enabled;
        private static bool _applying;
        private static bool _active;
        private static bool _inited;
        private static bool _ok;
        private static void Init()
        {
            if (_inited) return;
            _inited = true;
            Type t = typeof(PlayerEquipment);
            _ok = t != null && AccessTools.Method(t, "updateVision") != null;
            Loader.Log("theme updateVision ok=" + _ok);
        }
        public static void UpdateTick()
        {
            Init();
            if (Enabled && !_active)
                Apply();
            else if (!Enabled && _active)
                Restore();
        }
        private static void Apply()
        {
            if (_applying) return;
            _applying = true;
            try
            {
                LevelLighting.vision = ELightingVision.MILITARY;
                LevelLighting.nightvisionColor = Color.white;
                LevelLighting.nightvisionFogIntensity = 0.15f;
                LevelLighting.updateLighting();
                _active = true;
            }
            finally
            {
                _applying = false;
            }
        }
        private static void Restore()
        {
            if (_applying) return;
            _applying = true;
            try
            {
                LevelLighting.vision = ELightingVision.NONE;
                LevelLighting.nightvisionColor = Color.white;
                LevelLighting.nightvisionFogIntensity = 0f;
                LevelLighting.updateLighting();
                _active = false;
            }
            finally
            {
                _applying = false;
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEquipment), "updateVision")]
        static bool UpdateVisionPrefix(PlayerEquipment __instance)
        {
            if (!Enabled) return true;
            return __instance.player != Player.LocalPlayer;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelLighting), "updateLighting")]
        static void UpdateLightingPostfix()
        {
            if (!Enabled || _applying || !_active) return;
            if (LevelLighting.vision != ELightingVision.MILITARY)
                Apply();
        }
    }
}
