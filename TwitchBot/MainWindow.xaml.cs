using Newtonsoft.Json;  //JSON Deserialiser, ladattu erikseen
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;  //tarvitaan TCP-yhteyteen
using System.Windows;

namespace TwitchBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {


        WebClient m_WebClient;  //apia
        string apiUrl;          //apia


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
        Boolean timer_Running = false;


        public MainWindow()
        {

            m_WebClient = new WebClient();
            this.apiUrl = "https://api.twitch.tv/kraken/streams/";


            sendMessageQueue = new Queue<string>(); //viesteille jono
            this.userName = ConfigurationManager.AppSettings["Username"];  //twitch käyttäjän nimi LOWER CASE
            this.channelName = ConfigurationManager.AppSettings["Channel"]; //chattihuone (ROOM) johon liitytään
            this.password = File.ReadAllText(oauth); //twitch chat OAuth Avain
            //this.channelName = userName;
            chatCommandId = "PRIVMSG";
            chatMessagePrefix = ":" + userName + "!" + userName + "@" + userName + ".tmi.twitch.tv PRIVMSG #" + channelName + " :";  //viestit näyttää chattiin menneessää tältä

            InitializeComponent();
            Timer();
          


            /*timeri chatin päivitykselle + botti
            System.Windows.Threading.DispatcherTimer dispatcherTimer2 = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer2.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer2.Interval = new TimeSpan(0, 0, 0, 0, 500);  //päivittää joka sekunti
             * */
        }

        private void Timer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);  //päivittää joka sekunti
            dispatcherTimer.Start();
            Reconnect();

            /*

            if (timer_Running == true)
            {
                //timeri chatin päivitykselle
                
                
            }
            else
            {

                dispatcherTimer.Stop();
                txtChat.AppendText("\r\n" + "BOOLEAN ON FALSE NYT");
            }
            */

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

            txtChat.AppendText("\r\n"+"SUCCESFULLY CONNECTED TO CHATROOM #"+channelName); //tulostus


        }


        /*

        private void HandleChecked(object sender, RoutedEventArgs e)
        {
            txtChat.AppendText("\r\n" + "toggled XD");
            timer_Running = true;
            Timer();

        }

        private void HandleUnchecked(object sender, RoutedEventArgs e)
        {
            txtChat.AppendText("\r\n" + "Untoggled XD");
            timer_Running = false;
            Timer();
         

        }

        */
        void dispatcherTimer_Tick(object sender, EventArgs e) //homma toimii timerin perässä
        {


            if (!tcpClient.Connected)
            {
                
                Reconnect();
                txtChat.AppendText("\r\n" + "SUCCESFULLY CONNECTED TO CHATROOM #" + channelName); //tulostus
            }


            TrySendingMessages();
            TryReceiveMessages();
            loadApistats();

            //txtChat.AppendText("\r\n" + DateTime.Now.Second.ToString());  //testi

            // var massage = reader.ReadLine();
            // txtChat.AppendText("\r\n" + massage);
            txtChat.ScrollToEnd();
            


        }

        
        void loadApistats()  //apia
        {
            //var url = apiUrl + userName;  //käytä tätä
           //var url = "https://api.twitch.tv/kraken/streams/steel_tv";

           apiUrl = "https://api.twitch.tv/kraken/streams/"+channelName;
           string json = m_WebClient.DownloadString(apiUrl);

          TwitchTV twitchTV = JsonConvert.DeserializeObject<TwitchTV>(json);

          if (twitchTV.stream != null)
          {

              // if (twitchTV.stream.channel.followers != 0L) 
              lblViewers.Content = string.Format("Viewers {0:000}", twitchTV.stream.viewers);
              lblFollowers.Content = string.Format("Followers {0:000}", twitchTV.stream.channel.followers);
              lblViews.Content = string.Format("Views {0:000}", twitchTV.stream.channel.views);
          }
          else
          {
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


   
            //commands
            if (message.StartsWith("!commands"))
            {
                SendMessage("Current commands include !moro, !toucan, !merio, !marjo, !aim, !specs");
            }

            if (message.StartsWith("!moro"))
            {
                SendMessage("Mene roskiin " + speaker);
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

            //bännit
            if (message.Contains("slaidu"))
            {
                SendMessage("/timeout "+speaker+" 30");
                SendMessage("Erittäin loso sana");
            }

            if (message.Contains("lompsa"))
            {
                SendMessage("/timeout " + speaker + " 60");
                SendMessage("Erittäin loso sana");
            }
        }



        void SendMessage(string message)
        {
            sendMessageQueue.Enqueue(message);  //viesti jonoon


        }




    }
}
