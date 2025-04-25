using Godot;
using System;
using System.Text;
using System.Security.Cryptography;
using System.Reflection.Metadata;

namespace FourColors
{
	internal class Player
	{
		public string player_name { get; set; }
		private int player_age { get; set; }
		public string player_id {get; set;} //Set for nakama Device ID

		public bool ishost { get; set; }

		public const string Collection = "PlayerInfo";
		public string Key { get; set; } = "PlayerKey";


		public Godot.Node player_scene {get; set;}


		public Player(string player_name, bool ishost){
			this.player_name = player_name;
			this.player_id = player_name;//GenerateRandomString(6);
			this.ishost = ishost;

			this.Key = player_name;
			GD.Print($"Player name {player_name} / {player_id} Joined");
		}

		private static string GenerateRandomString(int length)
		{
			if (length < 6 || length > 128)
				throw new ArgumentOutOfRangeException("Length must be between 6 and 128");

			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			StringBuilder result = new StringBuilder(length);
			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
			{
				byte[] buffer = new byte[sizeof(uint)];
				while (result.Length < length)
				{
					rng.GetBytes(buffer);
					uint num = BitConverter.ToUInt32(buffer, 0);
					result.Append(chars[(int)(num % (uint)chars.Length)]);
				}
			}

			return result.ToString();
		}

		public string GetCollectionName()
		{
			return Collection;
		}

	}
}
