using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

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
                emulatorProc.EnableRaisingEvents = true;
                emulatorProc.Exited += new EventHandler(EmulatorExited);
                _procHandle = OpenProcess(PROCESS_VM_READ, false, emulatorProc.Id);

                int bytesRead = 0;
                byte[] buffer = new byte[4];
                ReadProcessMemory((int)_procHandle, ROM_START_PTR, buffer, buffer.Length, ref bytesRead);
                _romOffset = BitConverter.ToUInt32(buffer, 0);

                if (_romOffset == 0)
                {
                    // Rom hasn't been loaded yet, so rom offset not chosen
                    Thread.Sleep(250);
                    continue;
                }

                _callback.OnHook();
                _hooked = true;
                _gameData = new GameData();
                Run();
            }
            Console.WriteLine("thread down");
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
            int lastPlayerCount = 0;
            bool[] charSelected = new bool[4];
            bool[] winRecorded = new bool[4];

            while (!_shouldStop && _hooked)
            {
                switch (_gameData.GetState())
                {
                    case GameData.State.CHARACTER_SELECT:
                        int playerCount = ReadProcessMemory(Offsets.PlayerCount);
                        _gameData.SetPlayerCount(playerCount);

                        if (playerCount != lastPlayerCount)
                        {
                            lastPlayerCount = playerCount;
                            Console.WriteLine("player count: " + playerCount);
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
                                Console.WriteLine("player " + i + " selected " + Types.CharacterName(character));
                            }
                            else if (charSelected[i] && charSel == 0)
                            {
                                charSelected[i] = false;
                                _gameData.SetPlayerChar(i, 0);
                                Console.WriteLine("player " + i + " deselected their character");
                            }

                            // If all the players playing have selected, we will move to next state
                            allSelected &= charSelected[i];
                        }

                        if (allSelected)
                        {
                            _gameData.SetState(GameData.State.COURSE_SELECT);
                            Console.WriteLine("moving to course selection");
                        }

                        break;

                    case GameData.State.COURSE_SELECT:
                        bool goBack = false;

                        // First, detect when we might have gone back to the character selection screen
                        for (int i = 0; i < _gameData.GetPlayerCount(); i++)
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
                            Console.WriteLine("cancelled course selection");
                        }

                        int cup = ReadProcessMemory(Offsets.Cup);
                        int course = ReadProcessMemory(Offsets.Course);
                        _gameData.SetCup(cup);
                        _gameData.SetCourse(course);

                        int inRace = ReadProcessMemory(Offsets.InRace);
                        // 1 in 2 player mode, 3 in 3-4 player mode
                        if (inRace == 1)
                        {
                            Console.WriteLine("2 player mode");
                        }
                        else if (inRace == 3)
                        {
                            Console.WriteLine("3-4 player mode");
                        }
                        if (inRace == 1 || inRace == 3)
                        {
                            _gameData.SetState(GameData.State.RACING);
                            for (int i = 0; i < 4; i++)
                            {
                                winRecorded[i] = false;
                            }
                            Console.WriteLine("started course: " + Types.CourseName(cup, course) + " (" + Types.CupName(cup) + " Cup)");
                        }

                        break;

                    case GameData.State.RACING:
                        goBack = false;

                        // First, detect when we might have gone back to the character selection screen
                        for (int i = 0; i < _gameData.GetPlayerCount(); i++)
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
                            Console.WriteLine("cancelled game, returned to character select");
                        }

                        // Next, check if somebody won a race
                        if (_gameData.GetPlayerCount() == 2)
                        {
                            for (int playerIndex = 0; playerIndex < _gameData.GetPlayerCount(); playerIndex++)
                            {
                                int wins = ReadProcessMemory(Offsets.Wins2p[playerIndex]);
                                if (wins > _gameData.Get2pWins(playerIndex))
                                {
                                    _gameData.AddWin(playerIndex);
                                    _dataStore.WriteWin(playerIndex,
                                                        _gameData.GetPlayerCount(),
                                                        1,
                                                        _gameData.GetPlayerName(playerIndex),
                                                        _gameData.GetPlayerChar(playerIndex),
                                                        _gameData.GetCup(),
                                                        _gameData.GetCourse());
                                    int otherPlayerIndex = playerIndex == 0 ? 1 : 0;
                                    _dataStore.WriteWin(otherPlayerIndex,
                                                        _gameData.GetPlayerCount(),
                                                        2,
                                                        _gameData.GetPlayerName(otherPlayerIndex),
                                                        _gameData.GetPlayerChar(otherPlayerIndex),
                                                        _gameData.GetCup(),
                                                        _gameData.GetCourse());
                                    Console.WriteLine(_gameData.GetPlayerName(playerIndex) + " won!");
                                    break;
                                }
                            }
                        }
                        else if (_gameData.GetPlayerCount() > 2)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                for (int playerIndex = 0; playerIndex < _gameData.GetPlayerCount(); playerIndex++)
                                {
                                    int wins = ReadProcessMemory(Offsets.Wins(_gameData.GetPlayerCount())[playerIndex,i]);
                                    if (wins > _gameData.GetMpWins(playerIndex)[i])
                                    {
                                        _gameData.AddWin(playerIndex, i);
                                        _dataStore.WriteWin(playerIndex,
                                                            _gameData.GetPlayerCount(),
                                                            (i + 1),
                                                            _gameData.GetPlayerName(playerIndex),
                                                            _gameData.GetPlayerChar(playerIndex),
                                                            _gameData.GetCup(),
                                                            _gameData.GetCourse());
                                        winRecorded[playerIndex] = true;
                                        Console.WriteLine(_gameData.GetPlayerName(playerIndex) + " placed " + Position(i) + "!");
                                        break;
                                    }
                                }
                            }
                            // Check that all wins have been recorded, and if so give player 4 a 4th place if 4 players playing
                            if (_gameData.GetPlayerCount() == 4)
                            {
                                // Index of 4th place player
                                int lossIndex = -1;
                                for (int playerIndex = 0; playerIndex < 4; playerIndex++)
                                {
                                    if (!winRecorded[playerIndex] && lossIndex == -1)
                                    {
                                        lossIndex = playerIndex;
                                    }
                                    else if (!winRecorded[playerIndex])
                                    {
                                        lossIndex = -1;
                                        break;
                                    }
                                }
                                if (lossIndex != -1)
                                {
                                    _dataStore.WriteWin(lossIndex,
                                                        _gameData.GetPlayerCount(),
                                                        4,
                                                        _gameData.GetPlayerName(lossIndex),
                                                        _gameData.GetPlayerChar(lossIndex),
                                                        _gameData.GetCup(),
                                                        _gameData.GetCourse());
                                    // Reset the recorded wins again
                                    for (int i = 0; i < 4; i++)
                                    {
                                        winRecorded[i] = false;
                                    }
                                }
                            }
                        }

                        // Finally, check if we went back to the course selection screen
                        inRace = ReadProcessMemory(Offsets.InRace);
                        if (inRace == 0)
                        {
                            _gameData.SetState(GameData.State.COURSE_SELECT);
                            Console.WriteLine("cancelled game, returned to course select");
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
                case 0:
                    return "1st";
                case 1:
                    return "2nd";
                case 2:
                    return "3rd";
                case 3:
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
