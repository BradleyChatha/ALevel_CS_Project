using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CS_Project.Game.Controllers;

/// <summary>
/// Contains everything related to the game board.
/// </summary>
namespace CS_Project.Game
{
    /// <summary>
    /// Contains the state of the game board, and provides an interface to manipulate it.
    /// </summary>
    public partial class Board
    {
        /// <summary>
        /// The amount of pieces used on the board.
        /// </summary>
        public const uint pieceCount = 3*3;

        private const uint _xIndex = 0; // Index in _board for the x player
        private const uint _oIndex = 1; // Index in _board for the o player

        private Board.Piece[] _board        { get; set; } // Current state of the Board.
        private Board.Flags   _flags        { get; set; }
        private Board.Stage   _stage        { get; set; }
        private Controller    _current      { get; set; } // The Controller who currently has control of the board.
        private int           _lastIndex    { get; set; } // The last index used to place a piece

        [Flags]
        private enum Flags : byte
        {
            HasSetPiece = 1 << 0 // Flag for whether the current controller has set its piece. Used to check for correctness
        }

        private enum Stage
        {
            Initialisation,
            InControllerTurn,    // The board is waiting for a controller to perform it's turn.
            AfterControllerTurn, // The controller has just made its turn.
            NoMatch
        }

        private Hash createHashFor(Board.Piece piece)
        {
            var hash = new Hash(piece);

            for(var i = 0; i < this._board.Length; i++)
                hash.setPiece(this._board[i], i);

            return hash;
        }

        private Piece checkForWin(out bool isTie)
        {
            Func<uint, uint, uint, Piece, bool> check = null;
            check = delegate(uint i1, uint i2, uint i3, Piece p)
            {
                return this._board[i1] == p
                    && this._board[i2] == p
                    && this._board[i3] == p;
            };

            isTie = false;

            var pieces = new Piece[]{ Board.Piece.x, Board.Piece.o };
            foreach(var piece in pieces)
            {
                if (check(0, 1, 2, piece)) return piece; // Top row
                if (check(3, 4, 5, piece)) return piece; // Middle row
                if (check(6, 7, 8, piece)) return piece; // Bottom row
                if (check(0, 4, 8, piece)) return piece; // Top left to bottom right, and vice-versa
                if (check(2, 4, 6, piece)) return piece; // Top right to bottom left, and vice-versa
                if (check(0, 3, 6, piece)) return piece; // Top left to bottom left, and vice-versa
                if (check(1, 4, 7, piece)) return piece; // Top middle to bottom middle, and vice-versa
                if (check(2, 5, 8, piece)) return piece; // Top right to bottom right, and vice-versa
            }
            
            var emptyCount = this._board.Count(p => p == Piece.empty);
            isTie          = (emptyCount == 0);

            return Piece.empty;
        }

        /// <summary>
        /// Default constructor for a board.
        /// </summary>  
        public Board()
        {
            this._stage = Stage.NoMatch;
            this._board = new Board.Piece[Board.pieceCount];

            for(var i = 0; i < this._board.Length; i++)
                this._board[i] = Piece.empty;
        }

        /// <summary>
        /// Starts a match between two controllers.
        /// 
        /// Note to self: Run all of this stuff in a seperate thread, otherwise the GUI will freeze.
        /// Use System.Collections.Concurrent.ConcurrentQueue to talk between the two threads.
        /// </summary>
        /// <param name="xCon">The controller for the X piece.</param>
        /// <param name="oCon">The controller for the O piece.</param>
        public void startMatch(Controller xCon, Controller oCon)
        {
            Debug.Assert(this._stage == Stage.NoMatch, "Attempted to start a match while another match is in progress.");

            #region Setup controllers.
            Debug.Assert(xCon != null, "The X controller is null.");
            Debug.Assert(oCon != null, "The O controller is null.");
            this._stage = Stage.Initialisation;

            xCon.onMatchStart(this, Piece.x);
            oCon.onMatchStart(this, Piece.o);

            // Reset some stuff
            this._lastIndex = int.MaxValue;
            #endregion

            #region Match turn logic
            Board.Piece turnPiece = Piece.o;     // The piece of who's turn it is.
            Board.Piece wonPiece  = Piece.empty; // The piece of who's won. Empty for no win.
            bool isTie            = false;
            while (wonPiece == Piece.empty && !isTie)
            {
                // Unset some flags
                this._flags &= ~Flags.HasSetPiece;

                #region Do controller turn
                this._stage     = Stage.InControllerTurn;
                var hash        = this.createHashFor(turnPiece);
                var controller  = (turnPiece == Piece.x) ? xCon : oCon;
                this._current   = controller;
                
                controller.onDoTurn(hash, this._lastIndex);
                Debug.Assert((this._flags & Flags.HasSetPiece) != 0, 
                             $"The controller using the {turnPiece} piece didn't place a piece.");
                #endregion

                #region Do after controller turn
                this._stage = Stage.AfterControllerTurn;
                hash        = this.createHashFor(turnPiece);
                controller.onAfterTurn(hash);
                #endregion

                #region Misc stuff
                wonPiece = this.checkForWin(out isTie);
                if(turnPiece == Piece.x)
                    turnPiece = Piece.o;
                else
                    turnPiece = Piece.x;
                #endregion
            }
            #endregion
            Debug.Assert(wonPiece != Piece.empty || isTie, "There was no win condition, but the loop still ended.");

            #region Process the win
            if(isTie)
            {
                xCon.onMatchEnd(MatchResult.Tied);
                oCon.onMatchEnd(MatchResult.Tied);
            }
            else if (wonPiece == Piece.o)
            {
                xCon.onMatchEnd(MatchResult.Lost);
                oCon.onMatchEnd(MatchResult.Won);
            }
            else
            {
                xCon.onMatchEnd(MatchResult.Won);
                oCon.onMatchEnd(MatchResult.Lost);
            }
            #endregion

            #region Reset variables
            this._stage   = Stage.NoMatch;
            this._current = null;

            for (var i = 0; i < this._board.Length; i++)
                this._board[i] = Piece.empty;
            #endregion
        }

