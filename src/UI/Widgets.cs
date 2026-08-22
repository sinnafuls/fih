using BepInEx.Configuration;
using fih.Cheats;
using ImGuiNET;
using UnityEngine.InputSystem;

namespace fih.UI;

/// <summary>
/// Reusable click-to-rebind pickers. Capture state is keyed by label so several pickers
/// can coexist without stealing each other's input.
/// </summary>
internal static class Widgets
{
    private static string _capturing;

    /// <summary>Click-to-rebind for a keyboard-only binding.</summary>
    internal static void KeyBind(string label, ConfigEntry<Key> entry)
    {
        var capturing = _capturing == label;

        ImGui.PushID(label);
        if (ImGui.Button(capturing ? "press a key..." : entry.Value.ToString(), new System.Numerics.Vector2(150f, 0f)))
            _capturing = capturing ? null : label;

        ImGui.SameLine();
        ImGui.TextUnformatted(label);

        if (capturing && TryReadKey(out var key))
        {
            // Escape cancels instead of binding an unusable key.
            if (key != Key.Escape) entry.Value = key;
            _capturing = null;
        }
        ImGui.PopID();
    }

    /// <summary>Click-to-rebind that accepts a mouse button or a key.</summary>
    internal static void AimBind(string label, ConfigEntry<AimButton> button, ConfigEntry<Key> key)
    {
        var capturing = _capturing == label;
        var current = button.Value == AimButton.Key ? key.Value.ToString() : button.Value.ToString();

        ImGui.PushID(label);
        if (ImGui.Button(capturing ? "press key or mouse..." : current, new System.Numerics.Vector2(180f, 0f)))
            _capturing = capturing ? null : label;

        ImGui.SameLine();
        ImGui.TextUnformatted(label);

        if (capturing)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) Commit(button, AimButton.RightMouse);
            else if (mouse != null && mouse.middleButton.wasPressedThisFrame) Commit(button, AimButton.MiddleMouse);
            // Left mouse also started the capture, so only accept it once that click is released.
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && !ImGui.IsItemActive())
                Commit(button, AimButton.LeftMouse);
            else if (TryReadKey(out var pressed))
            {
                if (pressed != Key.Escape)
                {
                    key.Value = pressed;
                    button.Value = AimButton.Key;
                }
                _capturing = null;
            }
        }
        ImGui.PopID();
    }

    private static void Commit(ConfigEntry<AimButton> entry, AimButton value)
    {
        entry.Value = value;
        _capturing = null;
    }

    private static bool TryReadKey(out Key key)
    {
        key = Key.None;

        var keyboard = Keyboard.current;
        if (keyboard == null) return false;

        foreach (var control in keyboard.allKeys)
        {
            if (!control.wasPressedThisFrame) continue;
            key = control.keyCode;
            return true;
        }

        return false;
    }
}
