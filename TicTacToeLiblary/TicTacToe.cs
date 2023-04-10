using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TicTacToeLiblary
{
    public static class TicTacToe { 
        public static string SqlString = "Data Source=DESKTOP-PGENPK7\\SQLEXPRESS;Initial Catalog=TicTacToeDB;Integrated Security=True;TrustServerCertificate=True";

        public static string RegisterUser(string username, string password)
        {
            string result = string.Empty;
            TcpClient tcpClient = new TcpClient("127.0.0.1", 10001);
            NetworkStream stream = tcpClient.GetStream();
            stream.Write(Encoding.UTF8.GetBytes("Register - " + username + " - Password - " + password));
            byte[] buffer = new byte[1024];
            stream.Read(buffer, 0, buffer.Length);
            result = Encoding.UTF8.GetString(buffer);
            return result;
        }
        public static User GetUser(string username, string password)
        {
            User user = null;
            TcpClient tcpClient = new TcpClient("127.0.0.1", 10001);
            NetworkStream stream = tcpClient.GetStream();
            BinaryFormatter formatter = new BinaryFormatter();
            stream.Write(Encoding.ASCII.GetBytes("Login - " + username + " - Password - " + password));
            byte[] buffer = new byte[9000];
            stream.Read(buffer, 0, buffer.Length);

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                user = (User)formatter.Deserialize(ms);
            }
            return user;
        }
        public static void SetAvatar(string username, byte[] bytes)
        {

            TcpClient tcpClient = new TcpClient("127.0.0.1", 10001);
            NetworkStream stream = tcpClient.GetStream();
            stream.Write(Encoding.ASCII.GetBytes("Set avatar - " + username));
            Thread.Sleep(1500);
            stream.Write(bytes);

        }
        public static User GetUser(string username)
        {
            User user = null;
            TcpClient tcpClient = new TcpClient("127.0.0.1", 10001);
            NetworkStream stream = tcpClient.GetStream();
            BinaryFormatter formatter = new BinaryFormatter();
            stream.Write(Encoding.ASCII.GetBytes("Get user - " + username));
            byte[] buffer = new byte[9000];
            stream.Read(buffer, 0, buffer.Length);


            using (MemoryStream ms = new MemoryStream(buffer))
            {
                user = (User)formatter.Deserialize(ms);
            }
            return user;
        }
        public static void UpdateUser(string commandSql)
        {
            using (SqlConnection connection = new SqlConnection(SqlString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(commandSql, connection);
                command.ExecuteNonQuery();


            }
        }

    }
}
