using System;

namespace ChatLibrary
{
    public class ChatModel
    {
        public override string ToString()
        {
            return Username;
        }
        public ChatModel()
        {
            Last_Activity = DateTime.Now;
        }
        public string Username { get; set; }

        private string _Messages;
        public string Messages
        {
            get { return _Messages; }
            set
            {
                _Messages = value;
                Last_Activity = DateTime.Now;
            }
        }
       
        public DateTime Last_Activity { get; private set; }
    }
}
