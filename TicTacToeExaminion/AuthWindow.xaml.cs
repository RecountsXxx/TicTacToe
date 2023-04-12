using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using TicTacToeLiblary;

namespace TicTacToeExaminion
{
    /// <summary>
    /// Логика взаимодействия для AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        private User user = null;
        private DispatcherTimer timer = new DispatcherTimer();

        public AuthWindow()
        {
            InitializeComponent();
            SetImage("Images/back.png",AuthGrid);

            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
        }

        #region Timer and SetImageForUIElement
        private void Timer_Tick(object? sender, EventArgs e)
        {
            ReportLabel.Opacity = 0;
            timer.Stop();
        }
        public static void SetImage(string imagePath,UIElement UI)
        {
            string finishPath = System.IO.Path.GetFullPath(imagePath);
            if (UI is Grid grid)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(File.ReadAllBytes(finishPath));
                bitmapImage.EndInit();
                grid.Background = new ImageBrush(bitmapImage);
                ((ImageBrush)grid.Background).Stretch = Stretch.UniformToFill;
            }
            if (UI is Image image)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(File.ReadAllBytes(finishPath));
                bitmapImage.EndInit();
                image.Source = bitmapImage;
            }
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
            Environment.Exit(0);
        }
        private void Window_DragDrop(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        #endregion

        #region Register and Login
        private void RegisterBtn_Click(object sender, MouseButtonEventArgs e)
        {
            string username = registerUsernameText.Text;
            string password = registerPasswordText.Text;
            if (username.Length >= 3 && password.Length >= 3)
            {
                string result = TicTacToeLiblary.TicTacToe.RegisterUser(username, password);
                if (result.Contains("Registration succefull!"))
                {
                    ReportLabel.Opacity = 1;
                    ReportLabel.Foreground = Brushes.GreenYellow;
                    ReportLabel.Content = "Registration succefull!";

                    loginGrid.Visibility = Visibility.Visible;
                    registerGrid.Visibility = Visibility.Hidden;
                    timer.Start();
                }
                else
                {
                    ReportLabel.Opacity = 1;
                    ReportLabel.Foreground = Brushes.OrangeRed;
                    ReportLabel.Content = "Username is busy";
                    timer.Start();
                }
            }
            else
            {
                ReportLabel.Opacity = 1;
                ReportLabel.Foreground = Brushes.OrangeRed;
                ReportLabel.Content = "Enter other field!";
                timer.Start();
            }

        }
        private void LoginBtn_Click(object sender, MouseButtonEventArgs e)
        {
            string username = loginUsernameText.Text;
            string password = loginPasswordText.Text;
            if (username.Length >= 3 && password.Length >= 3)
            {
                user = TicTacToeLiblary.TicTacToe.GetUser(username, password);
                if (user.Username != "null")
                {
                    MainWindow window = new MainWindow(user);
                    window.Show();
                    this.Close();
                }
                else
                {
                    ReportLabel.Opacity = 1;
                    ReportLabel.Foreground = Brushes.OrangeRed;
                    ReportLabel.Content = "Invalid username or login";
                    timer.Start();
                }
            }
            else
            {
                ReportLabel.Opacity = 1;
                ReportLabel.Foreground = Brushes.OrangeRed;
                ReportLabel.Content = "Enter other field!";
                timer.Start();
            }
        }
        private void LoginPage_Click(object sender, MouseButtonEventArgs e)
        {
            loginGrid.Visibility = Visibility.Visible;
            registerGrid.Visibility = Visibility.Hidden;
        }
        private void RegisterPageBtn_Click(object sender, MouseButtonEventArgs e)
        {
            loginGrid.Visibility = Visibility.Hidden;
            registerGrid.Visibility = Visibility.Visible;
        }
        #endregion
    }
}
