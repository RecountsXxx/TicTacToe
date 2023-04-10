using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TicTacToeLiblary;
using System.Data.SqlClient;

namespace GameServer
{
    public class GameModel
    {
        public string Username { get; set; }
        public List<string> Chooses { get; set; } = new List<string>();
        public GameModel(string Username)
        {
            this.Username = Username;
        }
    }
    internal class Program
    {
        private static int serverPort = 10002;
        private static IPAddress serverAdress = IPAddress.Parse("127.0.0.1");
        private static TcpClient clientOne;
        private static TcpClient clientTwo;
      

        private static List<string> busyCells = new List<string>();
        private static string responseOne = string.Empty;
        private static string responseTwo = string.Empty;
        private static int CounterTie = 0;
        private static bool GameIsEnded = false;

        private static string usernameOne = string.Empty;
        private static string usernameTwo = string.Empty;

        private static List<GameModel> gameList = new List<GameModel>();
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(serverAdress, serverPort);
            listener.Start();
            Console.WriteLine("Game server started on - " + "127.0.0.1" + ":" + serverPort);

            while (true)
            {
                clientOne = listener.AcceptTcpClient();
                NetworkStream nsOne = clientOne.GetStream();
                Console.WriteLine("Connected one client");
                byte[] buffer = new byte[8168];
                nsOne.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer);

