using System;
using System.Collections.Generic;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.Rendering;
namespace Kerix
{
    public static class Chams
    {
        public static bool PlayersEnabled = true;
        public static int PlayersStyle = 2;
        public static Color PlayersColor = new Color(1f, 0.22f, 0.28f, 1f);
        public static bool PlayersThroughWalls = true;
        public static bool HandsEnabled = true;
        public static int HandsStyle = 1;
        public static Color HandsColor = new Color(0.45f, 0.58f, 1f, 1f);
        public static bool WeaponEnabled = true;
        public static int WeaponStyle = 2;
        public static Color WeaponColor = new Color(0.92f, 0.94f, 1f, 1f);
        public static float GlowStrength = 2.2f;
        public static bool ViewFovEnabled;
        public static float ViewFovValue = 90f;
        public static readonly string[] StyleNames = { "Normal", "Soft Glow", "Glow", "Rainbow" };
        private static Material _playerMat;
        private static Material _handMat;
        private static Material _weaponMat;
        private static Material _ghostMat;
        private static Shader _shader;
        private static bool _shaderStandard;
        private static MaterialPropertyBlock _emptyBlock;
        private static readonly Dictionary<Renderer, Material> _applied = new Dictionary<Renderer, Material>();
        private static readonly Dictionary<Renderer, Material[]> _original = new Dictionary<Renderer, Material[]>();
        private static readonly Dictionary<Player, PlayerChams> _playerRenderers = new Dictionary<Player, PlayerChams>();
        private static Renderer[] _viewRenderers;
        private static Transform _lastFirstModel;
        private static float _nextRefresh;
        private static readonly List<Renderer> _chamRenderers = new List<Renderer>();
        private static bool _cmdDirty = true;
        private static bool _applying;
        private static CommandBuffer _cmd;
        private static Camera _ghostCam;
        private static bool _ghostAttached;
        private class PlayerChams
        {
            public Transform ThirdModel;
            public Renderer[] Renderers;
        }
        public static void UpdateTick()
        {
            float t = Time.unscaledTime;
            bool any = PlayersEnabled || HandsEnabled || WeaponEnabled;
            if (!any)
            {
                RestoreAll();
                TeardownGhost();
                return;
            }
            if (!EnsureMaterials()) return;
            UpdateMaterial(_playerMat, PlayersEnabled ? PlayersStyle : 0, PlayersColor, t);
            UpdateMaterial(_handMat, HandsEnabled ? HandsStyle : 0, HandsColor, t);
            UpdateMaterial(_weaponMat, WeaponEnabled ? WeaponStyle : 0, WeaponColor, t);
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 2f;
                RefreshPlayerRenderers();
                _viewRenderers = null;
            }
            _chamRenderers.Clear();
            bool changed = false;
            if (PlayersEnabled || WeaponEnabled)
                changed |= ApplySet();
            if (HandsEnabled || WeaponEnabled)
                changed |= ApplyViewmodel();
            UpdateGhost();
            if (changed) _cmdDirty = true;
            UpdateThroughWall();
        }
        private static bool EnsureMaterials()
        {
            if (_playerMat != null && _handMat != null && _weaponMat != null && _ghostMat != null) return true;
            if (_emptyBlock == null) _emptyBlock = new MaterialPropertyBlock();
            if (_shader == null)
            {
                string[] candidates = { "Standard", "Legacy Shaders/Specular", "Legacy Shaders/Diffuse" };
                for (int i = 0; i < candidates.Length; i++)
                {
                    Shader s = null;
                    try { s = Shader.Find(candidates[i]); } catch { }
                    if (s != null)
                    {
                        _shader = s;
                        _shaderStandard = i == 0;
                        break;
                    }
                }
                if (_shader == null)
                {
                    Loader.Log("Chams: no usable shader found");
                    return false;
                }
            }
            if (_playerMat == null) _playerMat = NewMat("KerixChams.Player");
            if (_handMat == null) _handMat = NewMat("KerixChams.Hand");
            if (_weaponMat == null) _weaponMat = NewMat("KerixChams.Weapon");
            if (_ghostMat == null)
            {
                Shader gs = null;
                try { gs = Shader.Find("GUI/Text Shader"); } catch { }
                if (gs != null)
                {
                    _ghostMat = new Material(gs);
                    _ghostMat.name = "KerixChams.Ghost";
                    _ghostMat.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return _playerMat != null && _handMat != null && _weaponMat != null && _ghostMat != null;
        }
        private static Material NewMat(string name)
        {
            Material m = new Material(_shader);
            m.name = name;
            m.hideFlags = HideFlags.HideAndDontSave;
            if (_shaderStandard)
            {
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Glossiness", 0.35f);
                m.SetColor("_EmissionColor", Color.black);
                m.DisableKeyword("_EMISSION");
            }
            return m;
        }
        private static void UpdateMaterial(Material m, int style, Color color, float t)
        {
            if (m == null) return;
            Color baseC = color;
            Color emit = color;
            float em = 0f, metal = 0f, smooth = 0.35f;
            switch (style)
            {
                case 1:
                    metal = 0.1f;
                    smooth = 0.5f;
                    baseC = Color.Lerp(color, Color.white, 0.1f);
                    em = Mathf.Max(0.5f, GlowStrength * 0.25f);
                    break;
                case 2:
                    metal = 0.2f;
                    smooth = 0.55f;
                    baseC = Color.Lerp(color * 0.55f, Color.white, 0.08f);
                    em = GlowStrength;
                    break;
                case 3:
                    float hue = Mathf.Repeat(t * 0.13f, 1f);
                    emit = Color.HSVToRGB(hue, 0.85f, 1f);
                    baseC = emit * 0.5f;
                    metal = 0.35f;
                    smooth = 0.7f;
                    em = GlowStrength * 0.9f;
                    break;
            }
            m.SetColor("_Color", baseC);
            if (_shaderStandard)
            {
                m.SetFloat("_Metallic", metal);
                m.SetFloat("_Glossiness", smooth);
                if (em > 0.001f)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emit * em);
                }
                else
                {
                    m.DisableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", Color.black);
                }
            }
            else
            {
                if (m.HasProperty("_SpecColor"))
                    m.SetColor("_SpecColor", Color.Lerp(baseC, Color.white, style == 1 ? 0.9f : 0.3f));
                if (m.HasProperty("_Shininess"))
                    m.SetFloat("_Shininess", style == 1 ? 0.75f : 0.25f);
            }
        }
        private static void UpdateGhost()
        {
            if (_ghostMat == null) return;
            Color c = PlayersStyle == 3
                ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.13f, 1f), 0.85f, 1f)
                : PlayersColor;
            c.a = 0.55f;
            _ghostMat.SetColor("_Color", c);
        }
        private static void RefreshPlayerRenderers()
        {
            Player local = Player.LocalPlayer;
            List<SteamPlayer> clients = Provider.clients;
            if (clients == null || local == null) return;
            bool changed = false;
            HashSet<Player> alive = new HashSet<Player>();
            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == local) continue;
                Player p = sp.player;
                try
                {
                    if (p.life == null || p.life.isDead || p.third == null) continue;
                    alive.Add(p);
                    PlayerChams pc;
                    if (!_playerRenderers.TryGetValue(p, out pc))
                    {
                        pc = new PlayerChams();
                        _playerRenderers[p] = pc;
                        changed = true;
                    }
                    Transform thirdModel = p.equipment != null ? p.equipment.thirdModel : null;
                    if (pc.ThirdModel != thirdModel)
                    {
                        pc.ThirdModel = thirdModel;
                        pc.Renderers = p.third.GetComponentsInChildren<Renderer>(true);
                        changed = true;
                    }
                }
                catch { }
            }
            List<Player> gone = null;
            foreach (KeyValuePair<Player, PlayerChams> kv in _playerRenderers)
            {
                if (!alive.Contains(kv.Key))
                {
                    if (gone == null) gone = new List<Player>();
                    gone.Add(kv.Key);
                }
            }
            if (gone != null)
            {
                for (int i = 0; i < gone.Count; i++) _playerRenderers.Remove(gone[i]);
                changed = true;
            }
            PruneOriginals();
            if (changed) _cmdDirty = true;
        }
        private static bool ApplySet()
        {
            bool changed = false;
            foreach (KeyValuePair<Player, PlayerChams> kv in _playerRenderers)
            {
                PlayerChams pc = kv.Value;
                if (pc == null || pc.Renderers == null) continue;
                for (int i = 0; i < pc.Renderers.Length; i++)
                {
                    Renderer r = pc.Renderers[i];
                    if (r == null) continue;
                    try
                    {
                        if (!r.gameObject.activeInHierarchy) continue;
                        bool isWeapon = pc.ThirdModel != null && r.transform.IsChildOf(pc.ThirdModel);
                        bool want = isWeapon ? WeaponEnabled : PlayersEnabled;
                        Material mat = isWeapon ? _weaponMat : _playerMat;
                        if (!want)
                        {
                            if (_applied.ContainsKey(r)) { RestoreOne(r); changed = true; }
                            continue;
                        }
                        Material applied;
                        if (_applied.TryGetValue(r, out applied) && applied == mat)
                        {
                            _chamRenderers.Add(r);
                            continue;
                        }
                        Material[] cur = r.sharedMaterials;
                        if (!IsAll(cur, mat))
                        {
                            if (!_original.ContainsKey(r)) _original[r] = cur;
                            r.sharedMaterials = Fill(cur != null ? cur.Length : 1, mat);
                            changed = true;
                        }
                        _applied[r] = mat;
                        _chamRenderers.Add(r);
                    }
                    catch { }
                }
            }
            return changed;
        }
        private static bool ApplyViewmodel()
        {
            Player local = Player.LocalPlayer;
            Transform first = local != null ? local.first : null;
            if (first == null || local.equipment == null || _handMat == null || _weaponMat == null)
                return false;
            Transform firstModel = local.equipment.firstModel;
            if (firstModel != null && !firstModel.IsChildOf(first)) firstModel = null;
            if (_viewRenderers == null || _lastFirstModel != firstModel)
            {
                _lastFirstModel = firstModel;
                CollectViewRenderers(local, firstModel);
                _cmdDirty = true;
            }
            bool changed = false;
            for (int i = 0; i < _viewRenderers.Length; i++)
            {
                Renderer r = _viewRenderers[i];
                if (r == null) continue;
                try
                {
                    bool isWeapon = firstModel != null && r.transform.IsChildOf(firstModel);
                    bool want = isWeapon ? WeaponEnabled : HandsEnabled;
                    Material mat = isWeapon ? _weaponMat : _handMat;
                    if (!want)
                    {
                        if (_applied.ContainsKey(r)) { RestoreOne(r); changed = true; }
                        continue;
                    }
                    Material applied;
                    if (_applied.TryGetValue(r, out applied) && applied == mat)
                    {
                        _chamRenderers.Add(r);
                        continue;
                    }
                    Material[] cur = r.sharedMaterials;
                    if (!IsAll(cur, mat))
                    {
                        if (!_original.ContainsKey(r)) _original[r] = cur;
                        r.sharedMaterials = Fill(cur != null ? cur.Length : 1, mat);
                        changed = true;
                    }
                    _applied[r] = mat;
                    _chamRenderers.Add(r);
                }
                catch { }
            }
            return changed;
        }
        private static void AddViewRenderers(HashSet<Renderer> seen, List<Renderer> list, Transform root)
        {
            if (root == null) return;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null) continue;
                string tn = r.GetType().Name;
                if (tn != "MeshRenderer" && tn != "SkinnedMeshRenderer") continue;
                if (seen.Add(r)) list.Add(r);
            }
        }
        private static void CollectViewRenderers(Player local, Transform firstModel)
        {
            HashSet<Renderer> seen = new HashSet<Renderer>();
            List<Renderer> list = new List<Renderer>(64);
            AddViewRenderers(seen, list, firstModel);
            try
            {
                AddViewRenderers(seen, list, local.animator != null ? local.animator.firstSkeleton : null);
            }
            catch { }
            try
            {
                Camera cam = MainCamera.instance;
                if (cam != null)
                    AddViewRenderers(seen, list, cam.transform.Find("Viewmodel"));
            }
            catch { }
            AddViewRenderers(seen, list, local.first);
            _viewRenderers = list.ToArray();
        }
        private static bool IsAll(Material[] mats, Material m)
        {
            if (mats == null || mats.Length == 0) return false;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != m) return false;
            return true;
        }
        private static Material[] Fill(int n, Material m)
        {
            Material[] arr = new Material[n];
            for (int i = 0; i < n; i++) arr[i] = m;
            return arr;
        }
        private static void RestoreOne(Renderer r)
        {
            if (r == null) return;
            Material[] orig;
            if (_original.TryGetValue(r, out orig))
            {
                _original.Remove(r);
                _applied.Remove(r);
                if (orig != null && orig.Length > 0)
                {
                    _applying = true;
                    try { r.sharedMaterials = orig; } finally { _applying = false; }
                }
                return;
            }
            _applied.Remove(r);
        }
        private static void PruneOriginals()
        {
            List<Renderer> dead = null;
            foreach (KeyValuePair<Renderer, Material[]> kv in _original)
            {
                if (kv.Key == null)
                {
                    if (dead == null) dead = new List<Renderer>();
                    dead.Add(kv.Key);
                }
            }
            if (dead != null)
                for (int i = 0; i < dead.Count; i++)
                    _original.Remove(dead[i]);
        }
        private static void RestoreAll()
        {
            if (_original.Count == 0 && _applied.Count == 0) return;
            List<Renderer> all = new List<Renderer>(_original.Keys);
            for (int i = 0; i < all.Count; i++)
                RestoreOne(all[i]);
            List<Renderer> extra = new List<Renderer>(_applied.Keys);
            for (int i = 0; i < extra.Count; i++)
                RestoreOne(extra[i]);
            _chamRenderers.Clear();
        }
        private static void TeardownGhost()
        {
            if (_ghostCam != null && _ghostAttached)
            {
                _ghostCam.RemoveCommandBuffer(CameraEvent.AfterEverything, _cmd);
                _ghostAttached = false;
            }
            _cmdDirty = true;
        }
        private static Camera MainGameCamera()
        {
            Camera cam = null;
            try { cam = MainCamera.instance; } catch { }
            if (cam == null) cam = Camera.main;
            return cam;
        }
        private static void UpdateThroughWall()
        {
            bool want = PlayersEnabled && PlayersThroughWalls && _chamRenderers.Count > 0 && _ghostMat != null;
            Camera main = want ? MainGameCamera() : null;
            if (!want || main == null)
            {
                if (_ghostAttached && _ghostCam != null)
                {
                    _ghostCam.RemoveCommandBuffer(CameraEvent.AfterEverything, _cmd);
                    _ghostAttached = false;
                    _ghostCam = null;
                }
                return;
            }
            if (_ghostCam != main)
            {
                if (_ghostCam != null && _ghostAttached)
                    _ghostCam.RemoveCommandBuffer(CameraEvent.AfterEverything, _cmd);
                if (_cmd == null)
                    _cmd = new CommandBuffer { name = "KerixChams" };
                main.AddCommandBuffer(CameraEvent.AfterEverything, _cmd);
                _ghostCam = main;
                _ghostAttached = true;
                _cmdDirty = true;
            }
            if (_cmd == null)
                _cmd = new CommandBuffer { name = "KerixChams" };
            if (_cmdDirty)
            {
                _cmd.Clear();
                for (int i = 0; i < _chamRenderers.Count; i++)
                {
                    Renderer r = _chamRenderers[i];
                    if (r != null) _cmd.DrawRenderer(r, _ghostMat);
                }
                _cmdDirty = false;
            }
        }
        [HarmonyPatch]
        private static class SwapGuard
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerEquipment), "ReceiveEquip")]
            private static void EquipPostfix(PlayerEquipment __instance)
            {
                if (!(PlayersEnabled || HandsEnabled || WeaponEnabled)) return;
                try
                {
                    if (__instance.player != Player.LocalPlayer) return;
                    _viewRenderers = null;
                    _cmdDirty = true;
                }
                catch { }
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Renderer), "set_sharedMaterials")]
            private static bool SharedPrefix(Renderer __instance, Material[] value)
            {
                if (_applying || !PlayersEnabled) return true;
                Material applied;
                if (!_applied.TryGetValue(__instance, out applied) || applied == null) return true;
                if (value != null && IsAll(value, applied)) return true;
                _applying = true;
                try { __instance.sharedMaterials = Fill(value != null ? value.Length : 1, applied); }
                finally { _applying = false; }
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Renderer), "set_material")]
            private static bool SinglePrefix(Renderer __instance, Material value)
            {
                if (_applying || !PlayersEnabled) return true;
                Material applied;
                if (!_applied.TryGetValue(__instance, out applied) || applied == null) return true;
                if (value == applied) return true;
                _applying = true;
                try { __instance.sharedMaterial = applied; }
                finally { _applying = false; }
                return false;
            }
        }
        [HarmonyPatch]
        private static class FovPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerLook), "UpdateMainCameraTargetFieldOfView")]
            private static void ForceFovPrefix(PlayerLook __instance, ref float target)
            {
                if (!ViewFovEnabled) return;
                try
                {
                    if (__instance.player != Player.LocalPlayer) return;
                    if (target < OptionsSettings.DesiredVerticalFieldOfView - 0.5f) return;
                    target = ViewFovValue;
                }
                catch { }
            }
        }
    }
}
