using System.Threading;

namespace CS_Project.Game.Controllers
{
    /// <summary>
    /// This controller is used alongside the GUI to allow the player to see, and interact with the game board.
    /// 
    /// IMPORTANT SELF NOTE: Remember that this controller runs in the *Game* thread, not the *GUI* thread.
    /// </summary>
    class PlayerGUIController : Controller
    {
        private MainWindow _window { get; set; }

        /// <summary>
        /// Updates the GUI to reflect the new state of the board.
        /// </summary>
        /// <param name="boardState">The new state of the baord.</param>
        /// <param name="turn">Who's turn it currently is.</param>
        private void updateGUI(Board.Hash boardState, Board.Piece turn)
        {
            this._window.Dispatcher.Invoke(() =>
            {
                this._window.updateBoard(boardState);
                this._window.updateText(null, $"It is {turn}'s turn");
            });
        }

        /// <summary>
        /// Constructor for the controller.
        /// </summary>
        /// <param name="window">The window that is displaying the GUI.</param>
        public PlayerGUIController(MainWindow window)
        {
            this._window = window;
        }

        public override void onMatchStart(Board board, Board.Piece myPiece)
        {
            base.onMatchStart(board, myPiece);

            // When the match starts, tell the player which piece they're using.
            this._window.Dispatcher.Invoke(() => 
            {
                this._window.updateText($"[You are {myPiece}]");
            });
        }

        public override void onMatchEnd(Board.Hash state, int index, MatchResult result)
        {
            // One the match has ended, figure out who won, and generate the appropriate win message.
            string message    = "";
            var    enemyPiece = (this.piece == Board.Piece.X) ? Board.Piece.O : Board.Piece.X;

                 if(result == MatchResult.Won)  message = $"You ({this.piece}) have won!";
            else if(result == MatchResult.Lost) message = $"The enemy ({enemyPiece}) has won!";
            else if(result == MatchResult.Tied) message = "It's a tie! No one wins.";
            else                                message = "[Unknown result]";

            // Then update the GUI to display who's won.
            this.updateGUI(state, this.piece);
            this._window.Dispatcher.Invoke(() => 
            {
                this._window.updateText(null, message);
                this._window.onEndMatch();
            });

            base.onMatchEnd(state, index, result);
        }

        public override void onAfterTurn(Board.Hash boardState, int index)
        {
            // After the player has done their turn, update the GUI to display it's the enemy's turn.
            this.updateGUI(boardState, (this.piece == Board.Piece.O) ? Board.Piece.X : Board.Piece.O);
        }

        public override void onDoTurn(Board.Hash boardState, int index)
        {
            // Update the GUI to display the opponent's last move, as well as to tell the user it's their turn.
            this.updateGUI(boardState, this.piece);

            // Let the player choose their piece
            // Note: This does not go through the dispatcher, since it can make it seem like the GUI drops input
            // (due to the latency of Dispatcher.Invoke). It *shouldn't* create a data-race, since nothing should be accessing it
            // when this code is running.
            // It's worth keeping this line in mind though, future me, in case strange things happen.
            this._window.unlockBoard();

            // Wait for the GUI to have signaled that the player has made a move.
            Message msg;
            while(true)
            {
                // Check every 50ms for a message.
                // If we didn't use a sleep, then the CPU usage skyrockets.
                if(!this._window.gameQueue.TryDequeue(out msg))
                {
                    Thread.Sleep(50);
                    continue;
                }

                // If we get a message not meant for us, requeue it.
                if(!(msg is PlayerPlaceMessage))
                {
                    this._window.gameQueue.Enqueue(msg);
                    continue;
                }

                // Otherwise, see if the placement is valid, and perform it.
                var info = msg as PlayerPlaceMessage;
                if(!boardState.isEmpty(info.index))
                    continue;

                this.board.set(info.index, this);
                break;
            }
        }
    }
}
