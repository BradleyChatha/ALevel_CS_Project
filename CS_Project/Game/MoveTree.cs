using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Contains the Move Tree.
/// </summary>
namespace CS_Project.Game
{
    /// <summary>
    /// A node describing a single move.
    /// </summary>
    public class Node : ICloneable
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
        /// This node's children.
        /// </summary>
        public List<Node> children { private set; get; }

        /// <summary>
        /// Creates a new Node
        /// </summary>
        /// <param name="hash">The 'Hash' of the board after the move was made.</param>
        /// <param name="index">The index of the slot that was changed.</param>
        /// <param name="won">How many times this move was used in a won match.</param>
        /// <param name="lost">The opposite of 'won'.</param>
        /// <exception cref="System.ArgumentNullException">When `hash` is null.</exception>
        public Node(Hash hash, uint index, uint won = 0, uint lost = 0)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");

            this.hash     = hash;
            this.index    = index;
            this.won      = won;
            this.lost     = lost;
            this.children = new List<Node>();
        }

        /// <summary>
        /// Clones the node, and all of it's children.
        /// </summary>
        /// <returns>A clone of this node.</returns>
        public object Clone()
        {
            var toReturn = new Node((Hash)this.hash.Clone(), this.index, this.won, this.lost);
            
            foreach(var child in this.children)
            {
                toReturn.children.Add((Node)child.Clone());
            }

            return toReturn;
        }
    }

    /// <summary>
    /// An implementation of a tree, which stores `MoveTree.Node` in it.
    /// 
    /// This class is used to store data about moves used in the game.
    /// </summary>
    public class MoveTree : ICloneable
    {
        /// <summary>
        /// The root of the tree.
        /// 
        /// This node will always have the hash of "........." and index of `uint.MaxValue`.
        /// </summary>
        public Node root { private set; get; }

        /// <summary>
        /// Create a new MoveTree.
        /// </summary>
        public MoveTree()
        {
            this.root = new Node(new Hash(Board.Piece.o, "........."), uint.MaxValue);
        }

        /// <summary>
        /// Walks through this tree along a given 'path' and returns the node from this tree at the end of the path.
        /// 
        /// Each node in the path must have 0 or 1 children, as it is a straight path to walk.
        /// 
        /// A certain 'depth' can be given. For example, a depth of 2 means only 2 nodes are walked to before this function returns.
        /// </summary>
        /// <param name="path">The root of the path to walk.</param>
        /// <param name="depth">The maximum number of nodes to walk past.</param>
        /// <returns>The node at the end of the walked path. Or `null` if the entire path couldn't be walked through.</returns>
        public Node walk(Node path, uint depth = uint.MaxValue)
        {
            if(path == null)
                throw new ArgumentNullException("path");

            if(depth == 0)
                throw new ArgumentOutOfRangeException("depth", "The depth must be 1 or more");

            uint walked      = 0;
            Node currentThis = this.root; // Current node in this tree
            Node currentPath = path;      // Current node in the path

            while(walked < depth && currentThis != null && currentPath != null)
            {
                walked += 1;
                Debug.Assert(currentPath.children.Count <= 1, "A node in the path contains more than 1 child.");

                bool found = false;
                foreach(var node in currentThis.children)
                {
                    if(node.hash.Equals(currentPath.hash))
                    {
                        currentThis = node;
                        currentPath = currentPath.children.ElementAtOrDefault(0);
                        found       = true;
                        break;
                    }
                }

                if(!found)
                    return null;
            }

            return currentThis;
        }

        public object Clone()
        {
            var toReturn  = new MoveTree();
            toReturn.root = (Node)this.root.Clone();            

            return toReturn;
        }
    }
}
