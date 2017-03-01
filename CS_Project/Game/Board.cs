using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private Board.Flags   _flags        { get; set; } // A set of bitflags used to keep track of some stuff.
        private Board.Stage   _stage        { get; set; } // The current state of the match.
        private Controller    _current      { get; set; } // The Controller who currently has control of the board.
        private int           _lastIndex    { get; set; } // The last index used to place a piece.

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

        /// <summary>
        /// Creates a hash of the board, using a given piece as the "myPiece".
        /// </summary>
        /// 
        /// <param name="piece">The piece that should be used as the "myPiece"</param>
        private Hash createHashFor(Board.Piece piece)
        {
            var hash = new Hash(piece);

            for(var i = 0; i < this._board.Length; i++)
                hash.setPiece(this._board[i], i);

            return hash;
        }

        /// <summary>
        /// Determines if anyone has won yet.
        /// </summary>
        /// 
        /// <param name="isTie">Set to 'true' if there was a tie.</param>
        /// 
        /// <returns>The piece that won, or Piece.empty if no one has won yet.</returns>
        private Piece checkForWin(out bool isTie)
        {
            // A closure that checks 3 spaces on the board, and returns true if they all are the 'p' piece.
            Func<uint, uint, uint, Piece, bool> check = null;
            check = delegate(uint i1, uint i2, uint i3, Piece p)
            {
                return this._board[i1] == p
                    && this._board[i2] == p
                    && this._board[i3] == p;
            };

            // Default isTie to false.
            isTie = false;

            // For both X and O, check all the possible win positions.
            var pieces = new Piece[]{ Board.Piece.X, Board.Piece.O };
            foreach(var piece in pieces)
            {
                if(check(0, 1, 2, piece)) return piece; // Top row
                if(check(3, 4, 5, piece)) return piece; // Middle row
                if(check(6, 7, 8, piece)) return piece; // Bottom row
                if(check(0, 4, 8, piece)) return piece; // Top left to bottom right, and vice-versa
                if(check(2, 4, 6, piece)) return piece; // Top right to bottom left, and vice-versa
                if(check(0, 3, 6, piece)) return piece; // Top left to bottom left, and vice-versa
                if(check(1, 4, 7, piece)) return piece; // Top middle to bottom middle, and vice-versa
                if(check(2, 5, 8, piece)) return piece; // Top right to bottom right, and vice-versa
            }
            
            // If there are no empty spaces, and the above checks didn't make the function return, then we've tied.
            var emptyCount = this._board.Count(p => p == Piece.Empty);
            isTie          = (emptyCount == 0);

            return Piece.Empty;
        }

        /// <summary>
        /// Default constructor for a board.
        /// </summary>  
        public Board()
        {
            this._stage = Stage.NoMatch;
            this._board = new Board.Piece[Board.pieceCount];

            for(var i = 0; i < this._board.Length; i++)
                this._board[i] = Piece.Empty;
        }

        /// <summary>
        /// Starts a match between two controllers.
        /// 
        /// Note to self: Run all of this stuff in a seperate thread, otherwise the GUI will freeze.
        /// Use System.Collections.Concurrent.ConcurrentQueue to talk between the two threads.
        /// </summary>
        /// 
        /// <param name="xCon">The controller for the X piece.</param>
        /// <param name="oCon">The controller for the O piece.</param>
        public void startMatch(Controller xCon, Controller oCon)
        {
            Debug.Assert(this._stage == Stage.NoMatch, "Attempted to start a match while another match is in progress.");

            #region Setup controllers.
            Debug.Assert(xCon != null, "The X controller is null.");
            Debug.Assert(oCon != null, "The O controller is null.");
            this._stage = Stage.Initialisation;

            // Inform the controllers what piece they're using.
            xCon.onMatchStart(this, Piece.X);
            oCon.onMatchStart(this, Piece.O);

            // Reset some stuff
            this._lastIndex = int.MaxValue;
            #endregion

            #region Match turn logic
            Board.Piece turnPiece = Piece.O;     // The piece of who's turn it is.
            Board.Piece wonPiece  = Piece.Empty; // The piece of who's won. Empty for no win.
            bool isTie            = false;
            while (wonPiece == Piece.Empty && !isTie) // While there hasn't been a tie, and no one has won yet.
            {
                // Unset some flags
                this._flags &= ~Flags.HasSetPiece;

                #region Do controller turn
                this._stage     = Stage.InControllerTurn;
                var hash        = this.createHashFor(turnPiece);        // Create a hash from the point of view of who's turn it is.
                var controller  = (turnPiece == Piece.X) ? xCon : oCon; // Figure out which controller to use this turn.
                this._current   = controller;
                
                controller.onDoTurn(hash, this._lastIndex); // Allow the controller to perform its turn.
                Debug.Assert((this._flags & Flags.HasSetPiece) != 0, 
                             $"The controller using the {turnPiece} piece didn't place a piece.");
                #endregion

                #region Do after controller turn
                this._stage = Stage.AfterControllerTurn;
                hash        = this.createHashFor(turnPiece);   // Create another hash for the controller
                controller.onAfterTurn(hash, this._lastIndex); // And let the controller handle its 'after move' logic
                #endregion

                #region Misc stuff
                wonPiece  = this.checkForWin(out isTie);                // See if someone's won/tied yet.
                turnPiece = (turnPiece == Piece.X) ? Piece.O : Piece.X; // Change who's turn it is
                #endregion
            }
            #endregion
            Debug.Assert(wonPiece != Piece.Empty || isTie, "There was no win condition, but the loop still ended.");

            #region Process the win
            // Create a hash for both controllers, then tell them whether they tied, won, or lost.
            var stateX = this.createHashFor(Piece.X);
            var stateO = this.createHashFor(Piece.O);
            if(isTie)
            {
                xCon.onMatchEnd(stateX, this._lastIndex, MatchResult.Tied);
                oCon.onMatchEnd(stateO, this._lastIndex, MatchResult.Tied);
            }
            else if (wonPiece == Piece.O)
            {
                xCon.onMatchEnd(stateX, this._lastIndex, MatchResult.Lost);
                oCon.onMatchEnd(stateO, this._lastIndex, MatchResult.Won);
            }
            else
            {
                xCon.onMatchEnd(stateX, this._lastIndex, MatchResult.Won);
                oCon.onMatchEnd(stateO, this._lastIndex, MatchResult.Lost);
            }
            #endregion

            #region Reset variables
            this._stage   = Stage.NoMatch;
            this._current = null;

            for (var i = 0; i < this._board.Length; i++)
                this._board[i] = Piece.Empty;
            #endregion
        }

        /// <summary>
        /// Sets a piece on the board.
        /// </summary>
        /// 
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

            Debug.Assert(this._board[index] == Piece.Empty,
                         "A controller attempted to place its piece over another piece. Enough information is passed to prevent this.");

            this._board[index] = controller.piece;
            this._lastIndex    = index;
            this._flags       |= Flags.HasSetPiece;
        }

        /// <summary>
        /// Determines if the given controller is the controller who's turn it currently is.
        /// </summary>
        /// 
        /// <param name="controller">The controller to check.</param>
        /// 
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
        public enum Piece : byte
        {
            /// <summary>
            /// The X piece
            /// </summary>
            X = 0,

            /// <summary>
            /// The O piece
            /// </summary>
            O = 1,

            /// <summary>
            /// An empty board piece
            /// </summary>
            Empty = 2
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
            public const char myChar = 'M';

            /// <summary>
            /// Char that represents an empty space
            /// </summary>
            public const char emptyChar = '.';

            /// <summary>
            /// The piece that the other player is using.
            /// </summary>
            public Board.Piece otherPiece { private set; get; }

            /// <summary>
            /// The piece that the user of this class is using.
            /// </summary>
            public Board.Piece myPiece { private set; get; }

            private Hash(Board.Piece myPiece, bool dummyParam)
            {
                if (myPiece == Board.Piece.Empty)
                    throw new HashException("myPiece must not be Board.Piece.empty");

                // Figure out who is using what piece.
                this.myPiece    = myPiece;
                this.otherPiece = (myPiece == Board.Piece.O) ? Board.Piece.X
                                                             : Board.Piece.O;
            }

            /// <summary>
            /// Default constructor for a Hash
            /// 
            /// Note that the 'myPiece' for the hash will be 'Board.Piece.x'
            /// </summary>
            public Hash() : this(Board.Piece.X)
            { }

            /// <summary>
            /// Constructs a new Hash.
            /// </summary>
            /// 
            /// <exception cref="CS_Project.Game.HashException">If `myPiece` is `Board.Piece.empty`</exception>
            /// 
            /// <param name="myPiece">The piece that you are using, this is needed so the class knows how to correctly format the hash.</param>
            public Hash(Board.Piece myPiece) : this(myPiece, new string(Hash.emptyChar, 9))
            {
            }

            /// <summary>
            /// Constructs a new Hash from a given hash string.
            /// </summary>
            /// 
            /// <exception cref="CS_Project.Game.HashException">If `myPiece` is `Board.Piece.empty`</exception>
            /// 
            /// <param name="myPiece">The piece that you are using, this is needed so the class knows how to correctly format the hash.</param>
            /// <param name="hash">
            ///     The hash string to use.
            /// 
            ///     An internal check is made with every function call, that determines if the hash is still correct:
            ///         * The hash's length must be the same as 'Board.pieceCount'
            ///         * The hash's characters must only be made up of 'Hash.myChar', 'Hash.otherChar', and 'Hash.emptyChar'.
            ///     
            ///     If the given hash fails to meet any of these checks, then an error box will be displayed.
            ///     In the future, when I can be bothered, exceptions will be thrown instead so the errors can actually be handled.
            /// </param>
            public Hash(Board.Piece myPiece, IEnumerable<char> hash) : this(myPiece, false)
            {
                this._hash = hash.ToArray();
                this.checkCorrectness();
            }

            /// <returns>The actual hash itself, properly formatted.</returns>
            public override string ToString()
            {
                this.checkCorrectness();
                return new string(this._hash); // Create an immutable copy of the hash.
            }

            /// <summary>
            /// Sets a piece in the hash.
            /// </summary>
            /// 
            /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
            /// <exception cref="CS_Project.Game.HashException">If `allowOverwrite` is false, and there is a non-empty piece at 'index'</exception>
            /// 
            /// <param name="piece">The piece to use</param>
            /// <param name="index">The index to place the piece</param>
            /// <param name="allowOverwrite">See the `HashException` part of this documentation</param>
            public void setPiece(Board.Piece piece, int index, bool allowOverwrite = false)
            {
                // Enforce the behaviour of `allowOverwrite`
                if (this.getPieceChar(index) != Hash.emptyChar && !allowOverwrite)
                    throw new HashException($"Attempted to place {piece} at index {index}, however a non-null piece is there and allowOverwrite is false. Hash = {this._hash}");

                // Figure out which character to use to represent `piece`.
                char pieceChar = '\0';

                     if (piece == this.myPiece)    pieceChar = Hash.myChar;
                else if (piece == this.otherPiece) pieceChar = Hash.otherChar;
                else                               pieceChar = Hash.emptyChar;
                    
                // Then place that character into the hash.
                this._hash[index] = pieceChar;
                this.checkCorrectness();
            }

            /// <summary>
            /// Gets the board piece at a certain index.
            /// </summary>
            /// 
            /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
            /// 
            /// <param name="index">The index to use</param>
            /// 
            /// <returns>The board piece at 'index'</returns>
            public Board.Piece getPiece(int index)
            {
                Board.Piece piece = Board.Piece.Empty;
                var pieceChar     = this.getPieceChar(index);

                // Convert the character into a Board.Piece
                switch (pieceChar)
                {
                    case Hash.emptyChar: piece = Board.Piece.Empty; break;
                    case Hash.myChar:    piece = this.myPiece;      break;
                    case Hash.otherChar: piece = this.otherPiece;   break;

                    default: Debug.Assert(false, "This should not have happened"); break;
                }

                return piece;
            }

            /// <summary>
            /// Determines if a specific piece in the hash is the user's.
            /// </summary>
            /// 
            /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
            /// 
            /// <param name="index">The index to check.</param>
            /// 
            /// <returns>`true` if the piece at `index` belongs to the user of this class. `false` otherwise.</returns>
            public bool isMyPiece(int index)
            {
                return this.getPieceChar(index) == Hash.myChar;
            }

            /// <summary>
            /// Determines if a specific piece in the hash is empty.
            /// </summary>
            /// <exception cref="System.ArgumentOutOfRangeException">If index is >= `Board.pieceCount`</exception>
            /// 
            /// <param name="index">The index to check.</param>
            /// 
            /// <returns>`true` if the piece at `index` is empty. `false` otherwise.</returns>
            public bool isEmpty(int index)
            {
                return this.getPieceChar(index) == Hash.emptyChar;
            }

            /// <summary>
            /// Clones the Hash.
            /// </summary>
            /// 
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
            /// (From Future Me: There does seem to be an invariant in C#, but it's far too late to bother changing this)
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
            /// 
            /// <exception cref="System.ArgumentOutOfRangeException">Thrown if `index` is >= to `Board.pieceCount`</exception>
            /// 
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

                // See 'TREE version 2' in the deserialise function for the format.

                byte[] bytes = new byte[3];
                for(int i = 0; i < Board.pieceCount; i++)
                {
                    var piece     = this.getPiece(i); // The piece at the slot.
                    var byteIndex = (i * 2) / 8;      // Index into 'bytes' for which byte to modify.
                    var bitOffset = (i * 2) % 8;      // The offset into the byte to write the data to.
                    var pieceBits = 0;

                         if(piece == Piece.Empty)     pieceBits = 0;
                    else if(piece == this.myPiece)    pieceBits = 1;
                    else if(piece == this.otherPiece) pieceBits = 2;

                    bytes[byteIndex] |= (byte)((byte)pieceBits << bitOffset);
                }

                // Put in the 'M' and 'O' bits
                // 0000 0100 = 0x4 (O = X, M = O)
                // 0000 1000 = 0x8 (O = O, M = X)
                bytes[2] |= (this.myPiece == Piece.O) ? (byte)0x4 : (byte)0x8;

                output.Write(bytes);
            }

            // implement ISerialiseable.deserialise
            public void deserialise(BinaryReader input, uint version)
            {
                // TREE version 1
                if(version == 1)
                {
                    var length      = input.ReadByte();
                    this._hash      = input.ReadChars(length);
                    this.myPiece    = (Board.Piece)input.ReadByte();
                    this.otherPiece = (Board.Piece)input.ReadByte();
                }

                /**
                 * Format of a hash: (TREE version 2)
                 * [3 bytes]
                 * byte 1: 4433 2211
                 * byte 2: 8877 6655
                 * byte 3: 0000 MO99
                 * 
                 * Numbers such as '11' and '55' represent the Board.Piece in slot '1' and '5', respectively.
                 * 'M' and 'O' represent the Board.Piece of 'My' piece and 'Other' piece, respectively.
                 * '0' Represents 'unused'
                 * 
                 * For 'M' and 'O':
                 *   0 = Board.Piece.O
                 *   1 = Board.Piece.X
                 *   
                 *   So if the 'MO' bits were 0x40: M = O, O = X
                 *   If 'MO' were 0x80:             M = X, O = O
                 *   
                 * For '11' to '99':
                 *   0 = Empty
                 *   1 = M
                 *   2 = O
                 * **/
                if (version == 2)
                {
                    var bytes        = input.ReadBytes(3);
                    var identityBits = bytes[2] & 0xC; // Identity = The bits defining 'myPiece' and 'otherPiece'. 0xC = 1100
                    this.myPiece     = (identityBits & 0x4) == 0x4 ? Piece.O : Piece.X;
                    this.otherPiece  = (identityBits & 0x4) == 0x4 ? Piece.X : Piece.O;

                    for (int i = 0; i < Board.pieceCount; i++)
                    {
                        var byteIndex = (i * 2) / 8;  // Index into 'bytes' for which byte to use.
                        var bitOffset = (i * 2) % 8;  // The offset into the byte to write the data to.
                        var byte_     = bytes[byteIndex];
                        var piece     = (byte_ >> bitOffset) & 0x3; // 0x3 == 0000 0011

                        switch(piece)
                        {
                            case 0: this._hash[i] = Hash.emptyChar; break;
                            case 1: this._hash[i] = Hash.myChar; break;
                            case 2: this._hash[i] = Hash.otherChar; break;

                            default:
                                throw new IOException("");
                        }
                    }
                }

                this.checkCorrectness();
            }
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
