using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace fih.Cheats;

/// <summary>
/// Aim assist. PlayerCamera accumulates look input into a private Vector3 `_rot`
/// (x = pitch, y = yaw), so steering the aim means writing that accumulator.
/// </summary>
internal sealed class AimAssist : MonoBehaviour
{
    private static readonly AccessTools.FieldRef<PlayerCamera, Vector3> CameraRot =
        AccessTools.FieldRefAccess<PlayerCamera, Vector3>("_rot");

    internal static Creature CurrentTarget { get; private set; }

    private void Update()
    {
        CurrentTarget = null;
        if (!Cfg.AimEnabled.Value) return;

        var player = Player.LocalPlayer;
        var camera = player?.Camera;
        if (camera == null || GameInfo.CurCamera == null) return;
        if (!IsAimHeld()) return;

        var target = TargetFinder.Find(out var aimPoint);
        if (target == null) return;
        CurrentTarget = target;

        var origin = GameInfo.CurCamera.transform.position;
        var desired = Quaternion.LookRotation(aimPoint - origin).eulerAngles;
        var current = CameraRot(camera);

        // Euler angles wrap at 360; interpolating raw values spins the long way round.
        var pitch = Mathf.DeltaAngle(current.x, desired.x);
        var yaw = Mathf.DeltaAngle(current.y, desired.y);

        if (!Cfg.AimSnap.Value)
        {
            // Fraction of the remaining error per second, so framerate independent.
            var t = 1f - Mathf.Exp(-Cfg.AimSmoothing.Value * Time.deltaTime);
            pitch *= t;
            yaw *= t;
        }

        current.x += pitch;
        current.y += yaw;
        CameraRot(camera) = current;
    }

    private static bool IsAimHeld()
    {
        switch (Cfg.AimButton.Value)
        {
            case AimButton.RightMouse: return Mouse.current?.rightButton.isPressed == true;
            case AimButton.LeftMouse: return Mouse.current?.leftButton.isPressed == true;
            case AimButton.MiddleMouse: return Mouse.current?.middleButton.isPressed == true;
            case AimButton.Always: return true;
            default:
                var keyboard = Keyboard.current;
                return keyboard != null && keyboard[Cfg.AimKey.Value].isPressed;
        }
    }
}

internal enum AimButton
{
    RightMouse,
    LeftMouse,
    MiddleMouse,
    Key,
    Always
}
