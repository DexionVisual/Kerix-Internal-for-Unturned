using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
namespace Kerix
{
    public static class Freecam
    {
        public static bool Enabled;
        public static float Speed = 14f;
        private static Camera _cam;
        private static Vector3 _pos;
        private static float _yaw;
        private static float _pitch;
        private static Transform _origParent;
        private static readonly List<Behaviour> _disabled = new List<Behaviour>();
        private static readonly List<GameObject> _hidden = new List<GameObject>();
        private static bool _captured;
        private static PropertyInfo _mainCameraProp;
        private static bool _inited;
        static Freecam()
        {
            Camera.onPreCull += OnPreCull;
        }
        private static void Init()
        {
            if (_inited) return;
            _inited = true;
            Type t = Type.GetType("SDG.Unturned.MainCamera, Assembly-CSharp");
            if (t != null)
                _mainCameraProp = t.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
        }
        private static Camera FindCamera()
        {
            Init();
            try
            {
                if (_mainCameraProp != null)
                {
                    Camera c = _mainCameraProp.GetValue(null, null) as Camera;
                    if (c != null) return c;
                }
            }
            catch { }
            return Camera.main;
        }
        private static void ParkBehaviours(Transform root, bool includeSelf)
        {
            if (root == null) return;
            Behaviour[] comps = includeSelf
                ? root.GetComponents<Behaviour>()
                : root.GetComponentsInParent<Behaviour>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                Behaviour b = comps[i];
                if (b == null || !b.enabled) continue;
                if (b is Camera) continue;
                string tn = b.GetType().Name;
                if (tn == "MainCamera") continue;
                if (tn.Contains("CameraController") || tn.Contains("Orbit") || tn.Contains("FreeLook"))
                {
                    b.enabled = false;
                    if (!_disabled.Contains(b)) _disabled.Add(b);
                }
            }
        }
        private static void HideChild(Transform root, string name)
        {
            if (root == null) return;
            Transform c = root.Find(name);
            if (c == null || !c.gameObject.activeSelf) return;
            c.gameObject.SetActive(false);
            _hidden.Add(c.gameObject);
        }
        private static void Capture(Camera cam)
        {
            _cam = cam;
            _pos = cam.transform.position;
            Vector3 e = cam.transform.eulerAngles;
            _pitch = e.x > 180f ? e.x - 360f : e.x;
            _yaw = e.y;
            HideChild(cam.transform, "Viewmodel");
            HideChild(cam.transform, "Spot");
            ParkBehaviours(cam.transform.parent, false);
            ParkBehaviours(cam.transform, true);
            _origParent = cam.transform.parent;
            cam.transform.parent = null;
            _captured = true;
        }
        private static void Release()
        {
            if (_cam != null && _origParent != null && _cam.transform.parent == null)
                _cam.transform.parent = _origParent;
            _origParent = null;
            for (int i = 0; i < _disabled.Count; i++)
            {
                Behaviour b = _disabled[i];
                if (b != null) b.enabled = true;
            }
            _disabled.Clear();
            for (int i = 0; i < _hidden.Count; i++)
                if (_hidden[i] != null) _hidden[i].SetActive(true);
            _hidden.Clear();
            _cam = null;
        }
        private static void ApplyPose()
        {
            if (_cam == null) return;
            _cam.transform.position = _pos;
            _cam.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
        private static void OnPreCull(Camera cam)
        {
            if (!Enabled || _cam == null || cam != _cam) return;
            ApplyPose();
        }
        public static void LateUpdate()
        {
            if (!Enabled)
            {
                if (_captured) Release();
                return;
            }
            Camera cam = FindCamera();
            if (cam == null)
            {
                Enabled = false;
                Release();
                return;
            }
            Player local = Player.LocalPlayer;
            try
            {
                if (local == null || local.life == null || local.life.isDead ||
                    (local.movement != null && local.movement.getVehicle() != null))
                {
                    Enabled = false;
                    Release();
                    return;
                }
            }
            catch { }
            if (!_captured) Capture(cam);
            else
            {
                _cam = cam;
                if (cam.transform.parent != null) cam.transform.parent = null;
            }
            bool menuOpen = KerixMain.Instance != null && KerixMain.Instance.MenuOpen;
            if (!menuOpen)
            {
                _yaw += Input.GetAxis("Mouse X") * 2.4f;
                _pitch -= Input.GetAxis("Mouse Y") * 2.4f;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
                Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
                float sp = Speed * (Input.GetKey(KeyCode.LeftShift) ? 3f : 1f);
                Vector3 mv = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) mv += Vector3.forward;
                if (Input.GetKey(KeyCode.S)) mv -= Vector3.forward;
                if (Input.GetKey(KeyCode.D)) mv += Vector3.right;
                if (Input.GetKey(KeyCode.A)) mv -= Vector3.right;
                if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) mv += Vector3.up;
                if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) mv -= Vector3.up;
                _pos += rot * mv.normalized * sp * Time.unscaledDeltaTime;
            }
            ApplyPose();
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerLook), "updateLook")]
        static bool UpdateLookPrefix(PlayerLook __instance)
        {
            if (!Enabled) return true;
            return __instance.player != Player.LocalPlayer;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerLook), "Update")]
        static bool LookUpdatePrefix(PlayerLook __instance)
        {
            if (!Enabled) return true;
            return __instance.player != Player.LocalPlayer;
        }
    }
}
