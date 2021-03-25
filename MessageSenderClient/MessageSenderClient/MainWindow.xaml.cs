using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MessageSenderClient
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainViewModel _mainViewModel;

		public MainWindow()
		{
			InitializeComponent();
		}

		private async void ButtonSendClickEventHandler(object sender, RoutedEventArgs e)
		{
			await _mainViewModel.SendMessage();
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			_mainViewModel = (MainViewModel)App.Current.FindResource("MainViewModel");
		}

		private async void ButtonUpdateClickEventHandler(object sender, RoutedEventArgs e)
		{
			await _mainViewModel.UpdateMessages();
		}

		private async void ButtonRegisterClickEventHandler(object sender, RoutedEventArgs e)
		{
			await _mainViewModel.RegisterClient();
		}

		private async void ButtonUpdateUsersClickEventHandler(object sender, RoutedEventArgs e)
		{
			await _mainViewModel.UpdateRegisteredUsers();
		}
	}
}
