using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace WpfClient
{
	public class ClientSocket
	{
		TcpClient tcpClient;
		Thread thrListenForMessages;
		NetworkStream stream;

		string strName;
		IPAddress ipaAddress;
		int iPort;


		bool bListen = true;

		public delegate void MessageReceivedEventHandler (object sender, ChatLibrary.Message receivedMessage);
		public delegate void CommandReceivedEventHandler (object sender, ChatLibrary.Message commandMessage);

        public event MessageReceivedEventHandler MessageReceived;
		public event CommandReceivedEventHandler CommandReceived;
		
				        
		public ClientSocket(string name, string ipAddress, string port)
		{
			
			strName		= name;

			ipaAddress	= IPAddress.Parse(ipAddress);
			iPort		= int.Parse(port);
			IPEndPoint serverEndpoint = new IPEndPoint (ipaAddress , iPort);

			tcpClient	= new TcpClient ();
			tcpClient.Connect(serverEndpoint);

			thrListenForMessages = new Thread (new ThreadStart(ListenForMessages));
			thrListenForMessages.Start();

		}

		public string Name
		{
			get{return strName;}
		}
		public IPAddress SocketIPAddress
		{
			get{return ipaAddress;}
		}
		public int Port
		{
			get{return iPort;}
		}

		public void Login ()
		{
            ChatLibrary.Message loginMessage	= new ChatLibrary.Message ();
			loginMessage.Sender			= strName;
			loginMessage.Receiver		= ChatLibrary.Common.ServerName;
			loginMessage.MessageCommand	= ChatLibrary.Command.Login;

			SendMessage(loginMessage);
		}

		public void Logout ()
		{
            ChatLibrary.Message logoutMessage	= new ChatLibrary.Message ();
			logoutMessage.Sender		= strName;
			logoutMessage.Receiver		= ChatLibrary.Common.ServerName;
			logoutMessage.MessageCommand= ChatLibrary.Command.Logout;

			SendMessage(logoutMessage);
		}


		public void SendMessage (ChatLibrary.Message sendMessage)
		{
			NetworkStream stream	= tcpClient.GetStream();
			byte [] bytRawMessage	= sendMessage.GetRawMessage();		
			stream.Write(bytRawMessage, 0, bytRawMessage.Length);
		}

		public void Stop()
		{
			stream.Flush();
			stream.Close();
		}

		private void ListenForMessages ()
		{
			try
			{
				while (bListen)
				{
					stream	= tcpClient.GetStream();
					byte [] bytRawMessage	= new byte [1024];
					stream.Read(bytRawMessage, 0, bytRawMessage.Length);

                    ChatLibrary.Message receivedMessage = new ChatLibrary.Message (bytRawMessage);

						
					if (receivedMessage.MessageCommand.Equals(ChatLibrary.Command.Logout))
					{
						bListen = false;
					}
					if (receivedMessage.MessageCommand.Equals(ChatLibrary.Command.Conference)||
						receivedMessage.MessageCommand.Equals(ChatLibrary.Command.PersonalMessage)
						)
					{
						if (MessageReceived != null)
						{					
							MessageReceived(this, receivedMessage);
						}
					}
					else
					{
						if (CommandReceived != null)
						{
							CommandReceived(this, receivedMessage);
						}
					}
				}			
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			};
		}
	}
}
