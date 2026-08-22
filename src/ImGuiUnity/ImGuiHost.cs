using System;
using System.IO;
using BepInEx;
using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace fih.ImGuiUnity;

/// <summary>
/// Owns the ImGui context and drives one ImGui frame per Unity frame; draw data stays
/// valid until the next NewFrame, so the renderer runs from a camera callback.
/// </summary>
internal sealed class ImGuiHost : MonoBehaviour
{
    internal static bool IsOpen { get; private set; }
    internal static ImGuiHost Instance { get; private set; }

    /// <summary>Set when ImGui wants the mouse, so game input can be suppressed.</summary>
    internal static bool WantsInput => IsOpen;

    private readonly ImGuiInput _input = new ImGuiInput();
    private ImGuiRenderer _renderer;
    private IntPtr _context;
    private bool _frameReady;
    private bool _failed;
    private string _iniPath;
    private float _appliedScale;
    private ImGuiStyle _defaultStyle;
    private bool _haveDefaultStyle;
    private static bool _focusRequested;
    private EventSystem _capturedEventSystem;

    private void Awake()
    {
        Instance = this;

        try
        {
            _context = ImGui.CreateContext();
            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            // We pass baseVertex per command instead of rebasing indices on the CPU.
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            ImGui.StyleColorsDark();
            ImGuiTheme.Apply();

            // ImGui would otherwise write imgui.ini next to the game exe.
            _iniPath = Path.Combine(Paths.ConfigPath, "fih.imgui.ini");
            unsafe { io.NativePtr->IniFilename = null; }
            if (File.Exists(_iniPath)) ImGui.LoadIniSettingsFromDisk(_iniPath);

            _renderer = new ImGuiRenderer();
            _renderer.CreateFontAtlas(io);

            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            Plugin.Logger.LogInfo($"[imgui] initialised, ImGui {ImGui.GetVersion()}");
        }
        catch (Exception ex)
        {
            _failed = true;
            Plugin.Logger.LogError($"[imgui] init failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void Update()
    {
        if (_failed) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[Cfg.MenuKey.Value].wasPressedThisFrame) Toggle();

        if (!IsOpen)
        {
            _frameReady = false;
            return;
        }

        // A scene load can spawn a fresh EventSystem, so keep asserting the capture.
        CaptureEventSystem();

        ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO();
        ApplyScale(io);
        _input.Update(io);

        ImGui.NewFrame();
        UI.ImGuiMenu.Draw();
        ImGui.Render();
        _frameReady = true;

        if (io.WantSaveIniSettings)
        {
            ImGui.SaveIniSettingsToDisk(_iniPath);
            io.WantSaveIniSettings = false;
        }
    }

    /// <summary>
    /// Rescales the style. ScaleAllSizes multiplies in place, so the style is rebuilt from
    /// a pristine snapshot each time.
    /// </summary>
    private unsafe void ApplyScale(ImGuiIOPtr io)
    {
        var style = ImGui.GetStyle();
        if (!_haveDefaultStyle)
        {
            _defaultStyle = *style.NativePtr;
            _haveDefaultStyle = true;
        }

        // Screen.height is not final during Awake, so scale is checked every frame.
        var scale = Mathf.Max(1f, Screen.height / 1080f) * Cfg.UiScale.Value;
        if (Mathf.Approximately(scale, _appliedScale)) return;

        *style.NativePtr = _defaultStyle;
        style.ScaleAllSizes(scale);
        io.FontGlobalScale = scale;
        _appliedScale = scale;
        Plugin.Logger.LogInfo($"[imgui] ui scale {scale:F2} for {Screen.width}x{Screen.height}");
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!_frameReady || camera.cameraType != CameraType.Game) return;
        if (Camera.main != null && camera != Camera.main) return;

        try
        {
            _renderer.Render(context, ImGui.GetDrawData());
        }
        catch (Exception ex)
        {
            _failed = true;
            Plugin.Logger.LogError($"[imgui] render failed, disabling: {ex}");
        }
    }

    /// <summary>Consumed once by the menu so the window is raised when it opens.</summary>
    internal static bool TakeFocusRequest()
    {
        var requested = _focusRequested;
        _focusRequested = false;
        return requested;
    }

    private void Toggle()
    {
        IsOpen = !IsOpen;

        if (IsOpen)
        {
            _input.Attach();
            _focusRequested = true;
            CaptureEventSystem();
        }
        else
        {
            _input.Detach();
            ReleaseEventSystem();
        }

        // ToggleMouse also clears PlayerCamera's _mouseLocked so the camera stops
        // consuming mouse deltas.
        PlayerCamera.ToggleMouse(IsOpen);
        Plugin.Logger.LogInfo($"[imgui] menu {(IsOpen ? "opened" : "closed")}");
    }

    /// <summary>
    /// Disables the EventSystem while the menu is open: Player.BlockInputs does not cover
    /// uGUI, so clicks would still land on the game's own menus.
    /// </summary>
    private void CaptureEventSystem()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null || !eventSystem.enabled) return;

        _capturedEventSystem = eventSystem;
        eventSystem.enabled = false;
    }

    private void ReleaseEventSystem()
    {
        if (_capturedEventSystem == null) return;

        _capturedEventSystem.enabled = true;
        _capturedEventSystem = null;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        _input.Detach();
        ReleaseEventSystem();
        _renderer?.Dispose();

        if (_context != IntPtr.Zero)
        {
            ImGui.DestroyContext(_context);
            _context = IntPtr.Zero;
        }

        IsOpen = false;
        Instance = null;
    }
}
