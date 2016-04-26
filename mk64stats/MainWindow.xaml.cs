using mk64stats.Model;
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
        public List<PreviousPlayer> PreviousPlayers
        {
            get
            {
                return GetPreviousPlayers();
            }
        }
        private StatsHook _hook;
        private Thread _hookThread;

        public MainWindow()
        {
            InitializeComponent();
            
            _hook = new StatsHook(this);
            _hookThread = new Thread(_hook.RunHook);
            _hookThread.Start();

            DataContext = this;

            nameBox1.Tag = 0;
            nameBox2.Tag = 1;
            nameBox3.Tag = 2;
            nameBox4.Tag = 3;
            nameBox1.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                      new System.Windows.Controls.TextChangedEventHandler(PlayerNameComboEdited));
            nameBox2.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                      new System.Windows.Controls.TextChangedEventHandler(PlayerNameComboEdited));
            nameBox3.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                      new System.Windows.Controls.TextChangedEventHandler(PlayerNameComboEdited));
            nameBox4.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                      new System.Windows.Controls.TextChangedEventHandler(PlayerNameComboEdited));

            UpdateTextBox("waiting for hook");
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
            SetControlsEnabled(true);
        }

        public void OnUnhook()
        {
            UpdateTextBox("unhooked project64 process");
            SetControlsEnabled(false);
        }

        public void OnPlayerCountChange(int newCount)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                player2Grid.Visibility = (Visibility)(newCount < 2 ? 1 : 0);
                player3Grid.Visibility = (Visibility)(newCount < 3 ? 1 : 0);
                player4Grid.Visibility = (Visibility)(newCount < 4 ? 1 : 0);
            });
        }

        public void OnCharSelect(int playerIndex, int character)
        {
            Dispatcher.Invoke((Action)delegate
            {
                Image img = null;
                switch (playerIndex)
                {
                    case 1:
                        img = player1Char;
                        break;

                    case 2:
                        img = player2Char;
                        break;

                    case 3:
                        img = player3Char;
                        break;

                    case 4:
                        img = player4Char;
                        break;
                }

                img.Source = new BitmapImage(new Uri(Types.CharacterImg(character), UriKind.Relative));
            });
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

        private void SetControlsEnabled(bool enabled)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                nameBox1.IsEnabled = enabled;
                nameBox2.IsEnabled = enabled;
                nameBox3.IsEnabled = enabled;
                nameBox4.IsEnabled = enabled;
            });
        }

        private void PlayerNameComboEdited(object sender, TextChangedEventArgs e)
        {
            ComboBox playerNameCombo = (ComboBox)sender;
            int index = (int)playerNameCombo.Tag;
            object selectedItem = playerNameCombo.SelectedItem;
            string name = playerNameCombo.Text;

            // SetPlayerName gives a default name to player with blank name
            if (name == "")
            {
                name = null;
            }

            _hook.SetPlayerName(index, name);
            UpdateTextBox("set player " + (index + 1) + " name to " + (name ?? "player " + (index + 1)));
        }

        private List<PreviousPlayer> GetPreviousPlayers()
        {
            IDataStore dataStore = DataStoreFactory.New();
            return dataStore.GetPreviousPlayers();
        }
    }
}
