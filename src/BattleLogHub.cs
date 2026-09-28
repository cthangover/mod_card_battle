using System;
using Cthangover.CardBattle.UI;

namespace Cthangover.CardBattle
{
    /// <summary>
    /// Static sink for battle journal entries. Player action strategies and
    /// the battle core run without a reference to the UI screen, so they
    /// publish here instead of reaching into the scene tree; the
    /// <see cref="BattleLogPanel"/> subscribes while it exists and unsubscribes
    /// on tree exit, which keeps the hub empty outside of a battle. This is the
    /// same service-locator pattern the battle code already uses for
    /// <c>ActionExecutorHub</c> and <c>ShowDamageBehaviour</c>.
    /// </summary>
    public static class BattleLogHub
    {
        /// <summary>Fired for every entry; handlers receive the text and its kind.</summary>
        public static event Action<string, BattleLogKind> Entry;

        /// <summary>Publishes one journal row. No-op when no battle UI is listening.</summary>
        public static void Log(string text, BattleLogKind kind = BattleLogKind.Info)
            => Entry?.Invoke(text, kind);
    }
}
