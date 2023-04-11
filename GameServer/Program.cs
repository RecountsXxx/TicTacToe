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
    internal class Program
    {
        private static int serverPort = 10002;
        private static IPAddress serverAdress = IPAddress.Parse("127.0.0.1");

        private static List<LobbyModel> lobbyModels = new List<LobbyModel>();
        private static List<string> busyCells = new List<string>();
        private static string responseOne = string.Empty;
        private static string responseTwo = string.Empty;
        private static int CounterTie = 0;
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
                NetworkStream nsOne = client.GetStream();
                bufferLenght = nsOne.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bufferLenght);
                string username = response.Split(" ")[2];
                if (response.Contains("Goodbye"))
                {
                    lobbyModels.Remove(lobbyModels.Where(x => x.Username.Contains(username)).FirstOrDefault());
                    client.Close();
                    continue;
                }
                if (lobbyModels.Where(x => x.Username == username).FirstOrDefault() == null && response.Contains("PlayOnline"))
                    lobbyModels.Add(new LobbyModel(CounterID, username, client));
                if (lobbyModels.Count >= 2)
                {
                    Console.WriteLine("Users finded");
                    Random rd = new Random();
                    int index = rd.Next(1, lobbyModels.Count);
                    Thread thread = new Thread(() => HandleClient( lobbyModels[0], lobbyModels[index]));
                    thread.Start();
                    continue;

                }
            }

        }

            //while (true)
            //{

            //    clientOne = listener.AcceptTcpClient();
            //    NetworkStream nsOne = clientOne.GetStream();
            //    Console.WriteLine("Connected one client");
            //    bufferLenght = nsOne.Read(buffer, 0, buffer.Length);
            //    string response = Encoding.ASCII.GetString(buffer,0, bufferLenght);

            //    if (response.Contains("Cancel game"))
            //    {
            //        Console.WriteLine("One client disconected");
            //        clientOne.Close();
            //        continue;
            //    }
            //    else if (response.Contains("PlayOnline"))
            //    {

            //            usernameOne = response.Split(" ")[2];
            //            clientTwo = await listener.AcceptTcpClientAsync();
            //            NetworkStream nsTwo = clientTwo.GetStream();
            //            nsTwo.Read(buffer, 0, buffer.Length);
            //            response = Encoding.ASCII.GetString(buffer);
            //            if (!response.Contains(usernameOne))
            //            {
            //                if (response.Contains("PlayOnline"))
            //                {
            //                    usernameTwo = response.Split(" ")[2];
            //                    Console.WriteLine("Connected two client");


            //Thread thread = new Thread(() => HandleClient(usernameOne, usernameTwo));
            //thread.Start();
            //                }
            //                else
            //                {
            //                    clientTwo.Close();
            //                    return;
            //                }
            //            }
            //            else
            //            {
            //                clientOne.Close();
            //                clientTwo.Close();
            //            }

            //    }
            //}
        
        public static void HandleOneClient(ref LobbyModel userFirst, ref LobbyModel userSecond)
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
                    if (responseOne.Contains("Bye"))
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
                                string report = MathGame(userFirst,userSecond).Result;
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
            catch
            {
         
            }
        }
        public static void HandleTwoClient(ref LobbyModel userFirst, ref LobbyModel userSecond)
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
                    if (responseTwo.Contains("Bye"))
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
                                string report = MathGame(userSecond,userFirst).Result;
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
            catch
            {
     
            }
        }
        private async static void HandleClient(LobbyModel userFirst, LobbyModel userSecond)
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
                Thread threadOne = new Thread(() => HandleOneClient(ref userFirst,ref userSecond));
                threadOne.Start();

                Thread threadTwo = new Thread(() => HandleTwoClient(ref userFirst, ref userSecond));
                threadTwo.Start();

                while (true)
                {
                    if (GameIsEnded == true)
                    {
                        Thread.Sleep(1000);
                        //завершать потоки
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

        public static async Task<string> MathGame(LobbyModel userFirst,LobbyModel userTwo)
        {
            string result = "null";
            string username = "null";
            string usernameOne = "null";
            string usernameTwo = "null";
            NetworkStream streamOne = userFirst.client.GetStream();
            NetworkStream streamTwo = userTwo.client.GetStream();
            await Task.Run(() => {
                while (true)
                {
                    for (int i = 0; i < userFirst.Chooses.Count; i++)
                    {
                        if (userFirst.Chooses.Contains("0 0") && userFirst.Chooses.Contains("0 1") && userFirst.Chooses.Contains("0 2") ||
                        userFirst.Chooses.Contains("1 0") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("1 2") ||
                        userFirst.Chooses.Contains("2 0") && userFirst.Chooses.Contains("2 1") && userFirst.Chooses.Contains("2 2") ||
                        userFirst.Chooses.Contains("0 0") && userFirst.Chooses.Contains("1 0") && userFirst.Chooses.Contains("2 0") ||
                        userFirst.Chooses.Contains("0 1") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("2 1") ||
                        userFirst.Chooses.Contains("0 2") && userFirst.Chooses.Contains("1 2") && userFirst.Chooses.Contains("2 2") ||
                        userFirst.Chooses.Contains("0 2") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("2 0") ||
                        userFirst.Chooses.Contains("0 0") && userFirst.Chooses.Contains("1 1") && userFirst.Chooses.Contains("2 2"))
                        {
                             username = userFirst.Username.Replace("\0", "");
                             usernameOne = userFirst.Username.Replace("\0", "");
                             usernameTwo = userTwo.Username.Replace("\0", "");
                            result = username + " Win";
                            Console.WriteLine(username + " | " + usernameOne + " | " + usernameTwo);
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
                            username = userFirst.Username.Replace("\0", "");
                            usernameOne = userFirst.Username.Replace("\0", "");
                            usernameTwo = userTwo.Username.Replace("\0", "");
                            result = "Tie";

                            if (username == usernameOne)
                            {
                                User user = TicTacToe.GetUser(username);
                                TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameOne}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");

                                user = TicTacToe.GetUser(usernameTwo);
                                TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameTwo}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");
                            }

                            if (username == usernameTwo)
                            {
                                User user = TicTacToe.GetUser(username);
                                TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameTwo}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameOne + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameTwo}'");

                                user = user = TicTacToe.GetUser(usernameOne);
                                TicTacToe.UpdateUser($"update Users set Tie = {user.Tie += 1} where Username = '{usernameOne}'");
                                TicTacToe.UpdateUser($"update Users set GameHistory = '{user.GameHistory += $"Tie: " + usernameTwo + " | " + DateTime.Now.ToShortTimeString() + " - " + DateTime.Now.ToShortDateString() + " *"}' where Username = '{usernameOne}'");
                            }

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
                    return result;
                }
            });
            return result;
        }
    }
}
