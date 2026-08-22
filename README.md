# fih

Debug and cheat menu for **How to Fish** (Unity 6000.4.4, Mono). Built on BepInEx 5, HarmonyX
and Dear ImGui, rendered through URP with no shipped assets.

Author: sin · https://github.com/sinnafuls/fih

![menu](docs/menu.png)

## Features

Press **Insert** to open the menu. Every setting is a BepInEx `ConfigEntry`, so changes persist
to `BepInEx/config/fih.cfg` immediately and can also be edited by hand.

### Aim
- Aim assist that steers the camera toward the best target while a bind is held
- Click-to-rebind selector accepting a key or a mouse button, or always-on
- Smooth aiming with an adjustable, framerate-independent rate, or instant snap
- Target filters: drip (shiny) only, bosses and minibosses only
- Max distance (10-200 m) and max angle from the crosshair
- Live readout of the current target with drip, boss type and hp

### Weapon
- Magic bullet (silent aim): projectile velocities are rewritten at spawn, including the
  vectors sent to other clients, so the camera never moves
- No spread, no recoil (camera, tool and model), always ADS
- Rapid fire with a configurable shot interval and forced full auto
- Infinite ammo with automatic magazine refill
- One shot kill via the game's own one-shot server setting

### Player
- God mode (also unlocks mid-air jumping), no hunger, no poison, no fire
- Fly with adjustable vertical speed, movement speed and jump multipliers
- Money: current balance, configurable amount, free purchases
- Item spawner: all 85 spawnables read from the game's own registry, with search

### Fun
- Kill all live creatures, kill boss, teleport to nearest creature
- Unlock all inventory pockets, fill every bait type
- Boat speed multiplier, unlock boat + radar + max motor
- Unlock all islands, island dropdown, previous/next island
- Unlock or clear all Steam achievements

### Misc
- Force the game's dev commands and run a few of them directly
- Live player info panel and reflection dumps of the held item or player to the log
- Reset vitals and reset all toggles
- Keybind selectors for every hotkey
- Live UI scale slider on top of automatic display scaling

### Debug HUD
**F1** toggles an on-screen state panel; **F2** dumps game state to the log; **F3** dumps every
field of the held item, or of the player when empty-handed, walking the inheritance chain and
unwrapping FishNet `SyncVar` values.

## Notes

- **Host only.** The game is server-authoritative (FishNet). Vitals, economy, islands, boat and
  spawning all run through code gated on `IsServerInitialized`, so they work when you host and
  do nothing meaningful as a remote client.
- **Always ADS is not accuracy.** Spread is applied unconditionally and is not tied to ADS; use
  *No spread* for that. Always ADS also keeps the sniper overlay up and blocks sprint.
- The game's `/killallcreatures` only marks journal entries. The Fun tab damages live instances
  instead.

## Building

```
dotnet build
```

Requires .NET SDK 8+ and a local copy of the game. The game directory is resolved in this order:

1. `FIH_GAME_DIR` environment variable, or `dotnet build -p:GameDir=...`
2. the Steam install found in the registry
3. the roots listed in `SteamLibraryRoots` in `src/fih.csproj`

The build compiles against the game's own assemblies, then deploys `fih.dll` to
`BepInEx/plugins/fih/` and `cimgui.dll` to the game root. Build with `-p:NoDeploy=true` to skip
deployment.

## Layout

```
src/
  Plugin.cs            entry point: config, Harmony, components
  Cfg.cs               every setting in one place
  Patches/             Harmony patches
  Cheats/              runtime behaviour (aim, flight, world and economy actions)
  ImGuiUnity/          Dear ImGui backend: renderer, input, host, theme
  UI/                  menu, spawn browser, widgets, debug HUD
  Debugging/           state reader and reflection dumper
tools/                 dev scripts (type dumper, input/screenshot harness)
```

## ImGui backend

ImGui.NET is bindings only, so the Unity side is implemented here:

- **Renderer** builds one `Mesh` per frame with a submesh per `ImDrawCmd`, mapping ImGui's
  index ranges and base vertices directly, and draws it through a `CommandBuffer` on
  `RenderPipelineManager.endCameraRendering` with per-command scissor rects.
- **Material** is the stock `UI/Default` shader found at runtime, so no AssetBundle is needed.
- **Palette** is converted from sRGB to linear because URP consumes vertex colours as linear.
- **Input** forwards discrete key, character and mouse events from the Input System.
- **Focus** is captured by patching `Player.BlockInputs` and disabling Unity's `EventSystem`,
  which is what stops clicks reaching the game behind the menu.

## Credits

- [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX)
- [Dear ImGui](https://github.com/ocornut/imgui) and [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
