# Changelog

All notable changes to **NOXMFD: Vanilla Icons Plus Bridge** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [0.1.0] — 2026-09-09

Initial release.

- Mirrors [NO-VanillaIconsPLUS](https://github.com/xHellcat92x/NO-VanillaIconsPLUS)'s friendly/
  enemy/neutral tints and enemy-only AA/Special AA unit-type tints onto NOXMFD's MAP page, via
  `NOXMFD.Api.SetFactionColorOverride`/`SetUnitTypeColorOverride`.
- **VANILLA ICONS PLUS** EXT page mirroring all sixteen VanillaIconsPLUS ConfigurationManager
  settings (toggles, color pickers, sliders) — writes go straight into VanillaIconsPLUS's own
  `ConfigEntry`s, so its own live-apply logic fires exactly as if edited through its F1/F9 menu.
