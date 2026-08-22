using HarmonyLib;
using UnityEngine;

namespace fih.Patches;

/// <summary>
/// Boat speed. Thrust is applied server-side in Boat.ApplyInputForce and a remote client's
/// physics rig is kinematic, so this only does anything while we are the host.
/// </summary>
[HarmonyPatch(typeof(Boat), "ApplyInputForce")]
internal static class BoatPatches
{
    private static readonly AccessTools.FieldRef<Boat, BoatMotor> CurMotor =
        AccessTools.FieldRefAccess<Boat, BoatMotor>("_curMotor");

    [HarmonyPostfix]
    private static void ExtraThrust(Boat __instance)
    {
        var multiplier = Cfg.BoatSpeed.Value;
        if (multiplier <= 1f || !__instance.IsServerInitialized) return;

        var motor = CurMotor(__instance);
        var rigidbody = __instance.HiddenPhysicsRig;
        if (motor == null || motor.Propeller == null || rigidbody == null) return;

        // The stock call already applied 1x, so only the surplus is added here.
        var throttle = (float)__instance._driverYInput.Value;
        rigidbody.AddForceAtPosition(
            -motor.Propeller.right * (motor.Force * throttle * (multiplier - 1f)),
            motor.Propeller.position);

        // Awake clamps the rig to Boat._maxVelocity, which would eat the extra thrust.
        var cap = Cfg.BoatSpeed.Value * 30f;
        if (rigidbody.maxLinearVelocity < cap) rigidbody.maxLinearVelocity = cap;
    }
}
