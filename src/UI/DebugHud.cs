using System.Text;
using fih.Debugging;
using UnityEngine;
using UnityEngine.InputSystem;

namespace fih.UI;

/// <summary>
/// Hotkey state HUD, plus the log dumps the menu also calls.
/// </summary>
internal sealed class DebugHud : MonoBehaviour
{
    private const float RefreshSeconds = 0.1f;

    // OnGUI can run several times per frame, so the HUD string is composed in Update at
    // 10 Hz and OnGUI only draws the cached copy.
    private static readonly StringBuilder Buffer = new StringBuilder(1024);

    private Rect _window = new Rect(12f, 12f, 470f, 64f);
    private string _text = string.Empty;
    private bool _visible;
    private float _nextRefresh;
    private int _lines = 1;
    private GUIStyle _style;

    /// <summary>Formats the live state snapshot. Shared with the menu's Debug tab.</summary>
    internal static string Snapshot()
    {
        Buffer.Clear();
        GameState.Describe(Buffer);
        return Buffer.ToString();
    }

    internal static void LogState() => Plugin.Logger.LogInfo("state dump:\n" + Snapshot());

    /// <summary>Dumps the held item, or the player itself when empty-handed.</summary>
    internal static void LogTarget()
    {
        var player = Player.LocalPlayer;
        var held = player != null && player.Holding != null ? player.Holding.HeldItem : null;

        Buffer.Clear();
        GameState.DumpFields((object)held ?? player, Buffer);
        Plugin.Logger.LogInfo(Buffer.ToString());
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[Cfg.HudKey.Value].wasPressedThisFrame)
        {
            _visible = !_visible;
            if (_visible) Refresh();
            Plugin.Logger.LogInfo($"HUD {(_visible ? "shown" : "hidden")}");
        }

        if (keyboard[Cfg.DumpStateKey.Value].wasPressedThisFrame) LogState();
        if (keyboard[Cfg.DumpTargetKey.Value].wasPressedThisFrame) LogTarget();

        if (_visible && Time.unscaledTime >= _nextRefresh) Refresh();
    }

    private void OnGUI()
    {
        if (!_visible) return;

        _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false, richText = false };

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(_window, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(_window.x + 10f, _window.y + 8f, _window.width - 20f, _window.height - 16f), _text, _style);
    }

    private void Refresh()
    {
        _nextRefresh = Time.unscaledTime + RefreshSeconds;
        _text = Snapshot();

        // Sized here so GUIContent allocations stay out of OnGUI.
        _lines = 1;
        for (var i = 0; i < _text.Length; i++)
            if (_text[i] == '\n') _lines++;
        _window.height = _lines * 16f + 16f;
    }
}
