using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mk64stats
{
    class GameData
    {
        public enum State { CHARACTER_SELECT, COURSE_SELECT, RACING };
        
        private string[] _playerNames = new string[4];
        private int[] _characters = new int[4];
        private int[][] _multiplayerWins;
        private State _state;

        /**
         * Return a new GameData object, preserving the names of players if
         * an existing GameData object is supplied
         * <param name="old">Existing GameData</param>
         * <returns>A new GameData object with old player names if
         *          applicable
         * </returns>
         */
        public static GameData NewGameData(GameData old)
        {
            if (old == null)
            {
                return new GameData();
            }
            else
            {
                GameData gameData = new GameData();
                gameData._playerNames = old._playerNames;
                return gameData;
            }
        }

        public GameData()
        {
            _multiplayerWins = new int[4][];
            for (int i = 0; i < 4; i++)
            {
                _multiplayerWins[i] = new int[3];
            }
        }

        public State GetState()
        {
            return _state;
        }

        public void SetState(State state)
        {
            _state = state;
        }

        public string GetPlayerName(int playerIndex)
        {
            return _playerNames[playerIndex] ?? "player " + playerIndex;
        }

        public void SetPlayerName(int playerIndex, string name)
        {
            _playerNames[playerIndex] = name;
        }

        public int GetPlayerChar(int playerIndex)
        {
            return _characters[playerIndex];
        }

        public void SetPlayerChar(int playerIndex, int characterIndex)
        {
            _characters[playerIndex] = characterIndex;
        }

        public int[] GetMpWins(int playerIndex)
        {
            return _multiplayerWins[playerIndex];
        }

        public void AddWin(int playerIndex)
        {
            AddWin(playerIndex, 0);
        }

        public void AddWin(int playerIndex, int pos)
        {
            _multiplayerWins[playerIndex][pos]++;
        }
    }
}
