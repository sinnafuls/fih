using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace fih;

/// <summary>
/// Every setting in one place. BepInEx writes these to BepInEx/config/fih.cfg; assigning to
/// <c>.Value</c> at runtime persists back to that file.
/// </summary>
internal static class Cfg
{
    internal static ConfigEntry<Key> MenuKey;

    internal static ConfigEntry<float> UiScale;

    internal static ConfigEntry<bool> FreePurchases;
    internal static ConfigEntry<int> MoneyAmount;
    internal static ConfigEntry<int> BaitAmount;

    internal static ConfigEntry<bool> AimEnabled;
    internal static ConfigEntry<Cheats.AimButton> AimButton;
    internal static ConfigEntry<Key> AimKey;
    internal static ConfigEntry<bool> AimSnap;
    internal static ConfigEntry<float> AimSmoothing;
    internal static ConfigEntry<float> AimMaxDistance;
    internal static ConfigEntry<float> AimMaxAngle;
    internal static ConfigEntry<bool> AimDripOnly;
    internal static ConfigEntry<bool> AimBossOnly;

    internal static ConfigEntry<bool> SilentAim;
    internal static ConfigEntry<bool> NoSpread;
    internal static ConfigEntry<bool> NoRecoil;
    internal static ConfigEntry<bool> AlwaysAds;
    internal static ConfigEntry<bool> RapidFire;
    internal static ConfigEntry<float> FireInterval;
    internal static ConfigEntry<bool> InfiniteAmmo;
    internal static ConfigEntry<bool> OneShotKill;

    internal static ConfigEntry<float> BoatSpeed;
    internal static ConfigEntry<Key> HudKey;
    internal static ConfigEntry<Key> DumpStateKey;
    internal static ConfigEntry<Key> DumpTargetKey;
    internal static ConfigEntry<Key> GodModeKey;
    internal static ConfigEntry<Key> FlyKey;

    internal static ConfigEntry<bool> LogDamage;

    internal static ConfigEntry<bool> GodMode;
    internal static ConfigEntry<bool> Fly;
    internal static ConfigEntry<bool> NoHunger;
    internal static ConfigEntry<bool> NoPoison;
    internal static ConfigEntry<bool> NoFire;
    internal static ConfigEntry<float> SpeedMultiplier;
    internal static ConfigEntry<float> JumpMultiplier;
    internal static ConfigEntry<float> FlySpeed;
    internal static ConfigEntry<bool> EnableDevCommands;

