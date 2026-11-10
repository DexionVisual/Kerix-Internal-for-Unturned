using HarmonyLib;
using SDG.Unturned;

namespace Kerix
{
    public static class InstantEquip
    {
        public static bool Enabled;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEquipment), "get_IsEquipAnimationFinished")]
        static bool InstantEquipPrefix(PlayerEquipment __instance, ref bool __result)
        {
            if (!Enabled) return true;
            if (__instance.player != Player.LocalPlayer) return true;
            __result = true;
            return false;
        }
    }
}
