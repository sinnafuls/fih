using fih.ImGuiUnity;
using HarmonyLib;

namespace fih.Patches;

/// <summary>
/// Player.BlockInputs is the game's own input gate: PlayerMovement.Move and PlayerCamera both
/// consult it, so reporting true while the menu is open captures input without hooking input.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.BlockInputs), MethodType.Getter)]
internal static class InputPatches
{
    [HarmonyPostfix]
    private static void BlockWhileMenuOpen(ref bool __result) => __result |= ImGuiHost.WantsInput;
}
