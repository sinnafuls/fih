using HarmonyLib;

namespace fih.Cheats;

/// <summary>
/// Money, shops, slots, baits, achievements. Every shop RPC calls MoneyManager.CanAfford
/// then MoneyManager.RemoveMoney, so both are hooked.
/// </summary>
[HarmonyPatch]
internal static class EconomyCheats
{
    /// <summary>The RPCs early-out on CanAfford before RemoveMoney is reached.</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MoneyManager), nameof(MoneyManager.CanAfford))]
    private static void AlwaysAfford(ref bool __result) => __result |= Cfg.FreePurchases.Value;

    /// <summary>Purchases grant the item after deducting, so only skip the deduction.</summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MoneyManager), nameof(MoneyManager.RemoveMoney))]
    private static bool KeepMoney() => !Cfg.FreePurchases.Value;

    internal static void AddMoney(int amount)
    {
        var player = Player.LocalPlayer;
        if (player == null) return;

        MoneyManager.AddMoney(amount, player);
        Plugin.Logger.LogInfo($"[cheats] +${amount}, balance ${MoneyManager.Money}");
    }

    /// <summary>Server caps purchasable pockets at 5, so that is the ceiling here too.</summary>
    internal static void UnlockAllSlots()
    {
        var inventory = Player.LocalPlayer?.Inventory;
        if (inventory == null) return;

        inventory._extraSlots.Value = 5;
        Plugin.Logger.LogInfo("[cheats] inventory slots unlocked (extra=5)");
    }

    /// <summary>
    /// Bait counts live in PlayerInventory._ownedBaits, one entry per non-empty BaitInfo,
    /// indexed by GameInfo.GetIndexOfBait minus one (index 0 is the "empty" bait).
    /// </summary>
    internal static void FillBaits(int amount)
    {
        var inventory = Player.LocalPlayer?.Inventory;
        if (inventory == null) return;

        var baits = inventory._ownedBaits;
        for (var i = 0; i < baits.Count; i++) baits[i] = amount;

        Plugin.Logger.LogInfo($"[cheats] {baits.Count} bait types set to {amount}");
    }

    internal static void UnlockAchievements()
    {
        AchievementManager.ToggleAllAchievements(true);
        Plugin.Logger.LogInfo("[cheats] steam achievements unlocked");
    }

    internal static void LockAchievements()
    {
        AchievementManager.ToggleAllAchievements(false);
        Plugin.Logger.LogInfo("[cheats] steam achievements cleared");
    }
}
