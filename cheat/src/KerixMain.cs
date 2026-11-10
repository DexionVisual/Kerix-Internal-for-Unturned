using System.Collections;
using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
namespace Kerix
{
    public class KerixMain : MonoBehaviour
    {
        public static KerixMain Instance;
        public bool MenuOpen = true;
        private float _menuAnim = 1f;
        private float _espAnim = 1f;
        private bool _wasOpen;
        private float _fps = 60f;
        private void Awake()
        {
            Instance = this;
            Binds.RegisterAll();
            Config.Load(); 
            StartCoroutine(CursorKeeper());
        }
        private IEnumerator CursorKeeper()
        {
            while (true)
            {
                if (MenuOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                yield return new WaitForEndOfFrame();
            }
        }
        private void LateUpdate()
        {
            if (MenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Input.ResetInputAxes();
            }
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
                MenuOpen = !MenuOpen;
            if (Input.GetKeyDown(KeyCode.End))
                Esp.Enabled = !Esp.Enabled;
            if (MenuOpen)
            {
                Input.ResetInputAxes();
            }
            Aimbot.UpdateTick();
            Misc.UpdateTick();
            Chams.UpdateTick();
            Movement.UpdateTick();
            Binds.Update();
            Config.UpdateTick();
            float raw = Time.unscaledDeltaTime > 0.0001f ? 1f / Time.unscaledDeltaTime : 60f;
            _fps = Mathf.Lerp(_fps, raw, 0.05f);
            if (MenuOpen)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                _wasOpen = true;
            }
            else if (_wasOpen)
            {
                _wasOpen = false;
                if (Player.LocalPlayer != null)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            Movement.LateUpdate();
            Freecam.LateUpdate();
            if (Hud.EditMode && !MenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        private void OnGUI()
        {
            float dt = Time.unscaledDeltaTime;
            Ui.Prepare();
            _menuAnim = Ui.Ease(_menuAnim, MenuOpen ? 1f : 0f, 8f, dt);
            _espAnim = Ui.Ease(_espAnim, Esp.Enabled ? 1f : 0f, 10f, dt);
            if (_espAnim > 0.01f)
            {
                Ui.PushFade(_espAnim);
                Esp.Draw();
                Ui.PopFade();
            }
            Aimbot.UpdateTick();
            Aimbot.DrawSilentFovCircle();
            Aimbot.DrawSilentLine();
            Theme.UpdateTick();
            Minimap.Draw();
            Misc.DrawHitmarker();
            Movement.DrawDesyncHud();
            Misc.DrawFps(_fps);
            Hud.DrawEditOverlay();
            if (_menuAnim > 0.01f)
            {
                Ui.PushFade(Ui.Smooth(_menuAnim));
                Ui.RectFill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.38f));
                Ui.PopFade();
                Ui.PushFade(_menuAnim);
                Menu.Draw(_menuAnim);
                Ui.PopFade();
                Event e = Event.current;
                if (e != null && (e.type == EventType.MouseDown || e.type == EventType.MouseUp ||
                                  e.type == EventType.ScrollWheel))
                {
                    e.Use();
                }
            }
        }
    }
    [HarmonyPatch]
    internal static class InputGate
    {
        private static bool Block
        {
            get
            {
                return (KerixMain.Instance != null && KerixMain.Instance.MenuOpen) ||
                       Hud.EditMode;
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InputEx), "GetKey")]
        static bool GetKey(KeyCode key, ref bool __result)
        {
            if (!Block || key == KeyCode.Insert) return true;
            __result = false;
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InputEx), "GetKeyDown")]
        static bool GetKeyDown(KeyCode key, ref bool __result)
        {
            if (!Block || key == KeyCode.Insert) return true;
            __result = false;
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InputEx), "GetKeyUp")]
        static bool GetKeyUp(KeyCode key, ref bool __result)
        {
            if (!Block) return true;
            __result = false;
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InputEx), "ConsumeKeyDown")]
        static bool ConsumeKeyDown(KeyCode key, ref bool __result)
        {
            if (!Block) return true;
            __result = false;
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerLook), "simulate")]
        static void SilenceLook(ref float look_x, ref float look_y)
        {
            if (!Block) return;
            look_x = 0f;
            look_y = 0f;
        }
    }
}
