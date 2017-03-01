namespace CS_Project.Game.Controllers
{
    /// <summary>
    /// Contains the result of a match.
    /// </summary>
    public enum MatchResult
    {
        /// <summary>
        /// The controller won the match.
        /// </summary>
        Won,
        
        /// <summary>
        /// The controller lost the match.
        /// </summary>
        Lost,

        /// <summary>
        /// The controller tied with the other one.
        /// </summary>
        Tied
    }

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
        /// 
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
        /// 
        /// <param name="boardState">The final state of the board.</param>
        /// <param name="index">The index of where the last piece was placed on the board.</param>
        /// <param name="result">Contains the match result.</param>
        public virtual void onMatchEnd(Board.Hash boardState, int index, MatchResult result)
        {
            this.board = null;
            this.piece = Board.Piece.Empty;
        }

        /// <summary>
        /// Called whenever the controller has to process its turn.
        /// </summary>
        /// 
        /// <param name="boardState">
        ///     The hash of the current state of the board.
        /// 
        ///     The hash's 'myPiece' is set to the same one given to the controller with the 'onMatchStart' function.
        /// </param>
        /// <param name="index">
        ///     The index of the last move the other controller made. 
        ///     If the other controller hasn't made a move yet, then this will be int.MaxValue
        /// </param>
        public abstract void onDoTurn(Board.Hash boardState, int index);

        /// <summary>
        /// Called after the controller has taken its turn.
        /// </summary>
        /// 
        /// <param name="boardState">The state of the board after the controller's turn.</param>
        /// <param name="index">The index of where the last piece was placed on the board.</param>
        public abstract void onAfterTurn(Board.Hash boardState, int index);
    }
}
