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
        public ConcurrentQueue<Message> gameQueue; // Used so the gui thread can talk to the game thread.

        private void gameThreadMain()
        {
            // All variables for the game thread should be kept inside this function.
            // Use Dispatcher.Invoke when the game thread needs to modify the UI.
            // Use gameQueue so the GUI thread can speak to the game thread.
            var board = new Board();
            var state = GameState.Waiting;

            while(true)
            {
                // If no match is being done, listen to the message queue for things.
                if(state == GameState.Waiting)
                {
                    Message msg;
                    while(!this.gameQueue.TryDequeue(out msg))
                        Thread.Sleep(500);

                    if(msg is StartMatchMessage)
                    {
                        var info = msg as StartMatchMessage;
                        state    = GameState.DoingMatch;
                        board.startMatch(info.xCon, info.oCon);
                        state    = GameState.Waiting;
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.gameQueue   = new ConcurrentQueue<Message>();
            this._gameThread = new Thread(gameThreadMain);
            this._gameThread.Start();

            base.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            this._gameThread.Abort();
        }
    }
}
