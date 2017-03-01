using CS_Project.Game.Controllers;

namespace CS_Project.Game
{
    /// <summary>
    /// The base class for any class that can be sent over the message queue.
    /// </summary>
    public abstract class Message
    {
    }

    /// <summary>
    /// The message sent when the game thread should startup a match.
    /// </summary>
    public sealed class StartMatchMessage : Message
    {
        /// <summary>
        /// The controller for the 'X' piece.
        /// </summary>
        public Controller xCon { get; set; }

        /// <summary>
        /// The controller for the 'Y' piece.
        /// </summary>
        public Controller oCon { get; set; }
    }

    /// <summary>
    /// The message that is sent whenever the player chooses where to place his piece.
    /// </summary>
    public sealed class PlayerPlaceMessage : Message
    {
        /// <summary>
        /// The index of the slot that the player wants to place their slot in.
        /// </summary>
        public int index { get; set; }
    }

    /// <summary>
    /// This test message is used to tell the game thread to throw an Exception.
    /// </summary>
    public sealed class ThrowExceptionMessage : Message
    {
    }
}
