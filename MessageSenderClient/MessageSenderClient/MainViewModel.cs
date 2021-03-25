using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using MessageSenderMessageModel;
using Newtonsoft.Json;

namespace MessageSenderClient
{
	public class MainViewModel : DependencyObject
	{
		public static readonly DependencyProperty ReceiverNameProperty = DependencyProperty.Register(
			"ReceiverName", typeof(string), typeof(MainViewModel), new PropertyMetadata(default(string)));

		public static readonly DependencyProperty WrittenMessageProperty = DependencyProperty.Register(
			"WrittenMessage", typeof(string), typeof(MainViewModel), new PropertyMetadata(default(string)));

		private readonly TcpClient _client;

		public MainViewModel()
		{
			IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
			_client = new TcpClient();
			_client.Connect(endPoint);

			LoggedInUser = new UserCredentials {Login = "UserOne", Password = "1234"};
			ReceiverName = "UserTwo";
			Messages = new ObservableCollection<Message>();
			RegisteredUsers = new ObservableCollection<string>();
			UpdateRegisteredUsers();
		}

		public ObservableCollection<string> RegisteredUsers { get; set; }

		public ObservableCollection<Message> Messages { get; set; }

		public string ReceiverName
		{
			get => (string) GetValue(ReceiverNameProperty);
			set => SetValue(ReceiverNameProperty, value);
		}

		public UserCredentials LoggedInUser { get; set; }

		public string WrittenMessage
		{
			get => (string) GetValue(WrittenMessageProperty);
			set => SetValue(WrittenMessageProperty, value);
		}

		public async Task SendMessage()
		{
			StreamWriter writer = new StreamWriter(_client.GetStream());

			Message newMessage = new Message
			{
				ReceiverLogin = ReceiverName,
				SenderLogin = LoggedInUser.Login,
				Text = WrittenMessage,
				SendingTime = DateTime.Now
			};

			string serializedMessage = JsonConvert.SerializeObject(newMessage);

			await writer.WriteLineAsync("send#" + serializedMessage);
			await writer.FlushAsync();
		}

		public async Task UpdateRegisteredUsers()
		{
			NetworkStream networkStream = _client.GetStream();
			StreamWriter writer = new StreamWriter(networkStream);

			await writer.WriteLineAsync("registeredusers");
			await writer.FlushAsync();

			StreamReader reader = new StreamReader(networkStream);
			string usersInJson = await reader.ReadLineAsync();

			List<string> usersInList = JsonConvert.DeserializeObject<List<string>>(usersInJson);

			Dispatcher.Invoke(() => { RegisteredUsers.Clear(); });

			for (int i = 0; i < usersInList.Count; i++)
			{
				Dispatcher.Invoke(() => { RegisteredUsers.Add(usersInList[i]); });
			}
		}

		public async Task RegisterClient()
		{
			StreamWriter writer = new StreamWriter(_client.GetStream());

			await writer.WriteLineAsync($"register#{LoggedInUser.Login}");
			await writer.FlushAsync();
		}

		public async Task UpdateMessages()
		{
			NetworkStream networkStream = _client.GetStream();
			StreamWriter writer = new StreamWriter(networkStream);

			await writer.WriteLineAsync("update#" + LoggedInUser.Login + $"#{ReceiverName}");
			await writer.FlushAsync();

			StreamReader reader = new StreamReader(networkStream);
			string messagesInJson = await reader.ReadLineAsync();

			List<Message> messages = JsonConvert.DeserializeObject<List<Message>>(messagesInJson);

			Dispatcher.Invoke(() => { Messages.Clear(); });

			for (int i = 0; i < messages.Count; i++)
			{
				Dispatcher.Invoke(() => { Messages.Add(messages[i]); });
			}
		}
	}
}