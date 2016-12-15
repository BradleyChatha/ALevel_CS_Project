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
    public class AverageTests
    {
        [TestMethod()]
        public void statisticallyBestTest()
        {
            var m    = Hash.myChar;
            var o    = Hash.otherChar;
            var p    = Board.Piece.x;
            var tree = new MoveTree();
            tree.root.children.AddRange(new Node[] 
                                       {
                                           new Node(new Hash(p, $"{m}........"), 0, 6, 6), // 50% win
                                           new Node(new Hash(p, $".{m}......."), 1, 9, 3)  // 75% win
                                       });
            tree.root.children[0].children.AddRange(new Node[] // Adding to the 50% node
                                       {
                                           new Node(new Hash(p, $"{m}.{m}......"), 2, 6, 6), // 50% win (path average of 50%)
                                           new Node(new Hash(p, $"{m}..{m}....."), 3, 3, 9), // 25% win (path average of 62.5%)
                                       });
            tree.root.children[1].children.AddRange(new Node[] // Adding to the 75% node
                                       {
                                           new Node(new Hash(p, $"{m}{m}......."), 0, 9, 3) // 75% win (path average of 75%)
                                       });

            var best = Average.statisticallyBest(tree.root);
            Assert.IsTrue(best.averageWinPercent       == 75.0f);
            Assert.IsTrue(best.path.Count              == 2);
            Assert.IsTrue(best.path[0].hash.ToString() == $".{m}......."); // It should've chosen the 75% -> 75% path.
            Assert.IsTrue(best.path[1].hash.ToString() == $"{m}{m}.......");
        }
    }
}