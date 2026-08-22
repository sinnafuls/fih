using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace fih.ImGuiUnity;

/// <summary>
/// Feeds ImGui's event queue from Unity's Input System; ImGui is event driven, so key,
/// char and mouse edges are pushed rather than states polled.
/// </summary>
internal sealed class ImGuiInput
{
    private static readonly Dictionary<Key, ImGuiKey> KeyMap = BuildKeyMap();

    private bool _subscribed;

    internal void Attach()
    {
        if (_subscribed || Keyboard.current == null) return;
        Keyboard.current.onTextInput += OnTextInput;
        _subscribed = true;
    }

    internal void Detach()
    {
        if (!_subscribed || Keyboard.current == null) return;
        Keyboard.current.onTextInput -= OnTextInput;
        _subscribed = false;
    }

    private static void OnTextInput(char character)
    {
        // Control characters would show up as glyphs in text fields.
        if (character >= ' ') ImGui.GetIO().AddInputCharacter(character);
    }

    internal void Update(ImGuiIOPtr io)
    {
        io.DisplaySize = new System.Numerics.Vector2(Screen.width, Screen.height);
        // Defaults to (0,0) and ImGui multiplies clip rects by it, so unset means every
        // scissor is empty and nothing draws.
        io.DisplayFramebufferScale = System.Numerics.Vector2.One;
        io.DeltaTime = Mathf.Max(Time.unscaledDeltaTime, 1f / 1000f);

        UpdateMouse(io);
        UpdateKeyboard(io);
    }

    private static void UpdateMouse(ImGuiIOPtr io)
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Unity's mouse origin is bottom-left, ImGui's is top-left.
        var position = mouse.position.ReadValue();
        io.AddMousePosEvent(position.x, Screen.height - position.y);

        io.AddMouseButtonEvent(0, mouse.leftButton.isPressed);
        io.AddMouseButtonEvent(1, mouse.rightButton.isPressed);
        io.AddMouseButtonEvent(2, mouse.middleButton.isPressed);

        var scroll = mouse.scroll.ReadValue();
        if (scroll != Vector2.zero) io.AddMouseWheelEvent(scroll.x / 120f, scroll.y / 120f);
    }

    private static void UpdateKeyboard(ImGuiIOPtr io)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboard.ctrlKey.isPressed);
        io.AddKeyEvent(ImGuiKey.ModShift, keyboard.shiftKey.isPressed);
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboard.altKey.isPressed);

        foreach (var control in keyboard.allKeys)
        {
            if (!control.wasPressedThisFrame && !control.wasReleasedThisFrame) continue;
            if (KeyMap.TryGetValue(control.keyCode, out var key))
                io.AddKeyEvent(key, control.wasPressedThisFrame);
        }
    }

    private static Dictionary<Key, ImGuiKey> BuildKeyMap()
    {
        var map = new Dictionary<Key, ImGuiKey>
        {
            [Key.Space] = ImGuiKey.Space,
            [Key.Enter] = ImGuiKey.Enter,
            [Key.NumpadEnter] = ImGuiKey.KeypadEnter,
            [Key.Tab] = ImGuiKey.Tab,
            [Key.Backspace] = ImGuiKey.Backspace,
            [Key.Delete] = ImGuiKey.Delete,
            [Key.Escape] = ImGuiKey.Escape,
            [Key.Insert] = ImGuiKey.Insert,
            [Key.Home] = ImGuiKey.Home,
            [Key.End] = ImGuiKey.End,
            [Key.PageUp] = ImGuiKey.PageUp,
            [Key.PageDown] = ImGuiKey.PageDown,
            [Key.LeftArrow] = ImGuiKey.LeftArrow,
            [Key.RightArrow] = ImGuiKey.RightArrow,
            [Key.UpArrow] = ImGuiKey.UpArrow,
            [Key.DownArrow] = ImGuiKey.DownArrow,
            [Key.LeftShift] = ImGuiKey.LeftShift,
            [Key.RightShift] = ImGuiKey.RightShift,
            [Key.LeftCtrl] = ImGuiKey.LeftCtrl,
            [Key.RightCtrl] = ImGuiKey.RightCtrl,
            [Key.LeftAlt] = ImGuiKey.LeftAlt,
            [Key.RightAlt] = ImGuiKey.RightAlt,
            [Key.Minus] = ImGuiKey.Minus,
            [Key.Equals] = ImGuiKey.Equal,
            [Key.Comma] = ImGuiKey.Comma,
            [Key.Period] = ImGuiKey.Period,
            [Key.Slash] = ImGuiKey.Slash,
            [Key.Backslash] = ImGuiKey.Backslash,
            [Key.Semicolon] = ImGuiKey.Semicolon,
            [Key.Quote] = ImGuiKey.Apostrophe,
            [Key.LeftBracket] = ImGuiKey.LeftBracket,
            [Key.RightBracket] = ImGuiKey.RightBracket,
            [Key.Backquote] = ImGuiKey.GraveAccent
        };

        for (var i = 0; i < 26; i++) map[Key.A + i] = ImGuiKey.A + i;
        for (var i = 0; i < 9; i++) map[Key.Digit1 + i] = ImGuiKey._1 + i;
        map[Key.Digit0] = ImGuiKey._0;
        for (var i = 0; i < 12; i++) map[Key.F1 + i] = ImGuiKey.F1 + i;

        return map;
    }
}
