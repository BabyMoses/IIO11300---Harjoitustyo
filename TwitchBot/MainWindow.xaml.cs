using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Net.Sockets;  //tarvitaan TCP-yhteyteen
using System.IO;
using System.Windows.Threading;
namespace TwitchBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient tcpClient;
        StreamReader reader;
        StreamWriter writer;
        Boolean joined;
        public MainWindow()
        {
            InitializeComponent();
            Reconnect();

            //timeri chatille
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void Reconnect()
        {
            tcpClient = new TcpClient("irc.twitch.tv", 6667);  //tcp-yhteyden luonti
            reader = new StreamReader(tcpClient.GetStream());
            writer = new StreamWriter(tcpClient.GetStream());

            var userName = "babymoses";  //twitch käyttäjän nimi
            var password = File.ReadAllText("password.txt"); //twitch chat OAuth Avain

            writer.WriteLine("PASS " + password + Environment.NewLine
                + "NICK " + userName + Environment.NewLine
                + "USER " + userName + " 8 * :" + userName);
            writer.Flush();
            joined = false;
            
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {


            if (!tcpClient.Connected)
            {
                Reconnect();
            }

            if (tcpClient.Available > 0 || reader.Peek() >= 0)
            {
                var message = reader.ReadLine();
                lblChat.Content += "\r\n"+message;

            }
            else
            {
                if (!joined)
                {
                    writer.WriteLine("JOIN #babymoses");
                    writer.Flush();
                    joined = true;
                }
            }


            //lblChat.Content = DateTime.Now.Second;  //testi
        }







    }
}
