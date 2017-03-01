using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CS_Project.Game.Controllers
{
    /// <summary>
    /// The controller that provides the AI.
    /// </summary>
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
        private const string    _globalName = "global_move_tree"; // The name used to save the global move tree.

        /// <summary>
        /// Performs an action using the debug window's dispatcher (only if it's not null).
        /// </summary>
        private void doDebugAction(Action action)
        {
            if (this._debug != null)
                this._debug.Dispatcher.Invoke(action);
        }

        /// <summary>
        /// Adds a new node to the end of the local tree.
        /// </summary>
        /// 
        /// <param name="hash">The hash of the board.</param>
        /// <param name="index">The index of where the piece was placed.</param>
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

        /// <summary>
        /// Constructs a new version of the AI.
        /// 
        /// The AI will attempt to load its global tree when constructed.
        /// </summary>
        /// 
        /// <param name="window">A debug window for the local tree.</param>
        /// <param name="globalWindow">A debug window for the global tree.</param>
        public AI(NodeDebugWindow window = null, NodeDebugWindow globalWindow = null)
        {
            // Show the debug windows.
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

            // Setup variables.
            this._useRandom = false;
            this._rng       = new Random();

            // Load the move tree
            this._globalTree = GameFiles.loadTree(AI._globalName, false);
            if(this._globalTree == null)
                this._globalTree = Node.root;
        }

        // implement Controller.onMatchStart
        public override void onMatchStart(Board board, Board.Piece myPiece)
        {
            base.onMatchStart(board, myPiece);

            // Reset some variables.
            this._localTree = Node.root;
            this._useRandom = false;

            // Update the global tree debugger
            this.doDebugAction(() => this._globalDebug.updateNodeData(this._globalTree));
            this.doDebugAction(() => this._globalDebug.updateStatusText("[GLOBAL MOVE TREE DEBUGGER]"));
        }

        // implement Controller.onMatchEnd
        public override void onMatchEnd(Board.Hash state, int index, MatchResult result)
        {
            // The windows won't be closed, as I may still need them.
            // I can just close them manually afterwards.
            base.onMatchEnd(state, index, result);

            // If the last piece placed was by the other controller, then it won't have a node in the local tree.
            // So we quickly add it.
            if(!state.isMyPiece(index))
                this.addToLocal(state, index);

            // Now, the amount of nodes in the local tree should be the same as: Board.pieceCount - amountOfEmptySlots
            // If not, then we've not created a node somewhere.
            // (This test was created to prevent this bug from happening again. Praise be for the debug windows.)
            var emptyCount = 0; // How many spaces are empty
            for(int i = 0; i < Board.pieceCount; i++) // Count the empty spaces.
                emptyCount += (state.isEmpty(i)) ? 1 : 0;

            this._localTree.walkEveryPath(path =>
            {
                // Then make sure the tree's length is the same
                var amountOfMoves = Board.pieceCount - emptyCount;
                Debug.Assert(path.Count == amountOfMoves, 
                            $"We've haven't added enough nodes to the local tree!\n"
                          + $"empty = {emptyCount} | amountOfMoves = {amountOfMoves} | treeLength = {path.Count}");

                // Finally, bump the won/lost counters in the local tree
                foreach(var node in path)
                {
                    if(result == MatchResult.Won)
                        node.won += 1;
                    else if(result == MatchResult.Lost)
                        node.lost += 1;
                    else
                    {
                        // If we tie, don't bump anything up.
                    }
                }
            });

            // Then merge the local tree into the global one.
            Node.merge(this._globalTree, this._localTree);

            // Save the global tree, and update the debug window.
            GameFiles.saveTree(AI._globalName, this._globalTree);
            this.doDebugAction(() => this._globalDebug.updateNodeData(this._globalTree));
        }

        // implement Controller.onAfterTurn
        public override void onAfterTurn(Board.Hash boardState, int index)
        {
            // Add the AI's move.
            this.addToLocal(boardState, index);
        }

        // implement Controller.onDoTurn
        public override void onDoTurn(Board.Hash boardState, int index)
        {
            // Add the other controller's last move, if they made one.
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

            Node parent = null; // This is the node that will be used as the root in statisticallyBest

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

            // Then use statisticallyBest on the parent, so we can figure out our next move.
            var average = Average.statisticallyBest(parent);

            // If Average.statisticallyBest fails, fall back to doRandom.
            // Or, if the average win percent of the path is less than 25%, then there's a 25% chance to do a random move.
            if(average.path.Count == 0 
            ||(average.averageWinPercent < 25.0 && this._rng.NextDouble() < 0.25))
            {
                this._useRandom = true;
                this.doRandom(hash);
                return;
            }

            // Otherwise, get the first node. Make sure it's a move we make. Then perform it!
            var node = average.path[0];
            Debug.Assert(node.hash.isMyPiece((int)node.index), "Something's gone a *bit* wrong.");

            base.board.set((int)node.index, this);
        }

        // Uses the randomAll method for choosing a move.
        private void doRandom(Board.Hash hash)
        {
            this.doDebugAction(() => this._debug.updateStatusText("Function doRandom was chosen."));

            // Tis a bit naive, but meh.
            // Just keep generating a random number between 0 and 9 (exclusive) until we find an empty slot.
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
