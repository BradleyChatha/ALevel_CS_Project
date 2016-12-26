using Microsoft.VisualStudio.TestTools.UnitTesting;
using CS_Project.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace CS_Project.Tests
{
    [TestClass()]
    public class SerialiseTests
    {
        [TestMethod()]
        public void hashSerialiseTest()
        {
            var dir  = "Temp";
            var file = "Serialised_Hash.bin";
            var path = $"{dir}/{file}";

            if(!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var stream = File.Create(path))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var hash  = new Hash(Board.Piece.x, "MO.OM.MOM");
                    var hash2 = new Hash(Board.Piece.o, "OM.MO.OMO");

                    hash.serialise(writer);
                    hash2.serialise(writer);
                }
            }

            using (var stream = File.OpenRead(path))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var hash = new Hash();

                    hash.deserialise(reader);
                    Assert.AreEqual("MO.OM.MOM", hash.ToString());
                    Assert.AreEqual(Board.Piece.x, hash.myPiece);
                    Assert.AreEqual(Board.Piece.o, hash.otherPiece);

                    hash.deserialise(reader);
                    Assert.AreEqual("OM.MO.OMO", hash.ToString());
                    Assert.AreEqual(Board.Piece.o, hash.myPiece);
                    Assert.AreEqual(Board.Piece.x, hash.otherPiece);
                }
            }
        }
    }
}