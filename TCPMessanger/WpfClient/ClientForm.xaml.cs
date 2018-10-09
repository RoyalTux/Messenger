using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using ChatLibrary;
using System.Collections.ObjectModel;

namespace WpfClient
{
    /// <summary>
    /// Логика взаимодействия для ClientForm.xaml
    /// </summary>
    public partial class ClientForm : Window
    {
        
        internal ClientSocket clientSocket;
        List<ChatModel> chats;
        string ChoosenUser;

        public ClientForm(string Username, string IP, string Port)
        {
            InitializeComponent();
            ChoosenUser = null;
            chats = new List<ChatModel>();

            

            try
            {
                clientSocket = new ClientSocket
                    (Username, IP, Port);

                clientSocket.MessageReceived
                    += new ClientSocket.MessageReceivedEventHandler(clientSocket_MessageReceived);
                clientSocket.CommandReceived
                    += new ClientSocket.CommandReceivedEventHandler(clientSocket_CommandReceived);

                clientSocket.Login();
                
                txtAllMessages.Text = "Messenger: Connected";

                Message conferenceMessage = new Message();
                conferenceMessage.Sender = Common.Conference;
                conferenceMessage.MessageDetail = Username + " Connected";                     
                CreateNewClient(conferenceMessage);
                this.Title = Username;
            }
            catch (Exception exNotConnected)
            {
                MessageBox.Show
                    (exNotConnected.Message, "Messenger", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.Close();
            }
        }

        private delegate void CreateNewClientDelegate(Message clientMessage);
        private void CreateNewClient(ChatLibrary.Message clientMessage)
        {
            ChatModel chat = new ChatModel();
            chat.Username = clientMessage.MessageCommand.Equals(ChatLibrary.Command.Conference) ? ChatLibrary.Common.Conference : clientMessage.Sender;

            if (!clientMessage.MessageDetail.Equals(string.Empty))
            {
                chat.Messages += WriteMessage(clientMessage);
            }
            chats.Add(chat);

            UpdateListBox();
        }

        private delegate void UpdateListBoxDelegate();
        private void UpdateListBox()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new UpdateListBoxDelegate(this.UpdateListBox));
            }
            else
            {
                
                if (ChoosenUser != null)
                {
                    txtAllMessages.Text = chats.FirstOrDefault(x => x.Username == ChoosenUser).Messages;
                    //todo ex
                }
                lstClients.Items.Clear();
                foreach (var user in chats)
                {
                    lstClients.Items.Add(user.Username);
                }
                LableChoosenUser.Content = "Переписка с " + ChoosenUser;
            }
        }
        private void clientSocket_MessageReceived(object sender, ChatLibrary.Message receivedMessage)
        {
            if (receivedMessage.MessageCommand.Equals(Command.PersonalMessage) ||
                receivedMessage.MessageCommand.Equals(Command.Conference)
                )
            {
                if (receivedMessage.Receiver.Equals(clientSocket.Name) || receivedMessage.Receiver.Equals(Common.All))
                {
                    bool bWindowFound = false;
                    
                    foreach (var chat in chats)
                    {
                        if (chat.Username.Equals(receivedMessage.Sender))
                        {
                           
                            ChoosenUser = receivedMessage.Sender;
                            OpenChat();
                           
                            chat.Messages += WriteMessage(receivedMessage);
                            bWindowFound = true;
                            break;
                        }
                    }
                    if (!bWindowFound)
                    {
                        CreateNewClientDelegate createNewClientDelegate = new CreateNewClientDelegate(CreateNewClient);
                        Dispatcher.Invoke(createNewClientDelegate, new object[] { receivedMessage });
                    }
                }
            }
            UpdateListBox();
        }


        private void clientSocket_CommandReceived(object sender, ChatLibrary.Message commandMessage)
        {
            if (commandMessage.MessageCommand.Equals(ChatLibrary.Command.ClientList))
            {
                
                string[] strClientList = commandMessage.MessageDetail.Split(new char[] { '@' });
                for (int i = 0; i < strClientList.Length; i++)
                {
                    if (!strClientList[i].Equals(clientSocket.Name))
                    {
                        chats.Add(new ChatModel { Username = strClientList[i] });
                    }
                }

                for (int i = 0; i < chats.Count; i++)
                {
                    if ((!strClientList.Contains(chats[i].Username))
                        && chats[i].Username != "Server"
                        && chats[i].Username != Common.Conference)
                    {
                        chats.RemoveAt(i);
                        UpdateListBox();
                        i--;
                    }
                }

            }
            
            if (commandMessage.MessageCommand.Equals(ChatLibrary.Command.Logout))
            {
                RequestDisconnect();
            }
            UpdateListBox();
        }





        public string WriteMessage(Message receivedMessage)
        {
            string strWriteMessage = string.Empty;
            if (receivedMessage.MessageDetail.StartsWith("\r\n")) 
            {
                strWriteMessage = string.Format("{0}", receivedMessage.MessageDetail);
            }
            else
            {
                strWriteMessage = string.Format("\r\n{0} {1}: {2}", GetTime(), receivedMessage.Sender, receivedMessage.MessageDetail);
            }

            return strWriteMessage;
        }
        private string GetTime()
        {
            return DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to Exit?", "Messenger", MessageBoxButton.YesNo, MessageBoxImage.Information).Equals(MessageBoxResult.Yes))
            {
                if (clientSocket != null)
                {
                    RequestDisconnect();
                    
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void RequestDisconnect()
        {
            try
            {
                clientSocket.Logout();
                Environment.Exit(0);      
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Messenger", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

            if (ChoosenUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }
            
            Message sendMessage = new Message();
            sendMessage.Sender = clientSocket.Name;
            sendMessage.Receiver = ChoosenUser;
                 
            sendMessage.MessageCommand
                = ChoosenUser.Equals(ChatLibrary.Common.Conference) ? ChatLibrary.Command.Conference : ChatLibrary.Command.PersonalMessage;
            sendMessage.MessageDetail = txtNewMessage.Text;

            clientSocket.SendMessage(sendMessage);
            chats.FirstOrDefault(x => x.Username == ChoosenUser).Messages += WriteMessage(sendMessage);
            UpdateListBox();
            txtNewMessage.Clear();
        }

        private void lstClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ChoosenUser = lstClients.SelectedItem.ToString();
                LableChoosenUser.Content = "Переписка с " + ChoosenUser;
            }
            catch { return; }
            OpenChat();
        }

        private void OpenChat()
        {
            if (ChoosenUser == null) return;
            bool bIsFound = false;
            foreach (ChatModel chat in chats)
            {
                if (chat.Username.Equals(ChoosenUser))
                    bIsFound = true;
            }

            if (!bIsFound)
            {
                Message emptyMessage = new Message();
                emptyMessage.Sender = ChoosenUser;
                emptyMessage.MessageDetail = string.Empty;
                CreateNewClientDelegate createNewClientDelegate = new CreateNewClientDelegate(CreateNewClient);
                Dispatcher.Invoke(createNewClientDelegate, new object[] { emptyMessage });
            }
            UpdateListBox();
        }
    }
}
