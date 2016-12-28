using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CS_Project.Game.Controllers;

namespace CS_Project.Game.Tests
{
    [TestClass]
    public class BoardTests
    {
        class NullController : Controller
        {
            int last = 0;

            public override void onAfterTurn(Hash boardState)
            {
                Assert.IsTrue(boardState.isMyPiece(this.last));
            }

            public override void onDoTurn(Hash boardState, int index)
            {
                // Put a piece in any empty slot, that isn't on the first row.
                for(var i = 3; i < Board.pieceCount; i++)
                {
                    if(boardState.isEmpty(i))
                    {
                        base.board.set(i, this);
                        this.last = i;
                        break;
                    }
                }
            }
        }

        class StupidController : Controller
        {
            public override void onAfterTurn(Hash boardState)
            {
                var str = boardState.ToString().Replace(Hash.otherChar, Hash.emptyChar);

                Assert.IsTrue(str == "M........" || str == "MM......." || str == "MMM......");
            }

            public override void onDoTurn(Hash boardState, int index)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (boardState.isEmpty(i))
                    {
                        base.board.set(i, this);
                        break;
                    }
                }
            }
        }

        [TestMethod]
        public void basicMatchTest()
        {
            // This should be improved at some point.
            var board = new Board();
            board.startMatch(new NullController(), new StupidController());
        }
    }
}
