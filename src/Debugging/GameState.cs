using System;
using System.Collections;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace fih.Debugging;

/// <summary>
/// Reads live game state through Assembly-CSharp's public API, with reflection only for
/// private members.
/// </summary>
internal static class GameState
{
    private static readonly Assembly GameAssembly = typeof(Item).Assembly;
    private static readonly FieldInfo MaxHealthField = AccessTools.Field(typeof(PlayerVitals), "_maxHealth");
    private static readonly FieldInfo MaxFullnessField = AccessTools.Field(typeof(PlayerVitals), "_maxFullness");
    private static readonly FieldInfo AllItemsField = AccessTools.Field(typeof(GameInfo), "_allItems");
    private static readonly FieldInfo AllCreaturesField = AccessTools.Field(typeof(GameInfo), "_allCreatures");

    internal static int MaxHealth => (int)MaxHealthField.GetValue(null);
    internal static int MaxFullness => (int)MaxFullnessField.GetValue(null);

    internal static int RegistryCount(FieldInfo field) =>
        field.GetValue(null) is ICollection c ? c.Count : -1;

    internal static int SpawnableCount => RegistryCount(AllItemsField);
    internal static int CreatureCount => RegistryCount(AllCreaturesField);

    /// <summary>Appends a human-readable snapshot of the world and local player.</summary>
    internal static void Describe(StringBuilder sb)
    {
        sb.Append("fih debugger  t=").Append(Time.realtimeSinceStartup.ToString("F1")).Append("s\n");
        sb.Append("tickMulti=").Append(GameInfo.TickMulti.ToString("F2"))
          .Append("  spawnables=").Append(SpawnableCount)
          .Append("  creatures=").Append(CreatureCount).Append('\n');
        sb.Append("cheats");
        if (Cfg.GodMode.Value) sb.Append(" god");
        if (Cfg.Fly.Value) sb.Append(" fly");
        if (Cfg.NoHunger.Value) sb.Append(" nohunger");
        if (Cfg.NoPoison.Value) sb.Append(" nopoison");
        if (Cfg.NoFire.Value) sb.Append(" nofire");
        if (Cfg.EnableDevCommands.Value) sb.Append(" devcmds");
        sb.Append("  spd=").Append(Cfg.SpeedMultiplier.Value.ToString("F1"))
          .Append("x  jump=").Append(Cfg.JumpMultiplier.Value.ToString("F1")).Append("x\n");

        var player = Player.LocalPlayer;
        if (player == null)
        {
            sb.Append("\nPlayer.LocalPlayer = null (menu, or not spawned yet)");
            return;
        }

        var t = player.Transform;
        sb.Append("\nplayer ").Append(player.SteamName ?? "?")
          .Append(player.IsCrouching ? " [crouch]" : string.Empty)
          .Append(player.IsAfk ? " [afk]" : string.Empty)
          .Append(Player.IsInside ? " [inside]" : string.Empty).Append('\n');
        if (t != null)
        {
            var p = t.position;
            sb.Append("pos ").Append(p.x.ToString("F1")).Append(", ")
              .Append(p.y.ToString("F1")).Append(", ").Append(p.z.ToString("F1"));
            var rig = player.Rigidbody;
            if (rig != null) sb.Append("   speed ").Append(rig.linearVelocity.magnitude.ToString("F2")).Append(" m/s");
            sb.Append('\n');
        }

        var vitals = player.Vitals;
        if (vitals != null)
        {
            sb.Append("hp ").Append(vitals.Health).Append('/').Append(MaxHealth)
              .Append("   food ").Append(vitals.Fullness).Append('/').Append(MaxFullness)
              .Append("   poison ").Append((vitals.PoisonPercent * 100f).ToString("F0")).Append('%')
              .Append("   fire ").Append((vitals.FirePercent * 100f).ToString("F0")).Append("%\n");
        }

        var held = player.Holding != null ? player.Holding.HeldItem : null;
        if (held == null)
        {
            sb.Append("held  <empty>");
        }
        else
        {
            sb.Append("held  ").Append(held.name).Append("  id=").Append(held.ID)
              .Append("  type=").Append(held.Type).Append('\n');
            sb.Append("      worth ").Append(held.TotalWorth).Append(" (base ").Append(held.DefaultWorth)
              .Append(")  weight ").Append(held.RandomizedWeight.ToString("F2"))
              .Append("  cookness ").Append(held.Cookness.ToString("F2"));
            if (held.Fish != null) sb.Append("  [fish]");
            if (held.FishingRod != null) sb.Append("  [rod]");
        }
    }

    /// <summary>Dumps every declared field of a component, private included.</summary>
    internal static void DumpFields(object target, StringBuilder sb)
    {
        if (target == null) { sb.Append("nothing to dump"); return; }

        var runtimeType = target.GetType();
        sb.Append("=== ").Append((target as UnityEngine.Object)?.name ?? "?")
          .Append(" : ").Append(runtimeType.FullName).Append(" ===");

        // GetDeclaredFields only sees one level, so walk the base chain. Stopping at the
        // assembly boundary keeps the dump to game code rather than FishNet/Unity plumbing.
        for (var type = runtimeType; type != null && type.Assembly == GameAssembly; type = type.BaseType)
        {
            var fields = AccessTools.GetDeclaredFields(type);
            if (fields == null || fields.Count == 0) continue;

            sb.Append("\n-- ").Append(type.Name).Append(" --");
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                // FishNet's IL weaver injects one of these per NetworkBehaviour subclass.
                if (field.Name.StartsWith("NetworkInitialize___", StringComparison.Ordinal)) continue;

                sb.Append('\n').Append("  ").Append(field.FieldType.Name).Append(' ')
                  .Append(CleanName(field.Name)).Append(" = ").Append(Format(field, target));
            }
        }
    }

    /// <summary>auto-property backing fields read as "&lt;Health&gt;k__BackingField".</summary>
    private static string CleanName(string name)
    {
        var end = name.IndexOf(">k__BackingField", StringComparison.Ordinal);
        return end > 1 ? name.Substring(1, end - 1) : name;
    }

    private static string Format(FieldInfo field, object target)
    {
        try
        {
            var value = field.GetValue(target);
            if (value == null) return "null";

            // FishNet wraps replicated state in SyncVar<T>; the payload is on .Value.
            if (field.FieldType.IsGenericType && field.FieldType.Name.StartsWith("SyncVar", StringComparison.Ordinal))
            {
                var inner = AccessTools.Property(field.FieldType, "Value")?.GetValue(value);
                return $"SyncVar({inner ?? "null"})";
            }

            return value switch
            {
                ICollection c => $"{c.GetType().Name}[{c.Count}]",
                _ => value.ToString()
            };
        }
        catch (Exception ex)
        {
            return $"<threw {ex.GetType().Name}>";
        }
    }
}
