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
    /// Strategy for <c>ToEnemy</c> actions. Validates that the target is an enemy,
    /// highlights both cards with a red attack outline, executes the action via
    /// <see cref="ActionExecutorHub"/> and reports the outcome: floating damage /
    /// defence-loss numbers on the target, a colour pulse on the card and a
    /// journal row through <see cref="BattleLogHub"/>. Stun actions (those with a
    /// "Turn" above 1) additionally flash the electric zap over the target
    /// instead of printing damage, matching the mockup's stun feedback.
    /// </summary>
    public class AttackToEnemyStrategy : ICardActionStrategy
    {
        /// <summary>Returns <c>true</c> if the action type is <c>ToEnemy</c> and the target is an enemy card.</summary>
        public bool Check(ActionCharacter action, CharacterCardNode source, CharacterCardNode target)
        {
            return action.Type == ActionCharacterType.ToEnemy && target != null && !target.IsPlayer;
        }

        /// <summary>Shows a red attack overlay on both the dragged action card and the target enemy card.</summary>
        public void HighlightTarget(ActionCardNode actionCard, CharacterCardNode target)
        {
            actionCard.Attack();
            target.Attack();
        }

        /// <summary>
        /// Resolves the attack and publishes its result: damage and absorbed
        /// shield as floating numbers plus journal rows, or the stun effect with
        /// its zap flash for control actions.
        /// </summary>
        public void Execute(CharacterCardNode source, CharacterCardNode target, ActionCharacter action)
        {
            if (source?.Card == null || target?.Card == null || action == null)
                return;

            var result = ActionExecutorHub.Instance.Execute(action, source.Card, target.Card);
            source.UpdateInfo();
            target.UpdateInfo();

            if (!result.Result)
                return;

            var actionName = Translate(action.Name);
            var sourceName = Translate(source.Card.Name);
            var targetName = Translate(target.Card.Name);
            var isStun = action.GetInt("Turn", 0) > 1;

            var defenceLost = -result.Target.Defence;
            if (defenceLost > 0)
            {
                ShowDamageBehaviour.SpawnDefence(defenceLost, target, target.GlobalPosition);
                BattleLogHub.Log(
                    string.Format(Translate("battle/log_shield_absorb"), targetName, defenceLost),
                    BattleLogKind.Shield);
            }

            if (isStun)
            {
                target.PlayZap();
                BattleLogHub.Log(
                    string.Format(Translate("battle/log_stun"), targetName, action.GetInt("Turn", 0)),
                    BattleLogKind.Status);
                return;
            }

            if (result.Target.Damage > 0)
            {
                ShowDamageBehaviour.SpawnDamage(result.Target.Damage, target, target.GlobalPosition);
                target.Flash(UiTokens.DangerText);
                BattleLogHub.Log(
                    string.Format(Translate("battle/log_damage"),
                        actionName, sourceName, targetName, result.Target.Damage),
                    BattleLogKind.Damage);
            }
            else if (defenceLost == 0)
            {
                BattleLogHub.Log(
                    string.Format(Translate("battle/log_absorbed"), actionName, sourceName, targetName),
                    BattleLogKind.Shield);
            }
        }

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();
    }
}
