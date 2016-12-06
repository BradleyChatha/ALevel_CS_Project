using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Contains everything related to the game board.
/// </summary>
namespace CS_Project.Game
{
    /// <summary>
    /// Contains the state of the game board, and provides an interface to manipulate it.
    /// </summary>
    public class Board
    {
        /// <summary>
        /// The amount of pieces used on the board.
        /// </summary>
        public const uint pieceCount = 3*3;

        /// <summary>
        /// Describes the different board pieces
        /// </summary>
        public enum Piece
        {
            /// <summary>
            /// The X piece
            /// </summary>
            x,

            /// <summary>
            /// The O piece
            /// </summary>
            o,

            /// <summary>
            /// An empty board piece
            /// </summary>
            empty
        }
    }

    /// <summary>
    /// Contains a hash of the game board.
    /// </summary>
    public class Hash
    {
        private char[] _hash; // The hash itself

        /// <summary>
        /// Char that represents the other player's piece
        /// </summary>
        public const char otherChar = 'O';

        /// <summary>
        /// Char that represents the piece of the player using this class
        /// </summary>
        public const char myChar    = 'M';

        /// <summary>
        /// Char that represents an empty space
        /// </summary>
        public const char emptyChar = '.';

        /// <summary>
        /// The piece that the other player is using.
        /// </summary>
        public readonly Board.Piece otherPiece; // This and myPiece are readonly since once they've been set, they shouldn't be changed.

        /// <summary>
        /// The piece that the user of this class is using.
        /// </summary>
        public readonly Board.Piece myPiece;

        private Hash(Board.Piece myPiece, bool dummyParam)
        {
            if (myPiece == Board.Piece.empty)
                throw new HashException("myPiece must not be Board.Piece.empty");

            this.myPiece    = myPiece;
            this.otherPiece = (myPiece == Board.Piece.o) ? Board.Piece.x
                                                         : Board.Piece.o;
        }

        /// <summary>
        /// Constructs a new Hash.
        /// </summary>
        /// <param name="myPiece">The piece that you are using, this is needed so the class knows how to correctly format the hash.</param>
        /// <exception cref="Game.Hash">If `myPiece` is `Board.Piece.empty`</exception>
        public Hash(Board.Piece myPiece) : this(myPiece, new string(Hash.emptyChar, 9))
        {
        }

        /// <summary>
        /// Constructs a new Hash from a given hash string.
        /// </summary>
        /// <param name="myPiece">The piece that you are using, this is needed so the class knows how to correctly format the hash.</param>
        /// <param name="hash">
        /// The hash string to use.
        /// 
        /// An internal check is made with every function call, that determines if the hash is still correct:
        ///     * The hash's length must be the same as 'Board.pieceCount'
        ///     * The hash's characters must only be made up of 'Hash.myChar', 'Hash.otherChar', and 'Hash.emptyChar'.
        ///     
        /// If the given hash fails to meet any of these checks, then a message box will be displayed.
        /// In the future, when I can be bothered, exceptions will be thrown instead so this constructor is more user-friendly.
        /// </param>
        /// <exception cref="Game.Hash">If `myPiece` is `Board.Piece.empty`</exception>
        public Hash(Board.Piece myPiece, string hash) : this(myPiece, false)
        {
            this._hash = hash.ToCharArray();
            this.checkCorrectness();
        }

        /// <returns>The actual hash itself, properly formatted.</returns>
        public override string ToString()
        {
            this.checkCorrectness();
            return new string(this._hash);
        }

        /// <summary>
        /// Sets a piece in the hash.
        /// </summary>
        /// <param name="piece">The piece to use</param>
        /// <param name="index">The index to place the piece</param>
        /// <param name="allowOverwrite">See the `HashException` part of this documentation</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
        /// <exception cref="Game.HashException">If `allowOverwrite` is false, and there is a non-empty piece at 'index'</exception>
        public void setPiece(Board.Piece piece, int index, bool allowOverwrite = false)
        {
            if(this.getPieceChar(index) != Hash.emptyChar && !allowOverwrite)
                throw new HashException($"Attempted to place {piece} at index {index}, however a non-null piece is there and allowOverwrite is false. Hash = {this._hash}");

            char pieceChar = '\0';

            if      (piece == this.myPiece)     pieceChar = Hash.myChar;
            else if (piece == this.otherPiece)  pieceChar = Hash.otherChar;
            else                                pieceChar = Hash.emptyChar;

            this._hash[index] = pieceChar;
            this.checkCorrectness();
        }

        /// <summary>
        /// Gets the board piece at a certain index.
        /// </summary>
        /// <param name="index">The index to use</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
        /// <returns>The board piece at 'index'</returns>
        public Board.Piece getPiece(int index)
        {
            Board.Piece piece   = Board.Piece.empty;
            var pieceChar       = this.getPieceChar(index);

            switch(pieceChar)
            {
                case Hash.emptyChar: piece = Board.Piece.empty; break;
                case Hash.myChar:    piece = this.myPiece;      break;
                case Hash.otherChar: piece = this.otherPiece;   break;

                default: Debug.Assert(false, "This should not have happened"); break;
            }

            return piece;
        }

        /// <summary>
        /// Determines if a specific piece in the hash is the user's.
        /// </summary>
        /// <param name="index">The index to check.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
        /// <returns>`true` if the piece at `index` belongs to the user of this class. `false` otherwise.</returns>
        public bool isMyPiece(int index)
        {
            return this.getPieceChar(index) == Hash.myChar;
        }

        /// <summary>
        /// Determines if a specific piece in the hash is empty.
        /// </summary>
        /// <param name="index">The index to check.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
        /// <returns>`true` if the piece at `index` is empty. `false` otherwise.</returns>
        public bool isEmpty(int index)
        {
            return this.getPieceChar(index) == Hash.emptyChar;
        }

        /// <summary>
        /// Gets the character at a certain index in the hash.
        /// </summary>
        private char getPieceChar(int index)
        {
            this.enforceIndex(index);
            this.checkCorrectness();
            return this._hash[index];
        }

        /// <summary>
        /// In D, there's something called an 'invariant' function, which allows me to run code to make sure the class is still
        /// in correct condition after every function call.
        /// 
        /// This doesn't seem to exist in C#, so this funciton is called by every other function to make sure it's still correct.
        /// 
        /// This allows me to be confident that the class is working as it should be.
        /// </summary>
        private void checkCorrectness()
        {
            Debug.Assert(this._hash.Length == Board.pieceCount, 
                        $"The length of the hash is {this._hash.Length} when it should be {Board.pieceCount}");

            Debug.Assert(this._hash.All(c => (c == Hash.emptyChar || c == Hash.myChar || c == Hash.otherChar)),
                        $"The hash contains an invalid character. Hash = {this._hash}");
        }

        /// <summary>
        /// Enforces that `index` is between 0 and Board.pieceCount
        /// </summary>
        /// <param name="index">The index to check</param>
        private void enforceIndex(int index)
        {
            if (index >= Board.pieceCount)
                throw new ArgumentOutOfRangeException("index", $"index must be between 0 and {index}(exclusive)");
        }
    }


    /// <summary>
    /// (Auto generated from the 'Exception' code-snippet.)
    /// 
    /// Thrown whenever the 'Hash' class is used incorrectly.
    /// </summary>
    [Serializable]
    public class HashException : Exception
    {
        public  HashException() { }
        public  HashException(string message) : base(message) { }
        public  HashException(string message, Exception inner) : base(message, inner) { }
        protected  HashException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
