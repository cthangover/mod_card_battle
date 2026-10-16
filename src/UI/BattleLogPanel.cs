using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

using Cthangover.Mods.Interface.Base.Lists;
using Cthangover.Mods.Interface.Theming;
namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Battle journal docked between the player and action panels — the
    /// "Журнал боя" window from the interface mockup (mockups/card_battle).
    /// The node tree lives in <c>scenes/battle_log_panel.tscn</c>; rows are
    /// created at runtime because they are data, not layout, and are capped to
    /// <see cref="MaxRows"/> so a long battle cannot grow unbounded.
    /// <para/>
    /// The panel is the single subscriber of <see cref="BattleLogHub"/>, so any
    /// battle code can publish entries without knowing about the UI. The
    /// subscription is dropped on <see cref="Destruct"/> to keep the static hub
    /// clean between battles.
    /// </summary>
    public partial class BattleLogPanel : ModWidget
    {
        private const int MaxRows = 80;

        private Label _title;
        private Label _roundLabel;
        private ScrollContainer _scroll;
        private VBoxContainer _rows;

        /// <summary>Builds the scrollable battle log with coloured entries per <see cref="BattleLogKind"/>.</summary>
        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            _title = GetNode<Label>("Window/Column/Header/HeaderRow/TitleLabel");
            _roundLabel = GetNode<Label>("Window/Column/Header/HeaderRow/RoundLabel");
            _scroll = GetNode<ScrollContainer>("Window/Column/Scroll");
            _rows = GetNode<VBoxContainer>("Window/Column/Scroll/Rows");

            var windowStyle = UiKit.Frame(UiTokens.RadiusLg, UiTokens.Alpha(UiTokens.Surface0, 0.84f), UiTokens.Line);
            windowStyle.ContentMarginLeft = 0;
            windowStyle.ContentMarginRight = 0;
            windowStyle.ContentMarginTop = 0;
            windowStyle.ContentMarginBottom = 0;
            windowStyle.ShadowColor = new Color(0f, 0f, 0f, 0.6f);
            windowStyle.ShadowSize = 18;
            windowStyle.ShadowOffset = new Vector2(0f, 8f);
            GetNode<PanelContainer>("Window").AddThemeStyleboxOverride("panel", windowStyle);

            _title.Text = Translate(_title.Text);
            _title.AddThemeColorOverride("font_color", UiTokens.Muted);
            _roundLabel.AddThemeColorOverride("font_color", UiTokens.Faint);

            BattleLogHub.Entry += Append;
        }

        /// <summary>Unsubscribes from the shared <see cref="BattleLogHub"/> so the freed panel stops receiving entries.</summary>
        protected override void Destruct()
        {
            BattleLogHub.Entry -= Append;
        }

        /// <summary>Updates the "раунд N" caption in the header.</summary>
        public void SetRound(int round)
            => _roundLabel.Text = string.Format(Translate("battle/log_round"), round);

        /// <summary>Drops every row; called when the battle is cleared or restarted.</summary>
        public void Clear()
        {
            foreach (var child in _rows.GetChildren())
            {
                _rows.RemoveChild(child);
                child.QueueFree();
            }
        }

        /// <summary>
        /// Appends one colour-coded row and trims the oldest entries past the cap.
        /// The scroll is pushed to the bottom so the newest event stays visible.
        /// </summary>
        public void Append(string text, BattleLogKind kind)
        {
            var style = UiKit.Frame(0, BackgroundOf(kind), new Color(0f, 0f, 0f, 0f));
            style.BorderWidthLeft = 2;
            style.BorderColor = BorderOf(kind);
            style.ContentMarginLeft = 12;
            style.ContentMarginRight = 12;
            style.ContentMarginTop = 7;
            style.ContentMarginBottom = 7;

            var row = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeStyleboxOverride("panel", style);

            var label = UiKit.Label(text, UiTokens.FontBodySmall, TextOf(kind));
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            row.AddChild(label);

            _rows.AddChild(row);

            while (_rows.GetChildCount() > MaxRows)
            {
                var first = _rows.GetChild(0);
                _rows.RemoveChild(first);
                first.QueueFree();
            }

            _scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
        }

        private static Color BackgroundOf(BattleLogKind kind)
            => kind == BattleLogKind.Turn ? UiTokens.AccentSoft : new Color(0f, 0f, 0f, 0f);

        private static Color BorderOf(BattleLogKind kind)
        {
            switch (kind)
            {
                case BattleLogKind.Turn: return UiTokens.Accent;
                case BattleLogKind.Damage: return UiTokens.Danger;
                case BattleLogKind.Shield: return UiTokens.TypeResource;
                case BattleLogKind.Buff: return UiTokens.TypePassive;
                case BattleLogKind.Status: return UiTokens.Cooldown;
                case BattleLogKind.Dead: return UiTokens.LineFaint;
                default: return UiTokens.Line;
            }
        }

        private static Color TextOf(BattleLogKind kind)
        {
            switch (kind)
            {
                case BattleLogKind.Turn: return UiTokens.AccentBright;
                case BattleLogKind.Damage: return UiTokens.DangerText;
                case BattleLogKind.Shield: return UiTokens.Info;
                case BattleLogKind.Buff: return UiTokens.SuccessText;
                case BattleLogKind.Status: return UiTokens.Cooldown;
                case BattleLogKind.Dead: return UiTokens.Faint;
                default: return UiTokens.Muted;
            }
        }

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();
    }
}
