using UnityEngine;

namespace VanillaIconsPlusBridge
{
    // Polls GameAssets' base faction tints (docs/vanilla-icons-plus-extension.md — the "1." case
    // in Plugin.cs's header comment) and pushes them to NOXMFD.Api whenever they change. Polling,
    // not an event subscription, because VanillaIconsPLUS keeps these on private ConfigEntry<Color>
    // fields with no externally-reachable change notification.
    //
    // ponytail: a 1 s poll means up to 1 s of staleness after a live ConfigurationManager edit —
    // acceptable for a cosmetic tint, and unlikely to be noticed against a per-frame HUD read
    // anyway. Upgrade path if that ever matters: drop this poll entirely if VanillaIconsPLUS ever
    // exposes its own friendly/enemy/neutral ConfigEntrys publicly, and subscribe to their
    // SettingChanged the same way Plugin.cs already does for AAUnitsHUD/SpecialAAUnitsHUD.
    internal class Worker : MonoBehaviour
    {
        private const float PollIntervalSeconds = 1f;
        private float _nextPollAt;

        private string? _lastFriendly;
        private string? _lastEnemy;
        private string? _lastNeutral;

        private void Update()
        {
            if (Time.unscaledTime < _nextPollAt) return;
            _nextPollAt = Time.unscaledTime + PollIntervalSeconds;

            GameAssets ga = GameAssets.i;
            if (ga == null) return;

            string friendly = Plugin.Hex(ga.HUDFriendly);
            string enemy = Plugin.Hex(ga.HUDHostile);
            string neutral = Plugin.Hex(ga.HUDNeutral);
            if (friendly == _lastFriendly && enemy == _lastEnemy && neutral == _lastNeutral) return;

            _lastFriendly = friendly;
            _lastEnemy = enemy;
            _lastNeutral = neutral;
            NOXMFD.Api.SetFactionColorOverride(friendly, enemy, neutral);
        }
    }
}
