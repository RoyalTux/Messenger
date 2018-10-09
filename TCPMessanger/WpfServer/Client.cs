using System;
using System.Net;
using System.Net.Sockets;
using ChatLibrary;

namespace WpfServer
{
	public class Client
	{
		string strName;
		TcpClient tcpClient;
        public Client()
        {
            LastActivity = DateTime.Now;
        }
		public string Name 
		{
			get{return strName;}
			set{ this.strName = value;}			
		}
		public TcpClient Socket
		{
			get{return tcpClient;}
			set{ this.tcpClient = value;}
		}

        public DateTime LastActivity{ get; set; }
		public bool SendMessage (Message sendMessage)
		{
            try
            {
                NetworkStream stream = tcpClient.GetStream();
                stream.Write(sendMessage.GetRawMessage(), 0, sendMessage.GetRawMessage().Length);
            }
            catch {return false;}

            return true;
            
		}
	}

	public class ClientCollection: System.Collections.CollectionBase
	{
		
		public Client this[int index]
		{
			get {return (Client) List[index];}
			set { List[index] = value; }
		}

		public void Add (Client client)
		{
			List.Add(client);
		}
		public void Remove (string clientName)
		{
			for (int i=0;i<List.Count;i++)
			{
				if (((Client) List[i]).Name.Equals(clientName))
				{
					List.Remove(List[i]);
				}
			}

		}
		public bool Contains (string name)
		{
			for (int i=0;i<List.Count;i++)
			{
				if (((Client) List[i]).Name.Equals(name))
				{
					return true;
				}
			}
			return false;
		}
	}
}