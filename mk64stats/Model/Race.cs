using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mk64stats.Model
{
    class Race
    {
        private int _raceId;
        private int _cup;
        private int _course;
        private int _playerCount;
        private int[] _placement;

        public int RaceId
        {
            get
            {
                return _raceId;
            }
        }

        public int Cup
        {
            get
            {
                return _cup;
            }
        }

        public int Course
        {
            get
            {
                return _course;
            }
        }

        public int PlayerCount
        {
            get
            {
                return _playerCount;
            }
            set
            {
                _playerCount = value;
            }
        }

        public Race(int raceId)
        {
            _raceId = raceId;
            _placement = new int[4];
        }

        public int GetPlacement(int playerIndex)
        {
            return _placement[playerIndex];
        }

        public void SetCourse(int cup, int course)
        {
            _cup = cup;
            _course = course;
        }

        public void SetPlacement(int playerIndex, int pos)
        {
            _placement[playerIndex] = pos;

            // Auto-set 4th place
            if (_playerCount == 4 && AllSet())
            {
                for (int i = 0; i < _playerCount; i++)
                {
                    if (_placement[i] == 0)
                    {
                        _placement[i] = 4;
                        break;
                    }
                }
            }
        }

        public bool IsOver()
        {
            for (int i = 0; i < _playerCount; i++)
            {
                if (_placement[i] == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void Reset()
        {
            _placement = new int[4];
        }

        public void Reset(int raceId)
        {
            _raceId = raceId;
            _placement = new int[4];
        }

        /*
         * Return true if at least 3 of 4 placements have been recorded
         */
        private bool AllSet()
        {
            int set = 0;
            for (int i = 0; i < _playerCount; i++)
            {
                if (_placement[i] != 0)
                {
                    set++;
                }
            }
            return set >= _playerCount - 1;
        }
    }
}
