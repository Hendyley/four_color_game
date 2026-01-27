using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FourColors
{
    public class StoreItem
    {
        public bool Equip { get; set; }
        public bool Purchase { get; set; }
    }

    public class SaveData
    {
        public int Points { get; set; }
        public Dictionary<string, StoreItem> BG { get; set; } = new();
        public Dictionary<string, StoreItem> Tile { get; set; } = new();
        public Dictionary<string, StoreItem> Music { get; set; } = new();
        public Dictionary<string, string> CommonSettings { get; set; } = new();

        public SaveData()
        {
            Points = 0;
            BG = new();
            Tile = new();
            Music = new();
            CommonSettings = new();
        }

    }



}
