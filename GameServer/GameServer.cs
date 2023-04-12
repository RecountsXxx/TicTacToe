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
using System.Linq;
using System.Reflection;

namespace GameServer
{
    public class LobbyModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public TcpClient client { get; set; }
        public List<string> Chooses { get; set; } = new List<string>();
        public LobbyModel(int Id,string Username,TcpClient client)
        {
            this.Id = Id;
            this.Username = Username;
            this.client = client;
        }
    }
    internal class GameServer
    {
        private static int serverPort = 10002;
        private static IPAddress serverAdress = IPAddress.Parse("127.0.0.1");

        private static List<LobbyModel> lobbyModels = new List<LobbyModel>();
        private static string responseOne = string.Empty;
        private static string responseTwo = string.Empty;
        private static bool GameIsEnded = false;
        private static int CounterID = 0;

        static async Task Main(string[] args)
        {

            TcpListener listener = new TcpListener(serverAdress, serverPort);
            listener.Start();
            Console.WriteLine("Game server started on - " + "127.0.0.1" + ":" + serverPort);

            int bufferLenght = 0;
            byte[] buffer = new byte[1024];
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                CounterID++;
                List<string> busyCells = new List<string>();
                int CounterTie = 0;

                NetworkStream nsOne = client.GetStream();
                bufferLenght = nsOne.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bufferLenght);
                Console.WriteLine(response);

                string username = response.Split(" ")[2];
                if (response.Contains("PlayPC"))
                {
                    Thread thread = new Thread(() => HandleClientPC(new LobbyModel(0,username,client),busyCells, CounterTie));
                    thread.Start();
                }
                if (response.Contains("Goodbye"))
                {
                    lobbyModels.Remove(lobbyModels.Where(x => x.Username.Contains(username)).FirstOrDefault());
                    client.Close();
                }
                if (lobbyModels.Where(x => x.Username == username).FirstOrDefault() == null && response.Contains("PlayOnline"))
                    lobbyModels.Add(new LobbyModel(CounterID, username, client));
                if (lobbyModels.Count >= 2)
                {
                    Console.WriteLine("Users finded");
                    Random rd = new Random();
                    int index = rd.Next(1, lobbyModels.Count);
                    Thread thread = new Thread(() => HandleClient(lobbyModels[0], lobbyModels[index],busyCells,CounterTie));
                    thread.Start();

                }
            }

        }
        #region Helpfull func
        public static bool CheckCells(LobbyModel userFirst)
        {
            if (userFirst.Chooses.Contains("0 0") && userFirst.Chooses.Contains("0 1") && userFirst.Chooses.Contains("0 2") ||
                       userFirst.Chooses.Contains("1 0") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("1 2") ||
                       userFirst.Chooses.Contains("2 0") && userFirst.Chooses.Contains("2 1") && userFirst.Chooses.Contains("2 2") ||
                       userFirst.Chooses.Contains("0 0") && userFirst.Chooses.Contains("1 0") && userFirst.Chooses.Contains("2 0") ||
                       userFirst.Chooses.Contains("0 1") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("2 1") ||
                       userFirst.Chooses.Contains("0 2") && userFirst.Chooses.Contains("1 2") && userFirst.Chooses.Contains("2 2") ||
                       userFirst.Chooses.Contains("0 2") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("2 0") ||
                       userFirst.Chooses.Contains("0 0") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("2 2"))
                return true;
            else
                return false;
        }
        public static void UpdateDB(LobbyModel userFirst, LobbyModel userTwo, bool WinOrTie)
        {
            string usernameOne = userFirst.Username.Replace("\0", "");
            string usernameTwo = userTwo.Username.Replace("\0", "");

            if (WinOrTie == true)
            {
                if (userFirst.Username == usernameOne)
                {
                    User user = TicTacToe.GetUser(userFirst.Username);
                    TicTacToe.UpdateUser($"update Users set Wins = {user.Wins += 1} where Username = '{usernameOne}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You win: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");

                    user = TicTacToe.GetUser(usernameTwo);
                    TicTacToe.UpdateUser($"update Users set Loses = {user.Loses += 1} where Username = '{usernameTwo}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You lose: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");
                }

                if (userFirst.Username == usernameTwo)
                {
                    User user = TicTacToe.GetUser(userFirst.Username);
                    TicTacToe.UpdateUser($"update Users set Wins = {user.Wins += 1} where Username = '{usernameTwo}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You win: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");

                    user = user = TicTacToe.GetUser(usernameOne);
                    TicTacToe.UpdateUser($"update Users set Loses = {user.Loses += 1} where Username = '{usernameOne}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"You lose: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");
                }
            }
            else
            {
                if (userFirst.Username == usernameOne)
                {
                    User user = TicTacToe.GetUser(userFirst.Username);
                    TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameOne}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");

                    user = TicTacToe.GetUser(usernameTwo);
                    TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameTwo}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");
                }

                if (userFirst.Username == usernameTwo)
                {
                    User user = TicTacToe.GetUser(userFirst.Username);
                    TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameTwo}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");

                    user = user = TicTacToe.GetUser(usernameOne);
                    TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameOne}'");
                    TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");
                }

            }

        }
        public static async Task<string> MathGame(LobbyModel userFirst, LobbyModel userTwo,int CounterTie)
        {
            string result = "null";
            string username = "null";
            string usernameOne = "null";
            string usernameTwo = "null";
            NetworkStream streamOne = userFirst.client.GetStream();
            NetworkStream streamTwo = userTwo.client.GetStream();
            await Task.Run(() =>
            {
                while (true)
                {
                    username = userFirst.Username.Replace("\0", "");
                    usernameOne = userFirst.Username.Replace("\0", "");
                    usernameTwo = userTwo.Username.Replace("\0", "");

                    if (CheckCells(userFirst))
                    {
                        result = username + " Win";

                        UpdateDB(userFirst, userTwo, true);

                        streamOne.Write(Encoding.ASCII.GetBytes(result));
                        streamTwo.Write(Encoding.ASCII.GetBytes(result));
                        GameIsEnded = true;
                        break;
                    }
                    else if (CounterTie == 9)
                    {
                        result = "Tie";

                        UpdateDB(userFirst, userTwo, false);

                        streamOne.Write(Encoding.ASCII.GetBytes("Tie"));
                        streamTwo.Write(Encoding.ASCII.GetBytes("Tie"));
                        GameIsEnded = true;
                        break;
                    }
                    else
                    {
                        result = userFirst.Username + " Lose";
                        break;

                    }
                }
            });
            return result;
        }
        public static async Task<string> MathGamePC(LobbyModel user, LobbyModel bot, int CounterTie)
        {

            string result = "null";
            NetworkStream stream = user.client.GetStream();
            await Task.Run(() =>
            {
                if (CheckCells(bot))
                {
                    result ="Bot Win";
                    stream.Write(Encoding.ASCII.GetBytes(result));

                }
                if (CheckCells(user))
                {
                    result = user.Username + " Win";
                    stream.Write(Encoding.ASCII.GetBytes(result));

                }
                else if (CounterTie == 9)
                {
                    result = "Tie";
                    stream.Write(Encoding.ASCII.GetBytes(result));

                }
                else
                {
                    result = "Lose";

                }


            });
            return result;
        }
        #endregion

        #region Player game
        public static void HandleOneClient(LobbyModel userFirst, LobbyModel userSecond,List<string> busyCells,int CounterTie)
        {
            try
            {
                while (true)
                {
                    NetworkStream streamOne = userFirst.client.GetStream();
                    NetworkStream streamTwo = userSecond.client.GetStream();

                    byte[] bufferOne = new byte[1024];
                    streamOne.Read(bufferOne, 0, bufferOne.Length);
                    responseOne = Encoding.ASCII.GetString(bufferOne).Replace("\0", "");
                    if (responseOne.Contains("Goodbye"))
                    {
                        userFirst.client.Close();
                        streamTwo.Write(Encoding.ASCII.GetBytes("Your oponent is exited"));
                        userSecond.client.Close();
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
                            userFirst.Chooses.Add(responseOne);

                            CounterTie++;
                            if (userFirst.Chooses.Count >= 3)
                            {
                                Thread.Sleep(300);
                                string report = MathGame(userFirst, userSecond,CounterTie).Result;
                                Console.WriteLine(report);
                                if (report.Contains("Win") || report.Contains("Tie"))
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("One : busy");
                        }
                    }
                }
            }
            catch
            {

            }
        }
        public static void HandleTwoClient(LobbyModel userFirst, LobbyModel userSecond, List<string> busyCells, int CounterTie)
        {
            try
            {
                while (true)
                {
                    NetworkStream streamTwo = userSecond.client.GetStream();
                    NetworkStream streamOne = userFirst.client.GetStream();

                    byte[] bufferOne = new byte[1024];
                    streamTwo.Read(bufferOne, 0, bufferOne.Length);
                    responseTwo = Encoding.ASCII.GetString(bufferOne).Replace("\0", "");
                    if (responseTwo.Contains("Goodbye"))
                    {
                        userSecond.client.Close();
                        streamOne.Write(Encoding.ASCII.GetBytes("Your oponent is exited"));
                        userFirst.client.Close();
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
                            userSecond.Chooses.Add(responseTwo);
                            CounterTie++;
                            if (userSecond.Chooses.Count >= 3)
                            {
                                Thread.Sleep(300);
                                string report = MathGame(userSecond, userFirst, CounterTie).Result;
                                Console.WriteLine(report);
                                if (report.Contains("Win") || report.Contains("Tie"))
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Two : busy");
                        }
                    }
                }
            }
            catch
            {

            }
        }
        private async static void HandleClient(LobbyModel userFirst, LobbyModel userSecond, List<string> busyCells, int CounterTie)
        {
            Console.WriteLine("Game started!");

            lobbyModels.Remove(userFirst);
            lobbyModels.Remove(userSecond);

            User userOne = new User(userFirst.Username);
            User userTwo = new User(userSecond.Username);

            NetworkStream streamOne = userFirst.client.GetStream();
            NetworkStream streamTwo = userSecond.client.GetStream();

            streamOne.Write(Encoding.ASCII.GetBytes("Oponent finded! - Your turn"));
            streamTwo.Write(Encoding.ASCII.GetBytes("Oponent finded! - Oponent turn"));

            using (SqlConnection connection = new SqlConnection(TicTacToe.SqlString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand($"select * from Users where Username = '{userFirst.Username}'", connection);
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
                SqlCommand command = new SqlCommand($"select * from Users where Username = '{userSecond.Username}'", connection);
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
                Thread threadOne = new Thread(() => HandleOneClient(userFirst, userSecond, busyCells, CounterTie));
                threadOne.Start();

                Thread threadTwo = new Thread(() => HandleTwoClient(userFirst, userSecond, busyCells, CounterTie));
                threadTwo.Start();

                while (true)
                {
                    if (GameIsEnded == true)
                    {
                        Thread.Sleep(1000);
                        userFirst.client.Close();
                        userSecond.client.Close();
                        Console.WriteLine("Game Ended!");
                        busyCells = new List<string>();
                        CounterTie = 0;
                        GameIsEnded = false;
                        break;

                    }
                }
                return;
            });
        }
        #endregion

        #region PC game
        private async static void HandleClientPC(LobbyModel user, List<string> busyCells, int CounterTie)
        {
            List<string> botChosses = new List<string>();
            NetworkStream stream = user.client.GetStream();
            stream.Write(Encoding.ASCII.GetBytes("PC finded! Your turn"));

            await Task.Run(() =>
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer).Replace("\0","");

                    if (response.Contains("Goodbye"))
                    {
                        stream.Write(Encoding.ASCII.GetBytes("Your oponent is exited"));
                        user.client.Close();
                        break;
                    }
                    if (!busyCells.Contains(response))
                    {
                        busyCells.Add(response);
                        stream.Write(Encoding.ASCII.GetBytes("Your: " + response));
                        Console.WriteLine("Your: " + response);

                    }
                    else
                    {
                        Console.WriteLine("One : busy");
                    }
                    string sentResponse = "0 0";
                    while (true)
                    {
                        Random rd1 = new Random();
                        Random rd2 = new Random();
                        int one = rd1.Next(0, 3);
                        int two = rd2.Next(0, 3);
                        sentResponse = one + " " + two;
                        if (!busyCells.Contains(sentResponse))
                        {
                            busyCells.Add(sentResponse);
                            Console.WriteLine("Oponent: " + sentResponse);
                            stream.Write(Encoding.ASCII.GetBytes("Oponent: " + sentResponse));
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    user.Chooses.Add(response);
                    botChosses.Add(sentResponse);
                    CounterTie++;

                    if (user.Chooses.Count >= 2 && botChosses.Count >= 2)
                    {
                        Thread.Sleep(300);
                        LobbyModel bot = new LobbyModel(0, "Bot", null) { Chooses = botChosses };
                        string report = MathGamePC(user,bot, CounterTie).Result;
                        Console.WriteLine(report);
                        if (report.Contains("Win") || report.Contains("Tie"))
                        {
                            break;
                        }
                    }
                }  
            });
        }
        #endregion
    }
}
