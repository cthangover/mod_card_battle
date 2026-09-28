using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Battle header from the interface mockup (mockups/card_battle): a compact
    /// window in the upper left with the battle title, a phase badge that flips
    /// between player and enemy turns, the round counter and the number of
    /// enemies still standing. The node tree lives in
    /// <c>scenes/battle_hud.tscn</c>; this class only fetches the nodes and
    /// repaints the badge for the current phase.
    /// </summary>
    public partial class BattleHud : ModWidget
    {
        private Label _phaseLabel;
        private PanelContainer _phaseBadge;
        private Label _roundValue;
        private Label _enemiesValue;

        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            _phaseBadge = GetNode<PanelContainer>("Panel/Column/TitleRow/PhaseBadge");
            _phaseLabel = GetNode<Label>("Panel/Column/TitleRow/PhaseBadge/PhaseLabel");
            _roundValue = GetNode<Label>("Panel/Column/MetaRow/RoundValue");
            _enemiesValue = GetNode<Label>("Panel/Column/MetaRow/EnemiesValue");

            var panelStyle = UiKit.Frame(UiTokens.RadiusLg, UiTokens.Alpha(UiTokens.Surface0, 0.86f), UiTokens.Line);
            panelStyle.ContentMarginLeft = 18;
            panelStyle.ContentMarginRight = 18;
            panelStyle.ContentMarginTop = 12;
            panelStyle.ContentMarginBottom = 13;
            panelStyle.ShadowColor = new Color(0f, 0f, 0f, 0.6f);
            panelStyle.ShadowSize = 18;
            panelStyle.ShadowOffset = new Vector2(0f, 8f);
            GetNode<PanelContainer>("Panel").AddThemeStyleboxOverride("panel", panelStyle);

            var title = GetNode<Label>("Panel/Column/TitleRow/TitleLabel");
            title.Text = Translate(title.Text);
            title.AddThemeColorOverride("font_color", UiTokens.InkBright);

            foreach (var caption in new[] { "RoundCaption", "EnemiesCaption" })
            {
                var label = GetNode<Label>("Panel/Column/MetaRow/" + caption);
                label.Text = Translate(label.Text);
                label.AddThemeColorOverride("font_color", UiTokens.Muted);
            }

            _phaseLabel.Text = Translate(_phaseLabel.Text);
            _roundValue.AddThemeColorOverride("font_color", UiTokens.AccentBright);
            _enemiesValue.AddThemeColorOverride("font_color", UiTokens.AccentBright);
        }

        /// <summary>
        /// Repaints the phase badge: blue for the player phase, red for the
        /// enemy phase — the same colour language as the targeting highlights.
        /// </summary>
        public void SetTurn(bool player)
        {
            var accent = player ? UiTokens.Info : UiTokens.TypeEnemy;
            var style = UiKit.Frame(UiTokens.RadiusXs, UiTokens.BadgeFill(accent), UiTokens.BadgeLine(accent));
            style.ContentMarginLeft = 7;
            style.ContentMarginRight = 7;
            style.ContentMarginTop = 2;
            style.ContentMarginBottom = 2;

            _phaseBadge.AddThemeStyleboxOverride("panel", style);
            _phaseLabel.AddThemeColorOverride("font_color", accent);
            _phaseLabel.Text = Translate(player ? "battle/turn_player" : "battle/turn_enemy");
        }

        /// <summary>Writes the round number into the meta row.</summary>
        public void SetRound(int round) => _roundValue.Text = round.ToString();

        /// <summary>Writes the number of enemies still alive into the meta row.</summary>
        public void SetEnemiesAlive(int count) => _enemiesValue.Text = count.ToString();

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();
    }
}
