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
using System.Threading.Tasks;
//using System.Text.Json;
using Newtonsoft.Json;

public partial class MultiplayerPage : Node
{
	const int PORT = 7350;			//NAKAMA
	const int MAX_Sessiontraffic = 100;
	public int MAX_PLAYERS = 4;
	public int currentplayers = 0;
	const string SERVER_ADDRESS = "127.0.0.1";
	private string roomId;
	private string Hostname;
	private int playerId = -1;
	private List<Player> playerList = new List<Player>();
	private Player mainplayer;
	//public static Dictionary<string, PlayerHand> PlayerHands = new();

	public ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
	
	private Button HostButton, JoinButton, WatchButton, CopyRoomID;
	private LineEdit PlayernameInput, JoinRoomIdInput;
	private Label RoomLabel, GeneratedRoomID, TLabel;
	private OptionButton RoomSizeInput;
	private Node MainTable;


	[Export] public PackedScene startgamescene;
	
	public bool IsHost { get; private set; } = false;

	private static Client client;
	private static ISession session;
	private static ISocket socket;
	private static IMatch match;
	private static MultiplayerPage Client;

	private IApiGroup currentSelectedGroup;
	private IGroupUserListGroupUser currentlySelectedUser;
	private IChannel currentChat;
	//private List<ChatChannel> chatChannels = new List<ChatChannel>();

	[Signal]
	public delegate void PlayerDataSyncEventHandler(Json data);
	[Signal]
	public delegate void PlayerJoinGameEventHandler(string data);

	[Signal]
	public delegate void PlayerReadyGameEventHandler(string data);


	public override void _Ready()
	{
		// UI setup
		HostButton = (Button)FindChild("Hostbutton");
		JoinButton = (Button)FindChild("JoinButton");
		WatchButton = (Button)FindChild("WatchButton");
		CopyRoomID = (Button)FindChild("CopyRoomID");

		PlayernameInput = (LineEdit)FindChild("Playername");
		RoomSizeInput = (OptionButton)FindChild("Roomsize");
		JoinRoomIdInput = (LineEdit)FindChild("Joinroomid");

		RoomLabel = (Label)FindChild("RoomLabel");
		GeneratedRoomID = (Label)FindChild("GeneratedRoomID");
		TLabel = (Label)FindChild("TestingLabel");

		// Signals
		HostButton.Pressed += OnHostButtonPressed;
		JoinButton.Pressed += OnJoinButtonPressed;

		RoomSizeInput.AddItem("1");
		RoomSizeInput.AddItem("2");
		RoomSizeInput.AddItem("3");
		RoomSizeInput.AddItem("4");
		RoomSizeInput.ItemSelected += onOptionSelected;


		if (Client != null)
		{
			GD.Print("removing second instance");
			QueueFree();
		}
		else
		{

			Client = this;
		}
		readyAsync();

	}

	private void onOptionSelected(long index)
	{
		MAX_PLAYERS = int.Parse(RoomSizeInput.GetItemText((int)index));
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
		IsHost = true;

		mainplayer = new Player(PlayernameInput.Text,true);
		playerList.Add(mainplayer);
		Hostname = mainplayer.player_name;
		currentplayers++;
		
		session = await client.AuthenticateCustomAsync(mainplayer.player_id, mainplayer.player_name);
		//// Store playerinfo if not available
		//NakamaStore(player.GetCollectionName(), player.Key, player);
		
		var room = new MatchRoom(MAX_PLAYERS);
		
		await client.UpdateAccountAsync(session, mainplayer.player_name, mainplayer.player_name);
		GD.Print($"{mainplayer.player_name} Hosting with Room ID: {room.RoomID}");

		await CreateNJoinMatch(room.RoomID);

		RoomLabel.Text = "Created Room with ID:              ";
		GeneratedRoomID.Text = room.RoomID;
		CopyRoomID.Visible = true;

	}



	private async void OnJoinButtonPressed()
	{
		//DisableButtons();
		IsHost = false;

		mainplayer = new Player(PlayernameInput.Text, false);
		session = await client.AuthenticateCustomAsync(mainplayer.player_id, mainplayer.player_name);

		string inputRoomId = JoinRoomIdInput.Text.Trim();
		await client.UpdateAccountAsync(session, mainplayer.player_name, mainplayer.player_name);

		await CreateNJoinMatch(inputRoomId);

		var data = Encoding.UTF8.GetBytes(mainplayer.player_name);
		await socket.SendMatchStateAsync(match.Id, 0, data);


	}

	public async Task CreateNJoinMatch(string roomId)
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
		//GD.Print(@event.ToString());
	}

	private void onMatchState(IMatchState state)
	{
		string data = Encoding.UTF8.GetString(state.State);
		switch (state.OpCode) //content of the data sent
		{
			case 0: // Players Join
				CallDeferred(nameof(EmitPlayerJoinGameSignal), data);
				PlayerJoiningInfo(data);
				break;
			case 1: // Chats
				string[] content = JsonConvert.DeserializeObject<string[]>(data);
				GD.Print($"Received data from user {content[0]} : {content[1]} ");
				CallDeferred(nameof(Dothis), content); 
				break;
			case 2: // Start Game?
				CallDeferred(nameof(EmitReadytostart), data);
				break;
			case 3:
				CallDeferred(nameof(EmitSyncTiles), data);
				break;
		}
	}

	private void Dothis(string[] content)
	{
		TLabel.Text = content[1];
		GD.Print($"I {mainplayer.player_name} received from {content[0]}");
	}

	public void EmitSyncTiles(Json data)
	{
		EmitSignal(SignalName.PlayerDataSync, data);
	}
	
	public void EmitPlayerJoinGameSignal(string data)
	{
		if (!IsHost)
			return;

		EmitSignal(SignalName.PlayerJoinGame, data); 
	}

	public void EmitReadytostart(string data)
	{
		EmitSignal(SignalName.PlayerReadyGame, data);
	}

	public static async void SyncData(string data, int opcode)
	{
		await socket.SendMatchStateAsync(match.Id, opcode, data);
	}

	/// <summary>
	/// ////////////////////////////////////////////////////////////
	/// </summary>
	/// 
	private void PlayerJoiningInfo(string data)
	{
		var player = new Player(data, false);
		playerList.Add(player);
		currentplayers++;
		GD.Print($" Host {mainplayer.player_name} says {player.player_name} Joined. Current Player count {currentplayers}");
		//NakamaStore("PlayerList", Hostname, playerList);

		if (currentplayers == MAX_PLAYERS)
		{
			GameStart();
		}
	}



	public async void _on_button_pressed()
	{

		var tes = (LineEdit)FindChild("testfield");
		var array = new string[] { mainplayer.player_name, tes.Text };
		string arrayjson = JsonConvert.SerializeObject(array);
		var data = Encoding.UTF8.GetBytes(arrayjson);
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
			Value = Nakama.TinyJson.JsonWriter.ToJson(classtype),
			PermissionRead = permissionread,  //0 is everyone can , 1 is owner and serveronly
			PermissionWrite = permissionwrite
		};

		await client.WriteStorageObjectsAsync(session, new[] { writeStorageObject });
		GD.Print("NakamaStore Completed!!");

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
		GD.Print("Start button pressed");    
		StartGamePage startmenu = (StartGamePage)startgamescene.Instantiate();
		AddChild(startmenu);
	}

/////////////////////////// RPC Calling Funtions
}
