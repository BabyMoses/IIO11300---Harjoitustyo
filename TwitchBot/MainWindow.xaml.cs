using Newtonsoft.Json;  //JSON Deserialiser, ladattu erikseen
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;  //tarvitaan TCP-yhteyteen
using System.Windows;
using System.Windows.Media;


namespace TwitchBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {

        WebClient m_WebClient;
        string defaultApiUrl;
        TcpClient tcpClient;
        StreamReader reader;
        StreamWriter writer;
        string userName;
        string password;
        string channelName;
        string chatMessagePrefix;
        string chatPrefix;
        string oauth;
        DateTime lastMessage;
        Queue<string> sendMessageQueue;
        Boolean botRunning;


        public MainWindow()
        {

            botRunning = false;
            m_WebClient = new WebClient();
            this.defaultApiUrl = "https://api.twitch.tv/kraken/streams/";  //streamien JSON urlin sijainti
            sendMessageQueue = new Queue<string>(); //viesteille jono
            this.userName = ConfigurationManager.AppSettings["Username"];  //twitch käyttäjän nimi LOWER CASE
            this.channelName = ConfigurationManager.AppSettings["Channel"]; //chattihuone (ROOM) johon liitytään       
            this.oauth = ConfigurationManager.AppSettings["Path"];  //oauth polku configista
            chatPrefix = "PRIVMSG";
            chatMessagePrefix = ":" + userName + "!" + userName + "@" + userName + ".tmi.twitch.tv PRIVMSG #" + channelName + " :";  //viestit lähtee chattiin tässä muodossa

            InitializeComponent();

            if (File.Exists(oauth))
            {
                this.password = File.ReadAllText(oauth); //twitch chat OAuth Avaimen polku appconffista
            }
            else
            {
                txtChat.AppendText("\r\n" + "The Filepath: " + oauth + " for the Oauth-key seems to be wrong" + "\r\n", "red");
            }


            try
            {
                Reconnect();
            }
            catch (Exception)
            {
                txtChat.AppendText("Error connecting to the chat ", "Red");
            }

        }

        private void Reconnect() //chatpannulle/chatroomiin liittyminen
        {

            tcpClient = new TcpClient("irc.twitch.tv", 6667);  //tcp-yhteyden luonti
            reader = new StreamReader(tcpClient.GetStream());
            writer = new StreamWriter(tcpClient.GetStream());
            writer.WriteLine("PASS " + password + Environment.NewLine + "NICK " + userName + Environment.NewLine);  //tällä autentikoidaan ihtemme pannulle
            //writer.WriteLine("CAP REQ :twitch.tv/membership"); //tulostaa chatin moderaattorit (turha)
            writer.WriteLine("JOIN #" + channelName);  //chatroomiin liittyminen
            writer.Flush();
            lastMessage = DateTime.Now;  //timestamppi tulille


            //katsotaan menikö yhdistys läpi
            var message = reader.ReadLine();
            if (message.Contains("001"))
            {
                txtChat.AppendText("SUCCESFULLY CONNECTED TO CHATROOM #" + channelName + "..." + "\r\n", "Chartreuse");
                Timer(); // TIMERI PÄÄLLE
            }

            else
            {
                txtChat.AppendText("\r\n" + "ERROR WHILE LOGGING IN TO CHAT, PLEASE CHECK YOUR CREDENTIALS IN .CFG AND START THE PROGRAM AGAIN" + "\r\n", "Red");
            }


        }

