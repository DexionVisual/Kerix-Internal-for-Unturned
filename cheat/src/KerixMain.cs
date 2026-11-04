using System.Collections;
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
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
                MenuOpen = !MenuOpen;

            if (Input.GetKeyDown(KeyCode.End))
                Esp.Enabled = !Esp.Enabled;

            Aimbot.UpdateTick();
            Misc.UpdateTick();

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

            Aimbot.DrawFovOverlay();

            Misc.DrawRadar();
            Misc.DrawHitmarker();
            Misc.DrawFps(_fps);

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
}
