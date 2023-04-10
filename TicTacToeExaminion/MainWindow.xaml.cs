using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using TicTacToeLiblary;

namespace TicTacToeExaminion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //убрать фиксированные пути, исправить вывод победителя, исправить
        //нажатие подключиться много раз, когда много раз нажимаешь vs Player исправить это

        private TcpClient server = new TcpClient();
        private NetworkStream stream = null;
        private DispatcherTimer timer = new DispatcherTimer();
        private User user = null;

        private bool yourQueue = true;
        private bool IsSearchingGame = false;
        private string YourTurnAvatar = "C:\\Users\\Bogdan\\source\\repos\\TicTacToeExaminion\\TicTacToeExaminion\\Images\\circle.png";
        private string OponentTurnAvatar = "C:\\Users\\Bogdan\\source\\repos\\TicTacToeExaminion\\TicTacToeExaminion\\Images\\cross.png";

        public MainWindow(User user)
        {
            InitializeComponent();
            this.user = user;

            LoadUserInfo();

            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += Timer_Tick;
        }

        #region Window buttons

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }

        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        private void Window_DragDrop(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private void ExitAccount_Click(object sender, MouseButtonEventArgs e)
        {
            AuthWindow window = new AuthWindow();
            window.Show();
            this.Close();
        }
        private void LeaveGame_Click(object sender, MouseButtonEventArgs e)
        {
            labelVsPlayer.Content = "vs Player";
            LeaveGameBtn.Visibility = Visibility.Hidden;
            ExitAccountBtn.Visibility = Visibility.Visible;
            SlowOpacityGameGrid();
            IsSearchingGame = false;
            foreach (var item in GameGridTemp.Children)
            {
                Border border = (Border)item;
                border.Background = Brushes.CornflowerBlue;

            }
            stream = server.GetStream();
            stream.Write(Encoding.ASCII.GetBytes("Bye"));
            server.Close();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (server.Available > 0)
            {
                stream = server.GetStream();
                stream.Write(Encoding.ASCII.GetBytes("Bye"));
                server.Close();
            }
        }
        #endregion

        private async void PlayPlayer_Click(object sender, MouseButtonEventArgs e)
        {
            if (IsSearchingGame == false)
            {
                IsSearchingGame = true;
                labelVsPlayer.Content = "Stop";
                User oponent = null;
                server = new TcpClient();
                server.Connect("127.0.0.1", 10002);
                stream = server.GetStream();
                stream.Write(Encoding.ASCII.GetBytes("PlayOnline - " + user.Username));
                await Task.Run(() =>
                {
                    while (true)
                    {
                        byte[] buffer = new byte[18184];
                        stream.Read(buffer, 0, buffer.Length);
                        string response = Encoding.ASCII.GetString(buffer);
                        string[] arrResponse = response.Split(" ");
                        if (response.Contains("Oponent finded!"))
                        {
                            if (arrResponse.Contains("Your"))
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    LabelTurn.Content = "Your turn";
                                    yourQueue = true;
                                    YourTurnAvatar = @"C:\Users\Bogdan\source\repos\TicTacToeExaminion\TicTacToeExaminion\Images\circle.png";
                                    YourIcon.Source = BitmapFrame.Create(new Uri(YourTurnAvatar));

                                    OponentTurnAvatar = @"C:\Users\Bogdan\source\repos\TicTacToeExaminion\TicTacToeExaminion\Images\cross.png";
                                    OponentIcon.Source = BitmapFrame.Create(new Uri(OponentTurnAvatar));
                                }));
                            }
                            else
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    LabelTurn.Content = "Oponent turn";
                                    yourQueue = false;
                                    OponentTurnAvatar = @"C:\Users\Bogdan\source\repos\TicTacToeExaminion\TicTacToeExaminion\Images\circle.png";

                                    OponentIcon.Source = BitmapFrame.Create(new Uri(OponentTurnAvatar));

                                    YourTurnAvatar = @"C:\Users\Bogdan\source\repos\TicTacToeExaminion\TicTacToeExaminion\Images\cross.png";
                                    YourIcon.Source = BitmapFrame.Create(new Uri(YourTurnAvatar));
                                }));
                            }
                            Dispatcher.Invoke(new Action(() =>
                            {
                                LeaveGameBtn.Visibility = Visibility.Visible;
                                ExitAccountBtn.Visibility = Visibility.Hidden;
                            }));
                            stream.Read(buffer, 0, buffer.Length);

                            BinaryFormatter formatter = new BinaryFormatter();
                            using (MemoryStream ms = new MemoryStream(buffer))
                            {
                                oponent = (User)formatter.Deserialize(ms);
                            }
                            Dispatcher.Invoke(new Action(() =>
                            {
                                userNameGameLabelTwo.Content = oponent.Username;
                                userNameGameLabel.Content = user.Username;
                            }));
                            try
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    BitmapImage bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = new MemoryStream(user.PathAvatar);
                                    bitmapImage.EndInit();
                                    AvatarUserGameImage.Source = bitmapImage;
                                }));
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    BitmapImage bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = new MemoryStream(oponent.PathAvatar);
                                    bitmapImage.EndInit();
                                    AvatarUserGameImageTwo.Source = bitmapImage;
                                }));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                            SlowOpacityMainGrid();
                            timer.Start();
                            break;
                        }
                    }
                });
            }
            else
            {
                IsSearchingGame = false;
                labelVsPlayer.Content = "vs Player";
            }
        }
        private void PlayPC_Click(object sender, MouseButtonEventArgs e)
        {
            //LeaveGameBtn.Visibility = Visibility.Visible;
            //ExitAccountBtn.Visibility = Visibility.Hidden;
            //SlowOpacityMainGrid();
        }

        private async void SelectField_Click(object sender, MouseButtonEventArgs e)
        {
            if (yourQueue == true)
            {
                stream = server.GetStream();
                string selectedCell = ((Border)sender).Tag.ToString();
                stream.Write(Encoding.ASCII.GetBytes(selectedCell));
            }
        }
        private void SelectAvatar_Click(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.ShowDialog();
            string pathAvatar = dialog.FileName;
            if (pathAvatar.Length > 1)
            {
                Image myImage = new Image();
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(pathAvatar);
                bitmap.EndInit();
                AvatarUserImage.Source = bitmap;
                TicTacToe.SetAvatar(user.Username, File.ReadAllBytes(pathAvatar));
            }
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                if (server.Available > 0)
                {
                    NetworkStream stream = server.GetStream();
                    byte[] buffer = new byte[1024];
                    stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer).Replace("\0", "");

                    if (response.Contains("Your oponent is exited"))
                    {
                        MessageBox.Show(response);
                        ResetUI();
                        timer.Stop();
                        return;
                    }
                    if (response.Contains("Win") || response.Contains("Tie"))
                    {
                        MessageBox.Show(response, "Result", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUserInfo();
                        ResetUI();
                        timer.Stop();
                        return;
                    }

                    string[] arrResponse = response.Split(" ");
                    if (arrResponse.Length > 1 && arrResponse[0].Contains("Your:"))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LabelTurn.Content = "Oponent turn";
                        }));
                        string YourSelected = arrResponse[1] + " " + arrResponse[2];

                        if (yourQueue == true)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                foreach (var item in GameGridTemp.Children)
                                {
                                    Border border = (Border)item;
                                    if (border.Tag.ToString().Contains(YourSelected))
                                    {
                                        BitmapImage backgroundImage = new BitmapImage(new Uri(YourTurnAvatar));
                                        ImageBrush backgroundBrush = new ImageBrush(backgroundImage);
                                        border.Background = backgroundBrush;
                                    }
                                }
                            }));
                            yourQueue = false;
                        }
                    }
                    if (arrResponse.Length > 1 && arrResponse[0].Contains("Oponent:"))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LabelTurn.Content = "Your turn";
                        }));
                        string OponentSelected = arrResponse[1] + " " + arrResponse[2];

                        if (yourQueue != true)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                foreach (var item in GameGridTemp.Children)
                                {
                                    Border border = (Border)item;
                                    if (border.Tag.ToString().Contains(OponentSelected))
                                    {
                                        BitmapImage backgroundImage = new BitmapImage(new Uri(OponentTurnAvatar));
                                        ImageBrush backgroundBrush = new ImageBrush(backgroundImage);
                                        border.Background = backgroundBrush;
                                    }
                                }
                                yourQueue = true;
                            }));
                        }
                    }
                }
            });
        }

        private async void SlowOpacityGameGrid()
        {
            await Task.Factory.StartNew(() =>
            {
                double opacity = 0;
                for (double i = 1.0; i > 0.0; i -= 0.1)
                {
                    opacity = i;
                    Thread.Sleep(15);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        GameGrid.Opacity = opacity;
                    }));

                }

                for (double i = 0.0; i < 1.1; i += 0.1)
                {
                    opacity = i;
                    Thread.Sleep(15);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainGrid.Opacity = opacity;
                    }));
                }
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    GameGrid.IsEnabled = false;
                    MainGrid.IsEnabled = true;
                    MainGrid.Visibility = Visibility.Visible;
                    GameGrid.Visibility = Visibility.Hidden;
                }));
            });
        }
        private async void SlowOpacityMainGrid()
        {
            await Task.Factory.StartNew(() =>
            {
                double opacity = 0;
                for (double i = 1.0; i > 0.0; i -= 0.1)
                {
                    opacity = i;
                    Thread.Sleep(15);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainGrid.Opacity = opacity;
                    }));

                }

                for (double i = 0.0; i < 1.1; i += 0.1)
                {
                    opacity = i;
                    Thread.Sleep(15);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        GameGrid.Opacity = opacity;
                    }));

                }
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    GameGrid.IsEnabled = true;
                    MainGrid.IsEnabled = false;
                    GameGrid.Visibility = Visibility.Visible;
                    MainGrid.Visibility = Visibility.Hidden;
                }));
            });
        }
        private void ResetUI()
        {
            server.Close();
            Dispatcher.Invoke(new Action(() => {
                labelVsPlayer.Content = "vs Player";
                LeaveGameBtn.Visibility = Visibility.Hidden;
                ExitAccountBtn.Visibility = Visibility.Visible;
                IsSearchingGame = false;
                labelVsPlayer.Content = "vs Player";
                foreach (var item in GameGridTemp.Children)
                {
                    Border border = (Border)item;
                    border.Background = Brushes.CornflowerBlue;

                }
                SlowOpacityGameGrid();
            }));
        }
        private void LoadUserInfo()
        {
            user = TicTacToe.GetUser(user.Username, user.Password);
            Dispatcher.BeginInvoke(new Action(() => {
                UsernameLabel.Content = "Username: " + user.Username;
                WinsLabel.Content = "Wins: " + user.Wins;
                LosesLabel.Content = "Loses: " + user.Loses;
                TiesLabel.Content = "Ties: " + user.Tie;
                if (user.GameHistory.Length > 1)
                {
                    string[] arr = user.GameHistory.Split("*");
                    GameHistoryListBox.Items.Clear();
                    foreach (var item in arr)
                        GameHistoryListBox.Items.Add(item);

                }
                try
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = new MemoryStream(user.PathAvatar);
                    bitmapImage.EndInit();
                    AvatarUserImage.Source = bitmapImage;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }));
        }
    }
}
