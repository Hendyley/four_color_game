using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FourColors
{
    internal class MatchRoom
    {
        public string RoomID;
        public int MaxPlayer;

        public MatchRoom(int MaxPlayer)
        {
            this.RoomID = GenerateRoomId();
            this.MaxPlayer = MaxPlayer;
        }
        private string GenerateRoomId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random rand = new Random();
            char[] id = new char[6];
            for (int i = 0; i < id.Length; i++)
                id[i] = chars[rand.Next(chars.Length)];
            return new string(id);
        }

    }



}
