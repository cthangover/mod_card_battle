using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Godot;

using Cthangover.Mods.Interface.Base.Lists;
using Cthangover.Mods.Interface.Theming;
namespace Cthangover.CardBattle.UI
{
    /// <summary>
    /// Centre-screen turn banner ("Ход игрока" / "Ход врага") from the interface
    /// mockup (mockups/card_battle). The plate lives in
    /// <c>scenes/battle_banner.tscn</c>; the class only repaints the caption and
    /// runs the fade-in / hold / fade-out tween. Tweens are driven by the
    /// process delta, so the battle speed control scales them together with the
    /// action animations.
    /// </summary>
    public partial class BattleBanner : ModWidget
    {
        private const float FadeInSeconds = 0.25f;
        private const float HoldSeconds = 1.0f;
        private const float FadeOutSeconds = 0.3f;

        private PanelContainer _plate;
        private Label _label;
        private Tween _tween;

        /// <summary>Builds the centre-screen turn/victory banner label.</summary>
        protected override void Construct()
        {
            CthangoverTheme.Apply(this);

            _plate = GetNode<PanelContainer>("Plate");
            _label = GetNode<Label>("Plate/Label");

            var style = UiKit.Frame(0, UiTokens.Alpha(UiTokens.Page, 0.62f), new Color(0f, 0f, 0f, 0f));
            style.BorderWidthTop = 1;
            style.BorderWidthBottom = 1;
            style.BorderColor = UiTokens.AccentLine;
            style.ContentMarginLeft = 44;
            style.ContentMarginRight = 44;
            style.ContentMarginTop = 12;
            style.ContentMarginBottom = 12;
            _plate.AddThemeStyleboxOverride("panel", style);

            _label.AddThemeColorOverride("font_color", UiTokens.InkBright);
            _plate.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
        }

        /// <summary>Pops the banner with the given caption, then fades it away.</summary>
        public void Show(string text)
        {
            _label.Text = text;
            _tween?.Kill();
            _plate.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
            _tween = CreateTween();
            _tween.TweenProperty(_plate, "modulate:a", 1f, FadeInSeconds).SetTrans(Tween.TransitionType.Cubic);
            _tween.TweenInterval(HoldSeconds);
            _tween.TweenProperty(_plate, "modulate:a", 0f, FadeOutSeconds).SetTrans(Tween.TransitionType.Cubic);
        }

        /// <summary>Hides the banner immediately (battle cleared).</summary>
        public void Clear()
        {
            _tween?.Kill();
            _plate.Modulate = UiTokens.Alpha(UiTokens.White, 0f);
        }
    }
}
