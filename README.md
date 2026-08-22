# fih

Debug and cheat menu for **How to Fish** (Unity 6000.4.4, Mono). BepInEx 5 + HarmonyX, with a
Dear ImGui interface rendered through URP without shipping any Unity assets.

## Install

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64, Mono) into the game
   folder and run the game once so it generates its directories.
2. Grab the [latest release](https://github.com/sinnafuls/fih/releases) and put the files in place:

   | File | Destination |
   | --- | --- |
   | `fih.dll` | `How to Fish/BepInEx/plugins/fih/` |
   | `ImGui.NET.dll` | `How to Fish/BepInEx/plugins/fih/` |
   | `cimgui.dll` | `How to Fish/` (next to the exe) |

3. Launch and press **Insert**.

`cimgui.dll` is the native Dear ImGui library and is loaded by name from the process directory,
so it has to sit next to the exe rather than in the plugin folder. Without it the menu silently
fails to initialise and logs `[imgui] init failed` to `BepInEx/LogOutput.log`.

Settings are written to `BepInEx/config/fih.cfg` and window layout to `fih.imgui.ini`. Anything
changed in the menu persists immediately.

## Features

<details>
<summary><b>Aim</b></summary>

- Aim assist that steers the camera toward the best target while a bind is held
- Click-to-rebind selector accepting a key or a mouse button, or always-on
- Smooth aiming at an adjustable, framerate-independent rate, or instant snap
- Target filters: drip (shiny) only, bosses and minibosses only
- Max distance (10-200 m) and max angle from the crosshair
- Live readout of the current target with drip state, boss type and hp

</details>

<details>
<summary><b>Weapon</b></summary>

- **Magic bullet (silent aim)** - projectile velocities are rewritten at spawn, including the
  vectors sent to other clients, so shots fly at the target without the camera moving
- No spread, no recoil (camera, tool and model), always ADS
- Rapid fire with configurable shot interval and forced full auto
- Infinite ammo with automatic magazine refill
- One shot kill, via the game's own one-shot server setting

</details>

<details>
<summary><b>Player</b></summary>

- God mode (also unlocks mid-air jumping), no hunger, no poison, no fire
- Fly with adjustable vertical speed
- Movement speed and jump multipliers
- Money: live balance, configurable amount, free purchases
- Item spawner: all 85 spawnables read from the game's own registry, with search

</details>

<details>
<summary><b>Fun</b></summary>

- Kill all live creatures, kill boss, teleport to nearest creature
- Unlock all inventory pockets, fill every bait type
- Boat speed multiplier, unlock boat + radar + max motor
- Unlock all islands, island dropdown, previous/next island
- Unlock or clear all Steam achievements

</details>

<details>
<summary><b>Misc</b></summary>

- Force the game's dev commands and run some of them directly
- Live player info panel
- Reflection dumps of the held item or player to the log
- Reset vitals, reset all toggles
- Keybind selectors for every hotkey
- Live UI scale slider on top of automatic display scaling

</details>

<details>
<summary><b>Debug HUD</b></summary>

- **F1** on-screen state panel: world stats, position, speed, vitals, held item
- **F2** dump game state to the log
- **F3** dump every field of the held item, or of the player when empty-handed - walks the
  inheritance chain and unwraps FishNet `SyncVar` values

</details>

## Notes

- **Host only.** The game is server-authoritative (FishNet). Vitals, economy, islands, boat and
  spawning all run through code gated on `IsServerInitialized`, so they work when you host and
  do nothing meaningful as a remote client.
- **Always ADS is not accuracy.** Spread is applied unconditionally and is not tied to ADS; use
  *No spread* for that. Always ADS also keeps the sniper overlay up and blocks sprint.
- The game's own `/killallcreatures` only marks journal entries. The Fun tab damages live
  instances instead.

## Building

```
dotnet build
```

Requires .NET SDK 8+ and a local copy of the game. The game directory is resolved in order:

1. `FIH_GAME_DIR` environment variable, or `dotnet build -p:GameDir=...`
2. the Steam install found in the registry
3. the roots listed in `SteamLibraryRoots` in `src/fih.csproj`

The build compiles against the game's own assemblies, then deploys `fih.dll` and `ImGui.NET.dll`
to `BepInEx/plugins/fih/` and `cimgui.dll` to the game root. Use `-p:NoDeploy=true` to skip
deployment.

## Layout

```
src/
  Plugin.cs        entry point: config, Harmony, components
  Cfg.cs           every setting in one place
  Patches/         Harmony patches
  Cheats/          runtime behaviour (aim, flight, world and economy actions)
  ImGuiUnity/      Dear ImGui backend: renderer, input, host, theme
  UI/              menu, spawn browser, widgets, debug HUD
  Debugging/       state reader and reflection dumper
```

<details>
<summary><b>How the ImGui backend works</b></summary>

ImGui.NET is bindings only, so the Unity side is implemented here:

- **Renderer** builds one `Mesh` per frame with a submesh per `ImDrawCmd`, mapping ImGui's index
  ranges and base vertices directly, then draws it through a `CommandBuffer` on
  `RenderPipelineManager.endCameraRendering` with per-command scissor rects.
- **Material** is the stock `UI/Default` shader resolved at runtime, so no AssetBundle is needed.
- **Palette** is converted from sRGB to linear, because URP consumes vertex colours as linear.
- **Input** forwards discrete key, character and mouse events from the Input System.
- **Focus** is captured by patching `Player.BlockInputs` and disabling Unity's `EventSystem`,
  which is what stops clicks reaching the game behind the menu.

</details>

## Credits

- [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX)
- [Dear ImGui](https://github.com/ocornut/imgui) and [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
