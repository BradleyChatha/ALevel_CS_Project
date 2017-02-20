using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_Project.Game
{
    /// <summary>
    /// Contains some static/constant data used to change a few things.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// A string form of the project's current version.
        /// </summary>
        public const string versionString = "v0.2.2 prototype";

        /// <summary>
        /// The text to be displayed in the game's help box.
        /// 
        /// While this would make more sense to be put in the 'MainWindow' class, it'd also look a bit ugly (since it's so large).
        /// </summary>
        public const string helpBoxInfo = "Welcome to tic-tac-toe!\n\n"

                                        + "In this game you will play a game called 'tic-tac-toe' against an AI.\n\n"

                                        + "Tic-tac-toe is played with two players, where one player (in this case, you) plays as 'O', "
                                        + "while the other player (in this case, the AI) plays as 'X'.\n\n"

                                        + "'X' and 'O' take turns to place their piece ('X' or 'O') on an empty spot in a 3x3 grid.\n\n"

                                        + "The goal is for either player to get 3 of their pieces in a row, this can be done "
                                        + "horizontally (left-to-right), vertically (top-to-bottom), or diagonally (top-left to bottom-right, or top-right to bottom-left).\n\n"
                                       
                                        + "Once this is done, the player who got 3 in a row wins. "
                                        + "It is also possible neither player is able to get 3 in a row during a match, making it a tie.";
    }
}
