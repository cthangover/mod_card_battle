using System.Collections.Generic;
using Cthangover.Core.Cards;
using Cthangover.Core.Audio;
using Cthangover.Core.Scenes;
using Cthangover.Core.Utils;
using Cthangover.Core.Characters;
using Cthangover.Core.UI;
using Cthangover.Core.UI.CharacterPanel;
using Cthangover.Core.UI.Theming;
using Godot;

using Cthangover.Mods.Interface.Base;
namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Central input handler for the card battle UI rebuilt from the mockup
    /// (mockups/card_battle). Processes mouse clicks and drags: click a party
    /// card to open its action deck in the "Действия" window, drag an action
    /// card onto a valid target (enemy for attacks, ally for support, self for
    /// self-buffs) and execute it on drop. Valid targets are decided by
    /// <see cref="Player.CardActionStrategyFactory"/>; after every executed
    /// action the controller fires <see cref="OnActionExecuted"/> so the core can
    /// end the turn when all action points are spent.
    /// <para/>
    /// Actions are instantiated from <c>scenes/action_card.tscn</c> and parented
    /// into the grid of <see cref="BattleActionsPanel"/>, which owns the empty
    /// state, the point pips and the effect preview well; the controller only
    /// feeds it. Hit-testing uses the card root rects exposed through
    /// <see cref="ICard.GetControlNode"/>.
    /// </summary>
    public partial class CardController : InputHandlerNode
    {
        private CardBattleScreen _screen;
        private BattleCardPanel[] _panels;

        private readonly List<CharacterCardNode> _characterCards = new();
        private readonly List<ActionCardNode> _actionCards = new();

        private CharacterCardNode _selectedCharacter;
        private CharacterCardNode _targetCharacter;
        private ActionCardNode _currentActionCard;

        private bool _isDragging;
        private bool _dragValid;
        private const float DragThreshold = 15f;
        private Vector2 _pressPosition;
        private Node _originalActionCardParent;

        /// <summary>
        /// Uniform scale factor applied to all action cards created by this
        /// controller. Set by the battle core from the viewport fitting
        /// calculation.
        /// </summary>
        public float CardScale { get; set; } = 1f;

        /// <summary>
        /// Fired after a successful drag-and-drop action execution. Subscribed by
        /// the battle core to auto-end the player's turn when all points are spent.
        /// </summary>
        public event System.Action OnActionExecuted;

        /// <summary>
        /// Binds the controller to the assembled battle screen: keeps the panels
        /// for hit-testing and redraws and the actions panel for the deck grid and
        /// the hover preview. Called once by the battle core right after the
        /// screen is added to the battle scene.
        /// </summary>
        public void Setup(CardBattleScreen screen)
        {
            _screen = screen;
            _panels = new[] { screen.PlayerPanel, screen.EnemyPanel };
        }

        /// <summary>Routes mouse input into card selection and target picking for the player turn.</summary>
        protected override void OnInput(InputEvent @event)
        {
            if (Cthangover.Core.Battle.BattleSceneContext.Instance != null &&
                (Cthangover.Core.Battle.BattleSceneContext.Instance.IsWait ||
                 Cthangover.Core.Battle.BattleSceneContext.Instance.IsDestroyed))
            {
                return;
            }

            if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    _pressPosition = mouseButton.GlobalPosition;
                    _isDragging = false;
                    _dragValid = false;
                    OnBeginDrag(mouseButton.GlobalPosition);
                }
                else
                {
                    if (_isDragging && _dragValid)
                        OnEndDrag(mouseButton.GlobalPosition);
                    else
                        OnPointerClick(mouseButton.GlobalPosition);
                    _isDragging = false;
                    _dragValid = false;
                }
            }

            if (@event is InputEventMouseMotion mouseMotion &&
                mouseMotion.ButtonMask.HasFlag(MouseButtonMask.Left) &&
                _dragValid)
            {
                if (!_isDragging && (mouseMotion.GlobalPosition - _pressPosition).Length() > DragThreshold)
                    _isDragging = true;
                if (_isDragging)
                    OnDrag(mouseMotion.GlobalPosition);
            }
        }

        /// <summary>
        /// Clears previous action cards and fades in a fresh deck for
        /// <paramref name="character"/>: one card per battle-usable action, dimmed
        /// when the character cannot pay its cost. Called when a party card is
        /// clicked.
        /// </summary>
        public void ShowActions(CharacterCardNode character)
        {
            if (character?.Card?.Actions == null)
            {
                GameLogger.Log("CARD_CTRL",
                    "ShowActions: NULL actions for character " + (character?.Card?.Name ?? "null"),
                    LogLevel.Error);
                return;
            }

            DiscardActionCharacters();

            var points = character.Card.Attributes.Point.Value;
            foreach (var action in character.Card.Actions)
            {
                if (!action.Type.UseInBattle())
                    continue;

                var cardNode = CreateAction(action, CardScale);
                cardNode.SetDisabled(points < ActionCharacterUi.CostOf(action));
                cardNode.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
                cardNode.Hovered += OnActionCardHovered;
                cardNode.Unhovered += OnActionCardUnhovered;
                _actionCards.Add(cardNode);

                var tween = CreateTween();
                tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
                tween.TweenProperty(cardNode, "modulate:a", 1f, 0.25f);
            }

            _screen.ActionsPanel.RefreshEmpty();
        }

        /// <summary>Fades the current deck out and frees it.</summary>
        public void DiscardActionCharacters()
        {
            if (_actionCards.Count == 0)
                return;

            var oldCards = new List<ActionCardNode>(_actionCards);
            _actionCards.Clear();

            foreach (var card in oldCards)
            {
                if (!GodotObject.IsInstanceValid(card))
                    continue;

                card.MouseFilter = Control.MouseFilterEnum.Ignore;
                card.Hovered -= OnActionCardHovered;
                card.Unhovered -= OnActionCardUnhovered;

                var tween = CreateTween();
                tween.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
                tween.TweenProperty(card, "modulate:a", 0f, 0.15f);
                tween.TweenCallback(Callable.From(card.QueueFree));
            }

            _screen.ActionsPanel.RefreshEmpty();
        }

        /// <summary>
        /// Drops the source and target highlights, optionally resets the actions
        /// window back to its empty state and redraws the panels so the Z-order
        /// returns to normal. Called after each executed action, on empty clicks
        /// and when a drag is cancelled.
        /// </summary>
        public void ClearSelections(bool clearSource = true)
        {
            if (_currentActionCard != null)
            {
                _currentActionCard.Unselect();
                _currentActionCard = null;
            }

            if (_selectedCharacter != null && clearSource)
            {
                _selectedCharacter.Unselect();
                _selectedCharacter = null;
                DiscardActionCharacters();
                _screen.ActionsPanel.ClearSubject();
            }

            ClearTargetMarkers();

            if (_panels != null)
            {
                foreach (var panel in _panels)
                    panel?.Redraw();
            }
        }

        /// <summary>
        /// Removes the targeting frame from the current target and forgets it.
        /// Every path that changes or abandons a target must go through here:
        /// dragging over the gap between cards first nulls the target, and
        /// without the explicit <see cref="CharacterCardNode.Unselect"/> the
        /// discarded card would keep its red or grey marker forever.
        /// </summary>
        private void ClearTargetMarkers()
        {
            if (_targetCharacter != null && GodotObject.IsInstanceValid(_targetCharacter))
                _targetCharacter.Unselect();
            _targetCharacter = null;
        }

        /// <summary>Immediately frees all action card nodes without animation (battle cleanup).</summary>
        public void ClearActions()
        {
            foreach (var card in _actionCards)
                card.QueueFree();
            _actionCards.Clear();
        }

        /// <summary>
        /// Instantiates an <see cref="ActionCardNode"/> inside the actions grid
        /// and fills it from the action definition.
        /// </summary>
        public ActionCardNode CreateAction(ActionCharacter actionCard, float cardScale)
        {
            var card = (ActionCardNode)TscnScenes.LoadAndBuild("scenes/action_card.tscn");
            card.EnsureConstructed();
            _screen.ActionsPanel.Body.AddChild(card);
            card.Scale = new Vector2(cardScale, cardScale);
            card.Init(actionCard);
            return card;
        }

        /// <summary>
        /// Resolves the currently selected action against the selected target
        /// through the matching <see cref="Player.ICardActionStrategy"/>.
        /// Called by <see cref="OnEndDrag"/> when a valid drop is confirmed.
        /// </summary>
        public void DoActionToTarget()
        {
            var strategy = Player.CardActionStrategyFactory.Get(
                _currentActionCard.Card, _selectedCharacter, _targetCharacter);
            strategy?.Execute(_selectedCharacter, _targetCharacter, _currentActionCard.Card);
        }

        private void OnActionCardHovered(ActionCardNode card)
            => _screen.ActionsPanel.SetDescription(card.DescribeFrom(_selectedCharacter?.Card));

        private void OnActionCardUnhovered(ActionCardNode card)
            => _screen.ActionsPanel.SetDescriptionDefault();

        private void OnPointerClick(Vector2 screenPosition)
        {
            var source = TryFindCardByPosition(screenPosition, CollectCharacters());

            if (source == null || !source.IsPlayer || source.IsDead)
            {
                ClearSelections();
                return;
            }

            if (source != _selectedCharacter && _selectedCharacter != null)
                _selectedCharacter.Unselect();

            var audioService = SceneContextNode.Instance?.GetSceneRoot<Node>("AudioService") as IAudioService;
            audioService?.PlaySound("battle/card_show_actions", SoundType.CardEffect);

            _selectedCharacter = source;
            _screen.ActionsPanel.SetSubject(TranslationServer.Translate(source.Card.Name).ToString());
            _screen.ActionsPanel.SetPoints(source.Card.Attributes.Point.Value, source.Card.Attributes.Point.BaseValue);
            ShowActions(_selectedCharacter);
            _selectedCharacter.Select();
        }

        private void OnBeginDrag(Vector2 screenPosition)
        {
            var source = TryFindCardByPosition(screenPosition, _actionCards);

            if (source == null)
                return;

            if (source.IsDisabled)
            {
                BattleLogHub.Log(
                    string.Format(Translate("battle/log_not_enough_points"), ActionCharacterUi.Translate(source.Card.Name)),
                    BattleLogKind.Status);
                return;
            }

            if (_currentActionCard != null && source != _currentActionCard)
                _currentActionCard.Unselect();

            _dragValid = true;
            var audioService = SceneContextNode.Instance?.GetSceneRoot<Node>("AudioService") as IAudioService;
            audioService?.PlaySound("battle/card_pick", 4, SoundType.CardEffect);

            _currentActionCard = source;
            var control = _currentActionCard.GetControlNode();
            _originalActionCardParent = control.GetParent();
            var globalPos = control.GlobalPosition;
            // Отдельный CanvasLayer поверх всего боя: иначе карта рисуется
            // под ветками панелей врагов и союзников.
            control.Reparent(_screen.DragLayer);
            control.GlobalPosition = globalPos;

            var pickTween = CreateTween();
            pickTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            pickTween.TweenProperty(control, "scale", new Vector2(1.1f, 1.1f), 0.1f);

            foreach (var card in _actionCards)
            {
                if (card != _currentActionCard && GodotObject.IsInstanceValid(card))
                    card.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
        }

        private void OnDrag(Vector2 screenPosition)
        {
            if (_currentActionCard == null)
                return;

            var cardControl = _currentActionCard.GetControlNode();
            cardControl.GlobalPosition = screenPosition - cardControl.Size / 2;

            var target = TryFindCardByRect(cardControl, _characterCards);
            if (target == _targetCharacter)
                return;

            ClearTargetMarkers();

            if (target == null)
            {
                _currentActionCard.Unselect();
                return;
            }

            _targetCharacter = target;
            var strategy = Player.CardActionStrategyFactory.Get(
                _currentActionCard.Card, _selectedCharacter, target);

            if (strategy != null && strategy.Check(_currentActionCard.Card, _selectedCharacter, target))
            {
                strategy.HighlightTarget(_currentActionCard, _targetCharacter);
            }
            else
            {
                _targetCharacter.Invalid();
                _currentActionCard.Invalid();
            }
        }

        private void OnEndDrag(Vector2 screenPosition)
        {
            foreach (var card in _actionCards)
            {
                if (card != null && GodotObject.IsInstanceValid(card))
                    card.MouseFilter = Control.MouseFilterEnum.Stop;
            }

            if (_selectedCharacter != null && _currentActionCard != null && _targetCharacter != null)
            {
                var strategy = Player.CardActionStrategyFactory.Get(
                    _currentActionCard.Card, _selectedCharacter, _targetCharacter);

                if (strategy != null && strategy.Check(_currentActionCard.Card, _selectedCharacter, _targetCharacter))
                {
                    if (_originalActionCardParent != null)
                    {
                        _currentActionCard.GetControlNode().Reparent(_originalActionCardParent);
                        _originalActionCardParent = null;
                    }

                    var audioService = SceneContextNode.Instance?.GetSceneRoot<Node>("AudioService") as IAudioService;
                    audioService?.PlaySound("battle/card_drop", 4, SoundType.CardEffect);

                    DoActionToTarget();

                    if (_panels != null)
                    {
                        foreach (var panel in _panels)
                            panel?.Redraw();
                    }

                    ClearSelections();
                    OnActionExecuted?.Invoke();
                    return;
                }
            }

            if (_currentActionCard != null)
            {
                var control = _currentActionCard.GetControlNode();
                if (_originalActionCardParent != null)
                {
                    control.Reparent(_originalActionCardParent);
                    _originalActionCardParent = null;
                }
                var resetTween = CreateTween();
                resetTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
                resetTween.TweenProperty(control, "scale", Vector2.One, 0.15f);

                BattleLogHub.Log(
                    string.Format(Translate("battle/log_invalid_target"), ActionCharacterUi.Translate(_currentActionCard.Card.Name)),
                    BattleLogKind.Status);
            }

            ClearSelections(false);
        }

        private T TryFindCardByPosition<T>(Vector2 screenPosition, IList<T> cards) where T : class, ICard
        {
            foreach (var card in cards)
            {
                if (card == null || !GodotObject.IsInstanceValid(card.GetControlNode()))
                    continue;
                var rect = card.GetControlNode();
                if (rect.GetGlobalRect().HasPoint(screenPosition))
                    return card;
            }
            return null;
        }

        private T TryFindCardByRect<T>(Control draggingRect, IList<T> cards) where T : class, ICard
        {
            var screenPoint = draggingRect.GlobalPosition + draggingRect.Size / 2;
            foreach (var card in cards)
            {
                if (card == null || !GodotObject.IsInstanceValid(card.GetControlNode()))
                    continue;
                var rect = card.GetControlNode();
                if (rect.GetGlobalRect().HasPoint(screenPoint))
                    return card;
            }
            return null;
        }

        private List<CharacterCardNode> CollectCharacters()
        {
            _characterCards.Clear();
            foreach (var panel in _panels)
            {
                if (panel == null)
                    continue;
                foreach (var card in panel.CardList)
                    _characterCards.Add(card);
            }
            return _characterCards;
        }

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();
    }
}
