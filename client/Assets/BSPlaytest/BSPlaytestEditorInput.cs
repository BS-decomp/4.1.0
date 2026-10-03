// BS-decomp / Block Strike 4.1.0 — keyboard + mouse for the editor (editor-only).
//
// This is part of the playtest TOOL, not of the game. No game script is
// touched: it only pushes values into the same bus the on-screen controls use
//
//   InputJoystick   -> InputManager.SetAxis("Horizontal" / "Vertical", v)
//   InputTouchLook  -> InputManager.SetAxis("Mouse X" / "Mouse Y", v)
//   InputButton     -> InputManager.SetButtonDown/Up(name)
//
// and flips `UICamera.useTouch/useMouse` while play mode runs, because the
// scenes are authored for Android and NGUI's touch branch ignores the mouse.
//
// Mouse capture
//   Left Alt toggles it. Captured  = you control the player, the badge in the
//   bottom-left corner is solid white. Released = the cursor is free for the
//   UI, the badge is dimmed and NOTHING is forwarded to the game (no movement,
//   no look, no shooting, no weapon switching).
//   Capture is also released automatically while you type in a chat field,
//   while the game is paused (timeScale 0) and when the Game view loses focus;
//   it comes back by itself afterwards.
//
// Keys
//   WASD / arrows  move          mouse  look           Left Alt  capture
//   1 rifle  2 pistol  3 knife   5 bomb (Use)          Space  jump
//   LMB fire   RMB aim   R reload   E use   Q next weapon
//   Tab stats  T chat   P pause   V mic   C crouch   Shift run

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public class BSPlaytestEditorInput : MonoBehaviour
{
    [Range(0.5f, 40f)]
    public float lookSensitivity = 8f;
    public KeyCode captureKey = KeyCode.LeftAlt;
    public bool captureOnStart = true;

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
        new Binding(KeyCode.Alpha5, "Use"),      // bomb: 4.1.0 plants it with "Use"
        new Binding(KeyCode.Q, "SelectWeapon"),
        new Binding(KeyCode.Tab, "Statistics"),
        new Binding(KeyCode.T, "Chat"),
        new Binding(KeyCode.P, "Pause"),
        new Binding(KeyCode.V, "Microphone"),
        new Binding(KeyCode.C, "Crouch"),
        new Binding(KeyCode.LeftShift, "Run"),
    };

    private readonly HashSet<string> pressed = new HashSet<string>();
    private bool userWantsCapture;
    private bool captured;
    private Vector2 lastAxis = Vector2.zero;
    private Vector2 lastLook = Vector2.zero;
    private float nextSweep;
    private Texture2D cursorIcon;
    private GUIStyle labelStyle;
    private bool bombHintShown;

    private void Start()
    {
        userWantsCapture = captureOnStart;
        SweepUICameras();
        Debug.Log("[BS Playtest] PC controls: Left Alt captures/releases the mouse. " +
                  "WASD move, mouse look, LMB fire, RMB aim, Space jump, 1/2/3 rifle/pistol/knife, " +
                  "5 bomb (Use), R reload, E use, Q next weapon, Tab stats, T chat, P pause.");
    }

    private void OnDisable()
    {
        ReleaseEverything();
        SetCursor(false);
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextSweep)
        {
            nextSweep = Time.unscaledTime + 1f;
            SweepUICameras();   // new scenes bring their own UICamera
        }

        if (Input.GetKeyDown(captureKey))
        {
            userWantsCapture = !userWantsCapture;
        }

        bool typing = IsTyping();
        bool paused = Time.timeScale == 0f;
        bool wanted = userWantsCapture && !typing && !paused && Application.isFocused;

        if (wanted != captured)
        {
            captured = wanted;
            SetCursor(captured);
            if (!captured)
            {
                ReleaseEverything();
            }
        }
        else if (captured && Cursor.lockState != CursorLockMode.Locked)
        {
            // Unity drops the lock on scene loads and focus changes.
            SetCursor(true);
        }

        if (!captured)
        {
            return;
        }

        UpdateMoveAxes();
        UpdateLookAxes();
        UpdateButtons();
        UpdateWeaponHotkeys();
    }

    // ------------------------------------------------------------------ //

    private static bool IsTyping()
    {
        try
        {
            if (UIInput.selection != null || UIInput.current != null)
            {
                return true;
            }
        }
        catch { }
        return false;
    }

    private void ReleaseEverything()
    {
        foreach (string button in pressed)
        {
            try { InputManager.SetButtonUp(button); } catch { }
        }
        pressed.Clear();
        if (lastAxis != Vector2.zero)
        {
            InputManager.SetAxis("Horizontal", 0f);
            InputManager.SetAxis("Vertical", 0f);
            lastAxis = Vector2.zero;
        }
        if (lastLook != Vector2.zero)
        {
            InputManager.SetAxis("Mouse X", 0f);
            InputManager.SetAxis("Mouse Y", 0f);
            lastLook = Vector2.zero;
        }
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
        if (axis != lastAxis)
        {
            InputManager.SetAxis("Horizontal", axis.x);
            InputManager.SetAxis("Vertical", axis.y);
            lastAxis = axis;
        }
    }

    private void UpdateLookAxes()
    {
        Vector2 look = new Vector2(
            Input.GetAxis("Mouse X") * lookSensitivity,
            Input.GetAxis("Mouse Y") * lookSensitivity);
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
                if (binding.key == KeyCode.Alpha5 && !bombHintShown)
                {
                    bombHintShown = true;
                    Debug.Log("[BS Playtest] 4.1.0 has no bomb weapon slot — the bomb is planted with " +
                              "the \"Use\" action, which is what 5 (and E) send.");
                }
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

    /// <summary>1 / 2 / 3 pick a weapon directly, like a desktop shooter.</summary>
    private void UpdateWeaponHotkeys()
    {
        WeaponType? want = null;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { want = WeaponType.Rifle; }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) { want = WeaponType.Pistol; }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) { want = WeaponType.Knife; }
        if (want == null)
        {
            return;
        }
        try
        {
            ControllerManager controller = GameManager.GetController();
            if (controller != null && controller.PlayerInput != null && controller.PlayerInput.PlayerWeapon != null)
            {
                controller.PlayerInput.PlayerWeapon.SetWeapon(want.Value);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] weapon hotkey failed: " + e.Message);
        }
    }

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

    // ------------------------------------------------------------------ //
    // the badge in the bottom-left corner
    // ------------------------------------------------------------------ //

    private void OnGUI()
    {
        if (cursorIcon == null)
        {
            cursorIcon = BuildCursorIcon();
        }
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleLeft;
        }

        float alpha = captured ? 1f : 0.45f;
        const float width = 104f;
        const float height = 24f;
        // bottom-left, one line above the health label so it is not covered
        Rect box = new Rect(12f, Screen.height - height - 86f, width, height);

        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(box.x + 6f, box.y + 4f, 11f, 16f), cursorIcon);
        labelStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(box.x + 23f, box.y, box.width - 25f, box.height), "Left ALT", labelStyle);
        GUI.color = old;
    }

    /// <summary>Small arrow-cursor glyph, built in code so the tool stays
    /// self-contained (no textures added to the project).</summary>
    private static Texture2D BuildCursorIcon()
    {
        string[] mask =
        {
            "1..........",
            "11.........",
            "121........",
            "1221.......",
            "12221......",
            "122221.....",
            "1222221....",
            "12222221...",
            "122222221..",
            "1222222221.",
            "122222111..",
            "12221221...",
            "1221.1221..",
            "121...1221.",
            "11.....1221",
            "1.......111",
        };
        Texture2D texture = new Texture2D(11, 16, TextureFormat.ARGB32, false);
        texture.hideFlags = HideFlags.DontSave;
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            string row = mask[15 - y];
            for (int x = 0; x < 11; x++)
            {
                char c = x < row.Length ? row[x] : '.';
                Color color = c == '2' ? Color.white
                    : c == '1' ? new Color(0f, 0f, 0f, 0.9f)
                    : new Color(0f, 0f, 0f, 0f);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }
}
#endif
