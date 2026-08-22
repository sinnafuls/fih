using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ImGuiNET;

namespace fih.UI;

/// <summary>
/// Searchable spawn catalogue read from GameInfo's private static name-&gt;prefab dictionary,
/// so the list is exactly what DazedCommands' "/spawn" accepts.
/// </summary>
internal static class SpawnBrowser
{
    private static readonly FieldInfo NameToSpawnableField = AccessTools.Field(typeof(GameInfo), "_nameToSpawnable");

    private static readonly List<string> All = new List<string>();
    private static readonly List<string> Filtered = new List<string>();

    private static string _search = string.Empty;
    private static string _lastSearch = null;
    private static string _selected = string.Empty;
    private static string _status = string.Empty;

    /// <summary>Reads the catalogue once the game has populated it (GameInfo.Awake).</summary>
    private static void EnsureLoaded()
    {
        if (All.Count > 0) return;
        if (!(NameToSpawnableField?.GetValue(null) is IDictionary catalogue)) return;

        foreach (DictionaryEntry entry in catalogue)
            if (entry.Key is string name) All.Add(name);

        All.Sort(StringComparer.OrdinalIgnoreCase);
        _lastSearch = null;
        Plugin.Logger.LogInfo($"[imgui] spawn catalogue loaded: {All.Count} entries");
    }

    internal static void Draw()
    {
        EnsureLoaded();

        if (All.Count == 0)
        {
            ImGui.TextDisabled("Catalogue not populated yet - load into a world first.");
            return;
        }

        ImGui.SetNextItemWidth(-90f);
        ImGui.InputTextWithHint("##search", "search items...", ref _search, 64);
        ImGui.SameLine();
        if (ImGui.Button("Clear")) _search = string.Empty;

        // Refilter only when the query changes.
        if (_search != _lastSearch)
        {
            _lastSearch = _search;
            Filtered.Clear();
            foreach (var name in All)
                if (_search.Length == 0 || name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                    Filtered.Add(name);
        }

        ImGui.Text($"{Filtered.Count} / {All.Count} spawnables");

        var canSpawn = Cfg.EnableDevCommands.Value;
        if (!canSpawn) ImGui.TextDisabled("Enable dev commands (Commands tab) to spawn.");
        // Fixed height: a negative one collapses to nothing inside a collapsing header.
        if (ImGui.BeginChild("##items", new System.Numerics.Vector2(0f, 220f), ImGuiChildFlags.Borders))
        {
            foreach (var name in Filtered)
            {
                if (ImGui.Selectable(name, name == _selected)) _selected = name;
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0)) Spawn(name, 1);
            }
        }
        ImGui.EndChild();

        ImGui.BeginDisabled(!canSpawn || _selected.Length == 0);
        if (ImGui.Button("Spawn")) Spawn(_selected, 1);
        ImGui.SameLine();
        if (ImGui.Button("x5")) Spawn(_selected, 5);
        ImGui.SameLine();
        if (ImGui.Button("x10")) Spawn(_selected, 10);
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.TextDisabled(_selected.Length > 0 ? _selected : "nothing selected");

        if (_status.Length > 0) ImGui.TextUnformatted(_status);
    }

    private static void Spawn(string name, int count)
    {
        var handled = true;
        for (var i = 0; i < count && handled; i++) handled = DazedCommands.IsCommand($"/spawn {name}");

        _status = $"/spawn {name} x{count} -> {(handled ? "handled" : "rejected")}";
        Plugin.Logger.LogInfo($"[imgui] {_status}");
    }
}
