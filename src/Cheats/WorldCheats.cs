using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace fih.Cheats;

/// <summary>
/// World-level actions: creatures, boat, islands, reset. All of these write server state,
/// which works because we are the FishNet host.
/// </summary>
internal static class WorldCheats
{
    private static readonly List<string> IslandNames = new List<string>();

    /// <summary>
    /// The game's own "/killallcreatures" only marks journal entries, so damage each
    /// spawned instance instead.
    /// </summary>
    internal static int KillAllCreatures()
    {
        var player = Player.LocalPlayer;
        if (player == null) return 0;

        var killed = 0;
        foreach (var creature in Object.FindObjectsByType<Creature>())
        {
            if (!TargetFinder.IsAlive(creature)) continue;

            creature.LocalHit(creature.transform, creature.transform.position,
                Vector3.down, player, 999999, false, Vector3.zero);
            killed++;
        }

        Plugin.Logger.LogInfo($"[cheats] killed {killed} creatures");
        return killed;
    }

    internal static bool KillBoss()
    {
        var boss = BossManager.Boss;
        var player = Player.LocalPlayer;
        if (boss == null || player == null) return false;

        boss.LocalHit(boss.transform, boss.transform.position, Vector3.down, player, 999999, false, Vector3.zero);
        Plugin.Logger.LogInfo("[cheats] boss hit for 999999");
        return true;
    }

    /// <summary>LocalTeleport is the right entry point: it also flags the position sync.</summary>
    internal static bool TeleportToNearestCreature()
    {
        var player = Player.LocalPlayer;
        var creature = TargetFinder.FindNearest(out var position);
        if (player == null || creature == null) return false;

        player.LocalTeleport(position + Vector3.up * 2f, player.Transform.eulerAngles.y);
        Plugin.Logger.LogInfo($"[cheats] teleported to {creature.name}");
        return true;
    }

    internal static void UnlockBoat()
    {
        var boat = BoatManager.Boat;
        if (boat == null) return;

        boat.UnlockBoat();
        boat.UnlockBoatRadar();

        // SetMotor only ever increases, and the list length is the ceiling.
        var motors = AccessTools.Field(typeof(Boat), "_motors").GetValue(boat) as System.Collections.ICollection;
        if (motors != null && motors.Count > 0) boat._motorIndex.Value = (byte)(motors.Count - 1);

        Plugin.Logger.LogInfo($"[cheats] boat unlocked, radar unlocked, motor {boat.MotorIndex}");
    }

    internal static void UnlockAllIslands()
    {
        var manager = OnlineIslandManager.Instance;
        if (manager == null || IslandManager.TotalIslands <= 0) return;

        manager.UnlockIsland((byte)(IslandManager.TotalIslands - 1));
        Plugin.Logger.LogInfo($"[cheats] islands unlocked up to {OnlineIslandManager.MaxIslandUnlocked}");
    }

    internal static void TeleportToIsland(byte index)
    {
        OnlineIslandManager.TpToSpecificIsland(index);
        Plugin.Logger.LogInfo($"[cheats] teleporting to island {index}");
    }

    internal static void NextIsland(bool previous) => OnlineIslandManager.TpToNextIsland(previous);

    /// <summary>Island n is build scene n+1; scene 0 is the persistent one.</summary>
    internal static List<string> GetIslandNames()
    {
        if (IslandNames.Count > 0) return IslandNames;

        for (var i = 0; i < IslandManager.TotalIslands; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i + 1);
            IslandNames.Add(string.IsNullOrEmpty(path) ? $"island {i}" : Path.GetFileNameWithoutExtension(path));
        }

        return IslandNames;
    }

    /// <summary>ServerResetVitals leaves fire untouched, so clear that too.</summary>
    internal static void ResetVitals()
    {
        var vitals = Player.LocalPlayer?.Vitals;
        if (vitals == null) return;

        vitals.ServerResetVitals();
        vitals._syncedFire.Value = 0;
        Plugin.Logger.LogInfo("[cheats] vitals reset");
    }
}
