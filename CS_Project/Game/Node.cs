using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Contains the Move Tree.
/// </summary>
namespace CS_Project.Game
{
    // Private class used in Node.merge to keep track of some data.
    // NodeMergeInfo keeps track of two versions of a node, and an index.
    // The 'node' Node is the version of the node that's in the 'source' tree.
    // The 'parent' Node is the version of the node that's in the 'destination' tree
    // The index is used as an index to 'node.children'.
    class NodeMergeInfo
    {
        internal Node node   { set; get; } // Node being used. (This node is in the 'source' tree)
        internal Node parent { set; get; } // The version of 'node' that is inside the 'destination' tree.
        internal int index   { set; get; } // Index used for node.children
    }

    /// <summary>
    /// A node describing a single move.
    /// </summary>
    public class Node : ICloneable, ISerialiseable
    {
        /// <summary>
        /// The hash of the board after the move was made.
        /// </summary>
        public Board.Hash hash { private set; get; }

        /// <summary>
        /// The index of what slot was changed this move.
        /// </summary>
        public uint index { private set; get; }

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
                return new Node(new Board.Hash(Board.Piece.X, "........."), uint.MaxValue);
            }
        }

        /// <summary>
        /// Creates a new Node
        /// </summary>
        /// <param name="hash">The 'Board.Hash' of the board after the move was made.</param>
        /// <param name="index">The index of the slot that was changed.</param>
        /// <param name="won">How many times this move was used in a won match.</param>
        /// <param name="lost">The opposite of 'won'.</param>
        /// <exception cref="System.ArgumentNullException">When `hash` is null.</exception>
        public Node(Board.Hash hash, uint index, uint won = 0, uint lost = 0)
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
            this.hash     = new Board.Hash();
        }

        /// <summary>
        /// Clones the node, and all of it's children.
        /// </summary>
        /// <returns>A clone of this node.</returns>
        public object Clone()
        {
            var toReturn = new Node((Board.Hash)this.hash.Clone(), this.index, this.won, this.lost);
            
            foreach(var child in this.children) 
                toReturn.children.Add((Node)child.Clone());

            return toReturn;
        }

        /// <summary>
        /// Given a list of hashes, walks through this tree of nodes, up to a certain depth, and performs
        /// an action on every node walked to.
        /// </summary>
        /// <param name="path">The path of hashes to follow.</param>
        /// <param name="action">The action to perform on every node followed.</param>
        /// <param name="depth">The maximum amount of nodes to walk to (inclusive).</param>
        /// <returns>
        /// 'true' if the entire 'path' was walked, or if 'depth' amount of nodes were walked to. 
        /// 'false' if the 'path' was cut short.
        /// </returns>
        public bool walk(IList<Board.Hash> path, Action<Node> action, uint depth = uint.MaxValue)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (action == null)
                throw new ArgumentNullException("action"); 

            if (depth == 0)
                throw new ArgumentOutOfRangeException("depth", "The depth must be 1 or more");

            uint walked = 0;
            var currentThis = this;                 // Current node in this tree
            var currentPath = path.GetEnumerator(); // Current hash in the path
            currentPath.MoveNext();

            // While:
            //     We haven't walked the full depth.
            // AND There are still nodes in the path we need to follow.
            while (walked < depth && walked < path.Count)
            {
                walked += 1;

                bool found = false; // If 'true', then a node from the path was found. If 'false', then the path ended prematurly
                foreach (var node in currentThis.children)
                {
                    if (node.hash.Equals(currentPath.Current))
                    {
                        currentPath.MoveNext();
                        currentThis = node;
                        found = true;

                        action(node);
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Walks over every possible path in the node tree, and calls an action on each path.
        /// </summary>
        /// <param name="action">
        /// The action to perform on every path. 
        /// Note that the parameter given is not a copy of the original, so anytime the parameter is stored somewhere,
        /// a copy should be created.
        /// </param>
        public void walkEveryPath(Action<List<Node>> action)
        {
            if(action == null)
                throw new ArgumentNullException("action");

            var path = new List<Node>();

            Func<Node, bool, bool> walk = null;
            walk = delegate (Node node, bool noAdd)
            {
                if (!noAdd) // Used so we don't add in the root node
                    path.Add(node);

                if (node.children.Count == 0)
                {
                    action(path);
                }
                else
                {
                    foreach (var child in node.children)
                    {
                        walk(child, false);
                        path.RemoveAt(path.Count - 1); // Shrink the list by 1 once its done, so we don't have to create a new list.
                    }
                }

                return false; // Dummy value
            };

            walk(this, true);
        }

        /// <summary>
        /// Merges the nodes (including the wins/losses counter) from a source tree, into a destination tree.
        /// 
        /// Any nodes in 'source' that don't belong in 'destination' will be created.
        /// </summary>
        /// <param name="destination">The tree that will be modified.</param>
        /// <param name="source">The tree providing the nodes to merge.</param>
        public static void merge(Node destination, Node source)
        {
            if(destination == null)
                throw new ArgumentNullException("destination");
            if(source == null)
                throw new ArgumentNullException("source");

            // If, for whatever reason, the source tree is empty. Then return, otherwise we'll crash a few lines down.
            if (source.children.Count == 0)
                return;

            var info     = new Stack<NodeMergeInfo>();        // Too annoying to explain, but this allows nodes with multiple children to be merged.
            var parent   = destination;                       // Current node in the destination tree. Starts off at the root.
            var local    = new NodeMergeInfo {node = source}; // Current node in the source tree.
            local.parent = parent;
            while (true)
            {
                // Get the next local node.
                // local will be set to null if there are no nodes left.
                while(true)
                {
                    // First, get how many children the node will have left for us.
                    var childCount = local.node.children.Count - local.index;

                    // Then, if there's still children, push the current local onto the info stack, and make the next child the new local.
                    if(childCount > 0)
                    {
                        info.Push(local);
                        local = new NodeMergeInfo { node = local.node.children[local.index++] };
                        break;
                    }
                    else
                    {
                        // Otherwise, if there's no children left.
                        if(info.Count == 0) // Break if the stack is empty
                        {
                            local = null;
                            break;
                        }

                        // Otherwise, pop the stack, set it to local, and run the loop over it.
                        local  = info.Pop();
                        parent = local.parent;
                    }
                }

                // If there are no more nodes left.
                if(local == null)
                    break;

                // Go over all the children in the parent.
                Node node = null;
                foreach (Node child in parent.children)
                {
                    // If the child is in the source tree, then set it as 'node'
                    if (child.hash.ToString() == local.node.hash.ToString())
                    {
                        node = child;
                        break;
                    }
                }

                // If no matching node was found, create it!
                if (node == null)
                {
                    // We create a new node so we don't add in all the children with it 
                    // (while correct, it breaks my original visualisation of the algorithm. I need this here to keep it sane in my head)
                    node = new Node(local.node.hash, local.node.index);
                    parent.children.Add(node);
                }

                // Then add in the win/loss counters
                node.won  += local.node.won;
                node.lost += local.node.lost;

                // Set the new parents
                parent       = node;
                local.parent = parent;
            }
        }

        // implement ISerialisable.serialise
        public void serialise(BinaryWriter output)
        {
            /*
             * Format of a serialised Node(TREE version 1):
             *  [Serialised Board.Hash of the Node]
             *  [4 bytes, Node.index]
             *  [4 bytes, Node.won]
             *  [4 bytes, Node.lost]
             *  [1 byte,  Node.children.count]
             *      [All of the nodes children are then serialised.]
             * */

            Debug.Assert(this.children.Count <= byte.MaxValue, "For some reason, this Node has over 255 children e_e?");

            this.hash.serialise(output);
            output.Write((uint)this.index);
            output.Write((uint)this.won);
            output.Write((uint)this.lost);
            output.Write((byte)this.children.Count);
            
            foreach(var node in this.children)
                node.serialise(output);
        }

        // implemented ISerialisable.deserialise
        public void deserialise(BinaryReader input, uint version)
        {
            // Version 1 of the TREE format.
            if(version == 1 || version == 2)
            {
                this.hash.deserialise(input, version);
                this.index = input.ReadUInt32();
                this.won   = input.ReadUInt32();
                this.lost  = input.ReadUInt32();

                var count     = input.ReadByte();
                this.children = new List<Node>();
                for(int i = 0; i < count; i++)
                {
                    var node = new Node();
                    node.deserialise(input, version); 
                    this.children.Add(node);
                }
            }
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
                if(this.path.Count == 0) // Avoid divide-by-zero errors
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
            var pathAverage = new Average();
            var bestAverage = new Average();

            root.walkEveryPath(path =>
            {
                pathAverage.path = path;

                if (pathAverage.averageWinPercent > bestAverage.averageWinPercent)
                    bestAverage.path = new List<Node>(pathAverage.path); // Copies the list
            });
            return bestAverage;
        }
    }
}
