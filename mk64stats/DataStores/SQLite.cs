using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using mk64stats.Model;

namespace mk64stats.DataStores
{
    class SQLite : IDataStore
    {
        private SQLiteConnection _dbConnection;

        public SQLite(string connectionString)
        {
            _dbConnection = new SQLiteConnection(connectionString);
            _dbConnection.Open();

            // Create the stats table if it does not already exist
            InitializeTables();
        }

        public void WriteWin(int raceId, int playerIndex, int playerCount, int position, string name, int character, int cup, int course)
        {
            string cmdStr = "insert into stats (race_id, player_index, player_count, position, name, character, cup, course, timestamp) VALUES (" +
                raceId + ", " + playerIndex + ", " + playerCount + ", " + position + ", '" + name + "', " + character + ", " + cup + ", " + course + ", " +
                (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds) + ");";
            SQLiteCommand cmd = new SQLiteCommand(cmdStr, _dbConnection);
            cmd.ExecuteNonQuery();
        }

        public int NextRaceId()
        {
            string cmdStr = "select max(race_id) + 1 " +
                "from stats";
            SQLiteCommand cmd = new SQLiteCommand(cmdStr, _dbConnection);
            object result = cmd.ExecuteScalar();
            if (result is DBNull)
            {
                return 1;
            }
            return (int)(long)result;
        }

        public List<PreviousPlayer> GetPreviousPlayers()
        {
            string cmdStr = "select distinct name from stats order by name asc";
            SQLiteCommand cmd = new SQLiteCommand(cmdStr, _dbConnection);
            SQLiteDataReader reader = cmd.ExecuteReader();

            List<PreviousPlayer> previousPlayers = new List<PreviousPlayer>();
            while (reader.Read())
            {
                previousPlayers.Add(new PreviousPlayer() { Name = reader.GetString(0) });
            }
            return previousPlayers;
        }

        public void Close()
        {
            _dbConnection.Close();
        }

        private void InitializeTables()
        {
            SQLiteCommand cmd = new SQLiteCommand(TABLE_EXISTS_SQL, _dbConnection);
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                reader.Close();
                cmd = new SQLiteCommand(CREATE_TABLE_SQL, _dbConnection);
                cmd.ExecuteNonQuery();
            }
        }

        private static readonly string CREATE_TABLE_SQL =
            "create table stats (" +
            "id integer primary key autoincrement," +
            "player_index integer," +
            "player_count integer," +
            "position integer," +
            "name text," +
            "character integer," +
            "cup integer," +
            "course integer," +
            "timestamp integer" +
            ");";

        private static readonly string TABLE_EXISTS_SQL =
            "select 1 from sqlite_master where name='stats' and type='table';";
    }
}
