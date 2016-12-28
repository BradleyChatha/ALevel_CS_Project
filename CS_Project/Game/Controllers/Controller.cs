using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_Project.Game.Controllers
{
    /// <summary>
    /// The base class for any class that can control the game board.
    /// </summary>
    public abstract class Controller
    {
        /// <summary>
        /// The instance of 'Board' that is currently using this controller.
        /// </summary>
        public Board board { private set; get; }

        /// <summary>
        /// The Board.Piece that this controller has been assigned to for the match.
        /// </summary>
        public Board.Piece piece { private set; get; }

        /// <summary>
        /// Called whenever a new match is started.
        /// </summary>
        /// <param name="board">The board that is using this controller.</param>
        /// <param name="myPiece">Which piece this controller has been given.</param>
        public virtual void onMatchStart(Board board, Board.Piece myPiece)
        {
            this.board = board;
            this.piece = myPiece;
        }

        /// <summary>
        /// Called whenever the match has ended.
        /// 
        /// Notes for inheriting classes: Call 'super.onMatchEnd' only at the end of the function.
        /// </summary>
        /// <param name="didIWin">'true' if this controller won. 'false' if the other controller won.</param>
        public virtual void onMatchEnd(bool didIWin)
        {
            this.board = null;
            this.piece = Board.Piece.empty;
        }

        /// <summary>
        /// Called whenever the controller has to process its turn.
        /// </summary>
        /// <param name="boardState">
        ///     The hash of the current state of the board.
        /// 
        ///     The hash's 'myPiece' is set to the same one given to the controller with the 'onMatchStart' function.
        /// </param>
        /// <param name="index">
        ///     The index of the last move the other controller made. 
        ///     If the other controller hasn't made a move yet, then this will be int.MaxValue
        /// </param>
        public abstract void onDoTurn(Hash boardState, int index);

        /// <summary>
        /// Called after the controller has taken its turn.
        /// </summary>
        /// <param name="boardState">The state of the board after the controller's turn.</param>
        public abstract void onAfterTurn(Hash boardState);
    }
}
