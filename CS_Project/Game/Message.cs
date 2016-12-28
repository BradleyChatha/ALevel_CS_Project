﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS_Project.Game.Controllers;

namespace CS_Project.Game
{
    /// <summary>
    /// The base class for any class that can be sent over the message queue.
    /// </summary>
    public abstract class Message
    {
    }

    /// <summary>
    /// The message sent when the game thread should startup a match.
    /// </summary>
    public sealed class StartMatchMessage : Message
    {
        /// <summary>
        /// The controller for the 'X' piece.
        /// </summary>
        public Controller xCon { get; set; }

        /// <summary>
        /// The controller for the 'Y' piece.
        /// </summary>
        public Controller oCon { get; set; }
    }
}