using System.Linq;
using Cthangover.Core.Cards.StatusEffect;
using Cthangover.Core.Mods.Resolvers;
using Cthangover.Core.Characters;
using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Party or enemy card on the battle field, rebuilt from the interface
    /// mockup (mockups/card_battle): full-bleed portrait, team-coloured frame,
    /// bottom info block with the name, four stat chips (HP/ЗАЩ/АТК/ОД) and an
    /// HP bar, plus stun badge, zap flash and selection frame in the upper right.
    /// The node tree lives in <c>scenes/character_card.tscn</c>; this class
    /// fetches the nodes once, dresses them with <see cref="UiTokens"/> styles and
    /// exposes the battle-facing API: <see cref="UpdateInfo"/> is the single
    /// refresh entry point for mutable stats, <see cref="Select"/> /
    /// <see cref="Attack"/> / <see cref="Invalid"/> paint target feedback for the
    /// drag-and-drop flow, <see cref="PlayZap"/> and <see cref="Flash"/> give
    /// impact feedback, <see cref="AllImages"/> feeds the dissolve shader of
    /// <see cref="CardDeathAnimation"/>.
    /// <para/>
    /// Implements <see cref="ICard"/> for hit-testing during drag-and-drop
    /// targeting and keeps the hover lift / Z-index layering rules of the
    /// original widget so overlapping cards stay readable.
    /// </summary>
    public partial class CharacterCardNode : ModWidget, ICard
    {
        private const float HoverLift = 10f;
        private const float HoverScale = 1.12f;

        private TextureRect _art;
        private Panel _teamFrame;
        private Label _nameLabel;
        private ProgressBar _hpBar;
        private Label _hpValue;
        private Label _defValue;
        private Label _atkValue;
        private Label _apValue;
        private VBoxContainer _status;
        private PanelContainer _stunBadge;
        private TextureRect _stunIcon;
        private Label _stunLabel;
        private TextureRect _zap;
        private Panel _fx;
        private TextureRect _selection;

        private Color _selectionState = Colors.Transparent;
        private float _baseY;
        private bool _isHovered;
        private bool _selected;
        private int _baseZIndex;
        private Vector2 _baseScale = Vector2.One;
        private Tween _hoverTween;
        private Tween _selectionTween;

        private static Texture2D _cachedSelectTex;
        private static bool _texturesLoaded;

        /// <summary>
        /// Distinguishes player-owned cards from enemy cards. Must be assigned by
        /// <see cref="BattleCardPanel"/> before <see cref="Init"/>, because the
        /// team frame colour is applied during initialisation.
        /// </summary>
        public bool IsPlayer { get; set; }

        /// <summary>
        /// Marks the card as dead. Set by <see cref="BattleCardPanel.Dead"/>,
        /// checked by the battle core when counting alive cards and when the
        /// enemy AI picks targets.
        /// </summary>
        public bool IsDead { get; set; }

        /// <summary>The character data model backing this card: stats, action deck, status effects.</summary>
        public Character Card { get; set; }

        /// <summary>
        /// Textures that receive the dissolve shader during the death animation:
        /// the portrait and the selection frame. The team frame and the info
        /// block are simple styleboxes and fade with the whole card node.
        /// </summary>
        public TextureRect[] AllImages => _allImages ??= new[] { _art, _selection };
        private TextureRect[] _allImages;

        /// <inheritdoc />
        public Control GetControlNode() => this;

        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            _art = GetNode<TextureRect>("Art");
            _teamFrame = GetNode<Panel>("TeamFrame");
            _nameLabel = GetNode<Label>("Info/Column/NameLabel");
            _hpBar = GetNode<ProgressBar>("Info/Column/HpBar");
            _hpValue = GetNode<Label>("Info/Column/Stats/HpChip/Row/Value");
            _defValue = GetNode<Label>("Info/Column/Stats/DefChip/Row/Value");
            _atkValue = GetNode<Label>("Info/Column/Stats/AtkChip/Row/Value");
            _apValue = GetNode<Label>("Info/Column/Stats/ApChip/Row/Value");
            _status = GetNode<VBoxContainer>("Status");
            _stunBadge = GetNode<PanelContainer>("Status/StunBadge");
            _stunIcon = GetNode<TextureRect>("Status/StunBadge/Row/StunIcon");
            _stunLabel = GetNode<Label>("Status/StunBadge/Row/StunLabel");
            _zap = GetNode<TextureRect>("Zap");
            _fx = GetNode<Panel>("Fx");
            _selection = GetNode<TextureRect>("Selection");

            Size = CustomMinimumSize;
            _baseScale = Scale;

            if (!_texturesLoaded)
            {
                _cachedSelectTex = Textures.Resolve("select");
                _texturesLoaded = true;
            }

            if (_cachedSelectTex != null)
                _selection.Texture = _cachedSelectTex;

            ApplyStyles();
            _selectionState = UiTokens.Alpha(UiTokens.White, 0f);

            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        /// <summary>
        /// Binds the card to a character: sets the portrait, resets selection
        /// visuals and refreshes every stat label through <see cref="UpdateInfo"/>.
        /// Called by <see cref="BattleCardPanel.Create"/>.
        /// </summary>
        public void Init(Character characterCard)
        {
            Card = characterCard;
            _baseScale = Scale;
            IsDead = false;

            if (_art != null && characterCard?.Image != null)
                _art.Texture = characterCard.Image;

            if (_selection != null)
                _selection.Modulate = UiTokens.Alpha(UiTokens.White, 0f);

            if (_fx != null)
                _fx.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
            if (_zap != null)
                _zap.Modulate = UiTokens.Alpha(UiTokens.White, 0f);

            _selectionState = UiTokens.Alpha(UiTokens.White, 0f);
            UpdateInfo();
        }

        /// <summary>
        /// Paints the team frame: blue for the party, gold for enemies — the
        /// same accent language the mockup uses for card outlines. Called by
        /// <see cref="BattleCardPanel.Create"/> after <see cref="Init"/>.
        /// </summary>
        public void SetTeamColors()
        {
            var accent = IsPlayer ? UiTokens.Info : UiTokens.Accent;
            var style = UiKit.Frame(UiTokens.RadiusMd, new Color(0f, 0f, 0f, 0f), UiTokens.Alpha(accent, 0.5f), 2);
            _teamFrame.AddThemeStyleboxOverride("panel", style);
        }

        /// <summary>
        /// Refreshes every mutable value from the character model: name, HP
        /// number and bar (turning red below 40%), defence, attack, action
        /// points and the stun badge with its remaining turns. Called after each
        /// action, on turn start, and by the panel after re-gridding.
        /// </summary>
        public void UpdateInfo()
        {
            if (Card?.Attributes == null)
                return;

            _nameLabel.Text = TranslationServer.Translate(Card.Name ?? string.Empty).ToString();

            var health = Card.Attributes.Health;
            var percent = Mathf.Clamp(health.Percent, 0f, 1f);
            _hpBar.Value = percent * 100f;
            _hpBar.AddThemeStyleboxOverride("background", UiKit.Frame(UiTokens.RadiusSm, UiTokens.LineDisabled));
            _hpBar.AddThemeStyleboxOverride("fill", UiKit.Frame(UiTokens.RadiusSm, percent < 0.4f ? UiTokens.HpLow : UiTokens.HpOk));

            _hpValue.Text = health.Value + "/" + health.BaseValue;
            _defValue.Text = Card.Attributes.Defence.Value.ToString();
            _atkValue.Text = Card.Attributes.Attack.Value.ToString();
            _apValue.Text = Card.Attributes.Point.Value.ToString();

            RefreshStun();
        }

        /// <summary>
        /// Sets the base Z-index used for layering cards. Selected cards jump to
        /// Z-index 4096 in <see cref="Select"/>; hovered cards rise by 100 so
        /// they cover their neighbours.
        /// </summary>
        public void SetBaseZIndex(int z)
        {
            _baseZIndex = z;
            if (!_selected)
                ZIndex = z;
        }

        /// <summary>Highlights the card as the chosen source or a valid support target (teal).</summary>
        public void Select()
        {
            _selected = true;
            _selectionState = UiTokens.TypeTarget;
            AnimateSelection(_selectionState);
            ZIndex = 4096;
        }

        /// <summary>Clears the selection highlight and restores the base Z-index.</summary>
        public void Unselect()
        {
            _selected = false;
            _selectionState = UiTokens.Alpha(UiTokens.White, 0f);
            AnimateSelection(_selectionState);
            ZIndex = _baseZIndex;
        }

        /// <summary>Highlights a valid enemy target for an attack (red).</summary>
        public void Attack()
        {
            _selectionState = UiTokens.TypeEnemy;
            AnimateSelection(_selectionState);
        }

        /// <summary>Grays out the card as an invalid target for the dragged action.</summary>
        public void Invalid()
        {
            _selectionState = UiTokens.Alpha(UiTokens.Muted, 0.8f);
            AnimateSelection(_selectionState);
        }

        /// <summary>Plays the electric stun flash over the portrait (additive blend).</summary>
        public void PlayZap()
        {
            if (_zap == null)
                return;

            _zap.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
            var tween = CreateTween();
            tween.TweenProperty(_zap, "modulate:a", 1f, 0.12f);
            tween.TweenProperty(_zap, "modulate:a", 0f, 0.45f).SetDelay(0.25f);
        }

        /// <summary>Pulses the whole card with a colour (damage red, shield blue).</summary>
        public void Flash(Color color)
        {
            _fx.Modulate = UiTokens.Alpha(color, 0f);
            var tween = CreateTween();
            tween.TweenProperty(_fx, "modulate:a", 0.55f, 0.12f);
            tween.TweenProperty(_fx, "modulate:a", 0f, 0.38f);
        }

        private void ApplyStyles()
        {
            var infoStyle = UiKit.Frame(UiTokens.RadiusMd, UiTokens.Alpha(UiTokens.Surface2, 0.93f), UiTokens.Line);
            infoStyle.CornerRadiusTopLeft = 0;
            infoStyle.CornerRadiusTopRight = 0;
            infoStyle.ContentMarginLeft = 8;
            infoStyle.ContentMarginRight = 8;
            infoStyle.ContentMarginTop = 6;
            infoStyle.ContentMarginBottom = 8;
            GetNode<PanelContainer>("Info").AddThemeStyleboxOverride("panel", infoStyle);

            _nameLabel.AddThemeColorOverride("font_color", UiTokens.InkBright);

            StyleChip("Info/Column/Stats/HpChip", UiTokens.HpText);
            StyleChip("Info/Column/Stats/DefChip", UiTokens.Ink);
            StyleChip("Info/Column/Stats/AtkChip", UiTokens.Ink);
            StyleChip("Info/Column/Stats/ApChip", UiTokens.AccentBright);

            var stunStyle = UiKit.Frame(UiTokens.RadiusXs, UiTokens.CooldownSoft, UiTokens.CooldownLine);
            stunStyle.ContentMarginLeft = 5;
            stunStyle.ContentMarginRight = 8;
            stunStyle.ContentMarginTop = 3;
            stunStyle.ContentMarginBottom = 3;
            _stunBadge.AddThemeStyleboxOverride("panel", stunStyle);
            _stunLabel.AddThemeColorOverride("font_color", UiTokens.Cooldown);

            _zap.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            _zap.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
            _fx.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
            _selection.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
        }

        private void StyleChip(string path, Color valueColor)
        {
            var chip = GetNode<PanelContainer>(path);
            var style = UiKit.Frame(UiTokens.RadiusXs, UiTokens.StatBg, UiTokens.StatLine);
            style.ContentMarginLeft = 6;
            style.ContentMarginRight = 6;
            style.ContentMarginTop = 2;
            style.ContentMarginBottom = 2;
            chip.AddThemeStyleboxOverride("panel", style);

            var row = chip.GetNode<HBoxContainer>("Row");
            var key = row.GetNode<Label>("Key");
            key.Text = TranslationServer.Translate(key.Text).ToString();
            key.AddThemeColorOverride("font_color", UiTokens.Muted);
            row.GetNode<Label>("Value").AddThemeColorOverride("font_color", valueColor);
        }

        private void RefreshStun()
        {
            var stun = StatusEffectService.Of(Card)?.ActiveStatusEffects?
                .FirstOrDefault(effect => effect.EffectType == StatusEffectType.Stun);

            if (stun == null)
            {
                _stunBadge.Visible = false;
                return;
            }

            _stunBadge.Visible = true;
            if (stun.Icon != null)
            {
                // Текстура приходит из данных эффекта: группа "characters" не
                // входит в texture_groups, поэтому Textures.Resolve("stun") её
                // не видит, а StatusEffectInfo.Icon грузится через TextureUtils.
                _stunIcon.Texture = stun.Icon;
                _zap.Texture = stun.Icon;
            }
            _stunLabel.Text = string.Format(
                TranslationServer.Translate("battle/stun_status").ToString(), stun.RemainingTurns);
        }

        private void AnimateSelection(Color target)
        {
            if (_selection == null)
                return;

            _selectionTween?.Kill();
            _selectionTween = CreateTween();
            _selectionTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            _selectionTween.TweenProperty(_selection, "modulate", target, 0.15f);
        }

        private void OnMouseEntered()
        {
            if (_isHovered)
                return;
            _isHovered = true;
            _baseY = Position.Y;

            if (!_selected)
                ZIndex = _baseZIndex + 100;

            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.SetParallel(true);
            _hoverTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            _hoverTween.TweenProperty(this, "position:y", _baseY - HoverLift, 0.15f);
            _hoverTween.TweenProperty(this, "scale", _baseScale * HoverScale, 0.15f);
        }

        private void OnMouseExited()
        {
            if (!_isHovered)
                return;
            _isHovered = false;

            if (!_selected)
                ZIndex = _baseZIndex;

            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.SetParallel(true);
            _hoverTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            _hoverTween.TweenProperty(this, "position:y", _baseY, 0.15f);
            _hoverTween.TweenProperty(this, "scale", _baseScale, 0.15f);
        }
    }
}
