using fih.Cheats;
using HarmonyLib;
using UnityEngine;

namespace fih.Patches;

/// <summary>
/// Weapon behaviour. Silent aim hooks ProjectileManager because the same velocity vectors are
/// copied into the ServerRpc writer, so remote clients see the redirected shot too.
/// </summary>
[HarmonyPatch]
internal static class WeaponPatches
{
    private static readonly AccessTools.FieldRef<Weapon, float> Spread =
        AccessTools.FieldRefAccess<Weapon, float>("_spread");
    private static readonly AccessTools.FieldRef<Weapon, bool> FullAuto =
        AccessTools.FieldRefAccess<Weapon, bool>("_fullAuto");
    private static readonly AccessTools.FieldRef<Weapon, float> TimeBetweenShots =
        AccessTools.FieldRefAccess<Weapon, float>("_timeBetweenShots");
    private static readonly AccessTools.FieldRef<Weapon, bool> NoShootingDuringAnim =
        AccessTools.FieldRefAccess<Weapon, bool>("_noShootingDuringShootAnim");
    private static readonly AccessTools.FieldRef<Weapon, bool> HoldingAdsInput =
        AccessTools.FieldRefAccess<Weapon, bool>("_holdingAdsInput");

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProjectileManager), nameof(ProjectileManager.AddProjectile))]
    private static void RedirectProjectile(Player owner, bool isLocal, Vector3 pos, ref Vector3 velocity)
    {
        if (!ShouldRedirect(owner, isLocal)) return;
        if (TargetFinder.Find(out var aimPoint) == null) return;

        velocity = (aimPoint - pos).normalized * velocity.magnitude;
    }

    /// <summary>Shotguns and any multi-pellet weapon come through here.</summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProjectileManager), nameof(ProjectileManager.AddProjectiles))]
    private static void RedirectProjectiles(Player owner, bool isLocal, Vector3 pos, Vector3[] velocities)
    {
        if (!ShouldRedirect(owner, isLocal) || velocities == null) return;
        if (TargetFinder.Find(out var aimPoint) == null) return;

        var direction = (aimPoint - pos).normalized;
        for (var i = 0; i < velocities.Length; i++)
            velocities[i] = direction * velocities[i].magnitude;
    }

    private static bool ShouldRedirect(Player owner, bool isLocal) =>
        Cfg.SilentAim.Value && isLocal && owner != null && owner == Player.LocalPlayer;

    /// <summary>
    /// Shoot() builds its spread cone from Quaternion.Euler(random * _spread), so zeroing the
    /// field for the duration of the call removes deviation without touching pellet counts.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Weapon), "Shoot")]
    private static void ZeroSpread(Weapon __instance, out float __state)
    {
        __state = Spread(__instance);
        if (Cfg.NoSpread.Value) Spread(__instance) = 0f;
    }

    /// <summary>Restores spread and, for infinite ammo, re-tops the magazine.</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Weapon), "Shoot")]
    private static void AfterShoot(Weapon __instance, float __state)
    {
        Spread(__instance) = __state;

        if (!Cfg.InfiniteAmmo.Value) return;

        var attachments = __instance.Attachments;
        if (attachments == null) return;

        // Keeping the magazine full also short-circuits Shoot's auto-reload queueing.
        AccessTools.PropertySetter(typeof(Weapon), nameof(Weapon.Ammo))
            .Invoke(__instance, new object[] { attachments.AmmoPerMag });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.Recoil))]
    private static bool NoCameraRecoil() => !Cfg.NoRecoil.Value;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerToolMovement), nameof(PlayerToolMovement.Recoil))]
    private static bool NoToolRecoil() => !Cfg.NoRecoil.Value;

    /// <summary>Without this the weapon model still kicks even when the camera does not.</summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Weapon), "AddModelRecoil")]
    private static bool NoModelRecoil() => !Cfg.NoRecoil.Value;

    /// <summary>
    /// HandleAiming re-derives IsAds from _holdingAdsInput every frame, so the input field is
    /// forced instead of the property.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Weapon), "HandleAiming")]
    private static void ForceAds(Weapon __instance)
    {
        if (Cfg.AlwaysAds.Value) HoldingAdsInput(__instance) = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Weapon), "Update")]
    private static void ApplyFireRate(Weapon __instance)
    {
        if (!Cfg.RapidFire.Value) return;

        FullAuto(__instance) = true;
        TimeBetweenShots(__instance) = Cfg.FireInterval.Value;
        NoShootingDuringAnim(__instance) = false;
    }

    /// <summary>The cooldown check also gates on animation length, so bypass it wholesale.</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Weapon), "HasCooldown")]
    private static void SkipCooldown(ref bool __result)
    {
        if (Cfg.RapidFire.Value && Cfg.FireInterval.Value <= 0.01f) __result = false;
    }

    /// <summary>The game's own one-shot dev setting: every damage site swaps in 99999 when true.</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ServerSettings), nameof(ServerSettings.OneShotEnabled), MethodType.Getter)]
    private static void ForceOneShot(ref bool __result) => __result |= Cfg.OneShotKill.Value;
}
