# NOXMFD Extension: Vanilla Icons Plus Bridge

[![NOXMFD](https://img.shields.io/badge/Requires-NOXMFD-blue)](https://github.com/roke77/NOXMFD)
[![VanillaIconsPLUS](https://img.shields.io/badge/Requires-VanillaIconsPLUS-lightgrey)](https://github.com/xHellcat92x/NO-VanillaIconsPLUS)
[![Version](https://img.shields.io/badge/Version-0.1.0-green)](CHANGELOG.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Mirrors [NO-VanillaIconsPLUS](https://github.com/xHellcat92x/NO-VanillaIconsPLUS)'s unit colors
onto [NOXMFD](https://github.com/roke77/NOXMFD)'s own MAP page, so a pilot who recolors
VanillaIconsPLUS's friendly/enemy/neutral tints and AA/Special AA unit highlighting sees the same
colors on NOXMFD's browser map, not just the game's native HUD/map. Also adds a **VANILLA ICONS
PLUS** page under NOXMFD's EXT nav mirroring every setting from VanillaIconsPLUS's own
ConfigurationManager (F1/F9) menu, so a pilot can toggle/recolor it without leaving the MFD.

Built entirely through NOXMFD's public extension API (`NOXMFD.Api`'s icon-color-override surface —
see NOXMFD's [`EXTENSIONS.md`](https://github.com/roke77/NOXMFD/blob/main/EXTENSIONS.md) and
[`docs/vanilla-icons-plus-extension.md`](https://github.com/roke77/NOXMFD/blob/main/docs/vanilla-icons-plus-extension.md)).
This repo does **not** modify NOXMFD's or VanillaIconsPLUS's source.

> [!IMPORTANT]
> **Install order:** BepInEx 5 → [NO-VanillaIconsPLUS](https://github.com/xHellcat92x/NO-VanillaIconsPLUS) → [NOXMFD](https://github.com/roke77/NOXMFD) (≥ 0.46.0*) → **`NOXMFD.VanillaIconsPlusBridge.dll`**.
>
> \* 0.46.0 is a placeholder — the icon-color-override API this bridge depends on hasn't shipped
> in a NOXMFD release yet at the time of writing. Update the version pinned in
> [`src/plugin/Plugin.cs`](src/plugin/Plugin.cs)'s `BepInDependency` and this README once it does.

---

## Table of contents

- [What it does](#what-it-does)
- [What's here](#whats-here)
- [Configuration](#configuration)
- [Building](#building)
- [Installing](#installing)
- [Cutting a release](#cutting-a-release)
- [Changelog](#changelog)
- [Credits](#credits)

---

## What it does

Two independent color sources, both pushed live — no restart needed after a color change through
ConfigurationManager:

- **Friendly / enemy / neutral base tints** — read straight from `GameAssets.HUDFriendly/
  HUDHostile/HUDNeutral`, the same fields VanillaIconsPLUS itself writes, polled once a second
  (VanillaIconsPLUS keeps these on private settings with no externally-subscribable change event).
- **Enemy-only AA / Special AA unit tint** — read from VanillaIconsPLUS's own public
  `AAUnitsHUD`/`SpecialAAUnitsHUD` settings and its `AAUnitHelper` unit-type whitelists, pushed
  immediately whenever either color setting changes.

It also registers a **VANILLA ICONS PLUS** page under NOXMFD's own EXT nav: every one of
VanillaIconsPLUS's sixteen ConfigurationManager settings (toggles, color pickers, sliders),
reachable straight from NOXMFD's MFD. A change there writes directly into VanillaIconsPLUS's own
`ConfigEntry`, so its own live-apply logic (and this bridge's own mirroring above) fires exactly as
if the pilot had used its F1/F9 menu.

![VANILLA ICONS PLUS settings page, showing the HUD, HUD Unit Icons, and MAP sections](images/VIP_SETTINGS.png)

## What's here

- `src/plugin/Plugin.cs` — registers the AA/Special AA color mirror, spawns the polling worker,
  and registers the VANILLA ICONS PLUS EXT page (asset resolver + command handler).
- `src/plugin/Worker.cs` — the persistent, `DontDestroyOnLoad` component that polls the base
  faction tints (see its header comment for why this can't just be an event subscription).
- `src/plugin/SettingsBridge.cs` — the full list of VanillaIconsPLUS's sixteen settings (key +
  type) and the read/write logic against its `ConfigFile`, used by both the page's state endpoint
  and its command handler.
- `src/plugin/PageAssets.cs` — embedded-resource lookup for `src/web/`'s HTML/CSS/JS.
- `src/web/vanilla-icons-plus.{html,css,js}` — the EXT page itself. Entirely generic over each
  setting's `data-key`/`data-kind` markup; the JS never names a setting.
- `lib/NOXMFD.dll` — compile-time reference only, not shipped to players (NOXMFD is already
  installed as its own plugin; `Private=false` in the `.csproj` keeps this project from bundling a
  second copy). Committed to this repo since it's this project's own dependency.
- `lib/VanillaIconsPLUS.dll` — same compile-time-only role, but gitignored: it's a third-party
  mod's binary, not this repo's to redistribute. See [Building](#building) for how to supply it
  yourself.

## Configuration

None of its own for the color-mirroring — recolor unit icons through VanillaIconsPLUS's own
ConfigurationManager menu, its `.cfg` file, or this extension's own VANILLA ICONS PLUS EXT page;
all three end up writing the same underlying settings, so any one of them is immediately visible
in the other two.

## Building

Requires a local Nuclear Option install with BepInEx 5, NOXMFD, and VanillaIconsPLUS already
installed. `lib/NOXMFD.dll` is already committed; `lib/VanillaIconsPLUS.dll` is not (see
[What's here](#whats-here)) — copy it in yourself before building:

```bash
cp "<your game install>/BepInEx/plugins/Com.Hellcat92.VanillaIconsPLUS/Com.Hellcat92.VanillaIconsPLUS_<version>.dll" lib/VanillaIconsPLUS.dll
```

Create a gitignored `GameDir.props` next to the `.csproj` pointing at your install if it isn't the
default Steam path:

```xml
<Project><PropertyGroup>
  <GameDir>D:\SteamLibrary\steamapps\common\Nuclear Option</GameDir>
</PropertyGroup></Project>
```

Then:

```bash
dotnet build VanillaIconsPlusBridge.csproj -c Release
```

The build's `DeployToGame` target copies the built DLL straight into
`$(GameDir)\BepInEx\plugins\` for you.

## Installing

1. Install BepInEx 5, [NO-VanillaIconsPLUS](https://github.com/xHellcat92x/NO-VanillaIconsPLUS),
   and [NOXMFD](https://github.com/roke77/NOXMFD) (see the version note above).
2. Drop `NOXMFD.VanillaIconsPlusBridge.dll` into `BepInEx/plugins/`.
3. Launch the game — colors set in VanillaIconsPLUS now also apply to NOXMFD's MAP page.

## Cutting a release

Bump `<Version>` in `VanillaIconsPlusBridge.csproj`, update `CHANGELOG.md`, tag with the bare
version (no `v` prefix, matching NOXMFD's own convention), and attach the built DLL to the GitHub
release.

## Changelog

See [`CHANGELOG.md`](CHANGELOG.md).

## Credits

- [xHellcat92x](https://github.com/xHellcat92x) — [NO-VanillaIconsPLUS](https://github.com/xHellcat92x/NO-VanillaIconsPLUS), the mod this bridges.
- [NOXMFD](https://github.com/roke77/NOXMFD) — the MFD this extends.
