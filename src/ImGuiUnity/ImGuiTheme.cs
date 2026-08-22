using System.Numerics;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using ImGuiNET;

namespace fih.ImGuiUnity;

/// <summary>Obsidian purple palette and window geometry, applied once over StyleColorsDark.</summary>
internal static class ImGuiTheme
{
    private static Vector4 Rgb(int hex, float alpha = 1f) => new Vector4(
        ((hex >> 16) & 0xFF) / 255f,
        ((hex >> 8) & 0xFF) / 255f,
        (hex & 0xFF) / 255f,
        alpha);

    internal static void Apply()
    {
        var style = ImGui.GetStyle();

        style.WindowRounding = 6f;
        style.ChildRounding = 4f;
        style.FrameRounding = 4f;
        style.PopupRounding = 4f;
        style.ScrollbarRounding = 4f;
        style.GrabRounding = 4f;
        style.TabRounding = 4f;
        style.WindowBorderSize = 1f;
        style.FrameBorderSize = 1f;
        style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
        style.WindowPadding = new Vector2(10f, 8f);
        style.FramePadding = new Vector2(8f, 4f);
        style.ItemSpacing = new Vector2(8f, 5f);
        style.ScrollbarSize = 12f;

        var obsidian = Rgb(0x14101A);
        var obsidianLight = Rgb(0x1D1726);
        var border = Rgb(0x2E2440);
        var violet = Rgb(0x7C4DDB);
        var violetDim = Rgb(0x4A3170);
        var violetBright = Rgb(0x9A6BFF);
        var text = Rgb(0xE7E2F0);

        var colors = style.Colors;
        colors[(int)ImGuiCol.Text] = text;
        colors[(int)ImGuiCol.TextDisabled] = Rgb(0x7A7290);
        colors[(int)ImGuiCol.WindowBg] = Rgb(0x14101A, 0.97f);
        colors[(int)ImGuiCol.ChildBg] = Rgb(0x18131F, 0.60f);
        colors[(int)ImGuiCol.PopupBg] = Rgb(0x18131F, 0.98f);
        colors[(int)ImGuiCol.Border] = border;
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f);

        colors[(int)ImGuiCol.FrameBg] = obsidianLight;
        colors[(int)ImGuiCol.FrameBgHovered] = violetDim;
        colors[(int)ImGuiCol.FrameBgActive] = violet;

        colors[(int)ImGuiCol.TitleBg] = Rgb(0x1B1526);
        colors[(int)ImGuiCol.TitleBgActive] = violetDim;
        colors[(int)ImGuiCol.TitleBgCollapsed] = obsidian;
        colors[(int)ImGuiCol.MenuBarBg] = Rgb(0x1B1526);

        colors[(int)ImGuiCol.ScrollbarBg] = Rgb(0x120E18, 0.60f);
        colors[(int)ImGuiCol.ScrollbarGrab] = violetDim;
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = violet;
        colors[(int)ImGuiCol.ScrollbarGrabActive] = violetBright;

        colors[(int)ImGuiCol.CheckMark] = violetBright;
        colors[(int)ImGuiCol.SliderGrab] = violet;
        colors[(int)ImGuiCol.SliderGrabActive] = violetBright;

        colors[(int)ImGuiCol.Button] = violetDim;
        colors[(int)ImGuiCol.ButtonHovered] = violet;
        colors[(int)ImGuiCol.ButtonActive] = violetBright;

        colors[(int)ImGuiCol.Header] = Rgb(0x2A1F3D);
        colors[(int)ImGuiCol.HeaderHovered] = violetDim;
        colors[(int)ImGuiCol.HeaderActive] = violet;

        colors[(int)ImGuiCol.Separator] = border;
        colors[(int)ImGuiCol.SeparatorHovered] = violet;
        colors[(int)ImGuiCol.SeparatorActive] = violetBright;

        colors[(int)ImGuiCol.ResizeGrip] = violetDim;
        colors[(int)ImGuiCol.ResizeGripHovered] = violet;
        colors[(int)ImGuiCol.ResizeGripActive] = violetBright;

        colors[(int)ImGuiCol.Tab] = Rgb(0x201829);
        colors[(int)ImGuiCol.TabHovered] = violet;
        colors[(int)ImGuiCol.TabSelected] = violetDim;
        colors[(int)ImGuiCol.TabDimmed] = Rgb(0x1A1422);
        colors[(int)ImGuiCol.TabDimmedSelected] = Rgb(0x2A1F3D);

        colors[(int)ImGuiCol.TextSelectedBg] = Rgb(0x7C4DDB, 0.35f);
        colors[(int)ImGuiCol.NavCursor] = violetBright;
        colors[(int)ImGuiCol.PlotHistogram] = violet;
        colors[(int)ImGuiCol.PlotHistogramHovered] = violetBright;

        ConvertToRenderColorSpace(colors);
    }

    /// <summary>
    /// URP consumes vertex colours as linear, so the sRGB palette is converted once here
    /// rather than per vertex.
    /// </summary>
    private static void ConvertToRenderColorSpace(RangeAccessor<Vector4> colors)
    {
        if (QualitySettings.activeColorSpace != ColorSpace.Linear) return;

        for (var i = 0; i < colors.Count; i++)
        {
            var c = colors[i];
            colors[i] = new Vector4(
                Mathf.GammaToLinearSpace(c.X),
                Mathf.GammaToLinearSpace(c.Y),
                Mathf.GammaToLinearSpace(c.Z),
                c.W); // alpha is already linear
        }
    }
}
