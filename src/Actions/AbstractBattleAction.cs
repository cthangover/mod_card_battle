using Cthangover.CardBattle.UI;
using Cthangover.Core.Battle.Actions;
using Cthangover.Core.Characters;
using Godot;

namespace Cthangover.CardBattle.Actions
{
    /// <summary>
    /// Base class for animated battle actions that move, rotate, and scale card controls
    /// through a sequence of visual phases. Subclasses implement <c>DoInternalAction</c> to advance
    /// a frame-by-frame animation state machine driven by <see cref="CardBattleCore"/>'s
    /// per-frame polling loop (<c>while (!DoAction()) await frame;</c>).
    /// Captures source/target positions and rotations in <see cref="DoStart"/> so that
    /// <see cref="DoEnd"/> can restore them regardless of what the animation did.
    /// </summary>
    public abstract class AbstractBattleAction : IBattleAction
    {
        /// <summary>Seconds timestamp of the current phase start (used for phase progress); -1 before the first phase.</summary>
        protected double Timestamp { get; set; } = -1;
        /// <summary>Playback speed multiplier applied to all phase durations.</summary>
        protected float Speed { get; set; }
        /// <summary>The action definition whose stats drive the animation.</summary>
        protected ActionCharacter Action { get; set; }
        /// <summary>Acting card node.</summary>
        protected CharacterCardNode Source { get; set; }
        /// <summary>Receiving card node.</summary>
        protected CharacterCardNode Target { get; set; }
        /// <summary>Visual control of the source card (null-safe abort when freed mid-animation).</summary>
        protected Control SourceControl { get; set; }
        /// <summary>Source position captured in <see cref="DoStart"/> for restoration in <see cref="DoEnd"/>.</summary>
        protected Vector2 SourcePos { get; set; }
        /// <summary>Source rotation captured in <see cref="DoStart"/>.</summary>
        protected float SourceRot { get; set; }
        /// <summary>Visual control of the target card.</summary>
        protected Control TargetControl { get; set; }
        /// <summary>Target position captured in <see cref="DoStart"/>.</summary>
        protected Vector2 TargetPos { get; set; }
        /// <summary>Target rotation captured in <see cref="DoStart"/>.</summary>
        protected float TargetRot { get; set; }

        /// <summary>
        /// Advances the animation by one frame. Called in a loop by <see cref="CardBattleCore.RunEnemyTurn"/>
        /// with <c>await root.ToSignal(frame)</c> between calls. Returns <c>true</c> when the animation
        /// has completed all phases and the action result has been applied.
        /// Returns <c>true</c> early if any required node became null (card was destroyed mid-animation).
        /// </summary>
        /// <returns><c>true</c> when the animation is finished or cannot proceed.</returns>
        public bool DoAction()
        {
            if (Source == null || Target == null || Action == null || SourceControl == null || TargetControl == null)
                return true;
            return DoInternalAction();
        }

        /// <summary>Frame tick of the animation; implementors advance phases and return true when finished.</summary>
        protected abstract bool DoInternalAction();
        /// <summary>Phase setup called once before the first <see cref="DoInternalAction"/> tick.</summary>
        protected abstract void DoInternalStart();
        /// <summary>Cleanup called once after completion — restore captured positions/rotations here.</summary>
        protected abstract void DoInternalEnd();

        protected AbstractBattleAction(CharacterCardNode source, CharacterCardNode target, ActionCharacter action, float speed = 1)
        {
            Source = source;
            Target = target;
            Action = action;
            Speed = speed;
        }

        /// <summary>
        /// Captures the current positions, rotations, and <see cref="Control"/> references of both source
        /// and target cards before the animation begins, then delegates to the subclass
        /// <see cref="DoInternalStart"/> for any additional setup (e.g. resetting timestamps).
        /// </summary>
        public void DoStart()
        {
            if (Source == null || Target == null || Action == null)
                return;

            SourceControl = Source.GetControlNode();
            SourcePos = SourceControl.GlobalPosition;
            SourceRot = SourceControl.Rotation;
            TargetControl = Target.GetControlNode();
            TargetPos = TargetControl.GlobalPosition;
            TargetRot = TargetControl.Rotation;

            DoInternalStart();
        }

        /// <summary>
        /// Restores source and target card controls to their original positions, rotations, and visual state
        /// captured during <see cref="DoStart"/>. Delegates specific cleanup to <see cref="DoInternalEnd"/>.
        /// </summary>
        public void DoEnd()
        {
            if (Source == null || Target == null || Action == null)
                return;
            DoInternalEnd();
        }
    }
}
