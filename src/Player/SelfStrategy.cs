using Cthangover.CardBattle.UI;
using Cthangover.Core.Battle;
using Cthangover.Core.Battle.Actions;
using Cthangover.Core.UI.Theming;
using Cthangover.TurnBattle;
using Cthangover.Core.Characters;
using Godot;

namespace Cthangover.CardBattle.Player
{
    /// <summary>
    /// Strategy for self-targeting actions (<c>ToSelf</c>). Validates that the
    /// source and target are the same card, highlights with a green selection
    /// outline and applies the effect to the caster. Shield gains print a
    /// floating blue number and a journal row; the card pulses to make the buff
    /// readable without reading the log.
    /// </summary>
    public class SelfStrategy : ICardActionStrategy
    {
        /// <summary>Returns <c>true</c> only when <paramref name="source"/> and <paramref name="target"/> are the same card.</summary>
        public bool Check(ActionCharacter action, CharacterCardNode source, CharacterCardNode target)
        {
            return source != null && source == target;
        }

        /// <summary>Shows a green selection overlay on both the dragged action card and the source card.</summary>
        public void HighlightTarget(ActionCardNode actionCard, CharacterCardNode target)
        {
            actionCard.Select();
            target.Select();
        }

        /// <summary>Executes the self-targeted action and reports shields and healing.</summary>
        public void Execute(CharacterCardNode source, CharacterCardNode target, ActionCharacter action)
        {
            if (source?.Card == null || action == null)
                return;

            var result = ActionExecutorHub.Instance.Execute(action, source.Card, source.Card);
            source.UpdateInfo();

            if (!result.Result)
                return;

            if (result.Target.Defence > 0)
            {
                ShowDamageBehaviour.SpawnDefence(result.Target.Defence, source, source.GlobalPosition);
                source.Flash(UiTokens.Info);
                BattleLogHub.Log(
                    string.Format(Translate("battle/log_shield_gain"), Translate(source.Card.Name), result.Target.Defence),
                    BattleLogKind.Buff);
            }

            if (result.Target.Damage > 0)
                ShowDamageBehaviour.SpawnDamage(result.Target.Damage, source, source.GlobalPosition, true);
        }

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();
    }
}
