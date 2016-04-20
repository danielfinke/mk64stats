using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace mk64stats.DataStores
{
    class FlatFile : IDataStore
    {
        private StreamWriter _outputWriter;

        public FlatFile(string dataFilePath)
        {
            _outputWriter = new StreamWriter(dataFilePath, true);
        }

        public void WriteWin(int playerIndex, int playerCount, int position, string name, int character, int cup, int course)
        {
            _outputWriter.WriteLine("p" + playerIndex + "/" +
                playerCount + "," +
                position + "," +
                name + "," +
                Types.CharacterName(character) + "," +
                cup + "," +
                course);
            _outputWriter.Flush();
        }

        public void Close()
        {
            _outputWriter.Close();
        }
    }
}
