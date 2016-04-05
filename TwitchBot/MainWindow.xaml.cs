using System;
using System.Collections.Generic;
using System.Configuration;
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
using System.Net;
using Newtonsoft.Json;  //JSON Deserialiser, ladattu erikseen

namespace TwitchBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {


        WebClient m_WebClient;
        string apiUrl;


        String oauth = ConfigurationManager.AppSettings["Path"];  //oauth polku configista
        TcpClient tcpClient;
        StreamReader reader;
        StreamWriter writer;
        string userName;
        string password;
        string channelName;
        string chatMessagePrefix;
        string chatCommandId;
        DateTime lastMessage;
        Queue<string> sendMessageQueue;


        public MainWindow()
        {

            m_WebClient = new WebClient();
            this.apiUrl = "https://api.twitch.tv/kraken/streams/";


            sendMessageQueue = new Queue<string>(); //viesteille jono
            this.userName = "babymoses";  //twitch käyttäjän nimi LOWER CASE
            this.password = File.ReadAllText(oauth); //twitch chat OAuth Avain
            this.channelName = userName;
            //this.channelName = "turbomarlin";
            chatCommandId = "PRIVMSG";
            chatMessagePrefix = ":" + userName + "!" + userName + "@" + userName + ".tmi.twitch.tv PRIVMSG #" + channelName + " :";  //viestit näyttää chattiin menneessää tältä

            InitializeComponent();


            //timeri chatin päivitykselle
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);  //päivittää joka sekunti
            dispatcherTimer.Start();
            Reconnect();
        }

        private void Reconnect()
        {
            tcpClient = new TcpClient("irc.twitch.tv", 6667);  //tcp-yhteyden luonti
            reader = new StreamReader(tcpClient.GetStream());
            writer = new StreamWriter(tcpClient.GetStream());



            writer.WriteLine("PASS " + password + Environment.NewLine
                + "NICK " + userName + Environment.NewLine
                + "USER " + userName + " 8 * :" + userName);  //tällä autentikoidaan ihtemme pannulle
            writer.WriteLine("CAP REQ :twitch.tv/membership"); //tulostaa chatin käyttäjät
            writer.WriteLine("JOIN #" + channelName);  //chatroomiin liittyminen


            writer.Flush();
            lastMessage = DateTime.Now;


        }


        void dispatcherTimer_Tick(object sender, EventArgs e) //homma toimii timerin perässä
        {


            if (!tcpClient.Connected)
            {
                Reconnect();
            }


            TrySendingMessages();
            TryReceiveMessages();


            //txtChat.AppendText("\r\n" + DateTime.Now.Second.ToString());  //testi

            // var massage = reader.ReadLine();
            // txtChat.AppendText("\r\n" + massage);
            txtChat.ScrollToEnd();


        }

        /*
        void LoadKraken()
        {
            //var url = apiUrl + userName;  //käytä tätä
           var url = "https://api.twitch.tv/kraken/streams/steel_tv";
           string json = m_WebClient.DownloadString(url);

          TwitchTV twitchTV = JsonConvert.DeserializeObject<TwitchTV>(json);

        }
        */




        void TrySendingMessages()
        {
            if (DateTime.Now - lastMessage > TimeSpan.FromSeconds(2))  //viestit jonoon, ettei botti voi spämmätä koko ajan (2 sekuntia)
            {
                if (sendMessageQueue.Count > 0)  //jos jonossa tavaraa niin syljetään tavarat ulos yksi kerrallaan
                {
                    var message = sendMessageQueue.Dequeue();
                    writer.WriteLine(chatMessagePrefix + message);
                    writer.Flush();
                    lastMessage = DateTime.Now;
                }
            }

        }

        void TryReceiveMessages()
        {
            if (tcpClient.Available > 0 || reader.Peek() >= 0)
            {

             

                var message = reader.ReadLine();
                var iCollon = message.IndexOf(":", 1);

                //pingiin vastaus (muuten tulee timeouttia 10minuutin jälkeen
                if (message.StartsWith("PING"))
                {
                    writer.WriteLine("PONG tmi.twitch.tv");
                    writer.Flush();
                }


                if (iCollon > 0)
                {
                    var command = message.Substring(1, iCollon + 1); //viesti tulee kahden kaksoispisteen välissä-> kaapataan se sieltä (skipataan eka ":")
                    if (command.Contains(chatCommandId))  //pelkästään chattiin lähetetyt viestit (ei irc:n muuta roskaa) suodatetaan PRIVMSG:n avulla
                    {
                        var iExl = command.IndexOf("!"); //käyttäjän nimestä koppi huutomerkistä
                        var speaker = command.Substring(0, iExl);
                        var chatMessage = message.Substring(iCollon + 1);

                        ReceiveMessage(speaker, chatMessage);

                    }

                }

              // txtChat.AppendText("\r\n" + message);  //kaikki mitä ircci syöttää




            }
        }

        void ReceiveMessage(string speaker, string message)
        {


            txtChat.AppendText("\r\n" + speaker + ": " + message); //tulostus
            //txtChat.AppendText(reader.ReadLine());


   

            if (message.StartsWith("!commands"))
            {
                SendMessage("Current commands include !moro, !toucan, !nikolas, !merio, !marjo");
            }

            if (message.StartsWith("!moro"))
            {
                SendMessage("Mene roskiin " + speaker);
            }


            if (message.StartsWith("!toucan"))
            {
                SendMessage("                 ▄▄▄▀▀▀▄▄███▄ ░░░░░▄▀▀░░░░░░░▐░▀██▌ ░░░▄▀░░░░▄▄███░▌▀▀░▀█ ░░▄█░░▄▀▀▒▒▒▒▒▄▐░░░░█▌ ░▐█▀▄▀▄▄▄▄▀▀▀▀▌░░░░░▐█▄ ░▌▄▄▀▀░░░░░░░░▌░░░░▄███████▄ ░░░░░░░░░░░░░▐░░░░▐███████████▄ ░░░░░le░░░░░░░▐░░░░▐█████████████▄ ░░░░toucan░░░░░░▀▄░░░▐██████████████▄ ░░░░░░has░░░░░░░░▀▄▄████████████████▄ ░░░░░arrived░░░░░░░░░░░░█▀██████");
            }

            if (message.StartsWith("!nikolas"))
            {
                SendMessage("Nikolas on pyllynaama");
            }

            if (message.StartsWith("!merio"))
            {
                SendMessage("▓▓▓▓▀█░░░░░ ░░░░░▄▀▓▓▄██████▄░░░ ░░░░▄█▄█▀░░▄░▄░█▀░░░ ░░░▄▀░██▄░░▀░▀░▀▄░░░ ░░░▀▄░░▀░▄█▄▄░░▄█▄░░ ░░░░░▀█▄▄░░▀▀▀█▀░░░░ ░░▄▄▓▀▀░░░░░░░▒▒▒▀▀▀▓▄░ ░▐▓▒░░▒▒▒▒▒▒▒▒▒░▒▒▒▒▒▒▓ ░▐▓░█░░░░░░░░▄░░░░░░░░█░ ░▐▓░█░░░(◐)░░▄█▄░░(◐)░░░█ ░▐▓░░▀█▄▄▄▄█▀░▀█▄▄▄▄█▀░ TITS'A ME, MARIO !");
            }

            if (message.StartsWith("!marjo"))
            {
                SendMessage("░░░░░░░░░░▄▄▄▄░░░░░░ ░░░░░░░▄▀▀▓▓▓▀█░░░░░ ░░░░░▄▀▓▓▄██████▄░░░ ░░░░▄█▄█▀░░▄░▄░█▀░░░ ░░░▄▀░██▄░░▀░▀░▀▄░░░ ░░░▀▄░░▀░▄█▄▄░░▄█▄░░ ░░░░░▀█▄▄░░▀▀▀█▀░░░░ ░░░░░░░█▄▄░░░░█░░░░░ ░░░░░░░█░░░░▀▀█░░░░░ ░░░░░░░█▀▀▀░▄▄█░░░░░ ░░░░░░░█░░░░░░█▄░░░░ ▄▄▄▄██▀▀░░░░░░░▀██░░ ░▄█▀░▀░░░░▄░░░░░░█▄▄ ▀▀█▄▄▄░░░▄██░░░░▄█░░");
            }

        }



        void SendMessage(string message)
        {
            sendMessageQueue.Enqueue(message);  //viesti jonoon


        }




    }
}
