using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using mk64stats.Model;

namespace mk64stats
{
    class StatsHook
    {
        private volatile bool _shouldStop;
        private volatile bool _hooked;
        private IntPtr _procHandle;
        private uint _romOffset;
        private IDataStore _dataStore;
        private GameData _gameData;
        private IStatsHook _callback;

        private const int PROCESS_VM_READ = 0x0010;
        private const int ROM_START_PTR = 0x7C6A0000;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
            int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        public interface IStatsHook
        {
            void OnHook();
            void OnUnhook();
            void Log(string msg);
        }

        public StatsHook(IStatsHook callback)
        {
            _dataStore = DataStoreFactory.New();
            _callback = callback;
        }

        public void RunHook()
        {
            while (!_shouldStop)
            {
                Process[] procs = Process.GetProcessesByName("project64");
                if (procs.Length < 1)
                {
                    // Wait for some time before querying for procs again
                    // Low timeout value so that CLI commands are somewhat responsive still
                    Thread.Sleep(250);
                    continue;
                }

                Process emulatorProc = procs[0];
                EventHandler evtHandler = new EventHandler(EmulatorExited);
                emulatorProc.EnableRaisingEvents = true;
                emulatorProc.Exited += evtHandler;
                _procHandle = OpenProcess(PROCESS_VM_READ, false, emulatorProc.Id);

                int bytesRead = 0;
                byte[] buffer = new byte[4];
                ReadProcessMemory((int)_procHandle, ROM_START_PTR, buffer, buffer.Length, ref bytesRead);
                _romOffset = BitConverter.ToUInt32(buffer, 0);

                if (_romOffset == 0)
                {
                    // Rom hasn't been loaded yet, so rom offset not chosen
                    emulatorProc.Exited -= evtHandler;
                    Thread.Sleep(250);
                    continue;
                }

                _callback.OnHook();
                _hooked = true;
                _gameData = new GameData();
                Run();
            }
            _callback.Log("thread down");
            _dataStore.Close();
        }

        public void SetPlayerName(int index, string name)
        {
            _gameData.SetPlayerName(index, name);
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }

