using Microsoft.VisualStudio.TestTools.UnitTesting;
using CS_Project.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_Project.Game.Tests
{
    [TestClass()]
    public class GameFilesTests
    {
        [TestMethod()]
        public void treeExistsTest()
        {
            const string name = "Test_dummy";

            Assert.IsFalse(GameFiles.treeExists(name));
            GameFiles.saveTree(name, Node.root);
            Assert.IsTrue(GameFiles.treeExists(name));

            GameFiles.removeTree(name);
        }

        [TestMethod()]
        public void removeTreeTest()
        {
            const string name = "Test_remove";

            Assert.IsFalse(GameFiles.treeExists(name));
            GameFiles.saveTree(name, Node.root);

            Assert.IsTrue(GameFiles.treeExists(name));
            GameFiles.removeTree(name);

            Assert.IsFalse(GameFiles.treeExists(name));

            // Check for exception
            try
            {
                GameFiles.removeTree(name, true);
                Assert.Fail("No exception was thrown.");
            }
            catch(Exception ex) { }
        }

        [TestMethod()]
        public void saveTreeLoadTreeTest()
        {
            const string name   = "Test_saveAndLoad";
            const uint   wins   = 20;
            const uint   losses = 30;

            // Create a simple tree
            var root  = Node.root;
            var piece = Board.Piece.X;
            root.children.Add(new Node(new Board.Hash(piece, "M........"), 0, wins, losses));
            root.children.Add(new Node(new Board.Hash(piece, "O........"), 0, losses, wins));
            root.children[0].children.Add(new Node(new Board.Hash(piece, "MO......."), 1, wins, losses));

            // Save it
            GameFiles.saveTree(name, root);

            // Then load it back in, and see if the nodes are still correct.
            root = GameFiles.loadTree(name);

            var node = root.children[0];
            Assert.IsTrue(node.hash.ToString() == "M........");
            Assert.IsTrue(node.index           == 0);
            Assert.IsTrue(node.won             == wins);
            Assert.IsTrue(node.lost            == losses);

            node = root.children[1];
            Assert.IsTrue(node.hash.ToString() == "O........");
            Assert.IsTrue(node.index           == 0);
            Assert.IsTrue(node.won             == losses);
            Assert.IsTrue(node.lost            == wins);

            node = root.children[0].children[0];
            Assert.IsTrue(node.hash.ToString() == "MO.......");
            Assert.IsTrue(node.index           == 1);
            Assert.IsTrue(node.won             == wins);
            Assert.IsTrue(node.lost            == losses);

            // And now to quickly check that some exceptions get thrown
            try
            {
                GameFiles.saveTree(name, root, false); // Cannot overwrite existing tree.
                Assert.Fail("No exception was thrown.");
            }
            catch (Exception) { }

            try
            {
                GameFiles.saveTree("s", null); // Cannot pass a null root node.
                Assert.Fail("No exception was thrown.");
            }
            catch (Exception) { }

            Assert.IsNull(GameFiles.loadTree("", false));
            
            try
            {
                GameFiles.loadTree(""); // No tree named ""
                Assert.Fail("No exception was thrown.");
            }
            catch(Exception) { }
        }
    }
}