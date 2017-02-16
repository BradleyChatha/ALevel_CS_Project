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
        Waiting,    // The game thread is waiting for messages to be sent to it.
        DoingMatch, // The game thread is processing a match.
        Crashed     // The game thread has thrown an exception.
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread                  _gameThread;
        private AI                      _aiInstance; // Since the AI has no way of reading in past data yet, I need to keep a single instance of it so it can 'remember' past games.
        private Label[]                 _slots;      // The labels that represent the slots on the board.
        public ConcurrentQueue<Message> gameQueue;   // Used so the gui thread can talk to the game thread.

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

            // Misc.
            this.Title += $" {Config.versionString}";
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
        public void updateBoard(Board.Hash hash)
        {
            var myChar = (hash.myPiece == Board.Piece.X) ? "X" : "O";
            var otherChar = (hash.otherPiece == Board.Piece.X) ? "X" : "O";

            for (var i = 0; i < this._slots.Length; i++)
            {
                var slot = this._slots[i];

                if (hash.isMyPiece(i)) slot.Content = myChar;
                if (!hash.isMyPiece(i)) slot.Content = otherChar;
                if (hash.isEmpty(i)) slot.Content = "";
            }
        }

        /// <summary>
        /// Updates the two text labels on the screen.
        /// 
        /// If a parameter is `null`, then the label won't be changed.
        /// </summary>
        /// <param name="topText">The text for the top of the screen.</param>
        /// <param name="bottomText">The text for the bottom of the screen.</param>
        public void updateText(string topText, string bottomText = null)
        {
            if(topText != null)
                this.userPieceLabel.Content = topText;

            if(bottomText != null)
                this.turnLabel.Content = bottomText;
        }

        private void onStartMatch(object sender, RoutedEventArgs e)
        {
            // First, hide the button from being pressed again
            this.startButton.Visibility = Visibility.Hidden;

            // Then, start up a match between the AI and the player
            if(this._aiInstance == null)
            {
                #if DEBUG
                this._aiInstance = new AI(new NodeDebugWindow(), new NodeDebugWindow());
                #else
                this._aiInstance = new AI(null, null);
                #endif
            }

            this.gameQueue.Enqueue(new StartMatchMessage
            {
                xCon = new PlayerGUIController(this),
                oCon = this._aiInstance
            });
        }

        /// <summary>
        /// Used by debug controls to control whether they're visible on screen or not.
        /// 
        /// If DEBUG is defined, the controls will be visible.
        /// Otherwise, the controls will not be visible.
        /// </summary>
        public static Visibility debugVisibility
        {
            get
            {
#if DEBUG
                return Visibility.Visible;
#else
                return Visibility.Collapsed;
#endif
            }
        }

        // Throws an exception in the game thread.
        // Used for testing reasons.
        private void debug_throwException_Click(object sender, RoutedEventArgs e)
        {
            this.gameQueue.Enqueue(new ThrowExceptionMessage());
        }
    }

    // This part of the partial MainWindow class is for anything ran on or related to the Game thread.
    public partial class MainWindow : Window
    {
        // Any exception thrown in the game thread is passed to this function (In the UI thread)
        // so that the UI can inform the user about something going wrong.
        public void reportException(Exception ex)
        {
            // Create the message to show the user.
            string msg = $"Something went wrong: {ex.Message}";

            // In debug mode, show the stack trace.
#if DEBUG
            msg += $"\n{ex.StackTrace}";
#endif

            MessageBox.Show(msg, "An exception was thrown", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void gameThreadMain()
        {
            // All variables for the game thread should be kept inside this function.
            // Use Dispatcher.Invoke when the game thread needs to modify the UI.
            // Use gameQueue so the GUI thread can speak to the game thread.
            var board = new Board();
            var state = GameState.Waiting;

            while (true)
            {
                try
                {
                    // If no match is being done, listen to the message queue for things.
                    if(state == GameState.Waiting)
                    {
                        // Check for a message every 0.5 seconds.
                        Message msg;
                        while (!this.gameQueue.TryDequeue(out msg))
                            Thread.Sleep(500);

                        // If we get a StartMatchMessage, then perform a match.
                        if(msg is StartMatchMessage)
                        {
                            var info = msg as StartMatchMessage;
                            state = GameState.DoingMatch;
                            board.startMatch(info.xCon, info.oCon);
                            state = GameState.Waiting;
                        }
                        else if(msg is ThrowExceptionMessage) // Used for testing the try-catch statement guarding this function.
                            throw new Exception();
                        else // Otherwise, requeue it
                            this.gameQueue.Enqueue(msg);
                    }
                    else if(state == GameState.Crashed) // If an exception is ever thrown, then some things have to be reset.
                    {
                        state = GameState.Waiting;
                        board = new Board(); // Making sure the board isn't in an invalid state.

                        this.Dispatcher.Invoke(() => 
                        {
                            this.updateText("[An error has occured]",
                                            "[Please start a new match]");
                        });
                    }
                }
                catch (Exception ex) // Catch any exceptions, and let the UI thread inform the user.
                {
                    // If it's a thread exception, don't bother reporting it.
                    // This is because it's most likely an exception telling the thread to close itself.
                    if(ex is ThreadAbortException)
                        return;

                    this.Dispatcher.Invoke(() => this.reportException(ex));
                    state = GameState.Crashed;
                }
            }
        }
    }
}
