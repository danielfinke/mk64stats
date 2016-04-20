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

        private int _playerCount;
        private string[] _playerNames = new string[4];
        private int[] _characters = new int[4];
        private int _cup;
        private int _course;
        private int[] _2pWins = new int[2];
        private int[][] _multiplayerWins;
        private State _state;

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

        public int GetPlayerCount()
        {
            return _playerCount;
        }

        public void SetPlayerCount(int count)
        {
            _playerCount = count;
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

        public int GetCup()
        {
            return _cup;
        }

        public void SetCup(int cup)
        {
            _cup = cup;
        }

        public int GetCourse()
        {
            return _course;
        }

        public void SetCourse(int course)
        {
            _course = course;
        }

        public int Get2pWins(int playerIndex)
        {
            return _2pWins[playerIndex];
        }

        public int[] GetMpWins(int playerIndex)
        {
            return _multiplayerWins[playerIndex];
        }

        public void AddWin(int playerIndex)
        {
            if (_playerCount == 2)
            {
                AddWin(playerIndex, 0);
                return;
            }
            throw new Exception();
        }

        public void AddWin(int playerIndex, int pos)
        {
            if (_playerCount == 2)
            {
                _2pWins[playerIndex]++;
            }
            else
            {
                _multiplayerWins[playerIndex][pos]++;
            }
        }
    }
}
