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
        public List<Node> children { set; get; }

        /// <summary>
        /// Calculates the percentage of games that have been won.
        /// </summary>
        public float winPercent
        {
            get
            {
                float total = (this.won + this.lost);
                return (this.won / total) * 100.0f;
            }
        }

        /// <summary>
        /// Calculates the percentage of games that have been lost.
        /// </summary>
        public float losePercent
        {
            get
            {
                float total = (this.won + this.lost);
                return (this.lost / total) * 100.0f;
            }
        }

        /// <summary>
        /// Creates a node suitable for use as a root node.
        /// </summary>
        public static Node root
        {
            get
            {
                return new Node(new Hash(Board.Piece.x, "........."), uint.MaxValue);
            }
        }

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
        /// Default constructor for a Node.
        /// </summary>
        public Node()
        {
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
                toReturn.children.Add((Node)child.Clone());

            return toReturn;
        }

        /// <summary>
        /// Walks through this tree along a given 'path' and returns the node from this tree at the end of the path.
        /// 
        /// Each node in the path must have 0 or 1 children, as it is a straight path to walk.
        /// 
        /// A certain 'depth' can be given. For example, a depth of 2 means only 2 nodes are walked to before this function returns.
        /// </summary>
        /// <param name="path">An enumeration of hashes, describing the path to walk.</param>
        /// <param name="depth">The maximum number of nodes to walk past.</param>
        /// <returns>The node at the end of the walked path. Or `null` if the entire path couldn't be walked through.</returns>
        public Node walk(IList<Hash> path, uint depth = uint.MaxValue)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (depth == 0)
                throw new ArgumentOutOfRangeException("depth", "The depth must be 1 or more");

            uint walked = 0;
            var currentThis = this;                 // Current node in this tree
            var currentPath = path.GetEnumerator(); // Current hash in the path
            currentPath.MoveNext();

            while (walked < depth && currentThis != null && walked < path.Count)
            {
                walked += 1;

                bool found = false;
                foreach (var node in currentThis.children)
                {
                    if (node.hash.Equals(currentPath.Current))
                    {
                        currentPath.MoveNext();
                        currentThis = node;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return null;
            }

            return currentThis;
        }
    }

    /// <summary>
    /// Contains a node path, and can calculate the average win percentage of the path.
    /// 
    /// Also contains static functions to help find, for example, the path that will most likely result in a win.
    /// </summary>
    public class Average
    {
        /// <summary>
        /// The node path
        /// </summary>
        public List<Node> path;

        /// <summary>
        /// Calculates the average win percentage of the path.
        /// </summary>
        public float averageWinPercent
        {
            get
            {
                if(this.path.Count == 0)
                    return 0;

                float totalPercent = 0;
                this.path.ForEach(node => totalPercent += node.winPercent);

                return (totalPercent / this.path.Count);
            }
        }

        /// <summary>
        /// Creates a new Average.
        /// </summary>
        public Average()
        {
            this.path = new List<Node>();
        }

        /// <summary>
        /// Finds the path in 'root' that is statistically the most likely to win.
        /// </summary>
        /// <param name="root">The root node to search through.</param>
        /// <returns>An 'Average' containing the path with the highest win percent.</returns>
        public static Average statisticallyBest(Node root)
        {
            var path = new Average();
            var best = new Average();

            Func<Node, bool, bool> walk = null;
            walk = delegate(Node node, bool noAdd) 
            {
                if(!noAdd) // Used so we don't add in the root node
                    path.path.Add(node);

                if(node.children.Count == 0)
                {
                    if(path.averageWinPercent > best.averageWinPercent)
                        best.path = new List<Node>(path.path); // Copies the list
                }
                else
                {
                    foreach(var child in node.children)
                    {
                        walk(child, false);
                        path.path.RemoveAt(path.path.Count - 1); // Shrink the list by 1 once its done
                    }
                }

                return false; // Dummy value
            };

            walk(root, true);
            return best;
        }
    }
}
