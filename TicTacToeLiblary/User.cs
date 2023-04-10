using System;

namespace TicTacToeLiblary
{
    [Serializable]
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string GameHistory { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
        public int Tie { get; set; }
        public byte[] PathAvatar { get; set; }
        public User(int Id, string Username, string Password, string GameHistory, int Wins, int Loses, int Tie, byte[] PathAvatar)
        {

            this.Username = Username;
            this.Password = Password;
            this.GameHistory = GameHistory;
            this.Wins = Wins;
            this.Loses = Loses;
            this.Tie = Tie;
            this.PathAvatar = PathAvatar;
        }
        public User(string username) { this.Username = username; }
    }
}
