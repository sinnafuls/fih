using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace fih.Cheats;

/// <summary>
/// Hotkeys plus the one cheat that cannot be a patch: flight needs per-frame velocity.
/// </summary>
internal sealed class CheatController : MonoBehaviour
{
    private static readonly AccessTools.FieldRef<PlayerMovement, float> ExtraGravity =
        AccessTools.FieldRefAccess<PlayerMovement, float>("_extraGravityForce");

    private bool _flying;
    private float _savedGravity;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[Cfg.GodModeKey.Value].wasPressedThisFrame)
        {
            Cfg.GodMode.Value = !Cfg.GodMode.Value;
            Plugin.Logger.LogInfo($"GodMode {(Cfg.GodMode.Value ? "on" : "off")} (also grants mid-air jumps)");
        }

        if (keyboard[Cfg.FlyKey.Value].wasPressedThisFrame)
        {
            Cfg.Fly.Value = !Cfg.Fly.Value;
            Plugin.Logger.LogInfo($"Fly {(Cfg.Fly.Value ? "on" : "off")}");
        }

        var player = Player.LocalPlayer;
        if (player == null) return;

        var movement = player.Movement;
        var rigidbody = player.Rigidbody;
        if (movement == null || rigidbody == null) return;

        if (Cfg.Fly.Value) Fly(movement, rigidbody, keyboard);
        else if (_flying) StopFlying(movement, rigidbody);
    }

    /// <summary>
    /// Move() recomputes vertical velocity from the rigidbody each physics step, so both
    /// rigidbody gravity and _extraGravityForce must be neutralised for flight.
    /// </summary>
    private void Fly(PlayerMovement movement, Rigidbody rigidbody, Keyboard keyboard)
    {
        if (!_flying)
        {
            _savedGravity = ExtraGravity(movement);
            _flying = true;
        }

        ExtraGravity(movement) = 0f;
        rigidbody.useGravity = false;

        var lift = 0f;
        if (keyboard[Key.Space].isPressed) lift += 1f;
        if (keyboard[Key.LeftCtrl].isPressed) lift -= 1f;

        var velocity = rigidbody.linearVelocity;
        velocity.y = lift * Cfg.FlySpeed.Value;
        rigidbody.linearVelocity = velocity;
    }

    private void StopFlying(PlayerMovement movement, Rigidbody rigidbody)
    {
        ExtraGravity(movement) = _savedGravity;
        rigidbody.useGravity = true;
        _flying = false;
    }
}
