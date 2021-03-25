using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageSenderMessageModel
{
	public class Message
	{
		public string SenderLogin { get; set; }
		public string ReceiverLogin { get; set; }
		public string Text { get; set; }
		public DateTime SendingTime { get; set; }
	}

	public class UserCredentials
	{
		public string Login { get; set; }
		public string Password { get; set; }
	}
}