        /// <summary>
        /// Sets a piece on the board.
        /// </summary>
        /// <param name="index">The index of where to place the piece.</param>
        /// <param name="controller">The controller that's placing the piece.</param>
        public void set(int index, Controller controller)
        {
            // There are so many ways to use this function wrong...
            // But I need these checks here to make sure my code is correct.
            Debug.Assert(this._stage == Stage.InControllerTurn,
                         "A controller attempted to place its piece outside of its onDoTurn function.");
            Debug.Assert(this.isCurrentController(controller), 
                         "Something's gone wrong somewhere. An incorrect controller is being used.");
            Debug.Assert(index < Board.pieceCount && index >= 0, 
                         $"Please use Board.pieceCount to properly limit the index. Index = {index}");
            Debug.Assert((this._flags & Flags.HasSetPiece) == 0,
                         "A controller has attempted to place its piece twice. This is a bug.");
            Debug.Assert(this._board[index] == Piece.empty,
                         "A controller attempted to place its piece over another piece. Enough information is passed to prevent this.");

            this._board[index] = controller.piece;
            this._lastIndex    = index;
            this._flags       |= Flags.HasSetPiece;
        }

        /// <summary>
        /// Determines if the given controller is the controller who's turn it currently is.
        /// </summary>
        /// <param name="controller">The controller to check.</param>
        /// <returns>'true' if 'controller' is the controller's who's controlling the current turn.</returns>
        public bool isCurrentController(Controller controller)
        {
            return (this._current == controller);
        }
    }

    // This part of 'Board' is used for anything that should be accessed like "Board.Piece"
    // Wheras the other part is for the actual board class.
    public partial class Board
    {
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
    public class Hash : ICloneable, ISerialiseable
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
        public Board.Piece otherPiece {private set; get;} // This and myPiece are readonly since once they've been set, they shouldn't be changed.

        /// <summary>
        /// The piece that the user of this class is using.
        /// </summary>
        public Board.Piece myPiece {private set; get;}

        private Hash(Board.Piece myPiece, bool dummyParam)
        {
            if (myPiece == Board.Piece.empty)
                throw new HashException("myPiece must not be Board.Piece.empty");

            this.myPiece    = myPiece;
            this.otherPiece = (myPiece == Board.Piece.o) ? Board.Piece.x
                                                         : Board.Piece.o;
        }

        /// <summary>
        /// Default constructor for a Hash
        /// 
        /// Note that the 'myPiece' for the hash will be 'Board.Piece.x'
        /// </summary>
        public Hash() : this(Board.Piece.x)
        { }

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
        public Hash(Board.Piece myPiece, IEnumerable<char> hash) : this(myPiece, false)
        {
            this._hash = hash.ToArray();
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
        /// Clones the Hash.
        /// </summary>
        /// <returns>A clone of this instance of Hash.</returns>
        public object Clone()
        {
            return new Hash(this.myPiece, this.ToString());
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

        // override object.Equals(auto generated with tweaks)
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Hash)obj;
            return (other.ToString() == this.ToString()) && (other.myPiece == this.myPiece);
        }

        // override object.GetHashCode(auto generated)
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // implement ISerialiseable.serialise
        public void serialise(BinaryWriter output)
        {
            this.checkCorrectness();
            Debug.Assert(this._hash.Length <= byte.MaxValue, "The hash's length can't fit into a byte.");

            output.Write((byte)this._hash.Length);
            output.Write((char[])this._hash);
            output.Write((byte)this.myPiece);
            output.Write((byte)this.otherPiece);
        }

        // implement ISerialiseable.deserialise
        public void deserialise(BinaryReader input)
        {
            var length      = input.ReadByte();
            this._hash      = input.ReadChars(length);
            this.myPiece    = (Board.Piece)input.ReadByte();
            this.otherPiece = (Board.Piece)input.ReadByte();

            this.checkCorrectness();
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
