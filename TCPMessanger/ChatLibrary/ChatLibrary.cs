using System;

namespace ChatLibrary
{
	public enum Command
	{
		Login			= 0,

		PersonalMessage	= 1,
		ClientList		= 2,
		Conference		= 3,
		Logout			= 4
	};

	public class Common
	{
		public const string ServerName	= "Server";
		public const string All			= "All";
		public const string Conference	= "Conference";
	}

	public class Message
	{
		
        string strSender;
		string strReceiver;
		Command cmdMessageCommand;
		string strMessageDetail;

		public Message ()
		{

		}

		public Message (byte [] rawMessage)
		{
			string strRawStringMessage 
				= System.Text.Encoding.UTF8.GetString (rawMessage);            
			string [] strRawStringMessageArray
				= strRawStringMessage.Split(new char []{'|'});
            
			this.strSender			= strRawStringMessageArray[1];
			this.strReceiver		= strRawStringMessageArray[2];
			this.cmdMessageCommand	= (Command) Convert.ToInt32(strRawStringMessageArray[3]);
			this.MessageDetail		= strRawStringMessageArray[4];
		}

		public string Sender
		{
			get{ return strSender;}
			set{ strSender = value;}
		}

		public string Receiver
		{
			get{ return strReceiver;}
			set{ strReceiver = value;}
		}
		public Command MessageCommand
		{
			get{ return cmdMessageCommand ;}
			set{ cmdMessageCommand = value;}
		}
		public string MessageDetail
		{
			get{ return strMessageDetail ;}
			set{ strMessageDetail = value;}
		}        

		public byte [] GetRawMessage ()
		{
			System.Text.StringBuilder sbMessage 
				= new System.Text.StringBuilder ("Message");
			sbMessage.Append("|");
			sbMessage.Append(strSender);
			sbMessage.Append("|");
			sbMessage.Append(strReceiver);
			sbMessage.Append("|");
			sbMessage.Append((int)cmdMessageCommand);
			sbMessage.Append("|");
			sbMessage.Append(strMessageDetail);
			sbMessage.Append("|");

			return System.Text.Encoding.UTF8.GetBytes(sbMessage.ToString());
		}
	}
}