using Microsoft.VisualStudio.TestTools.UnitTesting;
using CS_Project.Game;
using System;
using System.Linq;
using System.IO;

using Hash = CS_Project.Game.Board.Hash;

namespace Unittests
{
    [TestClass]
    public class Game_Hash
    {
        [TestMethod]
        public void testHash()
        {
            var hash = new Hash(Board.Piece.X);

            // First, check that all 9 indicies can be used (and also test isEmpty)
            foreach(int i in Enumerable.Range(0, (int)Board.pieceCount))
                Assert.IsTrue(hash.isEmpty(i));

            // Test setPiece and getPiece
            hash.setPiece(Board.Piece.Empty, 0);
            hash.setPiece(Board.Piece.O,     1);
            hash.setPiece(Board.Piece.X,     2);
            Assert.AreEqual(hash.getPiece(0), Board.Piece.Empty);
            Assert.AreEqual(hash.getPiece(1), Board.Piece.O);
            Assert.AreEqual(hash.getPiece(2), Board.Piece.X);

            // Test setPiece exception
            try
            {
                hash.setPiece(Board.Piece.X, 0, true);  // Set the 0th piece to x
                hash.setPiece(Board.Piece.O, 0, false); // Then try to overwrite it, with the overwrite flag to set to false. Triggering the exception.
                Assert.Fail("The exception for setPiece wasn't thrown.");
            }
            catch(HashException) { }

            // Test the enforceIndex exception
            try
            {
                hash.setPiece(Board.Piece.Empty, (int)Board.pieceCount);
                Assert.Fail("The exception for enforceIndex wasn't thrown.");
            }
            catch(ArgumentOutOfRangeException) { }

            // Test isMyPiece
            hash.setPiece(hash.myPiece, 0, true);
            Assert.IsTrue(hash.isMyPiece(0));

            // Test ToString
            hash.setPiece(hash.myPiece,      0, true);
            hash.setPiece(Board.Piece.Empty, 1, true);
            hash.setPiece(hash.otherPiece,   2, true);
            Assert.AreEqual(hash.ToString(), $"{Hash.myChar}.{Hash.otherChar}......");

            // Test second constructor of Hash
            hash = new Hash(Board.Piece.X, "MO.OM.MOM");

            var mineIndicies  = new int[]{0, 4, 6, 8};
            var otherIndicies = new int[]{1, 3, 7};
            var emptyIndicies = new int[]{2, 5};

            Array.ForEach(mineIndicies,  i => { Assert.IsTrue( hash.isMyPiece(i)); });
            Array.ForEach(otherIndicies, i => { Assert.IsTrue(!hash.isMyPiece(i)); });
            Array.ForEach(emptyIndicies, i => { Assert.IsTrue( hash.isEmpty(i)); });

            // Test Clone and Equals
            Assert.IsTrue(hash.Clone().Equals(hash));
        }

        [TestMethod()]
        public void hashSerialiseTest()
        {
            var dir = "Temp";
            var file = "Serialised_Hash.bin";
            var path = $"{dir}/{file}";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var stream = File.Create(path))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var hash = new Hash(Board.Piece.X, "MO.OM.MOM");
                    var hash2 = new Hash(Board.Piece.O, "OM.MO.OMO");

                    hash.serialise(writer);
                    hash2.serialise(writer);
                }
            }

            using (var stream = File.OpenRead(path))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var hash = new Hash();

                    hash.deserialise(reader, GameFiles.treeFileVersion);
                    Assert.AreEqual("MO.OM.MOM", hash.ToString());
                    Assert.AreEqual(Board.Piece.X, hash.myPiece);
                    Assert.AreEqual(Board.Piece.O, hash.otherPiece);

                    hash.deserialise(reader, GameFiles.treeFileVersion);
                    Assert.AreEqual("OM.MO.OMO", hash.ToString());
                    Assert.AreEqual(Board.Piece.O, hash.myPiece);
                    Assert.AreEqual(Board.Piece.X, hash.otherPiece);
                }
            }
        }
    }
}
