using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CS_Project.Game
{
    /// <summary>
    /// An interface that any class that can be serialised/unserialised should implement.
    /// 
    /// The reason a custom serialiser interface is used instead of using .Net's serialisation stuff, is because
    /// I want my code to have readonly, private, private set, etc. variables, but that doesn't play well with
    /// how .Net's serialisation seems to work.
    /// </summary>
    interface ISerialiseable
    {
        /// <summary>
        /// Writes data into the binary writer that can later be used to deserialise the object.
        /// </summary>
        /// 
        /// <param name="output">The output stream to write to</param>
        void serialise(BinaryWriter output);

        /// <summary>
        /// Reads data from the binary writer and changes the object to reflect the deserialised data.
        /// </summary>
        /// 
        /// <param name="input">The input stream to read from.</param>
        /// <param name="version">A version number used to specify the format of the data in `input`.</param>
        void deserialise(BinaryReader input, uint version);
    }
}