    internal static void Bind(ConfigFile config)
    {
        MenuKey = config.Bind("Keys", "Menu", Key.Insert, "Open/close the mod menu.");
        UiScale = config.Bind("UI", "Scale", 1f, new ConfigDescription(
            "Extra multiplier on top of the automatic display scaling of the ImGui menu.",
            new AcceptableValueRange<float>(0.5f, 3f)));
        HudKey = config.Bind("Keys", "Hud", Key.F1, "Show/hide the state HUD.");
        DumpStateKey = config.Bind("Keys", "DumpState", Key.F2, "Write the current game state to the log.");
        DumpTargetKey = config.Bind("Keys", "DumpTarget", Key.F3, "Reflection-dump the held item, or the player when empty-handed.");
        GodModeKey = config.Bind("Keys", "GodMode", Key.F5, "Toggle god mode.");
        FlyKey = config.Bind("Keys", "Fly", Key.F6, "Toggle flight.");

        LogDamage = config.Bind("Debugger", "LogDamage", true, "Log every PlayerVitals.TakeDamage call.");

        GodMode = config.Bind("Cheats", "GodMode", false,
            "Forces PlayerManager.InGodMode, the game's own damage gate. Also unlocks mid-air jumping, "
            + "because PlayerMovement.JumpInput ORs that flag into its jump condition. Host-wide: the "
            + "check inside TakeDamage is static, so no player takes damage while it is on.");
        Fly = config.Bind("Cheats", "Fly", false, "Disable gravity and control altitude (Space up, Left Ctrl down).");
        NoHunger = config.Bind("Cheats", "NoHunger", false, "Block PlayerVitals.LowerFullness, so fullness never drains.");
        NoPoison = config.Bind("Cheats", "NoPoison", false, "Block ApplyNewPoison (Pufferfish).");
        NoFire = config.Bind("Cheats", "NoFire", false, "Block ApplyNewFire (BowheadWhale, MainLava).");
        SpeedMultiplier = config.Bind("Cheats", "SpeedMultiplier", 1f,
            new ConfigDescription("Scales the local player's movement speed.", new AcceptableValueRange<float>(0.1f, 10f)));
        JumpMultiplier = config.Bind("Cheats", "JumpMultiplier", 1f,
            new ConfigDescription("Scales the local player's jump velocity.", new AcceptableValueRange<float>(0.1f, 10f)));
        FlySpeed = config.Bind("Cheats", "FlySpeed", 12f,
            new ConfigDescription("Vertical flight speed in m/s.", new AcceptableValueRange<float>(1f, 50f)));
        EnableDevCommands = config.Bind("Cheats", "EnableDevCommands", false,
            "Forces ClientSettings.CheatsEnabled so the game's own chat commands work as host: "
            + "/spawn <item>, /addmoney, /killboss, /nextisland, /allskins, /oneshot, /finishgame.");

        FreePurchases = config.Bind("Economy", "FreePurchases", false,
            "Forces MoneyManager.CanAfford true and skips RemoveMoney, so shops, bait, "
            + "extra pockets, weapon upgrades and boat upgrades all cost nothing.");
        MoneyAmount = config.Bind("Economy", "MoneyAmount", 10000,
            new ConfigDescription("Amount granted by the Add money button.",
                new AcceptableValueRange<int>(1, 1000000)));
        BaitAmount = config.Bind("Economy", "BaitAmount", 99,
            new ConfigDescription("Count written to every owned bait type.",
                new AcceptableValueRange<int>(1, 999)));

        AimEnabled = config.Bind("Aim", "Enabled", false, "Steer the camera toward the best target while the aim button is held.");
        AimButton = config.Bind("Aim", "Button", Cheats.AimButton.RightMouse, "Which input holds the aim.");
        AimKey = config.Bind("Aim", "Key", Key.LeftAlt, "Keyboard key used when Button is set to Key.");
        AimSnap = config.Bind("Aim", "Snap", false, "Instant aim instead of smoothing.");
        AimSmoothing = config.Bind("Aim", "Smoothing", 12f, new ConfigDescription(
            "Higher converges faster. Framerate independent.", new AcceptableValueRange<float>(1f, 40f)));
        AimMaxDistance = config.Bind("Aim", "MaxDistance", 80f, new ConfigDescription(
            "Ignore targets beyond this many metres.", new AcceptableValueRange<float>(10f, 200f)));
        AimMaxAngle = config.Bind("Aim", "MaxAngle", 45f, new ConfigDescription(
            "Only consider targets within this many degrees of the crosshair.",
            new AcceptableValueRange<float>(1f, 180f)));
        AimDripOnly = config.Bind("Aim", "DripOnly", false, "Only target drip (shiny) creatures.");
        AimBossOnly = config.Bind("Aim", "BossOnly", false, "Only target bosses and mini-bosses.");

        SilentAim = config.Bind("Weapons", "SilentAim", false,
            "Rewrites projectile velocity at spawn so shots fly at the target without moving the camera. "
            + "Applies to the networked vectors too, so other clients see the same trajectory.");
        NoSpread = config.Bind("Weapons", "NoSpread", false, "Zeroes Weapon._spread for the duration of each shot.");
        NoRecoil = config.Bind("Weapons", "NoRecoil", false, "Suppresses camera kick, tool kick and model recoil.");
        AlwaysAds = config.Bind("Weapons", "AlwaysAds", false,
            "Forces aim-down-sights. Note: sniper scope overlay stays up and sprint is blocked while a weapon is held.");
        RapidFire = config.Bind("Weapons", "RapidFire", false, "Forces full auto and overrides the shot interval.");
        FireInterval = config.Bind("Weapons", "FireInterval", 0.08f, new ConfigDescription(
            "Seconds between shots when RapidFire is on. At or below 0.01 the cooldown check is bypassed entirely.",
            new AcceptableValueRange<float>(0f, 1f)));
        InfiniteAmmo = config.Bind("Weapons", "InfiniteAmmo", false, "Refills the magazine after every shot.");
        OneShotKill = config.Bind("Weapons", "OneShotKill", false,
            "Forces the game's own ServerSettings.OneShotEnabled, which swaps every damage value for 99999.");

        BoatSpeed = config.Bind("World", "BoatSpeed", 1f, new ConfigDescription(
            "Extra thrust multiplier while driving the boat. Host only: on a remote client "
            + "the boat rig is kinematic and just replays server snapshots.",
            new AcceptableValueRange<float>(1f, 10f)));
    }
}
