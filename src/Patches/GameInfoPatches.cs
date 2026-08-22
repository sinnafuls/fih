using fih.Debugging;

using HarmonyLib;

namespace fih.Patches;

/// <summary>
/// Logs the game's static registry counts from GameInfo.Awake, the earliest point at which
/// those registries are populated.
/// </summary>
[HarmonyPatch(typeof(GameInfo), "Awake")]
internal static class GameInfoPatches
{
    [HarmonyPostfix]
    private static void AwakePostfix() =>
        Plugin.Logger.LogInfo(
            $"GameInfo.Awake ran: spawnables={GameState.SpawnableCount} creatures={GameState.CreatureCount} tickMulti={GameInfo.TickMulti}");
}
