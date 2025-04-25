using four_color_game.scripts.FourColors;
using FourColors;
using Godot;
using Nakama;
using Nakama.TinyJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

public partial class MultiplayerPage : Node
{
	const int PORT = 7350;			//NAKAMA
	const int MAX_Sessiontraffic = 100;
	public int MAX_PLAYERS = 4;
	public int currentplayers = 0;
	const string SERVER_ADDRESS = "127.0.0.1";
	private string roomId;
	private int playerId = -1;

	public ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
	
	private Button HostButton, JoinButton, WatchButton, CopyRoomID;
	private LineEdit HostnameInput, RoomSizeInput, JoinNameInput, JoinRoomIdInput, WatchRoomIdInput;
	private Label RoomLabel, GeneratedRoomID;
	private Node MainTable;


	[Export] public PackedScene PlayerScene;
	public bool IsHost { get; private set; } = false;

	private static Client client;
	private static ISession session;
	private static ISocket socket;
	private static IMatch match;
	private static MultiplayerPage Client;

	[Signal]
	public delegate void PlayerDataSyncEventHandler(string data);
	[Signal]
	public delegate void PlayerJoinGameEventHandler(string data);

	
	public override void _Ready()
	{
		// UI setup
		HostButton = (Button)FindChild("Hostbutton");
		JoinButton = (Button)FindChild("JoinButton");
		WatchButton = (Button)FindChild("WatchButton");
		CopyRoomID = (Button)FindChild("CopyRoomID");

		HostnameInput = (LineEdit)FindChild("Hostname");
		RoomSizeInput = (LineEdit)FindChild("Roomsize");
		JoinNameInput = (LineEdit)FindChild("Playername");
		JoinRoomIdInput = (LineEdit)FindChild("Joinroomid");
		WatchRoomIdInput = (LineEdit)FindChild("Watchroomid");

		RoomLabel = (Label)FindChild("RoomLabel");
		GeneratedRoomID = (Label)FindChild("GeneratedRoomID");


		// Signals
		HostButton.Pressed += OnHostButtonPressed;
		JoinButton.Pressed += OnJoinButtonPressed;




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

	public override void _Process(double delta)
	{
		//if(currentplayers == MAX_PLAYERS)
		//{
		//	GameStart();
		//}
	}

	private async void readyAsync()
	{
		client = new Client("http",SERVER_ADDRESS, PORT, "defaultkey");
		client.Timeout = 500;

		//Authenticate
		//var session = await client.AuthenticateDeviceAsync(OS.GetUniqueId()); //change this

		//GD.Print($"Authenticated with session token {session.AuthToken}");

	}

	private async void OnHostButtonPressed()
	{
		//DisableButtons();

		Player player = new Player(HostnameInput.Text,true);
		
		
		session = await client.AuthenticateCustomAsync(player.player_id,player.player_name);



		//// Store playerinfo if not available
		//NakamaStore(player.GetCollectionName(), player.Key, player);
		


		var room = new MatchRoom(MAX_PLAYERS);
		
		await client.UpdateAccountAsync(session, player.player_name,player.player_name);
		GD.Print($"{player.player_name} Hosting with Room ID: {room.RoomID}");

		CreateNJoinMatch(room.RoomID);

		RoomLabel.Text = "Created Room with ID:              ";
		GeneratedRoomID.Text = room.RoomID;
		CopyRoomID.Visible = true;

	}



	private async void OnJoinButtonPressed()
	{
		//DisableButtons();
		var player = new Player(JoinNameInput.Text, false);
		session = await client.AuthenticateCustomAsync(player.player_id, player.player_name);

		string inputRoomId = JoinRoomIdInput.Text.Trim();

		await client.UpdateAccountAsync(session, player.player_name, player.player_name);

		CreateNJoinMatch(inputRoomId);

	}

	public async void CreateNJoinMatch(string roomId)
	{
		socket = Socket.From(client);
		await socket.ConnectAsync(session);

		socket.ReceivedMatchPresence += onMatchPresence;
		socket.ReceivedMatchState += onMatchState;

		match = await socket.CreateMatchAsync(roomId);
		GD.Print($"Created Match with ID : {match.Id}");

		await socket.JoinMatchAsync(match.Id);
		GD.Print($"Joined Match with ID : {match.Id}");
	}

	private void onMatchPresence(IMatchPresenceEvent @event)
	{
		GD.Print(@event.ToString());
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
		var data = Encoding.UTF8.GetBytes("Hell !!!");

		await socket.SendMatchStateAsync(match.Id, 1, data);

	}

	public void _on_copy_room_id_pressed()
	{
		DisplayServer.ClipboardSet(GeneratedRoomID.Text);
	}


	private void DisableButtons()
	{
		HostButton.Disabled = true;
		JoinButton.Disabled = true;
		WatchButton.Disabled = true;
	}


	// Store data to Nakama
	protected async void NakamaStore(string collection, string key, Object classtype, int permissionread=1, int permissionwrite=1)
	{
		WriteStorageObject writeStorageObject = new WriteStorageObject
		{
			Collection = collection,
			Key = key,
			Value = JsonWriter.ToJson(classtype),
			PermissionRead = permissionread,  //0 is everyone can , 1 is owner and serveronly
			PermissionWrite = permissionwrite
		};

		await client.WriteStorageObjectsAsync(session, new[] { writeStorageObject });

	}

	// Get data from Nakama
	protected static async void NakamaGetStorageObject(string collection, string key)
	{
		var storageobject = new StorageObjectId
		{
			Collection = collection,
			Key = key,
			UserId = session.UserId
		};
		var result = await client.ReadStorageObjectsAsync(session, new[] {storageobject});
		if (result.Objects.Any())
		{
			var obj = result.Objects.First();
			GD.Print($"data received is {obj.Value} {JsonParser.FromJson<Player>(obj.Value)}");
		}

	}



	private void GameStart()
	{
		MainTable = (Node)PlayerScene.Instantiate();
		GetTree().Root.AddChild(MainTable); 
		GetTree().CurrentScene = MainTable;
	}



/////////////////////////// RPC Calling Funtions




}
