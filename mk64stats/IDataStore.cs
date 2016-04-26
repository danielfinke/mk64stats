using mk64stats.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mk64stats
{
    interface IDataStore
    {
        void WriteWin(int raceId, int playerIndex, int playerCount, int position, string name, int character, int cup, int course);
        int NextRaceId();
        List<PreviousPlayer> GetPreviousPlayers();
        void Close();
    }
}
