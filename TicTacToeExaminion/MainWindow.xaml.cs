using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
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
    public partial class MainWindow : Window
    {
        //убрать фиксированные пути, исправить вывод победителя, исправить
        //нажатие подключиться много раз, когда много раз нажимаешь vs Player исправить это

        private TcpClient server = new TcpClient();
        private NetworkStream stream = null;
        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerPC = new DispatcherTimer();
        private User user = null;
        private CancellationTokenSource cancelation = null;

        private bool yourQueue = true;
        private bool IsSearchingGame = false;
        private string YourTurnAvatar = "Images/circle.png";
        private string OponentTurnAvatar = "Images/cross.png";

        public MainWindow(User user)
        {
  
            InitializeComponent();
            this.user = user;

            AuthWindow.SetImage("Images/backGame.png", MainGameGrid);
            
            LoadUserInfo();

            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += PlayerTimer;

            timerPC.Interval = TimeSpan.FromMilliseconds(250);
            timerPC.Tick += TimerPC;
        }

        #region Play player or PC
        private void SetAvatarAndUserNameForGame(User user, Image image,Label label)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(user.PathAvatar);
                bitmapImage.EndInit();
                image.Source = bitmapImage;
                label.Content = user.Username;
            }));
        }
        private void SetIcons(string arrResponse)
        {
            if (arrResponse.Contains("Your"))
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    LabelTurn.Content = "Your turn";
                    yourQueue = true;

                    YourTurnAvatar = System.IO.Path.GetFullPath("Images/circle.png");
                    YourIcon.Source = BitmapFrame.Create(new Uri(YourTurnAvatar));

                    OponentTurnAvatar = System.IO.Path.GetFullPath("Images/cross.png");
                    OponentIcon.Source = BitmapFrame.Create(new Uri(OponentTurnAvatar));
                }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    LabelTurn.Content = "Oponent turn";
                    yourQueue = false;

                    YourTurnAvatar = System.IO.Path.GetFullPath("Images/cross.png");
                    YourIcon.Source = BitmapFrame.Create(new Uri(YourTurnAvatar));

                    OponentTurnAvatar = System.IO.Path.GetFullPath("Images/circle.png");
                    OponentIcon.Source = BitmapFrame.Create(new Uri(OponentTurnAvatar));
                }));
            }
            Dispatcher.Invoke(new Action(() =>
            {
                LeaveGameBtn.Visibility = Visibility.Visible;
                ExitAccountBtn.Visibility = Visibility.Hidden;
            }));
        }

        private async void PlayPlayer_Click(object sender, MouseButtonEventArgs e)
        {
            server = new TcpClient();
            server.Connect("127.0.0.1", 10002);
            stream = server.GetStream();
            if (IsSearchingGame == false)
            {
                IsSearchingGame = true;
                labelVsPlayer.Content = "Stop";
                User oponent = null;

                cancelation = new CancellationTokenSource();
                stream.Write(Encoding.ASCII.GetBytes("PlayOnline | " + user.Username));
                await Task.Run(async () =>
                {
                    try
                    {
                        if (cancelation.IsCancellationRequested)
                            return;
                        byte[] buffer = new byte[18184];
                        await stream.ReadAsync(buffer, 0, buffer.Length, cancelation.Token);
                        string response = Encoding.ASCII.GetString(buffer);
                        if (response.Contains("Oponent finded!"))
                        {
                            stream.Read(buffer, 0, buffer.Length);
                            BinaryFormatter formatter = new BinaryFormatter();
                            MemoryStream ms = new MemoryStream(buffer);
                            oponent = (User)formatter.Deserialize(ms);
                            ms.Dispose();

                            SetIcons(response);
                            SetAvatarAndUserNameForGame(user, AvatarUserGameImage,userNameGameLabel);
                            SetAvatarAndUserNameForGame(oponent, AvatarUserGameImageTwo,userNameGameLabelTwo);

                            IsSearchingGame = false;
                            SlowOpacityMainGrid();
                            timer.Start();
                        }
                    }
                    catch
                    {
                        if (server.Connected == true)
                        {
                            IsSearchingGame = false;
                            stream.Write(Encoding.ASCII.GetBytes("Goodbye | " + user.Username));
                            cancelation.Cancel();
                        }
                    }
                }, cancelation.Token);
            }
            else
            {
                IsSearchingGame = false;
                labelVsPlayer.Content = "vs Player";
                stream.Write(Encoding.ASCII.GetBytes("Goodbye | " + user.Username));
                cancelation.Cancel();
                server.Close();

            }
        }
        private async void PlayPC_Click(object sender, MouseButtonEventArgs e)
        {
            server = new TcpClient();
            server.Connect("127.0.0.1", 10002);
            stream = server.GetStream();
            stream.Write(Encoding.ASCII.GetBytes("PlayPC | " + user.Username));
            await Task.Run(() => {
                byte[] buffer = new byte[1024];
                stream.Read(buffer,0,buffer.Length);
                string response = Encoding.ASCII.GetString(buffer);
                if(response.Contains("PC finded!"))
                {
                    MessageBox.Show("PC Finded, game starting!", "", MessageBoxButton.OK, MessageBoxImage.Question);
                    SetIcons(response);
                    SetAvatarAndUserNameForGame(user, AvatarUserGameImage, userNameGameLabel);
                    SetAvatarAndUserNameForGame(new User("Bot") { PathAvatar = File.ReadAllBytes(System.IO.Path.GetFullPath("Images/userLogo.png")) }, AvatarUserGameImageTwo, userNameGameLabelTwo);

                    Dispatcher.Invoke(new Action(() => {
                        SlowOpacityMainGrid();
                        LeaveGameBtn.Visibility = Visibility.Visible;
                        ExitAccountBtn.Visibility = Visibility.Hidden;
                    }));
                    timerPC.Start();
                }
            });
        }
        #endregion

        #region Select field, select avatar, leave game
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
        private void LeaveGame_Click(object sender, MouseButtonEventArgs e)
        {

            labelVsPlayer.Content = "vs Player";
            labelVsPlayer.IsEnabled = true;
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
            stream.Write(Encoding.ASCII.GetBytes("Goodbye"));

        }
        #endregion

        #region Timers
        private async void PlayerTimer(object? sender, EventArgs e)
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
 
                        MessageBox.Show(response, "", MessageBoxButton.OK, MessageBoxImage.Question);
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
        private async void TimerPC(object? sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                if (server.Available > 0)
                {
                    NetworkStream stream = server.GetStream();
                    byte[] buffer = new byte[1024];
                    stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer).Replace("\0", "");
                    if (response.Contains("Win") || response.Contains("Tie"))
                    {

                        MessageBox.Show(response, "Result", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUserInfo();
                        ResetUI();
                        timerPC.Stop();
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
        #endregion

        #region SlowOpacity
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
        #endregion

        #region Reset UI, LoadUserInfo
        private void ResetUI()
        {
            server.Close();
            Dispatcher.Invoke(new Action(() =>
            {
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
            Dispatcher.BeginInvoke(new Action(() =>
            {
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
        #endregion

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
            this.Close();
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
            if (IsSearchingGame == true)
            {
                MessageBox.Show("Сперва отключитесь");

            }
            else
            {

                AuthWindow window = new AuthWindow();
                window.Show();
                this.Close();
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (server.Connected == true)
            {
                if (IsSearchingGame == true)
                {
                    server = new TcpClient();
                    server.Connect("127.0.0.1", 10002);
                    stream = server.GetStream();
                    stream.Write(Encoding.ASCII.GetBytes("Goodbye | " + user.Username));
                    server.Close();
                }
                else
                {
                    stream.Write(Encoding.ASCII.GetBytes("Goodbye"));
                    server.Close();
                }
            }
        }
        #endregion

    }
}
