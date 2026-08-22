using BepInEx;
using BepInEx.Logging;
using fih.Cheats;
using fih.ImGuiUnity;
using fih.UI;
using HarmonyLib;

namespace fih;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private Harmony _harmony;

    private void Awake()
    {
        Logger = base.Logger;
        Cfg.Bind(Config);

        // Harmony rewrites the JIT entry point of each patched method.
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        foreach (var patched in _harmony.GetPatchedMethods())
            Logger.LogInfo($"Patched {patched.DeclaringType?.Name}.{patched.Name}");

        // BepInEx hosts every plugin on one persistent GameObject, so these components
        // survive scene loads and receive Unity's Update/OnGUI callbacks.
        gameObject.AddComponent<DebugHud>();
        gameObject.AddComponent<ImGuiHost>();
        gameObject.AddComponent<CheatController>();
        gameObject.AddComponent<AimAssist>();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Menu: {Cfg.MenuKey.Value}");
    }

    private void OnDestroy() => _harmony?.UnpatchSelf();
}
