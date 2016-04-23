using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace mk64stats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, StatsHook.IStatsHook
    {
        private StatsHook _hook;
        private Thread _hookThread;

        public MainWindow()
        {
            InitializeComponent();
            
            _hook = new StatsHook(this);
            _hookThread = new Thread(_hook.RunHook);
            _hookThread.Start();

            UpdateTextBox("waiting for hook");

            //while (!shouldStop)
            //{
            //    string cmd = Console.ReadLine();
            //    string[] cmdParts = cmd.Split(' ');

            //    switch (cmdParts[0])
            //    {
            //        case "setname":
            //            int pNum;
            //            if (cmdParts.Length < 3 || !Int32.TryParse(cmdParts[1], out pNum))
            //            {
            //                InvalidCmd(cmd);
            //            }
            //            else
            //            {
            //                hook.SetPlayerName(pNum, cmdParts[2]);
            //                Console.WriteLine("player " + cmdParts[1] + " name set to " + cmdParts[2]);
            //            }
            //            break;
            //        case "stop":
            //            shouldStop = true;
            //            hook.RequestStop();
            //            break;
            //        default:
            //            InvalidCmd(cmd);
            //            break;
            //    }
            //}
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _hook.RequestStop();
            _hookThread.Join();
        }

        public void OnHook()
        {
            UpdateTextBox("hooked project64 process");
        }

        public void OnUnhook()
        {
            UpdateTextBox("unhooked project64 process");
        }

        public void Log(string msg)
        {
            UpdateTextBox(msg);
        }

        private void UpdateTextBox(string msg)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                textBox.AppendText(msg + "\r\n");
                textBox.ScrollToEnd();
            });
        }
    }
}
