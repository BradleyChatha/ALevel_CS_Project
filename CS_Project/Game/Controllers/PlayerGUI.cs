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

        private void updateGUI(Hash boardState, Board.Piece turn)
        {
            this._window.Dispatcher.Invoke(() =>
            {
                this._window.updateBoard(boardState);
                this._window.turnLabel.Content = $"It is {turn}'s turn";
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

            this._window.Dispatcher.Invoke(() => 
            {
                this._window.userPieceLabel.Content = $"[You are {myPiece}]";
            });
        }

        public override void onMatchEnd(MatchResult result)
        {
            string message    = "";
            var    enemyPiece = (this.piece == Board.Piece.x) ? Board.Piece.o : Board.Piece.x;

                 if(result == MatchResult.Won)  message = $"You ({this.piece}) have won!";
            else if(result == MatchResult.Lost) message = $"The enemy ({enemyPiece}) has won!";
            else if(result == MatchResult.Tied) message = "It's a tie! No one wins.";
            else                                message = "[Unknown result]";

            this._window.Dispatcher.Invoke(() => 
            {
                this._window.turnLabel.Content = message;
            });

            base.onMatchEnd(result);
        }

        public override void onAfterTurn(Hash boardState)
        {
            // After the player has done their turn, update the GUI
            this.updateGUI(boardState, (this.piece == Board.Piece.o) ? Board.Piece.x : Board.Piece.o);
        }

        public override void onDoTurn(Hash boardState, int index)
        {
            // Update the GUI to display the opponent's last move.
            this.updateGUI(boardState, this.piece);

            // Wait for the GUI to have signaled that the player has made a move.
            Message msg;
            while(true)
            {
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

                var info = msg as PlayerPlaceMessage;
                if(!boardState.isEmpty(info.index))
                    continue;

                this.board.set(info.index, this);
                break;
            }
        }
    }
}
