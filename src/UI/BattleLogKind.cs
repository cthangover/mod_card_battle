namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Semantic category of a battle journal row, mirrored from the mockup's
    /// colour scheme (mockups/card_battle). The panel maps each kind to a
    /// left border accent, a background tint and a text colour, so callers
    /// only decide what happened, never how it is painted.
    /// </summary>
    public enum BattleLogKind
    {
        /// <summary>Neutral note: enemy intent, hints, odds and ends.</summary>
        Info,
        /// <summary>Turn or battle boundary (gold, highlighted row).</summary>
        Turn,
        /// <summary>Health loss (red).</summary>
        Damage,
        /// <summary>Shield gain or absorption (blue).</summary>
        Shield,
        /// <summary>Support buff (green).</summary>
        Buff,
        /// <summary>Status effect such as stun (warm).</summary>
        Status,
        /// <summary>Character defeat (faint).</summary>
        Dead,
    }
}
