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
    public class NodeTests
    {
        [TestMethod()]
        public void walkTest()
        {
            var m = Hash.myChar;
            var o = Hash.otherChar;
            var p = Board.Piece.x;

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
    }
}