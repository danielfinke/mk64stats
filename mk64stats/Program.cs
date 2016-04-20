using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace mk64stats
{
    class Program
    {
        static void Main(string[] args)
        {
            Main main = new Main();
            main.Run();
        }
    }

    class Main: StatsHook.IStatsHook
    {
        public void Run()
        {
            bool shouldStop = false;
            StatsHook hook = new StatsHook(this);
            Thread hookThread = new Thread(hook.RunHook);
            hookThread.Start();

            Console.WriteLine("waiting for hook");

            while(!shouldStop)
            {
                string cmd = Console.ReadLine();
                string[] cmdParts = cmd.Split(' ');

                switch (cmdParts[0])
                {
                    case "setname":
                        int pNum;
                        if (cmdParts.Length < 3 || !Int32.TryParse(cmdParts[1], out pNum))
                        {
                            InvalidCmd(cmd);
                        }
                        else
                        {
                            hook.SetPlayerName(pNum, cmdParts[2]);
                            Console.WriteLine("player " + cmdParts[1] + " name set to " + cmdParts[2]);
                        }
                        break;
                    case "stop":
                        shouldStop = true;
                        hook.RequestStop();
                        break;
                    default:
                        InvalidCmd(cmd);
                        break;
                }
            }

            hookThread.Join();
            //Console.WriteLine("joined hook thread");
        }

        public void InvalidCmd(string cmd)
        {
            Console.WriteLine("invalid cmd: " + cmd);
        }

        public void OnHook()
        {
            Console.WriteLine("hooked project64 process");
        }

        public void OnUnhook()
        {
            Console.WriteLine("unhooked project64 process");
        }
    }
}
