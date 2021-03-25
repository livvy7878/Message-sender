using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using MessageSenderMessageModel;
using Newtonsoft.Json;

namespace MessageSenderServer
{
	class Program
	{
		private static TcpListener _listener;
		private static SQLiteConnection _connection;

		static Program()
		{
			_listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 55000);
			_connection = new SQLiteConnection("Data source=messages.db");
			_connection.Open();
		}

		static void Main(string[] args)
		{
			_listener.Start(10);
			WaitForClients();
		}

		private static void WaitForClients()
		{
			while (true)
			{
				TcpClient newClient = _listener.AcceptTcpClient();

				Task.Run(() => ReceiveClient(newClient));
			}
		}

		private static async void ReceiveClient(TcpClient client)
		{
			try
			{
				NetworkStream stream = client.GetStream();
				StreamReader reader = new StreamReader(stream);

				while (true)
				{
					while (stream.DataAvailable)
					{
						string readedLine = reader.ReadLine();
						ProcessMessage(readedLine, client);
					}

					await Task.Delay(20);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

		}

		private static async Task ProcessMessage(string message, TcpClient client)
		{
			string[] messageSplited = message.Split('#');

			switch (messageSplited[0])
			{
				case "send":
					RegisterMessage(messageSplited[1]);
					break;
				case "update":
					await SendMessagesToClient(client, messageSplited[1], messageSplited[2]);
					break;
				case "register":
					RegisterClient(messageSplited[1]);
					break;
				case "registeredusers":
					await SendUsersToClient(client);
					break;
			}
		}

		private static async Task SendUsersToClient(TcpClient client)
		{
			SQLiteCommand command = new SQLiteCommand("select * from user", _connection);

			SQLiteDataReader reader = command.ExecuteReader();

			List<string> users = new List<string>();

			while (reader.Read())
			{
				users.Add(reader.GetString(0));
			}
			reader.Close();

			string serializedUsers = JsonConvert.SerializeObject(users);
			StreamWriter writer = new StreamWriter(client.GetStream());

			await writer.WriteLineAsync(serializedUsers);
			try
			{
				await writer.FlushAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private static void RegisterClient(string clientLogin)
		{
			SQLiteCommand command = new SQLiteCommand("insert into user values($n)", _connection);

			command.Parameters.AddWithValue("$n", clientLogin);

			command.ExecuteNonQuery();
		}

		private static bool IsUserExists(string user)
		{
			SQLiteCommand command = new SQLiteCommand("select user.name from user where user.name = $r", _connection);

			command.Parameters.AddWithValue("$r", user);
			object findedUser;

			findedUser = command.ExecuteScalar();

			return findedUser == null;
		}

		private static void RegisterMessage(string message)
		{
			Message receivedMessage = JsonConvert.DeserializeObject<Message>(message);

			if (IsUserExists(receivedMessage.ReceiverLogin))
			{
				return;
			}

			if (IsUserExists(receivedMessage.SenderLogin))
			{
				return;
			}

			SQLiteCommand command = new SQLiteCommand("insert into message values" +
													  "((select user.rowid from user where user.name = $s), " +
													  "(select user.rowid from user where user.name = $r), $t, $time)", _connection);

			command.Parameters.AddWithValue("$r", receivedMessage.ReceiverLogin);
			command.Parameters.AddWithValue("$s", receivedMessage.SenderLogin);
			command.Parameters.AddWithValue("$t", receivedMessage.Text);
			command.Parameters.AddWithValue("$time", receivedMessage.SendingTime);

			command.ExecuteNonQuery();
		}

		private static async Task SendMessagesToClient(TcpClient client, string clientLogin, string queriedLogin)
		{
			SQLiteCommand command = new SQLiteCommand("select s.name, r.name, m.message_text, m.sending_time " +
													  "from message m " +
													  "left join user r on r.rowid = m.id_receiver " +
													  "left join user s on s.rowid = m.id_sender " +
													  "where m.id_receiver = (select user.rowid from user where user.name = $q) and " +
													  "m.id_sender = (select user.rowid from user where user.name = $c) " +
													  "union " +
													  "select s.name, r.name, m.message_text, m.sending_time " +
													  "from message m " +
													  "left join user r on r.rowid = m.id_receiver " +
													  "left join user s on s.rowid = m.id_sender " +
													  "where m.id_receiver = (select user.rowid from user where user.name = $c) and " +
													  "m.id_sender = (select user.rowid from user where user.name = $q) " +
													  "order by m.sending_time", _connection);

			command.Parameters.AddWithValue("$c", clientLogin);
			command.Parameters.AddWithValue("$q", queriedLogin);

			try
			{
				SQLiteDataReader reader = command.ExecuteReader();
				List<Message> messages = MakeMessageCollectionFromQuery(reader);
				StreamWriter writer = new StreamWriter(client.GetStream());
				string listInJson = JsonConvert.SerializeObject(messages);

				await writer.WriteLineAsync(listInJson);
				await writer.FlushAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private static List<Message> MakeMessageCollectionFromQuery(SQLiteDataReader reader)
		{
			List<Message> messages = new List<Message>();

			while (reader.Read())
			{
				try
				{
					Message newMessage = new Message()
					{
						ReceiverLogin = reader.GetString(0),
						SenderLogin = reader.GetString(1),
						Text = reader.GetString(2),
						SendingTime = reader.GetDateTime(3)
					};
					messages.Add(newMessage);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}

			reader.Close();

			return messages;
		}
	}
}