                if (response.Contains("PlayOnline"))
                {
                    usernameOne = response.Split(" ")[2];
                    clientTwo = listener.AcceptTcpClient();
                    NetworkStream nsTwo = clientTwo.GetStream();
                    nsTwo.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer);
                    if (!response.Contains(usernameOne))
                    {
                        if (response.Contains("PlayOnline"))
                        {
                            usernameTwo = response.Split(" ")[2];
                            Console.WriteLine("Connected two client");


                            Thread thread = new Thread(() => HandleClient(usernameOne, usernameTwo));
                            thread.Start();
                        }
                        else
                        {
                            clientTwo.Close();
                            return;
                        }
                    }
                    else
                    {
                        clientOne.Close();
                        clientTwo.Close();
                    }
                }
            }
        }
        public static void HandleOneClient(User userOne, User userTwo)
        {
            try
            {
                GameModel gameModel = new GameModel(userOne.Username);

                NetworkStream streamOne = clientOne.GetStream();
                NetworkStream streamTwo = clientTwo.GetStream();
                while (true)
                {
                    byte[] bufferOne = new byte[1024];
                    streamOne.Read(bufferOne, 0, bufferOne.Length);
                    responseOne = Encoding.ASCII.GetString(bufferOne).Replace("\0", "");
                    if (responseOne.Contains("Bye"))
                    {
                        clientOne.Close();
                        streamTwo.Write(Encoding.ASCII.GetBytes("Your oponent is exited"));
                        GameIsEnded = true;
                        break;
                    }
                    if (responseOne.Length > 0 && responseOne.Length < 4)
                    {
                        if (!busyCells.Contains(responseOne))
                        {
                            busyCells.Add(responseOne);
                            streamOne.Write(Encoding.ASCII.GetBytes("Your: " + responseOne));
                            streamTwo.Write(Encoding.ASCII.GetBytes("Oponent: " + responseOne));
                            gameModel.Chooses.Add(responseOne);

                            CounterTie++;
                            if (gameModel.Chooses.Count >= 3)
                            {
                                Thread.Sleep(100);
                                string report = MathGame(gameModel).Result;
                                Console.WriteLine(report);
                            }
                        }
                        else
                        {
                            Console.WriteLine("One : busy");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void HandleTwoClient(User userOne, User userTwo)
        {
            try
            {
                GameModel gameModel = new GameModel(userTwo.Username);
                NetworkStream streamTwo = clientTwo.GetStream();
                NetworkStream streamOne = clientOne.GetStream();
                while (true)
                {
                    byte[] bufferOne = new byte[1024];
                    streamTwo.Read(bufferOne, 0, bufferOne.Length);
                    responseTwo = Encoding.ASCII.GetString(bufferOne).Replace("\0", "");
                    if (responseTwo.Contains("Bye"))
                    {
                        clientTwo.Close();
                        streamOne.Write(Encoding.ASCII.GetBytes("Your oponent is exited"));
                        GameIsEnded = true;
                        break;
                    }
                    if (responseTwo.Length > 0 && responseTwo.Length < 4)
                    {
                        if (!busyCells.Contains(responseTwo))
                        {
                            busyCells.Add(responseTwo);
                            streamTwo.Write(Encoding.ASCII.GetBytes("Your: " + responseTwo));
                            streamOne.Write(Encoding.ASCII.GetBytes("Oponent: " + responseTwo));
                            gameModel.Chooses.Add(responseTwo);
                            CounterTie++;
                            if (gameModel.Chooses.Count >= 3)
                            {
                                Thread.Sleep(100);
                                string report = MathGame(gameModel).Result;
                                Console.WriteLine(report);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Two : busy");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async static void HandleClient(string usernameOne, string usernameTwo)
        {
            User userOne = new User(usernameOne);
            User userTwo = new User(usernameTwo);

            NetworkStream streamOne = clientOne.GetStream();
            NetworkStream streamTwo = clientTwo.GetStream();

            streamOne.Write(Encoding.ASCII.GetBytes("Oponent finded! - Your turn"));
            streamTwo.Write(Encoding.ASCII.GetBytes("Oponent finded! - Oponent turn"));

            using (SqlConnection connection = new SqlConnection(TicTacToe.SqlString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand($"select * from Users where Username = '{usernameOne}'", connection);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {

                        string username = reader.GetString(1);
                        if (!reader.IsDBNull(7))
                            userOne.PathAvatar = (byte[])reader.GetSqlBinary(7);
                    }
                }
            }
            using (SqlConnection connection = new SqlConnection(TicTacToe.SqlString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand($"select * from Users where Username = '{usernameTwo}'", connection);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string username = reader.GetString(1);
                        if (!reader.IsDBNull(7))
                            userTwo.PathAvatar = (byte[])reader.GetSqlBinary(7);

                    }
                }
            }

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, userTwo);
                streamOne.Write(ms.ToArray());


            }
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, userOne);
                streamTwo.Write(ms.ToArray());
            }

            await Task.Run(() =>
            {
                Thread threadOne = new Thread(() => HandleOneClient(userOne, userTwo));
                threadOne.Start();

                Thread threadTwo = new Thread(() => HandleTwoClient(userOne, userTwo));
                threadTwo.Start();

                while (true)
                {
                    if (GameIsEnded == true)
                    {
                        Task.Delay(1000);
                        clientOne.Close();
                        clientTwo.Close();
                        threadOne.Interrupt();
                        threadTwo.Interrupt();
                        Console.WriteLine("Game Ended!");
                        busyCells = new List<string>();
                        CounterTie = 0;
                        Thread.CurrentThread.Interrupt();
                        GameIsEnded = false;
                        break;

                    }
                }
                return;
            });
        }


        public static async Task<string> MathGame(GameModel gameModel)
        {
            string result = "null";
            NetworkStream streamOne = clientOne.GetStream();
            NetworkStream streamTwo = clientTwo.GetStream();
            await Task.Run(() => {
                while (true)
                {
                    for (int i = 0; i < gameModel.Chooses.Count; i++)
                    {
                        if (gameModel.Chooses.Contains("0 0") && gameModel.Chooses.Contains("0 1") && gameModel.Chooses.Contains("0 2") ||
                        gameModel.Chooses.Contains("1 0") && gameModel.Chooses.Contains("1 1") && gameModel.Chooses.Contains("1 2") ||
                        gameModel.Chooses.Contains("2 0") && gameModel.Chooses.Contains("2 1") && gameModel.Chooses.Contains("2 2") ||
                        gameModel.Chooses.Contains("0 0") && gameModel.Chooses.Contains("1 0") && gameModel.Chooses.Contains("2 0") ||
                        gameModel.Chooses.Contains("0 1") && gameModel.Chooses.Contains("1 1") && gameModel.Chooses.Contains("2 1") ||
                        gameModel.Chooses.Contains("0 2") && gameModel.Chooses.Contains("1 2") && gameModel.Chooses.Contains("2 2") ||
                        gameModel.Chooses.Contains("0 2") && gameModel.Chooses.Contains("1 1") && gameModel.Chooses.Contains("2 0") ||
                        gameModel.Chooses.Contains("0 0") && gameModel.Chooses.Contains("1 1") && gameModel.Chooses.Contains("2 2"))
                        {
                            string username = gameModel.Username.Replace("\0", "");
                            usernameOne = usernameOne.Replace("\0", "");
                            usernameTwo = usernameTwo.Replace("\0", "");
                            result = username + " Win";

                            if (username == usernameOne)
                            {
                                User user = TicTacToe.GetUser(username);
                                TicTacToe.UpdateUser($"update Users set Wins = {user.Wins += 1} where Username = '{usernameOne}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You win: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");

                                user = TicTacToe.GetUser(usernameTwo);
                                TicTacToe.UpdateUser($"update Users set Loses = {user.Loses += 1} where Username = '{usernameTwo}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You lose: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");
                            }

                            if (username == usernameTwo)
                            {
                                User user = TicTacToe.GetUser(username);
                                TicTacToe.UpdateUser($"update Users set Wins = {user.Wins += 1} where Username = '{usernameTwo}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You win: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");

                                user = user = TicTacToe.GetUser(usernameOne);
                                TicTacToe.UpdateUser($"update Users set Loses = {user.Loses += 1} where Username = '{usernameOne}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You lose: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");
                            }

                            streamOne.Write(Encoding.ASCII.GetBytes(result));
                            streamTwo.Write(Encoding.ASCII.GetBytes(result));

                            GameIsEnded = true;
                            break;
                        }
                        else if (CounterTie == 9)
                        {
                            result = "Tie";
                            streamOne.Write(Encoding.ASCII.GetBytes("Tie"));
                            streamTwo.Write(Encoding.ASCII.GetBytes("Tie"));
                            GameIsEnded = true;
                            break;
                        }
                        else
                        {
                            result = gameModel.Username + " Lose";
                            break;

                        }
                    }
                    return result;
                }
            });
            return result;
        }
    }
}
