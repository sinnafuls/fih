using HarmonyLib;
using UnityEngine;

namespace fih.Patches;

/// <summary>
/// Cheats that flip decisions the game already makes. The vitals SyncVars are written only
/// when IsServerInitialized, so these are authoritative on a host and cosmetic on a client.
/// </summary>
[HarmonyPatch]
internal static class CheatPatches
{
    /// <summary>
    /// PlayerVitals.TakeDamage gates on !PlayerManager.InGodMode, and PlayerMovement.JumpInput
    /// ORs InGodMode into its jump condition, so this also unlocks mid-air jumping.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.InGodMode), MethodType.Getter)]
    private static void GodMode(ref bool __result) => __result |= Cfg.GodMode.Value;

    /// <summary>Unlocks the game's own dev console: "/spawn", "/addmoney", "/killboss", ...</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClientSettings), nameof(ClientSettings.CheatsEnabled), MethodType.Getter)]
    private static void DevCommands(ref bool __result) => __result |= Cfg.EnableDevCommands.Value;

    /// <summary>The single funnel for hunger: LowerFullnessTick and Regenerate both route here.</summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerVitals), "LowerFullness")]
    private static bool NoHunger() => !Cfg.NoHunger.Value;

    /// <summary>Pufferfish is the only caller.</summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerVitals), nameof(PlayerVitals.ApplyNewPoison))]
    private static bool NoPoison() => !Cfg.NoPoison.Value;

    /// <summary>Called by BowheadWhale and MainLava only.</summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerVitals), nameof(PlayerVitals.ApplyNewFire))]
    private static bool NoFire() => !Cfg.NoFire.Value;

    /// <summary>
    /// Runs after the game's own walk/sprint/crouch lerp writes _curMoveSpeed, so the multiplier
    /// survives every transition. A triple-underscore prefix injects the private field by reference.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerMovement), "UpdateMoveSpeed")]
    private static void Speed(PlayerMovement __instance, ref float ____curMoveSpeed)
    {
        var multiplier = Cfg.SpeedMultiplier.Value;
        if (multiplier != 1f && IsLocal(__instance)) ____curMoveSpeed *= multiplier;
    }

    /// <summary>Scaling the velocity Jump() produced, rather than _jumpForce, leaves swim jumps intact.</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.Jump))]
    private static void Jump(PlayerMovement __instance, Rigidbody ____rig)
    {
        var multiplier = Cfg.JumpMultiplier.Value;
        if (multiplier == 1f || ____rig == null || !IsLocal(__instance)) return;

        var velocity = ____rig.linearVelocity;
        velocity.y *= multiplier;
        ____rig.linearVelocity = velocity;
    }

    /// <summary>Patches run for every player on a host, so scope movement cheats to us.</summary>
    private static bool IsLocal(PlayerMovement movement)
    {
        var local = Player.LocalPlayer;
        return local != null && local.Movement == movement;
    }
}
