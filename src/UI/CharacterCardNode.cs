using Cthangover.Core.Cards;
using Cthangover.Core.Mods;
using Cthangover.Core.Mods.Resolvers;
using Cthangover.Core.Characters;
using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// UI node representing a single character (player or enemy) on the battle field.
    /// Displays the character portrait, name, health bar, and stat labels (defence S, attack A, action points P).
    /// Implements <see cref="ICard"/> for hit-testing during drag-and-drop targeting.
    /// Supports team-colored outlines (blue for player, yellow for enemy), hover scale-up animation,
    /// and multiple selection highlight modes (<see cref="Select"/>, <see cref="Attack"/>, <see cref="Invalid"/>)
    /// driven by the <see cref="ICardActionStrategy"/> flow. Manages Z-index layering for selected,
    /// hovered, and normal states.
    /// </summary>
    public partial class CharacterCardNode : ModWidget, ICard
    {
        private ColorRect teamOutline;
        private TextureRect frame;
        private TextureRect image;
        private TextureRect selection;

        private ColorRect healthRect;
        private Label nameLabel;
        private Label defenceLabel;
        private Label attackLabel;
        private Label pointsLabel;

        private Color selectionState;
        private float defaultWidth;
        private float baseY;
        private bool isHovered;
        private Tween hoverTween;
        private Tween selectionTween;
        private static Texture2D cachedSelectTex;
        private static bool selectTexLoaded;

        /// <summary>
        /// Distinguishes player-owned cards from enemy cards. Affects team outline color in
        /// <see cref="SetTeamColors"/> and determines whether the card is clickable in
        /// <see cref="CardController.OnPointerClick"/>.
        /// </summary>
        public bool IsPlayer { get; set; }
        /// <summary>
        /// Marks the card as dead. Set by <see cref="BattleCardPanel.Dead"/>, checked by
        /// <see cref="CardBattleCore"/> when counting alive cards and by
        /// <see cref="CardBattleCore.RunEnemyTurn"/> when iterating for enemy actions.
        /// </summary>
        public bool IsDead { get; set; }

        private int _baseZIndex;
        private bool _selected;
        private Vector2 _baseScale;

        /// <inheritdoc />
        public TextureRect Frame => frame;
        /// <inheritdoc />
        public TextureRect Image => image;
        /// <summary>
        /// All <see cref="TextureRect"/> nodes on this card (frame, image, selection).
        /// Consumed by <see cref="CardDeathAnimation"/> to apply the dissolve shader to every visual element.
        /// </summary>
        public TextureRect[] AllImages => _allImages ??= new[] { frame, image, selection };
        private TextureRect[] _allImages;

        /// <summary>
        /// The <see cref="Character"/> data model backing this card. Contains attributes
        /// (health, attack, defence, points) and the action deck.
        /// </summary>
        public Character Card { get; set; }

        /// <summary>
        /// Returns <c>this</c> — character cards are self-contained <see cref="Control"/> nodes.
        /// </summary>
        public Control GetControlNode() => this;

        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            teamOutline = GetNode<ColorRect>("TeamOutline");
            image = GetNode<TextureRect>("Image");
            frame = GetNode<TextureRect>("Frame");
            selection = GetNode<TextureRect>("Selection");
            nameLabel = GetNode<Label>("InfoPanel/NameLabel");
            healthRect = GetNode<ColorRect>("InfoPanel/HealthBar/HealthRect");
            defenceLabel = GetNode<Label>("InfoPanel/Labels/DefenceLabel");
            attackLabel = GetNode<Label>("InfoPanel/Labels/AttackLabel");
            pointsLabel = GetNode<Label>("InfoPanel/Labels/PointsLabel");

            Size = CustomMinimumSize;

            defaultWidth = 230f;
            selectionState = UiTokens.Alpha(UiTokens.White, 0f);

            if (!selectTexLoaded)
            {
                cachedSelectTex = Textures.Resolve("select");
                selectTexLoaded = true;
            }
            if (cachedSelectTex != null)
            {
                selection.Texture = cachedSelectTex;
                selection.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                selection.StretchMode = TextureRect.StretchModeEnum.Scale;
            }

            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        /// <summary>
        /// Initializes the card with the given <paramref name="characterCard"/> data, sets the
        /// portrait and frame textures, resets selection visuals, and refreshes stat labels.
        /// Called by <see cref="BattleCardPanel.Create"/>.
        /// </summary>
        public void Init(Character characterCard)
        {
            Card = characterCard;
            _baseScale = Scale;

            if (image != null)
                image.Texture = characterCard.Image;

            if (frame != null && characterCard?.Image != null)
                frame.Texture = characterCard.Image;

            if (selection != null)
                selection.Modulate = UiTokens.Alpha(UiTokens.White, 0f);

            selectionState = UiTokens.Alpha(UiTokens.White, 0f);

            UpdateInfo();
        }

        /// <summary>
        /// Applies a team-colored outline: blue for player cards (<see cref="IsPlayer"/> = <c>true</c>),
        /// yellow for enemy cards. Called by <see cref="BattleCardPanel.Create"/> after initialization.
        /// </summary>
        public void SetTeamColors()
        {
            if (teamOutline != null)
                teamOutline.Color = IsPlayer
                    ? UiTokens.Alpha(UiTokens.Info, 0.5f)
                    : UiTokens.Alpha(UiTokens.Accent, 0.5f);
        }

        /// <summary>
        /// Sets the base Z-index used for layering cards. When not selected, the card renders at
        /// this Z-index. Selected cards jump to Z-index 4096 to render above all others.
        /// Called by <see cref="BattleCardPanel.SetCardPosition"/> during layout.
        /// </summary>
        public void SetBaseZIndex(int z)
        {
            _baseZIndex = z;
            if (!_selected)
                ZIndex = z;
        }

        /// <summary>
        /// Applies a target-accent selection highlight, raises Z-index to 4096 to ensure the card
        /// is clickable above other cards during drag-and-drop targeting, and animates
        /// the selection overlay to <see cref="UiTokens.TypeTarget"/>. Called when the player clicks a character card
        /// or when a valid support/self target is dragged over.
        /// </summary>
        public void Select()
        {
            _selected = true;
            selectionState = UiTokens.TypeTarget;
            AnimateSelection(selectionState);
            if (selection != null) selection.ZIndex = 1;
            ZIndex = 4096;
        }

        /// <summary>
        /// Clears the selection highlight, restores Z-index to <see cref="SetBaseZIndex"/> value,
        /// and fades the selection overlay to transparent.
        /// </summary>
        public void Unselect()
        {
            _selected = false;
            selectionState = UiTokens.Alpha(UiTokens.White, 0f);
            AnimateSelection(selectionState);
            if (selection != null) selection.ZIndex = 0;
            ZIndex = _baseZIndex;
        }

        /// <summary>
        /// Applies an enemy-accent highlight indicating this card is being targeted for an attack.
        /// Called by <see cref="ICardActionStrategy.HighlightTarget"/> during drag-over.
        /// </summary>
        public void Attack()
        {
            selectionState = UiTokens.TypeEnemy;
            AnimateSelection(selectionState);
            if (selection != null) selection.ZIndex = 1;
        }

        /// <summary>
        /// Applies a gray dimming highlight indicating this card is an invalid target
        /// for the currently dragged action card. Called by <see cref="CardController.OnDrag"/>
        /// when the strategy's <c>Check</c> returns <c>false</c>.
        /// </summary>
        public void Invalid()
        {
            selectionState = UiTokens.Alpha(UiTokens.Muted, 0.8f);
            AnimateSelection(selectionState);
            if (selection != null) selection.ZIndex = 1;
        }

        private void AnimateSelection(Color target)
        {
            if (selection == null) return;
            selectionTween?.Kill();
            selectionTween = CreateTween();
            selectionTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            selectionTween.TweenProperty(selection, "modulate", target, 0.15f);
        }

        /// <summary>
        /// Refreshes all displayed stats from the underlying <see cref="Character"/> data:
        /// name, defence, attack, action points, and health bar width (based on health percent).
        /// Called after every action execution and during initial setup.
        /// </summary>
        public void UpdateInfo()
        {
            if (healthRect == null || Card == null) return;

            if (nameLabel != null)
                nameLabel.Text = TranslationServer.Translate(Card.Name);
            if (defenceLabel != null)
                defenceLabel.Text = Card.Attributes.Defence.Value.ToString();
            if (attackLabel != null)
                attackLabel.Text = Card.Attributes.Attack.Value.ToString();
            if (pointsLabel != null)
                pointsLabel.Text = Card.Attributes.Point.Value.ToString();

            var sizeDelta = healthRect.Size;
            sizeDelta.X = Card.Attributes.Health.Percent * defaultWidth;
            healthRect.SetDeferred(Control.PropertyName.Size, sizeDelta);
        }

        private void OnMouseEntered()
        {
            if (isHovered) return;
            isHovered = true;
            baseY = Position.Y;

            if (!_selected)
                ZIndex = _baseZIndex + 100;

            hoverTween?.Kill();
            hoverTween = CreateTween();
            hoverTween.SetParallel(true);
            hoverTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            hoverTween.TweenProperty(this, "position:y", baseY - 10f, 0.15f);
            hoverTween.TweenProperty(this, "scale", _baseScale * 1.15f, 0.15f);
        }

        private void OnMouseExited()
        {
            if (!isHovered) return;
            isHovered = false;

            if (!_selected)
                ZIndex = _baseZIndex;

            hoverTween?.Kill();
            hoverTween = CreateTween();
            hoverTween.SetParallel(true);
            hoverTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            hoverTween.TweenProperty(this, "position:y", baseY, 0.15f);
            hoverTween.TweenProperty(this, "scale", _baseScale, 0.15f);
        }
    }
}
