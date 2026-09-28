using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cthangover.Core.Battle;
using Cthangover.Core.Battle.Actions;
using Cthangover.Core.Cards.StatusEffect;
using Cthangover.Core.Scenes;
using Cthangover.Core.UI.CharacterPanel;
using Cthangover.Core.Utils;
using Cthangover.Core.Characters;
using Cthangover.CardBattle.Player;
using Cthangover.CardBattle.UI;
using Godot;

namespace Cthangover.CardBattle
{
    /// <summary>
    /// Core orchestrator for the card-based turn battle system. Owns the full
    /// battle lifecycle: it instantiates the UI screen rebuilt from the mockup
    /// (mockups/card_battle, defined in <c>scenes/card_battle_screen.tscn</c>),
    /// cycles between player and enemy turns, drives the animated enemy actions
    /// through <see cref="AbstractBattleAction"/>, applies status effects each
    /// turn and detects win/loss when one side loses all cards.
    /// <para/>
    /// The core talks to the screen through its assembled widgets
    /// (<see cref="CardBattleScreen.Hud"/>, <see cref="BattleLogPanel"/>,
    /// <see cref="BattleBanner"/>, panels) instead of walking the scene tree, and
    /// publishes journal rows via <see cref="BattleLogHub"/> so other battle code
    /// can log without a UI reference. Implements <see cref="IBattleCore"/> so the
    /// battle engine can select this mod as the active battle type.
    /// </summary>
    public class CardBattleCore : IBattleCore, IBattleAutoPlayable
    {
        private const int PlayerCardLimit = 4;
        private const float BaseCardWidth = 230f;
        private const float BaseCardHeight = 420f;
        private const float CellOffset = 10f;

        private Character[] _playerChars;
        private Character[] _enemyChars;
        private IBattleContext _ctx;

        private CardBattleScreen _screen;
        private BattleCardPanel _playerPanel;
        private BattleCardPanel _enemyPanel;
        private CardController _cardController;
        private EndTurnButton _endTurnButton;

        private BattleSide _currentOrder;
        private bool _playerTurnActive;
        private int _round;

        /// <summary>Unique identifier for this battle core, used by the battle engine to select the card battle mod.</summary>
        public string Id => "card_battle";

        /// <summary>
        /// Provides <see cref="IActionExecutor"/> instances for the three card
        /// action types (physics/attack, physics/defence, physics/stun) used
        /// during action resolution.
        /// </summary>
        public IActionExecutorProvider ActionProvider { get; } = new Actions.CardBattleActionProvider();

        /// <summary>
        /// Instantiates the battle screen from <c>scenes/card_battle_screen.tscn</c>,
        /// attaches it to the battle scene's <c>Panel</c> and binds the panels,
        /// end-turn button and input controller. Enemy characters are deep-copied
        /// so defeats are tracked independently per battle instance. The
        /// <see cref="CardBattleActionProvider"/> is registered with
        /// <see cref="ActionExecutorHub"/>.
        /// </summary>
        public void Init(Character[] playerChars, Character[] enemyChars, IBattleContext ctx)
        {
            _playerChars = playerChars;
            _ctx = ctx;

            _enemyChars = new Character[enemyChars.Length];
            for (var i = 0; i < enemyChars.Length; i++)
                _enemyChars[i] = enemyChars[i]?.Copy() ?? enemyChars[i];

            ActionExecutorHub.Instance.SetActiveProvider(ActionProvider);

            var root = ctx.RootNode as Node;
            var panel = root.GetNodeOrNull<Control>("Panel") ?? root;

            _screen = (CardBattleScreen)TscnScenes.LoadAndBuild("scenes/card_battle_screen.tscn");
            _screen.Name = "CardBattleScreen";
            _screen.EnsureConstructed();
            panel.AddChild(_screen);

            _playerPanel = _screen.PlayerPanel;
            _enemyPanel = _screen.EnemyPanel;

            _endTurnButton = _screen.EndTurnButton;
            _endTurnButton.SetVisible(false);
            _endTurnButton.OnPressed += OnEndTurnPressed;

            _cardController = _screen.Controller;
            _cardController.Setup(_screen);
        }

        /// <summary>
        /// Calculates the pointer scale fitting both sides on screen, initializes
        /// every card and starts the first player turn. Subscribes to
        /// <c>OnCardDead</c> to record defeated enemies for the victory screen and
        /// to <c>OnActionExecuted</c> to detect automatic turn completion.
        /// </summary>
        public void Start()
        {
            var enemyCount = _enemyChars?.Length ?? 1;

            _enemyPanel.UseRows = enemyCount > 6;

            var cardScale = CalculateCardScale(enemyCount);
            _cardController.CardScale = cardScale;

            _playerPanel.Init(_playerChars, true, cardScale);
            _enemyPanel.Init(_enemyChars, false, cardScale);

            _enemyPanel.OnCardDead += OnEnemyCardDead;
            _cardController.OnActionExecuted += CheckPlayerTurnEnd;

            BattleSceneContext.Instance.OnBattleCleared += OnBattleCleared;

            _round = 0;
            _currentOrder = BattleSide.Player;
            StartPlayerTurn();
        }

