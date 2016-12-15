using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Contains the Move Tree.
/// </summary>
namespace CS_Project.Game
{
    /// <summary>
    /// An implementation of a tree, which stores `MoveTree.Node` in it.
    /// 
    /// This class is used to store data about moves used in the game.
    /// </summary>
    class MoveTree
    {

    }

    /// <summary>
    /// A node describing a single move.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// The hash of the board after the move was made.
        /// </summary>
        public readonly Hash hash;

        /// <summary>
        /// The index of what slot was changed this move.
        /// </summary>
        public readonly uint index;

        /// <summary>
        /// How many times this move was used in a game that was won.
        /// </summary>
        public uint won;

        /// <summary>
        /// How many times this move was used in a game that was lost.
        /// </summary>
        public uint lost;

        /// <summary>
        /// Creates a new Node
        /// </summary>
        /// <param name="hash">The 'Hash' of the board after the move was made.</param>
        /// <param name="index">The index of the slot that was changed.</param>
        /// <param name="won">How many times this move was used in a won match.</param>
        /// <param name="lost">The opposite of 'won'</param>
        public Node(Hash hash, uint index, uint won = 0, uint lost = 0)
        {
            if(hash == null)
                throw new ArgumentNullException("hash");

            this.hash  = hash;
            this.index = index;
            this.won   = won;
            this.lost  = lost;
        }
    }
}
