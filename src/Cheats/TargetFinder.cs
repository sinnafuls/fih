using UnityEngine;

namespace fih.Cheats;

/// <summary>
/// Finds the creature to aim at. ItemManager.Items is the live instance registry
/// (GameInfo._allCreatures holds prefabs), and the aim point is the centre of mass.
/// </summary>
internal static class TargetFinder
{
    /// <summary>Best target for the current filters, or null.</summary>
    internal static Creature Find(out Vector3 aimPoint)
    {
        aimPoint = Vector3.zero;

        var camera = GameInfo.CurCamera;
        var player = Player.LocalPlayer;
        if (camera == null || player == null) return null;

        var origin = camera.transform.position;
        var forward = camera.transform.forward;
        var maxDistance = Cfg.AimMaxDistance.Value;
        var maxAngle = Cfg.AimMaxAngle.Value;

        Creature best = null;
        var bestAngle = float.MaxValue;

        // Items has one entry per collider, so a creature can appear several times.
        foreach (var item in ItemManager.Items.Values)
        {
            if (item == null) continue;

            var creature = item.Creature;
            if (!IsAlive(creature)) continue;
            if (Cfg.AimDripOnly.Value && !creature.IsDrip) continue;
            if (Cfg.AimBossOnly.Value && creature.BossType == BossType.None) continue;

            var point = AimPoint(creature);
            var delta = point - origin;
            var distance = delta.magnitude;
            if (distance < 0.01f || distance > maxDistance) continue;

            var angle = Vector3.Angle(forward, delta);
            if (angle > maxAngle || angle >= bestAngle) continue;

            bestAngle = angle;
            best = creature;
            aimPoint = point;
        }

        return best;
    }

    internal static bool IsAlive(Creature creature) =>
        creature != null
        && creature.gameObject.activeInHierarchy
        && !creature.IsDestroying
        && !creature.IsDead;

    internal static Vector3 AimPoint(Creature creature)
    {
        var rigidbody = creature.Rig;
        return rigidbody != null ? rigidbody.worldCenterOfMass : creature.transform.position;
    }

    /// <summary>Nearest living creature to the player, ignoring aim filters.</summary>
    internal static Creature FindNearest(out Vector3 position)
    {
        position = Vector3.zero;

        var player = Player.LocalPlayer;
        if (player?.Transform == null) return null;

        var origin = player.Transform.position;
        Creature best = null;
        var bestDistance = float.MaxValue;

        foreach (var item in ItemManager.Items.Values)
        {
            var creature = item?.Creature;
            if (!IsAlive(creature)) continue;

            var point = AimPoint(creature);
            var distance = (point - origin).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = creature;
            position = point;
        }

        return best;
    }
}
