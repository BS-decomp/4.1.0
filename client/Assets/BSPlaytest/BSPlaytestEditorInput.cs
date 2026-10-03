// BS-decomp / Block Strike 4.1.0 — keyboard + mouse for the editor (editor-only).
//
// Why this exists
// ---------------
// The game is touch-only. `InputJoystick` and `InputTouchLook` read nothing but
// `Input.touchCount` / `Input.GetTouch`, and the editor on Windows produces no
// touches at all, so in play mode there is no movement and no camera look —
// exactly the "I am a cripple" symptom. NGUI's `UICamera` has the same problem:
// with `useTouch = true` (the Android setting baked into the scenes) it only
// runs `ProcessTouches()`, so on-screen buttons ignore the mouse.
//
// This component does NOT patch any game logic. It feeds the same bus the
// on-screen controls feed:
//
//   InputJoystick   -> InputManager.SetAxis("Horizontal" / "Vertical", v)
//   InputTouchLook  -> InputManager.SetAxis("Mouse X" / "Mouse Y", v)
//   InputButton     -> InputManager.SetButtonDown/Up(name)
//
// and flips `UICamera.useTouch/useMouse` at runtime so NGUI reacts to the mouse.
// Nothing is saved: scenes and prefabs stay untouched.
//
// Controls
//   WASD / arrows ....... move            Space ...... Jump
//   mouse ............... look            LMB ........ Fire
//   RMB ................. Aim             R .......... Reload
//   E ................... Use             Q .......... SelectWeapon
//   Tab ................. Statistics      T .......... Chat
//   P ................... Pause           V .......... Microphone
//   L ................... lock/unlock the cursor (unlock to click the UI)

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public class BSPlaytestEditorInput : MonoBehaviour
{
    [Range(0.5f, 40f)]
    public float lookSensitivity = 8f;
    public bool cursorLocked = true;

    private struct Binding
    {
        public KeyCode key;
        public string button;
        public Binding(KeyCode k, string b) { key = k; button = b; }
    }

    private static readonly Binding[] Bindings =
    {
        new Binding(KeyCode.Space, "Jump"),
        new Binding(KeyCode.Mouse0, "Fire"),
        new Binding(KeyCode.Mouse1, "Aim"),
        new Binding(KeyCode.R, "Reload"),
        new Binding(KeyCode.E, "Use"),
        new Binding(KeyCode.Q, "SelectWeapon"),
        new Binding(KeyCode.Tab, "Statistics"),
        new Binding(KeyCode.T, "Chat"),
        new Binding(KeyCode.P, "Pause"),
        new Binding(KeyCode.V, "Microphone"),
        new Binding(KeyCode.C, "Crouch"),
        new Binding(KeyCode.LeftShift, "Run"),
    };

    private readonly HashSet<string> pressed = new HashSet<string>();
    private Vector2 lastAxis = Vector2.zero;
    private Vector2 lastLook = Vector2.zero;
    private float nextUICameraSweep;

    private void Start()
    {
        SweepUICameras();
        SetCursor(cursorLocked);
        Debug.Log("[BS Playtest] keyboard/mouse input is active: WASD = move, mouse = look, " +
                  "LMB = fire, RMB = aim, Space = jump, R = reload, E = use, Q = weapon, " +
                  "Tab = stats, T = chat, P = pause, L = release the cursor.");
    }

    private void OnDisable()
    {
        foreach (string button in pressed)
        {
            InputManager.SetButtonUp(button);
        }
        pressed.Clear();
        InputManager.SetAxis("Horizontal", 0f);
        InputManager.SetAxis("Vertical", 0f);
        InputManager.SetAxis("Mouse X", 0f);
        InputManager.SetAxis("Mouse Y", 0f);
        SetCursor(false);
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextUICameraSweep)
        {
            nextUICameraSweep = Time.unscaledTime + 1f;
            SweepUICameras();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            cursorLocked = !cursorLocked;
            SetCursor(cursorLocked);
        }
        if (Cursor.lockState != CursorLockMode.Locked && cursorLocked && Input.GetMouseButtonDown(0))
        {
            // the editor drops the lock when the game view loses focus
            SetCursor(true);
        }

        UpdateMoveAxes();
        UpdateLookAxes();
        UpdateButtons();
    }

    private void UpdateMoveAxes()
    {
        float x = 0f;
        float y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { x -= 1f; }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { x += 1f; }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { y -= 1f; }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { y += 1f; }

        Vector2 axis = new Vector2(x, y);
        if (axis.sqrMagnitude > 1f)
        {
            axis.Normalize();
        }
        // Only speak when something changed, so the on-screen joystick still works.
        if (axis != lastAxis)
        {
            InputManager.SetAxis("Horizontal", axis.x);
            InputManager.SetAxis("Vertical", axis.y);
            lastAxis = axis;
        }
    }

    private void UpdateLookAxes()
    {
        Vector2 look = Vector2.zero;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            look.x = Input.GetAxis("Mouse X") * lookSensitivity;
            look.y = Input.GetAxis("Mouse Y") * lookSensitivity;
        }
        if (look != Vector2.zero || lastLook != Vector2.zero)
        {
            InputManager.SetAxis("Mouse X", look.x);
            InputManager.SetAxis("Mouse Y", look.y);
            lastLook = look;
        }
    }

    private void UpdateButtons()
    {
        for (int i = 0; i < Bindings.Length; i++)
        {
            Binding binding = Bindings[i];
            if (Input.GetKeyDown(binding.key))
            {
                InputManager.SetButtonDown(binding.button);
                pressed.Add(binding.button);
            }
            else if (Input.GetKeyUp(binding.key))
            {
                InputManager.SetButtonUp(binding.button);
                pressed.Remove(binding.button);
            }
        }
    }

    /// <summary>NGUI only processes touches when useTouch is on; the scenes are
    /// built for Android, so the mouse is ignored. Flip it for play mode.</summary>
    private static void SweepUICameras()
    {
        UICamera[] cameras = UnityEngine.Object.FindObjectsOfType<UICamera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].useTouch || !cameras[i].useMouse)
            {
                cameras[i].useTouch = false;
                cameras[i].useMouse = true;
            }
        }
    }

    private static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
#endif
