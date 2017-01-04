using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using CS_Project.Game;
using CS_Project.Game.Controllers;

namespace CS_Project
{
    enum GameState
    {
        Waiting,   // The game thread is waiting for messages to be sent to it.
        DoingMatch // The game thread is processing a match.
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread                  _gameThread;
        private Label[]                 _slots;    // The labels that represent the slots on the board.
        public ConcurrentQueue<Message> gameQueue; // Used so the gui thread can talk to the game thread.

        public MainWindow()
        {
            // Setup multi-threaded stuff.
            InitializeComponent();
            this.gameQueue   = new ConcurrentQueue<Message>();
            this._gameThread = new Thread(gameThreadMain);
            this._gameThread.Start();

            // Setup events
            base.Closed += MainWindow_Closed;

            // Setup the slots.
            this._slots = new Label[] { slot0, slot1, slot2,
                                        slot3, slot4, slot5,
                                        slot6, slot7, slot8 };
            foreach(var slot in this._slots)
                slot.MouseLeftButtonUp += onSlotPress;

            // TEMPORARY: Tell the game UI to start a match between 2 players.
            this.gameQueue.Enqueue(new StartMatchMessage
                                  {
                                      xCon = new PlayerGUIController(this),
                                      oCon = new PlayerGUIController(this)
                                  });
        }

        // If we don't abort the game thread, then the program will stay alive in the background.
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            this._gameThread.Abort();
        }

        // When one of the slots are pressed, send a PlayerPlaceMessage to the game thread, saying
        // which slot was pressed.
        private void onSlotPress(object sender, MouseEventArgs e)
        {
            // Only labels should be using this
            var label = sender as Label;

            // The last character of the labels is an index.
            var index = int.Parse(label.Name.Last().ToString());

            this.gameQueue.Enqueue(new PlayerPlaceMessage { index = index });
        }

        /// <summary>
        /// Updates the game board to reflect the given hash.
        /// </summary>
        /// <param name="hash">The 'Hash' containing the state of the board.</param>
        public void updateBoard(Hash hash)
        {
            var myChar    = (hash.myPiece    == Board.Piece.X) ? "X" : "O";
            var otherChar = (hash.otherPiece == Board.Piece.X) ? "X" : "O";

            for (var i = 0; i < this._slots.Length; i++)
            {
                var slot = this._slots[i];

                if(hash.isMyPiece(i))  slot.Content = myChar;
                if(!hash.isMyPiece(i)) slot.Content = otherChar;
                if(hash.isEmpty(i))    slot.Content = "";
            }
        }
    }

    // This part of the partial MainWindow class is for anything ran on the Game thread.
    public partial class MainWindow : Window
    {
        private void gameThreadMain()
        {
            // All variables for the game thread should be kept inside this function.
            // Use Dispatcher.Invoke when the game thread needs to modify the UI.
            // Use gameQueue so the GUI thread can speak to the game thread.
            var board = new Board();
            var state = GameState.Waiting;

            while (true)
            {
                // If no match is being done, listen to the message queue for things.
                if (state == GameState.Waiting)
                {
                    // Check for a message every 0.5 seconds.
                    Message msg;
                    while (!this.gameQueue.TryDequeue(out msg))
                        Thread.Sleep(500);

                    // If we get a StartMatchMessage, then perform a match.
                    if (msg is StartMatchMessage)
                    {
                        var info = msg as StartMatchMessage;
                        state = GameState.DoingMatch;
                        board.startMatch(info.xCon, info.oCon);
                        state = GameState.Waiting;
                    }
                    else // Otherwise, requeue it
                        this.gameQueue.Enqueue(msg);
                }
            }
        }
    }
}
