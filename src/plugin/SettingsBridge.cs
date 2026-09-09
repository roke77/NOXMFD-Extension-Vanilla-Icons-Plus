using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace VanillaIconsPlusBridge
{
    // Full mirror of every setting VanillaIconsPLUS exposes through BepInEx.ConfigurationManager's
    // own F1/F9 menu — all sixteen live under its single "Settings" section (VanillaIconsPLUS's own
    // Plugin.Awake). Reached through the plugin's public Config (BaseUnityPlugin.Config, a
    // BepInEx.Configuration.ConfigFile) rather than its declaring fields: three of the sixteen
    // (Friendly/Enemy/Neutral Units) are private on VanillaIconsPLUS's own Plugin class, but
    // ConfigFile.TryGetEntry finds any bound entry by (section, key) regardless of field
    // visibility, so this needs no reflection.
    internal static class SettingsBridge
    {
        private const string Section = "Settings";

        internal enum Kind { Bool, Int, Float, Color }

        internal readonly struct Setting
        {
            public readonly string Key;
            public readonly Kind Kind;
            public Setting(string key, Kind kind) { Key = key; Kind = kind; }
        }

        // Declaration order here is the order the settings page renders them in.
        internal static readonly Setting[] All =
        {
            new Setting("Show Player Names", Kind.Bool),
            new Setting("Friendly Player Names", Kind.Color),
            new Setting("Enemy Player Names", Kind.Color),
            new Setting("Disable Vanilla Friendly Hover Names", Kind.Bool),
            new Setting("HUD Player Name Font Size", Kind.Int),
            new Setting("HUD Player Name Vertical Offset", Kind.Float),

            new Setting("Friendly Units", Kind.Color),
            new Setting("Enemy Units", Kind.Color),
            new Setting("Neutral Units", Kind.Color),
            new Setting("Enemy AA Units", Kind.Color),
            new Setting("Enemy AA (Special) Units", Kind.Color),

            new Setting("Show Map Player Names", Kind.Bool),
            new Setting("Friendly Player Names (MAP)", Kind.Color),
            new Setting("Enemy Player Names (MAP)", Kind.Color),
            new Setting("MAP Player Name Font Size", Kind.Int),
            new Setting("MAP Player Name Vertical Offset", Kind.Float),
        };

        private static readonly Dictionary<string, Kind> KindByKey = BuildKindLookup();

        private static Dictionary<string, Kind> BuildKindLookup()
        {
            var d = new Dictionary<string, Kind>(StringComparer.Ordinal);
            foreach (Setting s in All) d[s.Key] = s.Kind;
            return d;
        }

        // Safe to call off the main thread (the extension's AssetResolver runs on an HTTP worker,
        // docs/extensions-api.md): every read here is a plain field/struct read through BepInEx's
        // ConfigFile, no Unity scene API touched.
        internal static string StateJson(ConfigFile config)
        {
            var sb = new StringBuilder("{");
            for (int i = 0; i < All.Length; i++)
            {
                if (i > 0) sb.Append(',');
                Setting s = All[i];
                sb.Append('"').Append(EscapeJson(s.Key)).Append("\":");
                switch (s.Kind)
                {
                    case Kind.Bool:
                        sb.Append(TryGet(config, s.Key, out bool b) && b ? "true" : "false");
                        break;
                    case Kind.Int:
                        sb.Append(TryGet(config, s.Key, out int i32) ? i32.ToString(CultureInfo.InvariantCulture) : "0");
                        break;
                    case Kind.Float:
                        sb.Append(TryGet(config, s.Key, out float f) ? f.ToString(CultureInfo.InvariantCulture) : "0");
                        break;
                    case Kind.Color:
                        sb.Append('"').Append(TryGet(config, s.Key, out Color c) ? Plugin.Hex(c) : "#000000").Append('"');
                        break;
                }
            }
            return sb.Append('}').ToString();
        }

        // Must run on the Unity main thread (NOXMFD guarantees this for a registered command
        // handler) — setting a ConfigEntry's Value fires VanillaIconsPLUS's own SettingChanged
        // handlers, which for the unit-color entries call into ApplyHUDTints/RefreshHUDIcons/
        // RefreshMapIcons and do touch Unity scene state.
        internal static bool Apply(ConfigFile config, string key, bool boolValue, float numberValue, string hexValue)
        {
            if (!KindByKey.TryGetValue(key, out Kind kind)) return false;
            switch (kind)
            {
                case Kind.Bool:
                    if (!TryGetEntry(config, key, out ConfigEntry<bool>? be)) return false;
                    be!.Value = boolValue;
                    return true;
                case Kind.Int:
                    if (!TryGetEntry(config, key, out ConfigEntry<int>? ie)) return false;
                    ie!.Value = Mathf.RoundToInt(numberValue);
                    return true;
                case Kind.Float:
                    if (!TryGetEntry(config, key, out ConfigEntry<float>? fe)) return false;
                    fe!.Value = numberValue;
                    return true;
                case Kind.Color:
                    if (!TryGetEntry(config, key, out ConfigEntry<Color>? ce)) return false;
                    if (!TryParseHex(hexValue, out Color color)) return false;
                    // ponytail: a plain <input type=color> has no alpha channel, so this preserves
                    // whatever alpha VanillaIconsPLUS already had rather than forcing opaque.
                    // Upgrade path: a 4-channel picker, if a real need for translucent tints shows up.
                    color.a = ce!.Value.a;
                    ce.Value = color;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGet<T>(ConfigFile config, string key, out T value)
        {
            if (TryGetEntry(config, key, out ConfigEntry<T>? entry)) { value = entry!.Value; return true; }
            value = default!;
            return false;
        }

        private static bool TryGetEntry<T>(ConfigFile config, string key, out ConfigEntry<T>? entry)
            => config.TryGetEntry(new ConfigDefinition(Section, key), out entry);

        private static bool TryParseHex(string hex, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(hex)) return false;
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length != 6) return false;
            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r)) return false;
            if (!byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g)) return false;
            if (!byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b)) return false;
            color = new Color(r / 255f, g / 255f, b / 255f, 1f);
            return true;
        }

        private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
