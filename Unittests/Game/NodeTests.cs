using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Hash = CS_Project.Game.Board.Hash;

namespace CS_Project.Game.Tests
{
    [TestClass()]
    public class NodeTests
    {
        [TestMethod()]
        public void walkTest()
        {
            var m = Hash.myChar;
            var o = Hash.otherChar;
            var p = Board.Piece.X;

            var root = Node.root;
            root.children.AddRange(new Node[] 
                                  {
                                      new Node(new Hash(p, $"{m}........"), 0),
                                      new Node(new Hash(p, $".{o}......."), 1)
                                  });
            root.children[0].children.Add(new Node(new Hash(p, $"{m}.{o}......"), 2));

            // Visualisation of what 'tree' looks like
            /*                               /----[0]----/ "M.O......"
             *      /----[0]----/ "M........"
             * root
             *      /----[1]----/ ".O......."
             * */

            // First, seeing if it returns null on an invalid path.
            // "M........" -> "MM......."
            var path = new Hash[] { new Hash(p, $"{m}........"),
                                    new Hash(p, $"{m}{m}.......")};
            Assert.IsNull(root.walk(path));

            // Then see if depth works
            Assert.IsTrue(path[0].Equals(root.walk(path, 1).hash));

            // Then finally see if it walks through things properly
            // We have to change the last hash in path first though
            path[1] = new Hash(p, $"{m}.{o}......"); // "M........" -> "M.O......"

            Assert.IsTrue(path[1].Equals(root.walk(path).hash));
        }

        [TestMethod()]
        public void nodeSerialiseTest()
        {
            var m    = Hash.myChar;
            var o    = Hash.otherChar;
            var dir  = "Temp";
            var file = "Serialised_Node.bin";
            var path = $"{dir}/{file}";

            if(!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var stream = File.Create(path))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var root = Node.root;
                    root.children.AddRange(new Node[]
                                          {
                                              new Node(new Hash(Board.Piece.X, $"{m}........"), 0, 3, 5),
                                              new Node(new Hash(Board.Piece.X, $".{m}......."), 1, 1, 4)
                                          });
                    root.children[0].children.Add(new Node(new Hash(Board.Piece.O, $"{m}{o}......."), 1, 2, 3));

                    root.serialise(writer);
                }
            }

            using (var stream = File.OpenRead(path))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var root = Node.root;
                    root.deserialise(reader);

                    var n = root.children[0]; // Current node we're asserting
                    Assert.AreEqual(new Hash(Board.Piece.X, $"{m}........"), n.hash);
                    Assert.AreEqual(0u,                                      n.index);
                    Assert.AreEqual(3u,                                      n.won);
                    Assert.AreEqual(5u,                                      n.lost);

                    n = root.children[1];
                    Assert.AreEqual(new Hash(Board.Piece.X, $".{m}......."), n.hash);
                    Assert.AreEqual(1u,                                      n.index);
                    Assert.AreEqual(1u,                                      n.won);
                    Assert.AreEqual(4u,                                      n.lost);

                    n = root.children[0].children[0];
                    Assert.AreEqual(new Hash(Board.Piece.O, $"{m}{o}......."), n.hash);
                    Assert.AreEqual(1u,                                        n.index);
                    Assert.AreEqual(2u,                                        n.won);
                    Assert.AreEqual(3u,                                        n.lost);
                }
            }
        }
    }
}