        private void Run()
        {
            bool[] charSelected = new bool[4];
            Race race = new Race(_dataStore.NextRaceId());

            while (!_shouldStop && _hooked)
            {
                switch (_gameData.GetState())
                {
                    case GameData.State.CHARACTER_SELECT:
                        int playerCount = ReadProcessMemory(Offsets.PlayerCount);
                        if (playerCount != race.PlayerCount)
                        {
                            _callback.Log("player count: " + playerCount);
                            race.PlayerCount = playerCount;
                        }

                        // Right after the game launches, playerCount will be 0.
                        // Wait until things get going before setting
                        bool allSelected = playerCount != 0;
                        for (int i = 0; i < playerCount; i++)
                        {
                            int charSel = ReadProcessMemory(Offsets.CharSelected[i]);
                            if (!charSelected[i] && charSel == 1)
                            {
                                charSelected[i] = true;
                                int character = ReadProcessMemory(Offsets.Chars[i]);
                                _gameData.SetPlayerChar(i, character);
                                _callback.Log("player " + (i+1) + " selected " + Types.CharacterName(character));
                            }
                            else if (charSelected[i] && charSel == 0)
                            {
                                charSelected[i] = false;
                                _gameData.SetPlayerChar(i, 0);
                                _callback.Log("player " + (i+1) + " deselected their character");
                            }

                            // If all the players playing have selected, we will move to next state
                            allSelected &= charSelected[i];
                        }

                        if (allSelected)
                        {
                            _gameData.SetState(GameData.State.COURSE_SELECT);
                            _callback.Log("moving to course selection");
                        }

                        break;

                    case GameData.State.COURSE_SELECT:
                        bool goBack = false;

                        // First, detect when we might have gone back to the character selection screen
                        for (int i = 0; i < race.PlayerCount; i++)
                        {
                            int charSel = ReadProcessMemory(Offsets.CharSelected[i]);
                            if (charSel != 1)
                            {
                                goBack = true;
                                break;
                            }
                        }

                        if (goBack)
                        {
                            _gameData.SetState(GameData.State.CHARACTER_SELECT);
                            _callback.Log("cancelled course selection");
                        }

                        int cup = ReadProcessMemory(Offsets.Cup);
                        int course = ReadProcessMemory(Offsets.Course);
                        race.SetCourse(cup, course);

                        int inRace = ReadProcessMemory(Offsets.InRace);
                        // 1 in 2 player mode, 3 in 3-4 player mode
                        if (inRace == 1)
                        {
                            _callback.Log("2 player mode");
                        }
                        else if (inRace == 3)
                        {
                            _callback.Log("3-4 player mode");
                        }
                        if (inRace == 1 || inRace == 3)
                        {
                            _gameData.SetState(GameData.State.RACING);
                            race.Reset();
                            _callback.Log("started course: " + Types.CourseName(cup, course) + " (" + Types.CupName(cup) + " Cup)");
                        }

                        break;

                    case GameData.State.RACING:
                        goBack = false;

                        // First, detect when we might have gone back to the character selection screen
                        for (int i = 0; i < race.PlayerCount; i++)
                        {
                            int charSel = ReadProcessMemory(Offsets.CharSelected[i]);
                            if (charSel != 1)
                            {
                                goBack = true;
                                break;
                            }
                        }

                        if (goBack)
                        {
                            _gameData.SetState(GameData.State.CHARACTER_SELECT);
                            _callback.Log("cancelled game, returned to character select");
                        }

                        // Next, check if somebody won a race
                        if (race.PlayerCount == 2)
                        {
                            for (int playerIndex = 0; playerIndex < race.PlayerCount; playerIndex++)
                            {
                                int wins = ReadProcessMemory(Offsets.Wins2p[playerIndex]);
                                if (wins > _gameData.GetMpWins(playerIndex)[0])
                                {
                                    _gameData.AddWin(playerIndex);

                                    int otherPlayerIndex = playerIndex == 0 ? 1 : 0;
                                    race.SetPlacement(playerIndex, 1);
                                    race.SetPlacement(otherPlayerIndex, 2);

                                    // Safe to assume in 2p the race is over
                                    _dataStore.WriteWin(race.RaceId,
                                                        0,
                                                        race.PlayerCount,
                                                        race.GetPlacement(0),
                                                        _gameData.GetPlayerName(playerIndex),
                                                        _gameData.GetPlayerChar(playerIndex),
                                                        race.Cup,
                                                        race.Course);
                                    _dataStore.WriteWin(race.RaceId,
                                                        1,
                                                        race.PlayerCount,
                                                        race.GetPlacement(1),
                                                        _gameData.GetPlayerName(playerIndex),
                                                        _gameData.GetPlayerChar(playerIndex),
                                                        race.Cup,
                                                        race.Course);

                                    race.Reset(_dataStore.NextRaceId());
                                    
                                    _callback.Log(_gameData.GetPlayerName(playerIndex) + " won!");
                                    break;
                                }
                            }
                        }
                        else if (race.PlayerCount > 2)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                for (int playerIndex = 0; playerIndex < race.PlayerCount; playerIndex++)
                                {
                                    int wins = ReadProcessMemory(Offsets.Wins(race.PlayerCount)[playerIndex,i]);
                                    if (wins > _gameData.GetMpWins(playerIndex)[i])
                                    {
                                        _gameData.AddWin(playerIndex, i);

                                        race.SetPlacement(playerIndex, i + 1);

                                        if (race.IsOver())
                                        {
                                            for (int j = 0; j < race.PlayerCount; j++)
                                            {
                                                int placement = race.GetPlacement(j);
                                                _dataStore.WriteWin(race.RaceId,
                                                                    j,
                                                                    race.PlayerCount,
                                                                    placement,
                                                                    _gameData.GetPlayerName(j),
                                                                    _gameData.GetPlayerChar(j),
                                                                    race.Cup,
                                                                    race.Course);

                                                _callback.Log(_gameData.GetPlayerName(j) + " placed " + Position(placement) + "!");
                                            }
                                            race.Reset(_dataStore.NextRaceId());
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // Finally, check if we went back to the course selection screen
                        inRace = ReadProcessMemory(Offsets.InRace);
                        if (inRace == 0)
                        {
                            _gameData.SetState(GameData.State.COURSE_SELECT);
                            _callback.Log("cancelled game, returned to course select");
                        }

                        break;
                }

                Thread.Sleep(1000);
            }
        }

        private void EmulatorExited(object sender, EventArgs e)
        {
            _hooked = false;
            _callback.OnUnhook();
        }
        
        private int ReadProcessMemory(uint offset)
        {
            byte[] buffer = ReadProcessMemory(offset, 1);
            return Convert.ToInt32(buffer[0]);
        }
        private byte[] ReadProcessMemory(uint offset, int length)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[length];
            ReadProcessMemory((int)_procHandle, GetOffset(offset), buffer, buffer.Length, ref bytesRead);
            return buffer;
        }

        private int GetOffset(uint offset)
        {
            return (int)_romOffset + (int)offset;
        }

        private string Position(int pos)
        {
            switch(pos)
            {
                case 1:
                    return "1st";
                case 2:
                    return "2nd";
                case 3:
                    return "3rd";
                case 4:
                    return "4th";
                default:
                    throw new Exception();
            }
        }

        private class Offsets
        {
            public static readonly uint PlayerCount = 0x8018edf0;
            public static readonly uint[] Chars = { 0x8018ede7, 0x8018ede6, 0x8018ede5, 0x8018ede4 };
            public static readonly uint[] CharSelected = { 0x8018edeb, 0x8018edea, 0x8018ede9, 0x8018ede8 };
            public static readonly uint Cup = 0x84001d90;
            //public static readonly uint CupSelected = 0x80143d39;
            public static readonly uint Course = 0x8018db8c;
            //public static readonly uint CourseSelected = 0x8011ae5e;
            public static readonly uint InRace = 0x84001d7c;
            public static readonly uint[] RaceTime = { 0x8018ca78, 0x8018cafc };

            public static readonly uint[] Wins2p = { 0x8000031f, 0x8000031e };
            private static readonly uint[,] _wins3p = { { 0x8000031d, 0x8000031c, 0x80000323 },
                                                        { 0x80000322, 0x80000321, 0x80000320 },
                                                        { 0x80000327, 0x80000326, 0x80000325 } };
            private static readonly uint[,] _wins4p = { { 0x80000324, 0x8000032b, 0x8000032a },
                                                        { 0x80000329, 0x80000328, 0x8000032f },
                                                        { 0x8000032e, 0x8000032d, 0x8000032c },
                                                        { 0x80000333, 0x80000332, 0x80000331 } };

            public static uint[,] Wins(int playerCount)
            {
                if (playerCount == 3)
                {
                    return _wins3p;
                }
                else if (playerCount == 4)
                {
                    return _wins4p;
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}
