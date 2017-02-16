using CS_Project.Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Hash = CS_Project.Game.Board.Hash;
using System;

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

            // This action is used with Node.walk, it will keep track of the last node it visited.
            Node last = null;
            Action<Node> getLast = (node => last = node);

            // First, seeing if it returns false on an invalid path.
            // "M........" -> "MM......."
            var path = new Hash[] { new Hash(p, $"{m}........"),
                                    new Hash(p, $"{m}{m}.......")};
            Assert.IsFalse(root.walk(path, getLast)); // "walk" returns false if the entire path couldn't be followed.
            Assert.IsTrue(last == root.children[0]);  // Confirm that the only node we walked to was "M........"

            // Then see if depth works
            Assert.IsTrue(root.walk(path, getLast, 1)); // It will return true now, since we walked 'depth' amount of nodes sucessfully
            Assert.IsTrue(last == root.children[0]);

            // Then finally see if it walks through an entire path properly
            // We have to change the last hash in path first though
            path[1] = new Hash(p, $"{m}.{o}......"); // "M........" -> "M.O......"

            Assert.IsTrue(root.walk(path, getLast));            // If this returns true, then the entire path was walked
            Assert.IsTrue(last == root.children[0].children[0]); // THen we make sure the last node walked to is correct.
        }

        [TestMethod()]
        public void nodeSerialiseTest()
        {
            var m = Hash.myChar;
            var o = Hash.otherChar;
            var dir = "Temp";
            var file = "Serialised_Node.bin";
            var path = $"{dir}/{file}";

            // Make sure the temp directory exists.
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Then, write out a simple tree to a file.
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

                    /* Tree:
                     *                                          /---[0]---/ "MO......."
                     *                  /---[0]---/ "M........"
                     * "........."(root)
                     *                  /---[1]---/ ".M......."
                     * */

                    root.serialise(writer);
                }
            }

            // Then, read the tree back in from the file, and confirm *every* node is correct.
            using (var stream = File.OpenRead(path))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var root = Node.root;
                    root.deserialise(reader, GameFiles.treeFileVersion);

                    var n = root.children[0]; // Current node we're asserting
                    Assert.AreEqual(new Hash(Board.Piece.X, $"{m}........"), n.hash);
                    Assert.AreEqual(0u, n.index);
                    Assert.AreEqual(3u, n.won);
                    Assert.AreEqual(5u, n.lost);

                    n = root.children[1];
                    Assert.AreEqual(new Hash(Board.Piece.X, $".{m}......."), n.hash);
                    Assert.AreEqual(1u, n.index);
                    Assert.AreEqual(1u, n.won);
                    Assert.AreEqual(4u, n.lost);

                    n = root.children[0].children[0];
                    Assert.AreEqual(new Hash(Board.Piece.O, $"{m}{o}......."), n.hash);
                    Assert.AreEqual(1u, n.index);
                    Assert.AreEqual(2u, n.won);
                    Assert.AreEqual(3u, n.lost);
                }
            }
        }

        [TestMethod()]
        public void mergeTest()
        {
            var m = Hash.myChar;
            var o = Hash.otherChar;

            // This is the tree that's going to be merged into.
            var destination = Node.root;

            // "M........" (W:2, L:1) -> [0] "MO......." (W:1 L:0)
            //                        -> [1] "M.O......" (W:1 L:1)
            // ".M......." (W:0 L:0)
            var source = Node.root;
            source.children.Add            (new Node(new Hash(Board.Piece.X, $"{m}........"),   0, 2, 1));
            source.children.Add            (new Node(new Hash(Board.Piece.X, $".{m}......."),   1, 0, 0));
            source.children[0].children.Add(new Node(new Hash(Board.Piece.X, $"{m}{o}......."), 1, 1, 0));
            source.children[0].children.Add(new Node(new Hash(Board.Piece.X, $"{m}.{o}......"), 2, 1, 1));

            // Doing it twice to make sure it works for nodes that exist, and ones that don't exist.
            Node.merge(destination, source);
            Node.merge(destination, source);

            var node = destination.children[0];
            Assert.IsTrue(node.hash.ToString()  == $"{m}........");
            Assert.IsTrue(node.index            == 0);
            Assert.IsTrue(node.won              == 4);
            Assert.IsTrue(node.lost             == 2);

            node = destination.children[1];
            Assert.IsTrue(node.hash.ToString()  == $".{m}.......");
            Assert.IsTrue(node.index            == 1);
            Assert.IsTrue(node.won              == 0);
            Assert.IsTrue(node.lost             == 0);

            node = destination.children[0].children[0];
            Assert.IsTrue(node.hash.ToString()  == $"{m}{o}.......");
            Assert.IsTrue(node.index            == 1);
            Assert.IsTrue(node.won              == 2);
            Assert.IsTrue(node.lost             == 0);

            node = destination.children[0].children[1];
            Assert.IsTrue(node.hash.ToString()  == $"{m}.{o}......");
            Assert.IsTrue(node.index            == 2);
            Assert.IsTrue(node.won              == 2);
            Assert.IsTrue(node.lost             == 2);
        }
    }
}