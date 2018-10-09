using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

using ChatLibrary;

namespace WpfServer
{
	public class SocketServer
	{
		TcpListener listener;
		IPAddress ipaAddress;
		int iPort;

		public delegate void ListenForMessageDelegate(Client client) ;
		public delegate void ClientConnectedEventHandler (object sender, Client  client);
		public delegate void ClientDisconnectingEventHandler (object sender, string clientName);
		public delegate void MessageReceivedEventHandler (object sender, Message clientMessage);

		public event ClientConnectedEventHandler ClientConnected;
		public event ClientDisconnectingEventHandler ClientDisconnecting;
		public event MessageReceivedEventHandler MessageReceived;

		Thread thrListenForClients;
		
		ListenForMessageDelegate listenForMessageDelegate ;

		bool bListenForClients;

		public SocketServer(string ipAddress, string port)
		{
            ipaAddress	= IPAddress.Parse(ipAddress);
			iPort		= int.Parse(port);

			IPEndPoint endPoint 
				= new IPEndPoint (ipaAddress, iPort);

			listener = new TcpListener (endPoint);
			listener.Start();
		}

		public IPAddress SocketIPAddress
		{
			get{return ipaAddress;}
		}
		public int Port
		{
			get{return iPort;}
		}

		public void Listen ()
		{
			thrListenForClients = new Thread (new ThreadStart(ListenForClients));
			thrListenForClients.Start();		
		}

		public void Stop ()
		{
			listener.Stop();
		}

		private void ListenForClients ()
		{
			bListenForClients = true;

			while (bListenForClients)
			{
				Client acceptClient = new Client ();
				try
				{					
					acceptClient.Socket = listener.AcceptTcpClient();

					listenForMessageDelegate 
						= new ListenForMessageDelegate (ListenForMessages);

					listenForMessageDelegate.BeginInvoke
						(acceptClient, new AsyncCallback(ListenForMessagesCallback), "Выполнено!");
				}
				catch (Exception)
				{
					
					bListenForClients = false;
				}
			}
		}

		private void ListenForMessages (Client client)
		{
			while (true)
			{
                byte[] bytAcceptMessage = new byte[1024];
                try
                {
                    NetworkStream stream = client.Socket.GetStream();
                    stream.Read(bytAcceptMessage, 0, bytAcceptMessage.Length);
                }
                catch { break; }
				

				Message message = new ChatLibrary.Message(bytAcceptMessage);
				if (message.MessageCommand.Equals(Command.Login))
				{
					if (ClientConnected != null)
					{
						client.Name	= message.Sender;
						ClientConnected(this, client);					              
					}
				}
				if (message.MessageCommand.Equals(Command.PersonalMessage)||
					message.MessageCommand.Equals(Command.Conference)
					)
				{
					if (MessageReceived != null)
					{
						MessageReceived(this, message);
					}
				}
				if (message.MessageCommand.Equals(Command.Logout))
				{
					if (ClientDisconnecting != null)
					{
						ClientDisconnecting(this, message.Sender);
					}
				}
				
			}
		}

		private void ListenForMessagesCallback (IAsyncResult ar)
		{
            try
            { listenForMessageDelegate.EndInvoke(ar); }
            catch { }
            
		}		
	}
}
