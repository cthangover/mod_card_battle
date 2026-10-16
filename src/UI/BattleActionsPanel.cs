using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

using Cthangover.Mods.Interface.Base.Lists;
using Cthangover.Mods.Interface.Theming;
namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// "Действия" window from the interface mockup (mockups/card_battle): the
    /// lower-right panel that opens when a party card is clicked. Layout lives
    /// in <c>scenes/battle_actions_panel.tscn</c>: a header with the character
    /// name and action-point pips, a two-column grid that receives the
    /// <see cref="ActionCardNode"/> instances, a description well and a footer
    /// hint. The class owns only content: it repaints pips, swaps the empty
    /// state and prints the hovered action's numbers.
    /// </summary>
    public partial class BattleActionsPanel : ModWidget
    {
        private const int PipSize = 11;

        private Label _subject;
        private HBoxContainer _pips;
        private Label _apHint;
        private Label _emptyLabel;
        private GridContainer _grid;
        private PanelContainer _descWell;
        private Label _descLabel;

        /// <summary>Container that receives action cards; owned by the input controller.</summary>
        public Control Body => _grid;

        /// <summary>Builds the bottom action bar shown while a card is selected.</summary>
        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            _subject = GetNode<Label>("Window/Column/Header/HeaderRow/SubjectLabel");
            _pips = GetNode<HBoxContainer>("Window/Column/Header/HeaderRow/Pips");
            _apHint = GetNode<Label>("Window/Column/Header/HeaderRow/ApHint");
            _emptyLabel = GetNode<Label>("Window/Column/Body/BodyColumn/EmptyLabel");
            _grid = GetNode<GridContainer>("Window/Column/Body/BodyColumn/Grid");
            _descWell = GetNode<PanelContainer>("Window/Column/DescWrap/DescWell");
            _descLabel = GetNode<Label>("Window/Column/DescWrap/DescWell/DescLabel");

            var windowStyle = UiKit.Frame(UiTokens.RadiusLg, UiTokens.Alpha(UiTokens.Surface0, 0.84f), UiTokens.Line);
            windowStyle.ContentMarginLeft = 0;
            windowStyle.ContentMarginRight = 0;
            windowStyle.ContentMarginTop = 0;
            windowStyle.ContentMarginBottom = 0;
            windowStyle.ShadowColor = new Color(0f, 0f, 0f, 0.6f);
            windowStyle.ShadowSize = 18;
            windowStyle.ShadowOffset = new Vector2(0f, 8f);
            GetNode<PanelContainer>("Window").AddThemeStyleboxOverride("panel", windowStyle);

            foreach (var path in new[]
            {
                "Window/Column/Header/HeaderRow/TitleLabel",
                "Window/Column/Header/HeaderRow/ApCaption",
                "Window/Column/Body/BodyColumn/EmptyLabel",
                "Window/Column/Footer/FooterRow/HintLabel",
                "Window/Column/Footer/FooterRow/AutoEndLabel",
            })
            {
                var label = GetNode<Label>(path);
                label.Text = Translate(label.Text);
            }

            GetNode<Label>("Window/Column/Header/HeaderRow/TitleLabel").AddThemeColorOverride("font_color", UiTokens.Muted);
            GetNode<Label>("Window/Column/Header/HeaderRow/ApCaption").AddThemeColorOverride("font_color", UiTokens.Muted);
            _apHint.AddThemeColorOverride("font_color", UiTokens.Faint);
            _emptyLabel.AddThemeColorOverride("font_color", UiTokens.Faint);
            GetNode<Label>("Window/Column/Footer/FooterRow/HintLabel").AddThemeColorOverride("font_color", UiTokens.Faint);
            GetNode<Label>("Window/Column/Footer/FooterRow/AutoEndLabel").AddThemeColorOverride("font_color", UiTokens.Faint);

            var descStyle = UiKit.Frame(UiTokens.RadiusMd, UiTokens.Surface2, UiTokens.Line);
            descStyle.ContentMarginLeft = 12;
            descStyle.ContentMarginRight = 12;
            descStyle.ContentMarginTop = 10;
            descStyle.ContentMarginBottom = 10;
            _descWell.AddThemeStyleboxOverride("panel", descStyle);
            ClearSubject();
        }

        /// <summary>Shows the selected character's name in the header.</summary>
        public void SetSubject(string name)
        {
            _subject.RemoveThemeColorOverride("font_color");
            _subject.AddThemeColorOverride("font_color", UiTokens.InkBright);
            _subject.Text = name;
        }

        /// <summary>Restores the "no character selected" header and the empty state.</summary>
        public void ClearSubject()
        {
            _subject.AddThemeColorOverride("font_color", UiTokens.Faint);
            _subject.Text = Translate("battle/no_character");
            _pips.Visible = false;
            _apHint.Text = string.Empty;
            SetDescriptionDefault();
        }

        /// <summary>Repaints the action-point pips for the selected character.</summary>
        public void SetPoints(int current, int basePoints)
        {
            _pips.Visible = true;
            foreach (var child in _pips.GetChildren())
            {
                _pips.RemoveChild(child);
                child.QueueFree();
            }

            for (var i = 0; i < basePoints; i++)
            {
                var on = i < current;
                var pip = new Panel
                {
                    MouseFilter = MouseFilterEnum.Ignore,
                    CustomMinimumSize = new Vector2(PipSize, PipSize),
                };
                pip.AddThemeStyleboxOverride("panel", UiKit.Frame(
                    PipSize / 2,
                    on ? UiTokens.AccentBright : UiTokens.LineDisabled,
                    on ? UiTokens.Accent : UiTokens.LineFaint));
                _pips.AddChild(pip);
            }

            _apHint.Text = current + " / " + basePoints;
        }

        /// <summary>Frees every action card and restores the empty state.</summary>
        public void ClearBody()
        {
            foreach (var child in _grid.GetChildren())
            {
                _grid.RemoveChild(child);
                child.QueueFree();
            }
            RefreshEmpty();
        }

        /// <summary>Hides the empty state as soon as the grid holds action cards.</summary>
        public void RefreshEmpty()
            => _emptyLabel.Visible = _grid.GetChildCount() == 0;

        /// <summary>Prints the hovered action preview ("Урон 5 · расход 1 ОД") in the well.</summary>
        public void SetDescription(string text)
        {
            _descLabel.AddThemeColorOverride("font_color", UiTokens.Muted);
            _descLabel.Text = text;
        }

        /// <summary>Restores the well's invitation text.</summary>
        public void SetDescriptionDefault()
        {
            _descLabel.AddThemeColorOverride("font_color", UiTokens.Faint);
            _descLabel.Text = Translate("battle/desc_default");
        }

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();
    }
}
