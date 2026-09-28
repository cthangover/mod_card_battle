using Godot;

namespace Cthangover.CardBattle
{
    /// <summary>
    /// Common contract for every card the input controller can hit-test. Both
    /// the character cards on the field and the draggable action cards expose
    /// their root <see cref="Control"/>, because layout, scale and the drag
    /// ghost all operate on the whole card rect rather than on an inner sprite.
    /// </summary>
    public interface ICard
    {
        /// <summary>Root control used for hit-testing and for reparenting during a drag.</summary>
        Control GetControlNode();
    }
}
