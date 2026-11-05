using System.Collections.Generic;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kerix
{
    public static class Chams
    {
        // players (third-person models of other players)
        public static bool PlayersEnabled = true;
        public static int PlayersStyle = 2;              // Glow
        public static Color PlayersColor = new Color(1f, 0.22f, 0.28f, 1f);
        public static bool PlayersThroughWalls = true;

        // local first-person hands
        public static bool HandsEnabled = true;
        public static int HandsStyle = 1;                // Metallic
        public static Color HandsColor = new Color(0.45f, 0.58f, 1f, 1f);

        // held item (own viewmodel + other players' third-person item)
        public static bool WeaponEnabled = true;
        public static int WeaponStyle = 2;               // Glow
        public static Color WeaponColor = new Color(0.92f, 0.94f, 1f, 1f);

        public static float GlowStrength = 2.2f;
        public static float PulseSpeed = 1.6f;

        // view fov
        public static bool ViewFovEnabled;
        public static float ViewFovValue = 90f;

        public static readonly string[] StyleNames = { "Normal", "Metallic", "Glow", "Pulse", "Rainbow" };

        private static Material _playerMat;
        private static Material _handMat;
        private static Material _weaponMat;
        private static Material _ghostMat;               // unlit through-wall material
        private static Shader _shader;
        private static bool _shaderStandard;

        private static CommandBuffer _cmd;
        private static Camera _chamCam;
        private static MaterialPropertyBlock _emptyBlock;

        private static readonly Dictionary<Renderer, Material[]> _original = new Dictionary<Renderer, Material[]>();
        private static readonly List<Renderer> _chamRenderers = new List<Renderer>();

        // ------------------------------------------------------------------
        // materials
        // ------------------------------------------------------------------

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
                Loader.Log("Chams: shader=" + _shader.name + " standard=" + _shaderStandard);
            }

            if (_playerMat == null) _playerMat = NewMat("KerixChams.Player");
            if (_handMat == null) _handMat = NewMat("KerixChams.Hand");
            if (_weaponMat == null) _weaponMat = NewMat("KerixChams.Weapon");

            if (_ghostMat == null)
            {
                Shader gs = null;
                try { gs = Shader.Find("Sprites/Default"); } catch { }
                if (gs != null)
                {
                    _ghostMat = new Material(gs);
                    _ghostMat.name = "KerixChams.Ghost";
                    _ghostMat.hideFlags = HideFlags.HideAndDontSave;
                    Loader.Log("Chams: ghost material ok");
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
                case 1: // Metallic
                    metal = 1f;
                    smooth = 0.85f;
                    baseC = Color.Lerp(color, Color.white, 0.12f) * 0.95f;
                    break;

                case 2: // Glow
                    metal = 0.2f;
                    smooth = 0.55f;
                    baseC = Color.Lerp(color * 0.55f, Color.white, 0.08f);
                    em = GlowStrength;
                    break;

                case 3: // Pulse
                    metal = 0.2f;
                    smooth = 0.55f;
                    baseC = Color.Lerp(color * 0.45f, Color.white, 0.08f);
                    em = GlowStrength * (0.28f + 0.72f * 0.5f * (1f + Mathf.Sin(t * Mathf.Max(0.05f, PulseSpeed) * 6.28318f)));
                    break;

                case 4: // Rainbow
                    float hue = Mathf.Repeat(t * 0.13f * Mathf.Max(0.05f, PulseSpeed), 1f);
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

        private static void UpdateGhost(float t)
        {
            if (_ghostMat == null) return;
            Color c = (PlayersEnabled && PlayersStyle == 4)
                ? Color.HSVToRGB(Mathf.Repeat(t * 0.13f * Mathf.Max(0.05f, PulseSpeed), 1f), 0.85f, 1f)
                : PlayersColor;
            // premultiply for the Sprites/Default blend, ~60% tint so the styled
            // material still shows through on visible players
            c.a = 0.6f;
            c.r *= c.a;
            c.g *= c.a;
            c.b *= c.a;
            _ghostMat.SetColor("_Color", c);
        }

        // ------------------------------------------------------------------
        // tick
        // ------------------------------------------------------------------

        public static void UpdateTick()
        {
            float t = Time.unscaledTime;
            bool any = PlayersEnabled || HandsEnabled || WeaponEnabled;

            bool mats = any && EnsureMaterials();
            if (mats)
            {
                UpdateMaterial(_playerMat, PlayersEnabled ? PlayersStyle : 0, PlayersColor, t);
                UpdateMaterial(_handMat, HandsEnabled ? HandsStyle : 0, HandsColor, t);
                UpdateMaterial(_weaponMat, WeaponEnabled ? WeaponStyle : 0, WeaponColor, t);
                UpdateGhost(t);
            }

            _chamRenderers.Clear();

            ApplyViewmodel();

            if (PlayersEnabled && mats)
                ApplyPlayerChams();
            else
                RestoreAll();

            UpdateThroughWallCam();
        }

        // ------------------------------------------------------------------
        // first-person hands / weapon
        // ------------------------------------------------------------------

        private static void ApplyViewmodel()
        {
            Player local = Player.LocalPlayer;
            Transform first = local != null ? local.first : null;

            if (first == null || local.equipment == null || (!HandsEnabled && !WeaponEnabled) ||
                _handMat == null || _weaponMat == null)
            {
                RestoreAll();
                return;
            }

            Transform firstModel = local.equipment.firstModel;
            if (firstModel != null && !firstModel.IsChildOf(first)) firstModel = null;

            Renderer[] rs = first.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null) continue;
                string tn = r.GetType().Name;
                if (tn != "MeshRenderer" && tn != "SkinnedMeshRenderer") continue;

                bool isWeapon = firstModel != null && r.transform.IsChildOf(firstModel);
                bool want = isWeapon ? WeaponEnabled : HandsEnabled;
                if (!want)
                {
                    RestoreOne(r);
                    continue;
                }

                Material mat = isWeapon ? _weaponMat : _handMat;
                Material[] cur = r.sharedMaterials;
                if (!(IsAll(cur, mat) && _original.ContainsKey(r)))
                {
                    if (!_original.ContainsKey(r)) _original[r] = cur;
                    r.sharedMaterials = Fill(cur != null ? cur.Length : 1, mat);
                }
                r.SetPropertyBlock(_emptyBlock);
            }

            PruneOriginals();
        }

        // ------------------------------------------------------------------
        // players (swap materials in place, no clones)
        // ------------------------------------------------------------------

        private static void ApplyPlayerChams()
        {
            Player local = Player.LocalPlayer;
            List<SteamPlayer> clients = Provider.clients;
            if (clients == null || local == null)
            {
                RestoreAll();
                return;
            }

            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == local) continue;
                Player p = sp.player;
                try
                {
                    if (p.life == null || p.life.isDead) continue;
                    if (p.third == null) continue;

                    Transform thirdModel = p.equipment != null ? p.equipment.thirdModel : null;
                    Renderer[] rs = p.third.GetComponentsInChildren<Renderer>(true);
                    for (int j = 0; j < rs.Length; j++)
                    {
                        Renderer r = rs[j];
                        if (r == null) continue;
                        string tn = r.GetType().Name;
                        if (tn != "MeshRenderer" && tn != "SkinnedMeshRenderer") continue;
                        if (!r.gameObject.activeInHierarchy) continue;

                        Material mat = thirdModel != null && r.transform.IsChildOf(thirdModel)
                            ? _weaponMat
                            : _playerMat;
                        Material[] cur = r.sharedMaterials;
                        if (!(IsAll(cur, mat) && _original.ContainsKey(r)))
                        {
                            if (!_original.ContainsKey(r)) _original[r] = cur;
                            r.sharedMaterials = Fill(cur != null ? cur.Length : 1, mat);
                        }
                        _chamRenderers.Add(r);
                    }
                }
                catch { }
            }

            PruneOriginals();
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
                if (orig != null && orig.Length > 0) r.sharedMaterials = orig;
            }
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
            if (_original.Count == 0) return;
            List<Renderer> all = new List<Renderer>(_original.Keys);
            for (int i = 0; i < all.Count; i++)
                RestoreOne(all[i]);
        }

        // ------------------------------------------------------------------
        // through-wall overlay camera (command buffer, no layers touched)
        // ------------------------------------------------------------------

        private static Camera MainGameCamera()
        {
            Camera cam = null;
            try { cam = MainCamera.instance; } catch { }
            if (cam == null) cam = Camera.main;
            return cam;
        }

        private static void UpdateThroughWallCam()
        {
            bool want = PlayersEnabled && PlayersThroughWalls && _chamRenderers.Count > 0;

            bool scope = false;
            Player local = Player.LocalPlayer;
            if (local != null && local.look != null)
            {
                try { scope = local.look.isScopeActive; } catch { }
            }
            if (scope) want = false;

            Camera main = want ? MainGameCamera() : null;

            if (!want || main == null)
            {
                if (_chamCam != null && _chamCam.enabled) _chamCam.enabled = false;
                return;
            }

            if (_cmd == null)
                _cmd = new CommandBuffer { name = "KerixChams" };

            if (_chamCam == null)
            {
                GameObject go = new GameObject("KerixChamsCamera");
                go.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                _chamCam = go.AddComponent<Camera>();
                _chamCam.AddCommandBuffer(CameraEvent.AfterEverything, _cmd);
                Loader.Log("Chams: overlay camera created");
            }

            _cmd.Clear();
            for (int i = 0; i < _chamRenderers.Count; i++)
            {
                Renderer r = _chamRenderers[i];
                if (r == null) continue;
                _cmd.DrawRenderer(r, _ghostMat);
            }

            _chamCam.CopyFrom(main);
            _chamCam.enabled = true;
            _chamCam.depth = 100f;
            _chamCam.cullingMask = 0;
            _chamCam.clearFlags = CameraClearFlags.Depth;

            // CopyFrom must never drop our command buffer
            if (_chamCam.GetCommandBuffers(CameraEvent.AfterEverything).Length == 0)
                _chamCam.AddCommandBuffer(CameraEvent.AfterEverything, _cmd);
        }

        // ------------------------------------------------------------------
        // view fov (game writes MainCamera.instance.fieldOfView every frame
        // in PlayerLook.Update from mainCameraTargetFieldOfView, so we hook
        // the target updater; scope/ADS zoom targets pass through untouched)
        // ------------------------------------------------------------------

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
                    // zoom targets are always below the user's base fov
                    if (target < OptionsSettings.DesiredVerticalFieldOfView - 0.5f) return;
                    target = ViewFovValue;
                }
                catch { }
            }
        }
    }
}
