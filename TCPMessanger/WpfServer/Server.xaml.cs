using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.Sockets;
using ChatLibrary;
using System.Windows.Threading;

namespace WpfServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SocketServer socketServer;
        ClientCollection clientCollection;
        private delegate void ReceiveMessageDelegate(TcpClient tcpClient);
        public MainWindow()
        {
            InitializeComponent();
            clientCollection = new ClientCollection();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                socketServer = new SocketServer(Adress.Text, Port.Text);

                socketServer.ClientConnected += new SocketServer.ClientConnectedEventHandler(socketServer_ClientConnected);
                socketServer.MessageReceived += new SocketServer.MessageReceivedEventHandler(socketServer_MessageReceived);
                socketServer.ClientDisconnecting += new SocketServer.ClientDisconnectingEventHandler(socketServer_ClientDisconnecting);
                
                socketServer.Listen();
                WriteMessage("Сервер включен!");

                SwitchItems();

                DispatcherTimer dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 30);
                dispatcherTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Messenger Server");
            }
            txtStatus.Focus();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < clientCollection.Count; i++)
            {
                TimeSpan diff = DateTime.Now.Subtract(clientCollection[i].LastActivity);
                if (diff.TotalMinutes > 10)
                {
                    socketServer_ClientDisconnecting(this, clientCollection[i].Name);
                }
            }
        }

        private void SwitchItems()
        {
            btnStart.IsEnabled = !btnStart.IsEnabled;
            btnStop.IsEnabled = !btnStop.IsEnabled;
            btnSendMessage.IsEnabled = !btnSendMessage.IsEnabled;
            btnKickClient.IsEnabled = !btnKickClient.IsEnabled;

            Adress.IsEnabled = !Adress.IsEnabled;
            Port.IsEnabled = !Port.IsEnabled;
        }

        private void socketServer_ClientConnected(object sender, Client client)
        {
            clientCollection.Add(client);
            AddClientToList(client);
            SendClientList();
        }

        private void socketServer_MessageReceived(object sender, Message clientMessage)
        {
            if (clientMessage.MessageCommand.Equals(Command.PersonalMessage))
            {
                clientMessage.MessageDetail = WriteMessage(clientMessage);
                for (int i = 0; i < clientCollection.Count; i++)
                {
                    if (clientMessage.Sender.Equals(clientCollection[i].Name))
                        clientCollection[i].LastActivity = DateTime.Now;


                    if (clientMessage.Receiver.Equals(clientCollection[i].Name))
                    {
                        if (!clientCollection[i].SendMessage(clientMessage))
                        {
                            socketServer_ClientDisconnecting(this, clientCollection[i].Name);
                        } 
                    }
                }
            }
            else if (clientMessage.MessageCommand.Equals(Command.Conference))
            {
                Announce(clientMessage);
                WriteMessage(clientMessage);
            }
        }
        private void socketServer_ClientDisconnecting(object sender, string clientName)
        {
            Message logoutMessage = new Message();
            logoutMessage.Sender = Common.ServerName;
            logoutMessage.Receiver = clientName;
            logoutMessage.MessageCommand = Command.Logout;

            for (int i = 0; i < clientCollection.Count; i++)
            {
                if (clientCollection[i].Name.Equals(clientName))
                {
                    if (clientCollection[i].SendMessage(logoutMessage))
                    {
                        clientCollection[i].Socket.Close();
                    }
                    clientCollection.Remove(clientName);
                }
            }

            SendClientList();

            logoutMessage.Receiver = ChatLibrary.Common.All;
            logoutMessage.MessageCommand = ChatLibrary.Command.PersonalMessage;
            logoutMessage.MessageDetail = WriteMessage("{0} Отключен", clientName);
            Announce(logoutMessage);

        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < clientCollection.Count; i++)
            {

                ChatLibrary.Message logoutMessage = new ChatLibrary.Message();
                logoutMessage.Sender = ChatLibrary.Common.ServerName;
                logoutMessage.Receiver = clientCollection[i].Name;
                logoutMessage.MessageCommand = ChatLibrary.Command.Logout;

                clientCollection[i].SendMessage(logoutMessage);
                clientCollection[i].Socket.Close();
                clientCollection.Remove(clientCollection[i].Name);
            }

            socketServer.Stop();

            lstClients.Items.Clear();
            WriteMessage("Сервер выключен!");
            SwitchItems();
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (lstClients.Items.Count > 0)
            {
                if (lstClients.SelectedIndex.Equals(-1))
                {
                    lstClients.SelectedIndex = 0;
                }

                ChatLibrary.Message messageToAnnounce = new ChatLibrary.Message();
                messageToAnnounce.Sender = ChatLibrary.Common.ServerName;
                messageToAnnounce.Receiver = lstClients.SelectedItem.ToString();
                messageToAnnounce.MessageCommand = ChatLibrary.Command.PersonalMessage;
                messageToAnnounce.MessageDetail = WriteMessage(Message.Text);

                Announce(messageToAnnounce);
                Message.Clear();
            }
        }

        private void Announce(ChatLibrary.Message messageToAnnounce)
        {
            
            for (int i = 0; i < clientCollection.Count; i++)
            {
                if(!clientCollection[i].SendMessage(messageToAnnounce))
                {
                    socketServer_ClientDisconnecting(this, clientCollection[i].Name);
                }
                
            }
            
        }

        private void AddClientToList(Client client)
        {
            string strWriteMessage = WriteMessage("{0} Подключен", client.Name);

            ChatLibrary.Message connectionMessage = new ChatLibrary.Message();
            connectionMessage.Sender = ChatLibrary.Common.ServerName;
            connectionMessage.Receiver = ChatLibrary.Common.All;
            connectionMessage.MessageCommand = ChatLibrary.Command.PersonalMessage;
            connectionMessage.MessageDetail = strWriteMessage;

            Announce(connectionMessage);

        }
        private void SendClientList()
        {
            string strClientList = string.Empty;
            for (int i = 0; i < clientCollection.Count; i++)
            {
                strClientList += clientCollection[i].Name + "@";
            }
            strClientList = strClientList.TrimEnd(new char[] { '@' });

            ChatLibrary.Message clientListMessage = new ChatLibrary.Message();
            clientListMessage.Sender = ChatLibrary.Common.ServerName;
            clientListMessage.Receiver = ChatLibrary.Common.All;
            clientListMessage.MessageCommand = ChatLibrary.Command.ClientList;
            clientListMessage.MessageDetail = strClientList;

            Announce(clientListMessage);
            PopulateClientList(strClientList);
        }

        private string GetTime()
        {
            return DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
        }
        
        private string WriteMessage(ChatLibrary.Message clientMessage)
        {

            string strWriteText
                = string.Format("\r\n{0} {1}: {2}", GetTime(), clientMessage.Sender, clientMessage.MessageDetail);

            string strDisplayMessageType = string.Empty;
            if (clientMessage.MessageCommand.Equals(ChatLibrary.Command.PersonalMessage))
            {
                strDisplayMessageType = "Personal ";
            }
            else if (clientMessage.MessageCommand.Equals(ChatLibrary.Command.Conference))
            {
                strDisplayMessageType = "Conference ";
            }


            MessageToForm("\r\n" + strDisplayMessageType + strWriteText.TrimStart(new char[] { '\r', '\n' }));

            return strWriteText;
        }



        private string WriteMessage(string message, params object[] args)
        {
            message = string.Format(message, args);
            string strWriteText
                = string.Format
                ("\r\n{0} {1}: {2}", GetTime(), ChatLibrary.Common.ServerName, message, args);
            
            MessageToForm(strWriteText);
            
            return strWriteText;
        }

        private delegate void MessageToFormDelegate(string message);
        private void MessageToForm(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new MessageToFormDelegate(this.MessageToForm), message);
            }
            else
            {
                txtStatus.Text += message;
            }
        }

        private delegate void PopulateClientListDelegate(string clientList);

        private void PopulateClientList(string clientList)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new PopulateClientListDelegate(this.PopulateClientList), clientList);
            }
            else
            {
                lstClients.Items.Clear();
                lstClients.Items.Add(ChatLibrary.Common.All);
                string[] strClientList = clientList.Split(new char[] { '@' });
                for (int i = 0; i < strClientList.Length; i++)
                {
                    lstClients.Items.Add(strClientList[i]);
                }
            }

        }

        
        private void Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnSendMessage.IsEnabled = Message.Text.Equals(string.Empty) ? false : true;
        }

        private void btnKickClient_Click(object sender, RoutedEventArgs e)
        {
            if (lstClients.Items.Count > 0)
            {
                if (!lstClients.SelectedIndex.Equals(0))
                {
                    socketServer_ClientDisconnecting(this, lstClients.SelectedItem.ToString());
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            socketServer.Stop();
        }
    }
}