        private float CalculateCardScale(int enemyCount)
        {
            var viewport = ((Node)_ctx.RootNode).GetViewport().GetVisibleRect().Size;
            var enemyPanelWidth = viewport.X * 0.96f;
            var playerPanelWidth = viewport.X * 0.55f;
            var enemyPanelHeight = viewport.Y * 0.446f;

            float maxCardScale;
            if (_enemyPanel.UseRows)
            {
                var maxPerRow = _enemyPanel.GetMaxCardsPerRow();
                var rows = _enemyPanel.GetCurrentRowCount(enemyCount);

                var widthScale = (enemyPanelWidth - (maxPerRow - 1) * CellOffset) / (maxPerRow * BaseCardWidth);
                var heightScale = (enemyPanelHeight - (rows - 1) * CellOffset) / (rows * BaseCardHeight);
                maxCardScale = Mathf.Max(0.1f, Mathf.Min(widthScale, heightScale));
            }
            else
            {
                var effectiveCount = Mathf.Max(enemyCount, 1);
                maxCardScale = Mathf.Max(0.1f,
                    (enemyPanelWidth - (effectiveCount - 1) * CellOffset) / (effectiveCount * BaseCardWidth));
            }

            var playerFitScale = Mathf.Max(0.1f,
                (playerPanelWidth - (PlayerCardLimit - 1) * CellOffset) / (PlayerCardLimit * BaseCardWidth));

            return Mathf.Min(Mathf.Min(maxCardScale, playerFitScale), 1f);
        }

        private void OnEnemyCardDead(CharacterCardNode card)
        {
            if (card?.Card == null)
                return;

            var ctx = BattleSceneContext.Instance;
            if (ctx != null)
            {
                ctx.RecordEnemyDefeated(card.Card);
                ctx.NotifyCharacterDied(card.Card);
            }

            _screen.Hud.SetEnemiesAlive(CountAlive(_enemyPanel));
            BattleLogHub.Log(string.Format(Translate("battle/log_dead"), card.Card.Name), BattleLogKind.Dead);
        }

        private void StartPlayerTurn()
        {
            _round += 1;
            _currentOrder = BattleSide.Player;
            _playerTurnActive = true;
            BattleSceneContext.Instance.IsWait = false;

            _cardController.ClearSelections();
            _cardController.ClearActions();
            _screen.ActionsPanel.ClearBody();
            _screen.ActionsPanel.ClearSubject();

            _endTurnButton?.SetVisible(true);

            foreach (var card in _playerPanel.CardList)
            {
                StatusEffectService.Of(card.Card).OnTurnStart();
                card.Card.Attributes.Point.Value = card.Card.Attributes.Point.BaseValue;
                card.UpdateInfo();
            }

            _screen.Hud.SetTurn(true);
            _screen.Hud.SetRound(_round);
            _screen.Hud.SetEnemiesAlive(CountAlive(_enemyPanel));
            _screen.LogPanel.SetRound(_round);
            _screen.Banner.Show(Translate("battle/turn_player"));
            BattleLogHub.Log(
                Translate("battle/log_turn_player") + " · " + string.Format(Translate("battle/log_round"), _round),
                BattleLogKind.Turn);
        }

        private void OnEndTurnPressed() => OnPlayerTurnEnd();

        private void CheckPlayerTurnEnd()
        {
            if (_currentOrder != BattleSide.Player)
                return;

            if (!HasAliveCards(_enemyPanel))
            {
                _ctx.EndBattle(BattleSide.Player);
                return;
            }

            foreach (var card in _playerPanel.CardList)
            {
                if (!card.IsDead && card.Card?.Attributes?.Point?.Value > 0)
                    return;
            }

            BattleLogHub.Log(Translate("battle/log_out_of_points"), BattleLogKind.Info);
            OnPlayerTurnEnd();
        }

        /// <summary>
        /// Ends the player's turn: hides the end-turn button, pauses the battle,
        /// plays the enemy-turn banner and journal line, waits for two frames plus
        /// a short pause so the UI settles, then runs the enemy AI. Ends the battle
        /// immediately when no enemies remain.
        /// </summary>
        public async void OnPlayerTurnEnd()
        {
            _playerTurnActive = false;
            _endTurnButton?.SetVisible(false);
            BattleSceneContext.Instance.IsWait = true;

            if (!HasAliveCards(_enemyPanel))
            {
                _ctx.EndBattle(BattleSide.Player);
                return;
            }

            _screen.Hud.SetTurn(false);
            _screen.Banner.Show(Translate("battle/turn_enemy"));
            BattleLogHub.Log(Translate("battle/log_turn_enemy"), BattleLogKind.Turn);

            var root = (Node)_ctx.RootNode;
            await root.ToSignal(root.GetTree(), "process_frame");
            await root.ToSignal(root.GetTree().CreateTimer(0.5f), "timeout");

            await RunEnemyTurn();
        }

