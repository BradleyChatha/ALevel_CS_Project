using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CS_Project.Game.Controllers
{
    public sealed class AI : Controller
    {
        // Trees
        private Node _globalTree;
        private Node _localTree;

        // Data needed for creating the localTree/performing a move.
        private bool    _useRandom; // If true, then the AI will perform a random move. This is used as a fallback.
        private Random  _rng;

        // Other
        private NodeDebugWindow _debug;       // Debug window for general info + local tree
        private NodeDebugWindow _globalDebug; // Debug window purely for the global tree.

        /// <summary>
        /// Performs an action using the debug window's dispatcher (only if it's not null).
        /// </summary>
        private void doDebugAction(Action action)
        {
            if (this._debug != null)
                this._debug.Dispatcher.Invoke(action);
        }

        private void addToLocal(Board.Hash hash, int index)
        {
            // This is a cheeky way to add onto the end of the local tree.
            // Since there is only a single path in the local tree, this is fine.
            this._localTree.walkEveryPath(path =>
            {
                var node = new Node(hash, (uint)index);

                if (path.Count == 0)
                    this._localTree.children.Add(node);
                else
                    path.Last().children.Add(node);
            });

            // Update the local tree debugger. A clone is made in case the tree is edited before the debug window finishes updating.
            this.doDebugAction(() => this._debug.updateNodeData((Node)this._localTree.Clone()));
        }

        public AI(NodeDebugWindow window, NodeDebugWindow globalWindow)
        {
            if(window != null)
            {
                this._debug = window;
                window.Show();
            }

            if(globalWindow != null)
            {
                this._globalDebug = globalWindow;
                globalWindow.Show();
            }

            this._useRandom  = false;
            this._rng        = new Random();
            this._globalTree = Node.root; // TEMPORARY: Currently there is no way to store/retrieve trees, so we use a new root every time.
        }

        public override void onMatchStart(Board board, Board.Piece myPiece)
        {
            base.onMatchStart(board, myPiece);

            // Reset the local tree and whatever else
            this._localTree = Node.root;
            this._useRandom = false;

            // Update the global tree debugger
            this.doDebugAction(() => this._globalDebug.updateNodeData(this._globalTree));
            this.doDebugAction(() => this._globalDebug.updateStatusText("[GLOBAL MOVE TREE DEBUGGER]"));
        }

        public override void onMatchEnd(Board.Hash state, int index, MatchResult result)
        {
            // The windows wont' be closed, as I may still need them.
            // I can just close them manually afterwards.
            base.onMatchEnd(state, index, result);

            // If the last piece placed was by the other controller, then it won't have a node in the local tree.
            // So we quickly add it.
            if(!state.isMyPiece(index))
                this.addToLocal(state, index);

            // Steps:
            // 1. Walk over the global tree using the local tree. (A custom walk will have to be done, Node.walk fails on non-existing nodes)
            // 2. If a node in the local tree doesn't exist in the global tree, create it. Set 'node' to this node.
            // 3. Otherwise, set 'node' to the node walked to.
            // 4. Bump up 'node's win/loss counter as needed.
            // 5. [When possible] save to the global tree file.

            // If, for whatever reason, the local tree is empty. Then return, otherwise we'll crash a few lines down.
            if(this._localTree.children.Count == 0)
                return;

            bool loop   = true;                         // Variable to control the while loop.
            var  parent = this._globalTree;             // Current node in the global tree. Starts off at the root.
            var  local  = this._localTree.children[0];  // Current node in the local tree.
            while(loop)
            {
                // Go over all the children in the parent.
                Node node  = null;
                foreach (Node child in parent.children)
                {
                    // If the child is in the local tree, then set it as 'node'
                    if(child.hash.ToString() == local.hash.ToString())
                    {
                        node = child;
                        break;
                    }
                }

                // If no matching node was found, create it!
                if(node == null)
                {
                    // We create a new node so we don't add in all the children with it 
                    // (while correct, it breaks my original visualisation of the algorithm. I need this here to keep it sane in my head)
                    node = new Node(local.hash, local.index);
                    parent.children.Add(node);
                }

                // Bump up the counters
                if(result == MatchResult.Won)
                    node.won += 1;
                else if(result == MatchResult.Lost)
                    node.lost += 1;
                else
                {
                    // If we tie... Then we don't do anything I guess.
                }

                // Break once we've been through all the nodes.
                if(local.children.Count == 0)
                    break;

                // Move onto the next nodes.
                parent = node;
                local  = local.children[0];
            }

            this.doDebugAction(() => this._globalDebug.updateNodeData(this._globalTree));
        }

        public override void onAfterTurn(Board.Hash boardState, int index)
        {
            // Add the AI's move.
            this.addToLocal(boardState, index);
        }

        public override void onDoTurn(Board.Hash boardState, int index)
        {
            // Add the other controller's last move.
            if(index != int.MaxValue)
                this.addToLocal(boardState, index);

            if(this._useRandom)
                this.doRandom(boardState);
            else
                this.doStatisticallyBest(boardState);
        }

        // Uses the statisticallyBest method for choosing a move.
        private void doStatisticallyBest(Board.Hash hash)
        {
            this.doDebugAction(() => this._debug.updateStatusText("Function doStatisticallyBest was chosen."));

            Node parent = null; // This is the node that will be used in statisticallyBest

            // If our local tree has some nodes in it, then...
            if(this._localTree.children.Count > 0)
            {
                // First, get the path of the local tree.
                List<Node> localPath = null;
                this._localTree.walkEveryPath(path => localPath = new List<Node>(path));

                // Then, attempt to walk through the global tree, and find the last node in the path.
                Node last     = null;
                var couldWalk = this._globalTree.walk(localPath.Select(n => n.hash).ToList(), 
                                                      n => last = n);

                // If we get null, or couldn't walk the full path, then fallback to doRandom
                if(!couldWalk || last == null)
                {
                    this._useRandom = true;
                    this.doRandom(hash);
                    return;
                }

                parent = last;
            }
            else // Otherwise, the global tree's root is the parent.
                parent = this._globalTree;

            // Then use statisticallyBest on the node, so we can figure out our next move.
            var average = Average.statisticallyBest(parent);

            // If Average.statisticallyBest fails, fall back to doRandom.
            if(average.path.Count == 0)
            {
                this._useRandom = true;
                this.doRandom(hash);
                return;
            }

            // Otherwise, get the first node. Make sure it's a move we make. Then perform it!
            var node    = average.path[0];
            Debug.Assert(node.hash.isMyPiece((int)node.index), "Something's gone a *bit* wrong.");

            base.board.set((int)node.index, this);
        }

        // Uses the randomAll method for choosing a move.
        private void doRandom(Board.Hash hash)
        {
            this.doDebugAction(() => this._debug.updateStatusText("Function doRandom was chosen."));

            // Tis a bit naive, but meh.
            while(true)
            {
                var index = this._rng.Next(0, (int)Board.pieceCount);
                
                if(hash.isEmpty(index))
                {
                    this.board.set(index, this);
                    break;
                }
            }
        }
    }
}
