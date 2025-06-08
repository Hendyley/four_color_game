using Godot;
using System;
using Nakama;
using System.Text;
using System.Collections.Generic;
using FourColors;
using Nakama.TinyJson;
using System.Linq;

public partial class NakamaClient : Node2D
{
	// Called when the node enters the scene tree for the first time.

	private static Client client;
	private static ISession session;
	private static ISocket socket;
	private static IMatch match;
	private static NakamaClient Client;

	public static Dictionary<string, PlayerInfo> Players = new();

	[Signal]
	public delegate void PlayerDataSyncEventHandler(string data);
	[Signal]
	public delegate void PlayerJoinGameEventHandler(string data);

	public override void _Ready()
	{
		if (Client == null)
		{
			Client = this;
		}
		else
		{
			QueueFree();
			return;
		}
		readyAsync();
	}

	private async void readyAsync()
	{
		client = new Client("http", "127.0.0.1", 7350, "defaultkey");
		client.Timeout = 500;

		//Authenticate
		//var session = await client.AuthenticateDeviceAsync(OS.GetUniqueId()); //change this

		//GD.Print($"Authenticated with session token {session.AuthToken}");

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public async void _on_button_button_down()
	{
		session = await client.AuthenticateEmailAsync(GetNode<LineEdit>("Panel/LineEdit").Text,
						GetNode<LineEdit>("Panel/LineEdit2").Text);

		GD.Print($"Authenticated with session token {session.AuthToken}");

		await client.UpdateAccountAsync(session, GetNode<LineEdit>("Panel/LineEdit").Text.Split("@")[0], "testdisplay", "http://test.com/test.png", "SG", "Singapore", "EST");
	}

	public async void _on_join_button_down()
	{
		socket = Socket.From(client);
		await socket.ConnectAsync(session);

		socket.ReceivedMatchPresence += onMatchPresence;
		socket.ReceivedMatchState += onMatchState;

		match = await socket.CreateMatchAsync(GetNode<LineEdit>("Panel2/LineEdit").Text);
		GD.Print($"Created Match with ID : {match.Id}");

		await socket.JoinMatchAsync(match.Id);
		GD.Print($"Joined Match with ID : {match.Id}");

	}

	public async void _on_join2_button_down()
	{
		socket = Socket.From(client);
		await socket.ConnectAsync(session);

		socket.ReceivedMatchPresence += onMatchPresence;
		socket.ReceivedMatchState += onMatchState;

		await socket.JoinMatchAsync(GetNode<LineEdit>("Panel2/LineEdit").Text);
		GD.Print($"Joined Match with ID : {GetNode<LineEdit>("Panel2/LineEdit2").Text}");

	}

	private void onMatchPresence(IMatchPresenceEvent @event)
	{
		//GD.Print(@event.ToString());
		foreach (var item in @event.Joins)
		{
			
		}
	}

	private void onMatchState(IMatchState state)
	{
		string data = Encoding.UTF8.GetString(state.State);
		GD.Print($"Received data from user : {data} ");
		switch (state.OpCode) //content of the data sent
		{
			case 0:
				CallDeferred(nameof(EmitPlayerJoinGameSignal), data);
				break;
			case 1:
				break;
		}
	}

	public void EmitPlayerJoinGameSignal(string data)
	{
		EmitSignal(SignalName.PlayerJoinGame, data); //Must Build first
	}

	public static async void SyncData(string data, int opcode)
	{
		await socket.SendMatchStateAsync(match.Id, opcode, data);
	}


	public async void _on_ping_button_down()
	{
		var data = Encoding.UTF8.GetBytes(GetNode<LineEdit>("Panel2/LineEdit2").Text);

		await socket.SendMatchStateAsync(match.Id, 1, data);

	}

	public async void _on_matchmaking_button_down()
	{

		socket = Socket.From(client);
		await socket.ConnectAsync(session);


		socket.ReceivedMatchPresence += onMatchPresence;
		socket.ReceivedMatchState += onMatchState;

		var query = "+properties.skill:>50 properties.mode:deathmatch";
		var stringProp = new Dictionary<string, string>{{ "mode", "deathmatch" }};
		var numericProp = new Dictionary<string, double> { { "skill", 100 } };

		var matchmakerticket = await socket.AddMatchmakerAsync(query, 2, 8, stringProp, numericProp);

		socket.ReceivedMatchmakerMatched += onMatchmakerMatched;
	}

	private async void onMatchmakerMatched(IMatchmakerMatched matched)
	{
		match = await socket.JoinMatchAsync(matched);
		GD.Print($"Joined match with id {match.Id}");
		
	}


	private async void _on_button_1_button_down() //store
	{
	
		PlayerInfo playerinfo = new PlayerInfo
		{
			name = "tester",
			skill = 100,
			level = 10
		};

		WriteStorageObject writeStorageObject = new WriteStorageObject
		{
			Collection = "playerinfo",
			Key = "player2Info",
			Value = JsonWriter.ToJson(playerinfo),
			PermissionRead = 1,  //0 is everyone can , 1 is owner and serveronly
			PermissionWrite = 1
		};

		await client.WriteStorageObjectsAsync(session, new[] { writeStorageObject });

	}

	private async void _on_button_2_button_down() //get data
	{
		var storageobject = new StorageObjectId
		{
			Collection = "playerinfo",
			Key = "playerInfo",
			UserId = session.UserId
		};
		var result = await client.ReadStorageObjectsAsync(session, new[] { storageobject });
		if(result.Objects.Any())
		{
			var obj = result.Objects.First();
			GD.Print($"data received is {obj.Value} {JsonParser.FromJson<PlayerInfo>(obj.Value)}");
		}
	}

	private async void _on_button_3_button_down() //list data
	{
		var limit = 2;
		var playerdatalist = await client.ListUsersStorageObjectsAsync(session, "playerinfo", session.UserId, limit);

		foreach(var data in playerdatalist.Objects)
		{
			GD.Print($"data is {data.Value}");
		}
	}

	private async void _on_get_friend_button_down()
	{
		var result = await client.ListFriendsAsync(session, 0, 10, null);
		// session, 0=only friend 2=invited 3=blocked, limit
		foreach ( var item in result.Friends)
		{
			//GD.Print(item.ToJson());
			GD.Print(item.User.Username);
		}
	}


	private async void _on_add_friend_button_down()
	{
		await client.AddFriendsAsync(session, null, new[] { GetNode<LineEdit>("Panel4/Friendname").Text } );
	}


	private async void _on_block_friend_button_down()
	{
		await client.BlockFriendsAsync(session, null, new[] { GetNode<LineEdit>("Panel4/Friendname").Text });
	}


	private async void _on_delete_friend_button_down()
	{
		await client.DeleteFriendsAsync(session, null, new[] { GetNode<LineEdit>("Panel4/Friendname").Text });
	}

	private void updateUserInfo()
	{ 
	}
}