        private void Timer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);  //päivittää joka 1/4 sekunti
            dispatcherTimer.Start();
        }

        private void HandleChecked(object sender, RoutedEventArgs e)  // BOTTI PÄÄLLÄ (ON)
        {
            txtChat.AppendText("\r\n" + "BOT IS NOW UP AND RUNNING!!" + "\r\n", "Chartreuse");
            botRunning = true;
            sendMessageQueue.Clear();
            Timer();
        }

        private void HandleUnchecked(object sender, RoutedEventArgs e)  // BOTTI POIS (OFF)
        {
            txtChat.AppendText("\r\n" + "BOT IS NOW OFFLINE =(" + "\r\n", "red");
            sendMessageQueue.Clear();
            botRunning = false;
        }


        void dispatcherTimer_Tick(object sender, EventArgs e) //homma toimii timerin tickien perässä
        {

            if (!tcpClient.Connected)
            {
                Reconnect();
            }

            try
            {
                if (botRunning == true)
                {
                    TrySendingMessages();
                }

                TryReceiveMessages();

            }

            catch (Exception ex)
            {
                txtChat.AppendText("Error getting/sending messages: " + ex.ToString() + "\r\n", "Red");
            }


            try
            {
                loadApistats();
            }
            catch (Exception ex)
            {
                txtChat.AppendText("Error loading Twitch API: " + ex.ToString() + "\r\n", "Red");

            }

            txtChat.ScrollToEnd();

        }


        void loadApistats()  //viewers, followers, total views tiedot apista
        {

            String apiUrl = defaultApiUrl + channelName;
            string json = m_WebClient.DownloadString(apiUrl);
            TwitchTV twitchTV = JsonConvert.DeserializeObject<TwitchTV>(json);  //newtonsoftin json desiarialiser tekee jsonista parsettamisen todella helpoksi

            if (twitchTV.stream != null)  //apin json statsit saatavilla *VAIN* jos streami on online
            {
                lblViewers.Content = string.Format("Viewers {0:000}", twitchTV.stream.viewers);
                lblFollowers.Content = string.Format("Followers {0:000}", twitchTV.stream.channel.followers);
                lblViews.Content = string.Format("Views {0:000}", twitchTV.stream.channel.views);
            }
            else
            {
                lblViewers.Foreground = new SolidColorBrush(Colors.Red);
                lblViewers.Content = "Stream Offline";
                lblFollowers.Content = "";
                lblViews.Content = "";
            }
        }


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
                    if (command.Contains(chatPrefix))  //pelkästään chattiin lähetetyt viestit (ei irc:n muuta roskaa) suodatetaan PRIVMSG:n avulla
                    {
                        var iExl = command.IndexOf("!"); //käyttäjän nimestä koppi huutomerkistä
                        var speaker = command.Substring(0, iExl);
                        var chatMessage = message.Substring(iCollon + 1);

                        ReceiveMessage(speaker, chatMessage);  //heitetään parsitut käyttäjät/viestit "tulostimeen" 

                    }

                }

                //txtChat.AppendText("\r\n" + message);  //kaikki mitä ircci syöttää, jos tarvii debugailla

            }
        }

        void ReceiveMessage(string speaker, string message)
        {


            txtChat.AppendText("\r\n" + speaker + ": ", "DarkOrange"); //käyttäjän tulostus 
            txtChat.AppendText(message, "#FFCDCDCD");  //viestin tulostus

            //commands
            if (message.StartsWith("!commands"))
            {
                SendMessage("Current commands include !moro, !toucan, !merio, !marjo, !specs");
            }

            if (message.StartsWith("!moro"))
            {
                SendMessage("No morjesta morjesta " + speaker + "!");
            }


            if (message.StartsWith("!toucan"))
            {
                SendMessage("                 ▄▄▄▀▀▀▄▄███▄ ░░░░░▄▀▀░░░░░░░▐░▀██▌ ░░░▄▀░░░░▄▄███░▌▀▀░▀█ ░░▄█░░▄▀▀▒▒▒▒▒▄▐░░░░█▌ ░▐█▀▄▀▄▄▄▄▀▀▀▀▌░░░░░▐█▄ ░▌▄▄▀▀░░░░░░░░▌░░░░▄███████▄ ░░░░░░░░░░░░░▐░░░░▐███████████▄ ░░░░░le░░░░░░░▐░░░░▐█████████████▄ ░░░░toucan░░░░░░▀▄░░░▐██████████████▄ ░░░░░░has░░░░░░░░▀▄▄████████████████▄ ░░░░░arrived░░░░░░░░░░░░█▀██████");
            }


            if (message.StartsWith("!merio"))
            {
                SendMessage("▓▓▓▓▀█░░░░░ ░░░░░▄▀▓▓▄██████▄░░░ ░░░░▄█▄█▀░░▄░▄░█▀░░░ ░░░▄▀░██▄░░▀░▀░▀▄░░░ ░░░▀▄░░▀░▄█▄▄░░▄█▄░░ ░░░░░▀█▄▄░░▀▀▀█▀░░░░ ░░▄▄▓▀▀░░░░░░░▒▒▒▀▀▀▓▄░ ░▐▓▒░░▒▒▒▒▒▒▒▒▒░▒▒▒▒▒▒▓ ░▐▓░█░░░░░░░░▄░░░░░░░░█░ ░▐▓░█░░░(◐)░░▄█▄░░(◐)░░░█ ░▐▓░░▀█▄▄▄▄█▀░▀█▄▄▄▄█▀░ TITS'A ME, MARIO !");
            }

            if (message.StartsWith("!marjo"))
            {
                SendMessage("░░░░░░░░░░▄▄▄▄░░░░░░ ░░░░░░░▄▀▀▓▓▓▀█░░░░░ ░░░░░▄▀▓▓▄██████▄░░░ ░░░░▄█▄█▀░░▄░▄░█▀░░░ ░░░▄▀░██▄░░▀░▀░▀▄░░░ ░░░▀▄░░▀░▄█▄▄░░▄█▄░░ ░░░░░▀█▄▄░░▀▀▀█▀░░░░ ░░░░░░░█▄▄░░░░█░░░░░ ░░░░░░░█░░░░▀▀█░░░░░ ░░░░░░░█▀▀▀░▄▄█░░░░░ ░░░░░░░█░░░░░░█▄░░░░ ▄▄▄▄██▀▀░░░░░░░▀██░░ ░▄█▀░▀░░░░▄░░░░░░█▄▄ ▀▀█▄▄▄░░░▄██░░░░▄█░░");
            }

            if (message.StartsWith("!specs"))
            {
                SendMessage("Ekeen™ vesijäähyt");
            }

            //containit

            if (message.Contains("aim"))
            {
                SendMessage("For me - aim - it is about precision");
            }

            if (message.Contains("spray"))
            {
                SendMessage("For me, spray is about control");
            }

            if (message.Contains("nikola"))
            {
                SendMessage("Nikolas on pyllynaama");
            }

            if (message.Contains("ohjelmoin"))
            {
                SendMessage("Ohjelmointi on todella kivaa ja palkitsevaa puuhaa! KappaRoss");
            }

            //bännit
            if (message.Contains("slaidu") || message.Contains("lompsa"))
            {
                SendMessage("/timeout " + speaker + " 30");
                SendMessage("Erittäin loso sana");
            }

        }



        void SendMessage(string message)
        {
            sendMessageQueue.Enqueue(message);  //viesti jonoon
        }


    }
}
