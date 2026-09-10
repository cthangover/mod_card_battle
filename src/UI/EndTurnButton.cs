using Cthangover.Core.UI;
using Godot;

namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// A styled button widget that allows the player to manually end their turn.
    /// Displayed during the player's turn and hidden during enemy turns and battle cleanup.
    /// Fires <see cref="OnPressed"/> when clicked, which <see cref="CardBattleCore"/> binds
    /// to <c>OnPlayerTurnEnd</c>. The button text is translated via the <c>"battle/end_turn"</c> key.
    /// </summary>
    public partial class EndTurnButton : ModWidget
    {
        private Button _button;

        /// <summary>
        /// Fired when the button is pressed. Subscribed by <see cref="CardBattleCore.OnEndTurnPressed"/>.
        /// </summary>
        public event System.Action OnPressed;

        protected override void Construct()
        {
            _button = GetNode<Button>("BtnEndTurn");

            Size = CustomMinimumSize;

            _button.Text = TranslationServer.Translate("battle/end_turn");
            _button.Pressed += () => OnPressed?.Invoke();
        }

        /// <summary>
        /// Shows or hides the underlying <see cref="Button"/> control.
        /// Called by <see cref="CardBattleCore"/> during turn transitions and battle cleanup.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_button != null)
                _button.Visible = visible;
        }
    }
}
