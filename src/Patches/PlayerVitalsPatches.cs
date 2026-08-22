using fih.Debugging;
using HarmonyLib;
using UnityEngine;

namespace fih.Patches;

/// <summary>Optional logging hooks on PlayerVitals.TakeDamage and Heal.</summary>
[HarmonyPatch(typeof(PlayerVitals))]
internal static class PlayerVitalsPatches
{
    /// <summary>Damage blocking lives in CheatPatches, which flips PlayerManager.InGodMode instead.</summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerVitals.TakeDamage))]
    private static void TakeDamagePrefix(PlayerVitals __instance, int amount, bool ignoreInvulnerability)
    {
        if (!Cfg.LogDamage.Value) return;

        var local = Player.LocalPlayer;
        var isLocal = local != null && local.Vitals == __instance;
        Plugin.Logger.LogInfo(
            $"TakeDamage amount={amount} local={isLocal} hp={__instance.Health} ignoreInvuln={ignoreInvulnerability}");
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerVitals.Heal))]
    private static void HealPostfix(PlayerVitals __instance, int amount) =>
        Plugin.Logger.LogInfo($"Heal amount={amount} -> hp={__instance.Health}/{GameState.MaxHealth}");
}
