using System.Numerics;
using BepInEx.Configuration;
using fih.Cheats;
using fih.Debugging;
using ImGuiNET;

namespace fih.UI;

/// <summary>
/// Menu contents. Immediate mode: widgets read and write ConfigEntry values directly, so
/// there is nothing to synchronise.
/// </summary>
internal static class ImGuiMenu
{
    private static int _islandIndex;
    private static string _status = string.Empty;

    internal static void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(470f, 560f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(70f, 70f), ImGuiCond.FirstUseEver);
        if (ImGuiUnity.ImGuiHost.TakeFocusRequest()) ImGui.SetNextWindowFocus();

        if (ImGui.Begin($"fih {MyPluginInfo.PLUGIN_VERSION}"))
        {
            if (ImGui.BeginTabBar("tabs"))
            {
                Tab("Aim", DrawAim);
                Tab("Weapon", DrawWeapon);
                Tab("Player", DrawPlayer);
                Tab("Fun", DrawFun);
                Tab("Misc", DrawMisc);
                ImGui.EndTabBar();
            }

            if (_status.Length > 0)
            {
                ImGui.Separator();
                ImGui.TextUnformatted(_status);
            }
        }
        ImGui.End();
    }

    private static void Tab(string label, System.Action body)
    {
        if (!ImGui.BeginTabItem(label)) return;
        body();
        ImGui.EndTabItem();
    }

    // ---- Aim --------------------------------------------------------------------

    private static void DrawAim()
    {
        Checkbox("Aim assist", Cfg.AimEnabled, "Steers the camera while the aim button is held.");

        ImGui.SeparatorText("Input");
        Widgets.AimBind("aim bind", Cfg.AimButton, Cfg.AimKey);
        var always = Cfg.AimButton.Value == AimButton.Always;
        if (ImGui.Checkbox("Always on (no button)", ref always))
            Cfg.AimButton.Value = always ? AimButton.Always : AimButton.RightMouse;

        ImGui.SeparatorText("Behaviour");
        Checkbox("Snap (instant)", Cfg.AimSnap, "No smoothing: locks straight onto the target.");
        ImGui.BeginDisabled(Cfg.AimSnap.Value);
        Slider("Smoothing", Cfg.AimSmoothing, "%.1f");
        ImGui.EndDisabled();
        Slider("Max distance", Cfg.AimMaxDistance, "%.0f m");
        Slider("Max angle", Cfg.AimMaxAngle, "%.0f deg");

        ImGui.SeparatorText("Target filters");
        Checkbox("Drip (shiny) only", Cfg.AimDripOnly, "Creature.IsDrip - the game renamed shiny to drip.");
        Checkbox("Bosses only", Cfg.AimBossOnly, "Creature.BossType != None, so minibosses count too.");

        ImGui.SeparatorText("Current target");
        var target = AimAssist.CurrentTarget;
        if (target == null) ImGui.TextDisabled("none");
        else ImGui.TextUnformatted($"{target.name}  drip={target.IsDrip}  boss={target.BossType}  hp={target.Hp}");
    }

    // ---- Weapon -----------------------------------------------------------------

    private static void DrawWeapon()
    {
        ImGui.SeparatorText("Projectiles");
        Checkbox("Magic bullet (silent aim)", Cfg.SilentAim,
            "Rewrites projectile velocity at spawn, including the vectors sent to other clients. "
            + "Uses the Aim tab's filters and range.");
        Checkbox("No spread", Cfg.NoSpread, "Zeroes Weapon._spread for each shot.");
        Checkbox("One shot kill", Cfg.OneShotKill, "Forces the game's ServerSettings.OneShotEnabled (99999 damage).");

        ImGui.SeparatorText("Handling");
        Checkbox("No recoil", Cfg.NoRecoil, "Camera kick, tool kick and model recoil.");
        Checkbox("Always ADS", Cfg.AlwaysAds,
            "Sniper scope overlay stays up and sprint is blocked while a weapon is held.");

        ImGui.SeparatorText("Rate of fire");
        Checkbox("Rapid fire / full auto", Cfg.RapidFire, null);
        ImGui.BeginDisabled(!Cfg.RapidFire.Value);
        Slider("Shot interval", Cfg.FireInterval, "%.3f s");
        ImGui.EndDisabled();
        Checkbox("Infinite ammo (auto refill)", Cfg.InfiniteAmmo, "Tops the magazine up after every shot.");
    }

    // ---- Player -----------------------------------------------------------------

    private static void DrawPlayer()
    {
        ImGui.SeparatorText("Vitals");
        Checkbox("God mode", Cfg.GodMode, $"{Cfg.GodModeKey.Value}. Also unlocks mid-air jumping.");
        Checkbox("No hunger", Cfg.NoHunger, null);
        Checkbox("No poison", Cfg.NoPoison, null);
        Checkbox("No fire", Cfg.NoFire, null);

        ImGui.SeparatorText("Movement");
        Checkbox("Fly", Cfg.Fly, $"{Cfg.FlyKey.Value}. Space up, Left Ctrl down.");
        Slider("Fly speed", Cfg.FlySpeed, "%.0f m/s");
        Slider("Speed", Cfg.SpeedMultiplier, "%.1fx");
        Slider("Jump", Cfg.JumpMultiplier, "%.1fx");

        ImGui.SeparatorText("Money");
        ImGui.TextUnformatted($"balance ${MoneyManager.Money}");
        var amount = Cfg.MoneyAmount.Value;
        ImGui.SetNextItemWidth(160f);
        if (ImGui.InputInt("amount", ref amount)) Cfg.MoneyAmount.Value = UnityEngine.Mathf.Clamp(amount, 1, 1000000);
        ImGui.SameLine();
        if (ImGui.Button("Add money")) EconomyCheats.AddMoney(Cfg.MoneyAmount.Value);
        Checkbox("Free purchases", Cfg.FreePurchases,
            "CanAfford always true and RemoveMoney skipped, so shops, upgrades and pockets are free.");

        if (ImGui.CollapsingHeader("Item spawner")) SpawnBrowser.Draw();
    }

    // ---- Fun / exploits ---------------------------------------------------------

    private static void DrawFun()
    {
        ImGui.SeparatorText("Creatures");
        if (ImGui.Button("Kill all creatures")) Status($"killed {WorldCheats.KillAllCreatures()} creatures");
        ImGui.SameLine();
        if (ImGui.Button("Kill boss")) Status(WorldCheats.KillBoss() ? "boss hit" : "no boss active");
        if (ImGui.Button("Teleport to nearest creature"))
            Status(WorldCheats.TeleportToNearestCreature() ? "teleported" : "no creature found");
        ImGui.TextDisabled("The game's /killallcreatures only marks journal entries, so this damages instances instead.");

        ImGui.SeparatorText("Inventory");
        if (ImGui.Button("Unlock all pockets")) { EconomyCheats.UnlockAllSlots(); Status("inventory slots unlocked"); }
        ImGui.SameLine();
        if (ImGui.Button($"Fill baits ({Cfg.BaitAmount.Value})"))
        {
            EconomyCheats.FillBaits(Cfg.BaitAmount.Value);
            Status($"every bait set to {Cfg.BaitAmount.Value}");
        }

        ImGui.SeparatorText("Boat");
        Slider("Boat speed", Cfg.BoatSpeed, "%.1fx");
        ImGui.BeginDisabled(BoatManager.Boat == null);
        if (ImGui.Button("Unlock boat + radar + max motor")) { WorldCheats.UnlockBoat(); Status("boat fully unlocked"); }
        ImGui.EndDisabled();
        if (BoatManager.Boat == null) ImGui.TextDisabled("no boat in this scene");

        ImGui.SeparatorText("Islands");
        // These statics dereference their singleton, which does not exist in the menu scene.
        if (OnlineIslandManager.Instance == null)
        {
            ImGui.TextDisabled("island manager not loaded - host or join a game first");
        }
        else
        {
            ImGui.TextUnformatted($"current {OnlineIslandManager.CurIsland}, unlocked up to {OnlineIslandManager.MaxIslandUnlocked}");
            if (ImGui.Button("Unlock all islands")) { WorldCheats.UnlockAllIslands(); Status("islands unlocked"); }

            var names = WorldCheats.GetIslandNames();
            if (names.Count > 0)
            {
                if (_islandIndex >= names.Count) _islandIndex = 0;
                ImGui.SetNextItemWidth(220f);
                if (ImGui.BeginCombo("island", names[_islandIndex]))
                {
                    for (var i = 0; i < names.Count; i++)
                        if (ImGui.Selectable(names[i], i == _islandIndex)) _islandIndex = i;
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                if (ImGui.Button("Go")) { WorldCheats.TeleportToIsland((byte)_islandIndex); Status($"island {_islandIndex}"); }
            }

            if (ImGui.Button("Prev island")) WorldCheats.NextIsland(true);
            ImGui.SameLine();
            if (ImGui.Button("Next island")) WorldCheats.NextIsland(false);
        }

        ImGui.SeparatorText("Steam");
        if (ImGui.Button("Unlock all achievements")) { EconomyCheats.UnlockAchievements(); Status("achievements unlocked"); }
        ImGui.SameLine();
        if (ImGui.Button("Clear achievements")) { EconomyCheats.LockAchievements(); Status("achievements cleared"); }
    }

    // ---- Misc -------------------------------------------------------------------

    private static void DrawMisc()
    {
        ImGui.SeparatorText("Dev commands");
        Checkbox("Force ClientSettings.CheatsEnabled", Cfg.EnableDevCommands,
            "Unlocks the game's own /commands and its dev keybinds. Host only.");
        ImGui.BeginDisabled(!Cfg.EnableDevCommands.Value);
        if (ImGui.Button("/finishgame")) Run("/finishgame");
        ImGui.SameLine();
        if (ImGui.Button("/allskins")) Run("/allskins");
        ImGui.SameLine();
        if (ImGui.Button("/slots")) Run("/slots");
        ImGui.EndDisabled();

        ImGui.SeparatorText("Player info");
        ImGui.TextUnformatted(DebugHud.Snapshot());

        ImGui.SeparatorText("Dumps (BepInEx/LogOutput.log)");
        if (ImGui.Button("Dump state")) DebugHud.LogState();
        ImGui.SameLine();
        if (ImGui.Button("Dump held item / player")) DebugHud.LogTarget();
        Checkbox("Log every TakeDamage", Cfg.LogDamage, null);

        ImGui.SeparatorText("Reset");
        if (ImGui.Button("Reset vitals")) { WorldCheats.ResetVitals(); Status("vitals reset"); }
        ImGui.SameLine();
        if (ImGui.Button("Reset all toggles")) ResetAll();

        ImGui.SeparatorText("Keybinds");
        ImGui.TextDisabled("click a bind, then press the key you want (Escape cancels)");
        Widgets.KeyBind("menu", Cfg.MenuKey);
        Widgets.KeyBind("god mode", Cfg.GodModeKey);
        Widgets.KeyBind("fly", Cfg.FlyKey);
        Widgets.KeyBind("state HUD", Cfg.HudKey);
        Widgets.KeyBind("dump state", Cfg.DumpStateKey);
        Widgets.KeyBind("dump target", Cfg.DumpTargetKey);

        ImGui.SeparatorText("Interface");
        Slider("UI scale", Cfg.UiScale, "%.2fx");
        ImGui.SameLine();
        if (ImGui.SmallButton("reset")) Cfg.UiScale.Value = 1f;
        ImGui.TextDisabled($"{ImGui.GetIO().Framerate:F0} FPS   ImGui {ImGui.GetVersion()}");
    }

    private static void ResetAll()
    {
        Cfg.AimEnabled.Value = false;
        Cfg.SilentAim.Value = false;
        Cfg.NoSpread.Value = false;
        Cfg.NoRecoil.Value = false;
        Cfg.AlwaysAds.Value = false;
        Cfg.RapidFire.Value = false;
        Cfg.InfiniteAmmo.Value = false;
        Cfg.OneShotKill.Value = false;
        Cfg.GodMode.Value = false;
        Cfg.NoHunger.Value = false;
        Cfg.NoPoison.Value = false;
        Cfg.NoFire.Value = false;
        Cfg.Fly.Value = false;
        Cfg.FreePurchases.Value = false;
        Cfg.SpeedMultiplier.Value = 1f;
        Cfg.JumpMultiplier.Value = 1f;
        Cfg.BoatSpeed.Value = 1f;
        Status("all toggles reset");
    }

    private static void Run(string command)
    {
        var handled = DazedCommands.IsCommand(command);
        Status($"{command} -> {(handled ? "handled" : "rejected")}");
    }

    private static void Status(string message)
    {
        _status = message;
        Plugin.Logger.LogInfo($"[imgui] {message}");
    }

    // ---- widgets ----------------------------------------------------------------

    private static void Checkbox(string label, ConfigEntry<bool> entry, string tooltip)
    {
        var value = entry.Value;
        if (ImGui.Checkbox(label, ref value)) entry.Value = value;
        if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
    }

    private static void Slider(string label, ConfigEntry<float> entry, string format)
    {
        var range = (AcceptableValueRange<float>)entry.Description.AcceptableValues;
        var value = entry.Value;
        if (ImGui.SliderFloat(label, ref value, range.MinValue, range.MaxValue, format)) entry.Value = value;
    }

}