        private async Task RunEnemyTurn()
        {
            _currentOrder = BattleSide.Enemy;
            _endTurnButton?.SetVisible(false);

            var root = (Node)_ctx.RootNode;

            foreach (var card in _enemyPanel.CardList.ToList())
            {
                if (card.IsDead || card.Card?.Actions == null || card.Card.Actions.Count == 0)
                    continue;

                StatusEffectService.Of(card.Card).OnTurnStart();

                if (StatusEffectService.Of(card.Card).HasStun())
                {
                    card.UpdateInfo();
                    card.PlayZap();
                    BattleLogHub.Log(
                        string.Format(Translate("battle/log_stun_skip"), card.Card.Name),
                        BattleLogKind.Status);
                    continue;
                }

                card.Card.Attributes.Point.Value = card.Card.Attributes.Point.BaseValue;

                var action = PickEnemyAction(card);
                if (action == null)
                    continue;

                var target = PickEnemyTarget(card, action);
                if (target == null)
                    continue;

                card.UpdateInfo();

                if (action.Type == ActionCharacterType.ToEnemy)
                {
                    BattleLogHub.Log(
                        string.Format(Translate("battle/log_enemy_action"),
                            card.Card.Name,
                            ActionCharacterUi.Translate(action.Name),
                            target.Card.Name),
                        BattleLogKind.Info);
                }

                var animatedAction = CreateAnimatedAction(card, target, action);
                if (animatedAction != null)
                {
                    animatedAction.DoStart();
                    while (!animatedAction.DoAction())
                        await root.ToSignal(root.GetTree(), "process_frame");
                    animatedAction.DoEnd();
                }

                await root.ToSignal(root.GetTree().CreateTimer(0.2f), "timeout");

                if (!HasAliveCards(_playerPanel))
                {
                    _ctx.EndBattle(BattleSide.Enemy);
                    return;
                }
            }

            if (!HasAliveCards(_playerPanel))
            {
                _ctx.EndBattle(BattleSide.Enemy);
                return;
            }

            StartPlayerTurn();
        }

        private ActionCharacter PickEnemyAction(CharacterCardNode card)
        {
            var actions = card.Card.Actions;
            if (actions == null || actions.Count == 0)
                return null;

            return actions[GD.RandRange(0, actions.Count - 1)];
        }

        private CharacterCardNode PickEnemyTarget(CharacterCardNode source, ActionCharacter action)
        {
            if (action.Type == ActionCharacterType.ToEnemy)
            {
                var alivePlayers = _playerPanel.CardList
                    .Where(c => !c.IsDead && c.Card?.Attributes?.Health?.Value > 0)
                    .ToList();

                if (alivePlayers.Count == 0)
                    return null;
                return alivePlayers[GD.RandRange(0, alivePlayers.Count - 1)];
            }

            if (action.Type == ActionCharacterType.ToAlias || action.Type == ActionCharacterType.ToSelf)
                return source;

            GameLogger.Log("CARD_BATTLE", $"PickEnemyTarget: unsupported action type {action.Type}", LogLevel.Warning);
            return null;
        }

        private Actions.AbstractBattleAction CreateAnimatedAction(
            CharacterCardNode source, CharacterCardNode target, ActionCharacter action)
        {
            if (action.Type == ActionCharacterType.ToEnemy)
                return new Actions.PhysicsAttackAction(source, target, action);

            if (action.Type == ActionCharacterType.ToAlias || action.Type == ActionCharacterType.ToSelf)
                return new Actions.PhysicsDefenceAction(source, target, action);

            GameLogger.Log("CARD_BATTLE", $"CreateAnimatedAction: unsupported action type {action.Type}", LogLevel.Warning);
            return null;
        }

        private static bool HasAliveCards(BattleCardPanel panel)
        {
            foreach (var card in panel.CardList)
            {
                if (!card.IsDead && card.Card?.Attributes?.Health?.Value > 0)
                    return true;
            }
            return false;
        }

        private static int CountAlive(BattleCardPanel panel)
        {
            var alive = 0;
            foreach (var card in panel.CardList)
            {
                if (!card.IsDead && card.Card?.Attributes?.Health?.Value > 0)
                    alive++;
            }
            return alive;
        }

        private void OnBattleCleared()
        {
            _screen?.Clear();
        }

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();

        /// <summary>
        /// <c>true</c> between <see cref="StartPlayerTurn"/> and the start of
        /// <see cref="OnPlayerTurnEnd"/> — matches the visibility window of the
        /// end-turn button, so automation presses exactly one turn per window.
        /// </summary>
        public bool IsWaitingForPlayerInput => _playerTurnActive;

        /// <summary>Automation hook: ends the player's turn without selecting cards.</summary>
        public void AdvancePlayerTurn() => OnPlayerTurnEnd();
    }
}
