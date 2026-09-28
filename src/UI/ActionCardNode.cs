using Cthangover.Core.Characters;
using Cthangover.Core.UI.CharacterPanel;
using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Draggable action card from the interface mockup (mockups/card_battle):
    /// the horizontal kit card with a type-coloured accent strip, action icon,
    /// name and ID, badges (targeting type, action-point cost, stun turns) and
    /// a property-chip row. The node tree lives in <c>scenes/action_card.tscn</c>;
    /// this class fetches the nodes, dresses them and fills the dynamic badge /
    /// chip rows using the shared <see cref="ActionCharacterUi"/> presentation
    /// rules, so the battle UI can never disagree with the character panel about
    /// an action's colour, caption or cost.
    /// <para/>
    /// Implements <see cref="ICard"/> so the input controller can hit-test drag
    /// targets; the hover style, disabled state and the preview builder
    /// (<see cref="DescribeFrom"/>) are the only UI surface the controller and
    /// the actions panel need.
    /// </summary>
    public partial class ActionCardNode : ModWidget, ICard
    {
        private PanelContainer _panel;
        private ColorRect _accent;
        private TextureRect _icon;
        private Label _nameLabel;
        private Label _idLabel;
        private HBoxContainer _badges;
        private HBoxContainer _props;

        private StyleBoxFlat _idleStyle;
        private StyleBoxFlat _hoverStyle;
        private bool _disabled;

        /// <summary>The action data model backing this card.</summary>
        public ActionCharacter Card { get; set; }

        /// <summary>True while the character cannot pay the card's action-point cost.</summary>
        public bool IsDisabled => _disabled;

        /// <summary>Raised when the cursor enters the card; the actions panel uses it to print the preview.</summary>
        public event System.Action<ActionCardNode> Hovered;

        /// <summary>Raised when the cursor leaves the card; the preview returns to its default text.</summary>
        public event System.Action<ActionCardNode> Unhovered;

        /// <inheritdoc />
        public Control GetControlNode() => this;

        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            _panel = GetNode<PanelContainer>("Panel");
            _accent = GetNode<ColorRect>("Panel/Row/Accent");
            _icon = GetNode<TextureRect>("Panel/Row/IconWrap/Icon");
            _nameLabel = GetNode<Label>("Panel/Row/Body/Column/NameRow/NameLabel");
            _idLabel = GetNode<Label>("Panel/Row/Body/Column/NameRow/IdLabel");
            _badges = GetNode<HBoxContainer>("Panel/Row/Body/Column/Badges");
            _props = GetNode<HBoxContainer>("Panel/Row/Body/Column/Props");

            _idleStyle = UiKit.Frame(UiTokens.RadiusMd, UiTokens.Surface1, UiTokens.LineCard);
            _hoverStyle = UiKit.Frame(UiTokens.RadiusMd, UiTokens.SurfaceHover, UiTokens.LineStrong);
            _panel.AddThemeStyleboxOverride("panel", _idleStyle);

            _nameLabel.AddThemeColorOverride("font_color", UiTokens.InkBright);
            _idLabel.AddThemeColorOverride("font_color", UiTokens.Faint);

            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        /// <summary>
        /// Fills the card from the action definition: icon, name, ID, accent
        /// strip, badges and property chips. Called by the input controller when
        /// the action deck of a character is opened.
        /// </summary>
        public void Init(ActionCharacter card)
        {
            Card = card;

            if (_icon != null && card?.Image != null)
                _icon.Texture = card.Image;

            _accent.Color = ActionCharacterUi.AccentColor(card.Type);
            _nameLabel.Text = ActionCharacterUi.Translate(card.Name);
            _idLabel.Text = card.ID;
            TooltipText = ActionCharacterUi.BuildTooltip(card);

            BuildBadges();
            BuildProps();
        }

        /// <summary>Refreshes the texts of an already initialised card.</summary>
        public void UpdateInfo()
        {
            if (Card == null)
                return;
            _nameLabel.Text = ActionCharacterUi.Translate(Card.Name);
        }

        /// <summary>
        /// Dims the card when the selected character cannot pay its cost; hover
        /// feedback is suppressed so the disabled state reads as inert.
        /// </summary>
        public void SetDisabled(bool disabled)
        {
            _disabled = disabled;
            Modulate = UiTokens.Alpha(UiTokens.White, disabled ? 0.45f : 1f);
            _panel.AddThemeStyleboxOverride("panel", _idleStyle);
        }

        /// <summary>
        /// Builds the hint printed in the actions panel when the card is
        /// hovered: the expected effect computed from the source character's
        /// stats ("Урон 5 (АТК 5 × 1.0) · расход 1 ОД").
        /// </summary>
        public string DescribeFrom(Character source)
        {
            if (Card == null || source?.Attributes == null)
                return string.Empty;

            var cost = ActionCharacterUi.CostOf(Card);
            if (Card.Type == ActionCharacterType.ToEnemy && Card.GetInt("Turn", 0) > 1)
                return string.Format(Translate("battle/preview_stun"), Card.GetInt("Turn", 0), cost);

            if (Card.Type == ActionCharacterType.ToEnemy)
            {
                var multiplier = Card.GetFloat("Attack", 1f);
                var damage = Mathf.RoundToInt(multiplier * source.Attributes.Attack.Value);
                return string.Format(
                    Translate("battle/preview_damage"),
                    damage,
                    source.Attributes.Attack.Value,
                    multiplier.ToString("0.0"),
                    cost);
            }

            return string.Format(Translate("battle/preview_defence"), Card.GetInt("Defence"), cost);
        }

        /// <summary>Placeholder for the source-card highlight; the drag ghost covers it.</summary>
        public void Select()
        {
        }

        /// <summary>Placeholder: target highlighting is applied by the action strategy.</summary>
        public void Attack()
        {
        }

        /// <summary>Placeholder: validity is painted on the target card instead.</summary>
        public void Invalid()
        {
        }

        /// <summary>Restores the neutral Z-index after a cancelled drag.</summary>
        public void Unselect()
        {
            ZIndex = 0;
        }

        private void BuildBadges()
        {
            foreach (var child in _badges.GetChildren())
            {
                _badges.RemoveChild(child);
                child.QueueFree();
            }

            var accent = ActionCharacterUi.AccentColor(Card.Type);
            _badges.AddChild(ActionCharacterUi.MakeBadge(
                ActionCharacterUi.Translate(ActionCharacterUi.TypeLabelKey(Card.Type)).ToUpperInvariant(), accent));

            var cost = ActionCharacterUi.CostText(Card);
            if (!string.IsNullOrEmpty(cost))
                _badges.AddChild(ActionCharacterUi.MakeBadge(cost, UiTokens.Info));

            var turns = Card.GetInt("Turn", 0);
            if (turns > 1 && Card.Type == ActionCharacterType.ToEnemy)
                _badges.AddChild(ActionCharacterUi.MakeBadge(
                    string.Format(Translate("battle/turns_badge"), turns), UiTokens.Cooldown));
        }

        private void BuildProps()
        {
            foreach (var child in _props.GetChildren())
            {
                _props.RemoveChild(child);
                child.QueueFree();
            }

            foreach (var pair in ActionCharacterUi.SortedProperties(Card))
                _props.AddChild(ActionCharacterUi.MakePropertyChip(pair.Key, pair.Value));
        }

        private void OnMouseEntered()
        {
            if (!_disabled)
                _panel.AddThemeStyleboxOverride("panel", _hoverStyle);
            Hovered?.Invoke(this);
        }

        private void OnMouseExited()
        {
            _panel.AddThemeStyleboxOverride("panel", _idleStyle);
            Unhovered?.Invoke(this);
        }

        private static string Translate(string key)
            => TranslationServer.Translate(key).ToString();
    }
}
