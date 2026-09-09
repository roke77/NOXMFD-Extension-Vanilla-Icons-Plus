using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VanillaIconsPlusBridge
{
    // Mirrors NO-VanillaIconsPLUS's unit colors onto NOXMFD's own MAP page icons — see NOXMFD's
    // docs/vanilla-icons-plus-extension.md for the full design. Two independent color sources:
    //
    // 1. The three base faction tints (GameAssets.HUDFriendly/HUDHostile/HUDNeutral) — VanillaIconsPLUS
    //    keeps these live via its own private ConfigEntry<Color> fields, which this plugin cannot
    //    reach or subscribe to. Worker polls GameAssets directly instead (see Worker below).
    // 2. VanillaIconsPLUS's enemy-only AA/Special AA unit-type tint (AAUnitsHUD/SpecialAAUnitsHUD,
    //    both public ConfigEntry<Color>) — pushed to NOXMFD.Api.SetUnitTypeColorOverride for every
    //    unit type name in AAUnitHelper's public whitelists, refreshed on SettingChanged.
    [BepInPlugin(Guid, "NOXMFD: Vanilla Icons Plus Bridge", MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.roque.NOXMFD", "0.46.0")]
    [BepInDependency("com.hellcat92.vanillaiconsplus", "1.5.5")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.roque.vanilla-icons-plus-bridge";
        private const int FactionEnemy = 2;   // matches NOXMFD's own faction ints (0 neutral/1 friendly/2 enemy)

        internal static ManualLogSource? Log;

        private ConfigEntry<Color>? _aaEntry;
        private ConfigEntry<Color>? _specialAaEntry;

        private void Awake()
        {
            Log = Logger;

            // VanillaIconsPLUS's own Instance singleton is internal to its assembly — find its
            // component directly instead. This runs during BepInEx chainloading, before the boot
            // -> MainMenu transition that kills the shared manager GameObject (docs/vanilla-icons-
            // plus-extension.md, docs/bepinex-configurationmanager.md), so the component is alive
            // here regardless of what happens to it afterward. ConfigEntry<T> is a plain C# object,
            // not a UnityEngine.Object, so holding onto these two references survives that
            // transition even though the component they came from may not.
            var vip = FindObjectOfType<VanillaIconsPLUS.Plugin>();
            if (vip == null)
            {
                Logger.LogWarning("VanillaIconsPLUS component not found at Awake — nothing to mirror this session.");
                return;
            }

            _aaEntry = vip.AAUnitsHUD;
            _specialAaEntry = vip.SpecialAAUnitsHUD;

            PushAaOverrides();
            _aaEntry.SettingChanged += (_, __) => PushAaOverrides();
            _specialAaEntry.SettingChanged += (_, __) => PushAaOverrides();

            // The base faction tints need a live Update() loop (Worker, below), which itself needs
            // to survive the same transition — spawn it fresh once a real scene exists, same
            // pattern NOXMFD's own Plugin.cs uses for its runtime.
            SceneManager.sceneLoaded += SpawnWorkerOnce;

            Logger.LogInfo("Ready — mirroring VanillaIconsPLUS colors onto NOXMFD's MAP page.");
        }

        private static void SpawnWorkerOnce(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= SpawnWorkerOnce;
            var go = new GameObject("VanillaIconsPlusBridgeWorker");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Worker>();
        }

        // Re-pushed on every AAUnitsHUD/SpecialAAUnitsHUD SettingChanged (e.g. a live edit through
        // ConfigurationManager). The whitelist itself (AAUnitHelper.AAUnitNames/SpecialAAUnitNames)
        // only changes via VanillaIconsPLUS's own AA_Whitelist.cfg, read once at its Awake, so
        // re-reading it here every time is cheap and always current for the session.
        private void PushAaOverrides()
        {
            if (_aaEntry == null || _specialAaEntry == null) return;

            string aaHex = Hex(_aaEntry.Value);
            foreach (string type in VanillaIconsPLUS.AAUnitHelper.AAUnitNames)
                NOXMFD.Api.SetUnitTypeColorOverride(type, aaHex, FactionEnemy);

            string specialHex = Hex(_specialAaEntry.Value);
            foreach (string type in VanillaIconsPLUS.AAUnitHelper.SpecialAAUnitNames)
                NOXMFD.Api.SetUnitTypeColorOverride(type, specialHex, FactionEnemy);
        }

        internal static string Hex(Color c)
        {
            int r = Mathf.Clamp((int)(c.r * 255f + 0.5f), 0, 255);
            int g = Mathf.Clamp((int)(c.g * 255f + 0.5f), 0, 255);
            int b = Mathf.Clamp((int)(c.b * 255f + 0.5f), 0, 255);
            return $"#{r:x2}{g:x2}{b:x2}";
        }
    }
}
