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
    public class MoveTreeTests
    {
        [TestMethod()]
        public void walkTest()
        {
            var m = Hash.myChar;
            var o = Hash.otherChar;
            var p = Board.Piece.x;

            var tree = new MoveTree();
            tree.root.children.AddRange(new Node[] 
                                       {
                                           new Node(new Hash(p, $"{m}........"), 0),
                                           new Node(new Hash(p, $".{o}......."), 1)
                                       });
            tree.root.children[0].children.Add(new Node(new Hash(p, $"{m}.{o}......"), 2));

            // Visualisation of what 'tree' looks like
            /*                               /----[0]----/ "M.O......"
             *      /----[0]----/ "M........"
             * root
             *      /----[1]----/ ".O......."
             * */

            // First, seeing if it returns null on an invalid path.
            // "M........" -> "MM......."
            var path        = new Node(new Hash(p, $"{m}........"),   0);
            path.children.Add(new Node(new Hash(p, $"{m}{m}......."), 1));
            Assert.IsNull(tree.walk(path));

            // Then see if depth works
            Assert.IsTrue(path.hash.Equals(tree.walk(path, 1).hash));

            // Then finally see if it walks through things properly
            // We have to change the last node in path first though
            path.children[0] = new Node(new Hash(p, $"{m}.{o}......"), 2);

            Node lastNode = path;
            while(lastNode.children.Count != 0)
                lastNode = lastNode.children[0];

            Assert.IsTrue(lastNode.hash.Equals(tree.walk(path).hash));
        }
    }
}