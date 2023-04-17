using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TicTacToeLiblary;
using System.Data.SqlClient;
using System.Data;
using System.Data.SqlTypes;

namespace AuthServer
{
    internal class AuthServer
    {
        public static int serverPort = 10001;
        public static IPAddress serverAdress = IPAddress.Parse("127.0.0.1");
         
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(serverAdress, serverPort);
            listener.Start();
            Console.WriteLine("Auth server started on - " + "127.0.0.1" + ":" + serverPort);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream ns = client.GetStream();
                byte[] buffer = new byte[8168];
                ns.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer);

                Thread thread = new Thread(() => HandleClient(client, response));
                thread.Start();
            }
        }

        static void HandleClient(TcpClient client, string response)
        {
            NetworkStream ns = client.GetStream();
            if (response.Contains("Login - "))
            {
                string[] arr = response.Split(" ");
                string username = arr[2];
                string password = arr[6].Replace("\0", "");
                ns.Write(Login(username, password).Result);

            }
            if (response.Contains("Register - "))
            {
                string[] arr = response.Split(" ");
                string username = arr[2];
                string password = arr[6].Replace("\0", "");
                ns.Write(Encoding.ASCII.GetBytes(Register(username, password).Result.ToString()));
            }
            if (response.Contains("Get user - "))
            {
                string[] arr = response.Split(" ");
                string username = arr[3];
                ns.Write(GetUser(username).Result);
            }
            if (response.Contains("Set avatar - "))
            {
                string[] arr = response.Split(" ");
                string username = arr[3];

                byte[] buffer = new byte[8168];
                ns.Read(buffer, 0, buffer.Length);

                SetAvatar(username, buffer);
            }
        }
        static async Task<byte[]> GetUser(string username)
        {
            User user = new User("null");
            await Task.Run(() =>
            {
                using (SqlConnection connection = new SqlConnection(TicTacToeLiblary.TicTacToe.SqlString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand($"select * from Users where Username = '{username}'", connection);
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string gameHistory = string.Empty;
                            byte[] pathAvatar = new byte[8168];
                            int wins = 0;
                            int loses = 0;
                            int tie = 0;
                            int id = reader.GetInt32(0);
                            string username = reader.GetString(1);
                            string password = reader.GetString(2);
                            if (!reader.IsDBNull(7))
                                pathAvatar = (byte[])reader.GetSqlBinary(7);
                            if (!reader.IsDBNull(3))
                                gameHistory = reader.GetString(3);
                            if (!reader.IsDBNull(4))
                                wins = reader.GetInt32(4);
                            if (!reader.IsDBNull(5))
                                loses = reader.GetInt32(5);
                            if (!reader.IsDBNull(6))
                                tie = reader.GetInt32(6);
                            user = new User(id, username, password, gameHistory, wins, loses, tie, pathAvatar);

                        }
                    }
                }
            });
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, user);
                return ms.ToArray();
            }
        }
        static async Task<byte[]> Login(string username, string password)
        {
            User user = new User("null");
            await Task.Run(() =>
            {
                using (SqlConnection connection = new SqlConnection(TicTacToeLiblary.TicTacToe.SqlString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand($"select * from Users where Username = '{username}' and Password = '{password}'", connection);
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string gameHistory = string.Empty;
                            byte[] pathAvatar = new byte[8168];
                            int wins = 0;
                            int loses = 0;
                            int tie = 0;
                            int id = reader.GetInt32(0);
                            string username = reader.GetString(1);
                            string password = reader.GetString(2);
                            if (!reader.IsDBNull(7))
                                pathAvatar = (byte[])reader.GetSqlBinary(7);
                            if (!reader.IsDBNull(3))
                                gameHistory = reader.GetString(3);
                            if (!reader.IsDBNull(4))
                                wins = reader.GetInt32(4);
                            if (!reader.IsDBNull(5))
                                loses = reader.GetInt32(5);
                            if (!reader.IsDBNull(6))
                                tie = reader.GetInt32(6);
                            user = new User(id, username, password, gameHistory, wins, loses, tie, pathAvatar);

                        }
                    }
                }
            });
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, user);
                return ms.ToArray();
            }
        }
        static async Task<string> Register(string username, string password)
        {
            string result = "Erorr";
            await Task.Run(() =>
            {
                using (SqlConnection connection = new SqlConnection(TicTacToeLiblary.TicTacToe.SqlString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand($"IF NOT EXISTS (SELECT * FROM Users WHERE Username = '{username}') insert into Users (Username,Password) values ('{username}','{password}')", connection);
                    if (command.ExecuteNonQuery() > 0)
                    {
                        result = "Registration succefull!";
                        SetAvatar(username, File.ReadAllBytes("C:\\Users\\Bogdan\\source\\repos\\TicTacToeExaminion\\TicTacToeExaminion\\Images\\userLogo.png")); ;
                    }
                    else
                        result = "User is contains, change username";

                  
                }
            });
            return result;
        }
        static async Task SetAvatar(string username, byte[] image_bytes)
        {
            await Task.Run(() =>
            {
                using (SqlConnection connection = new SqlConnection(TicTacToeLiblary.TicTacToe.SqlString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand($"update users set PathAvatar = (@ImageData) where Username = '{username}'", connection);
                    command.Parameters.Add("@ImageData", SqlDbType.Image, 1000000);
                    command.Parameters["@ImageData"].Value = image_bytes;
                    command.ExecuteNonQuery();


                }
            });
        }

    }
}
