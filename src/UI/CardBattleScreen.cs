using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Root of the card battle interface rebuilt from the mockup
    /// (mockups/card_battle). The whole layout lives in
    /// <c>scenes/card_battle_screen.tscn</c> and is composed of the mockup's
    /// parts: the HUD window, the enemy and party card panels, the "Действия"
    /// window, the battle journal, the turn banner and the end-turn button.
    /// This class is the single assembly point: it constructs every child widget
    /// once (mod widgets are not auto-constructed by Godot) and exposes them to
    /// the battle core and the input controller, so neither has to walk the
    /// scene tree by hand.
    /// </summary>
    public partial class CardBattleScreen : ModWidget
    {
        /// <summary>Top-left window with the phase badge, round and enemy count.</summary>
        public BattleHud Hud { get; private set; }

        /// <summary>Panel that manages the enemy card row(s); aligned right.</summary>
        public BattleCardPanel EnemyPanel { get; private set; }

        /// <summary>Panel that manages the party card row; aligned left.</summary>
        public BattleCardPanel PlayerPanel { get; private set; }

        /// <summary>Lower-right "Действия" window with the action card grid.</summary>
        public BattleActionsPanel ActionsPanel { get; private set; }

        /// <summary>Battle journal window that renders <see cref="BattleLogHub"/> entries.</summary>
        public BattleLogPanel LogPanel { get; private set; }

        /// <summary>Centre-screen turn banner.</summary>
        public BattleBanner Banner { get; private set; }

        /// <summary>End-turn button in the top-right corner, next to the battle speed bar.</summary>
        public EndTurnButton EndTurnButton { get; private set; }

        /// <summary>Global input handler that drives selection and drag-and-drop.</summary>
        public CardController Controller { get; private set; }

        /// <summary>
        /// Canvas layer above the whole battle UI (layer 100). The dragged action
        /// card is temporarily reparented here, so it always renders over enemy
        /// and party cards regardless of their Z-index inside the panels.
        /// </summary>
        public CanvasLayer DragLayer { get; private set; }

        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            Hud = GetNode<BattleHud>("Hud");
            Hud.EnsureConstructed();

            EnemyPanel = GetNode<BattleCardPanel>("EnemyPanel");
            EnemyPanel.EnsureConstructed();
            EnemyPanel.AlignType = "Right";

            PlayerPanel = GetNode<BattleCardPanel>("PlayerPanel");
            PlayerPanel.EnsureConstructed();

            ActionsPanel = GetNode<BattleActionsPanel>("ActionsPanel");
            ActionsPanel.EnsureConstructed();

            LogPanel = GetNode<BattleLogPanel>("LogPanel");
            LogPanel.EnsureConstructed();

            Banner = GetNode<BattleBanner>("Banner");
            Banner.EnsureConstructed();

            EndTurnButton = GetNode<EndTurnButton>("EndTurnButton");
            EndTurnButton.EnsureConstructed();

            Controller = GetNode<CardController>("CardController");

            DragLayer = GetNode<CanvasLayer>("DragLayer");
        }

        /// <summary>
        /// Releases every widget when the battle scene is cleared: cards, action
        /// deck, journal rows, banner and the end-turn button. Called by the
        /// battle core on <c>OnBattleCleared</c>.
        /// </summary>
        public void Clear()
        {
            EnemyPanel.ClearAll();
            PlayerPanel.ClearAll();
            ActionsPanel.ClearBody();
            ActionsPanel.ClearSubject();
            LogPanel.Clear();
            Banner.Clear();
            EndTurnButton.SetVisible(false);
        }
    }
}
