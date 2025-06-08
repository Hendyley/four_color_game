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
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection.Emit;
using static System.Collections.Specialized.BitVector32;
using System.Net.Sockets;


public partial class NakamaSingleton : Node
{
    public static NakamaSingleton Instance { get; private set; }

    public const int Port = 7350;                           // NAKAMA
    public const string ServerAddress = "127.0.0.1";
    const int MAX_Sessiontraffic = 100;
    public int CurrentPlayers { get; set; } = 0;
    public int CurrentTurn {  get; set; } = 0;
    public string RoomId;

    public Client Client { get; private set; }
    public ISession Session { get; private set; }
    public ISocket Socket { get; private set; }
    public IMatch Match { get; private set; }

    public List<Player> PlayerList { get; private set; } = new();
    public Dictionary<int,List<string>> PlayerHands { get; set; } = new();

    public Player MainPlayer { get; set; }
    public int MainPlayerTurn {  get; set; }
    public string Gamemode { get; set; }
    private string HostName;
    private int PlayerId = -1;
    public bool IsHost { get; private set; } = false;
    public int NumberOfPlayers { get; set; } = 4;
    private string[] content;


    private IApiGroup currentSelectedGroup;
    private IGroupUserListGroupUser currentlySelectedUser;
    private IChannel currentChat;

    [Signal] public delegate void MatchJoinedEventHandler(string roomId);
    [Signal] public delegate void PlayerJoinedEventHandler(string playerName);

    [Signal] public delegate void PlayerDataSyncEventHandler(Json data);
    [Signal] public delegate void PlayerJoinGameEventHandler(string data);
    [Signal] public delegate void PlayerReadyGameEventHandler(string data);
    [Signal] public delegate void PlayerTileUpdateEventHandler(string data);

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree(); // Ensure singleton
            return;
        }

        Instance = this;
        Name = "NakamaSingleton";
        Client = new Client("http", ServerAddress, Port, "defaultkey");
        Client.Timeout = 500;

        GD.Print("NakamaSingleton ready.");
    }

    public async Task HostGame(string playerName, int maxPlayers)
    {

        IsHost = true;
        NumberOfPlayers = maxPlayers;
        MainPlayer = new Player(playerName, true);
        CurrentPlayers++;
        MainPlayer.player_turn = CurrentPlayers;
        PlayerList.Add(MainPlayer);
        

        Session = await Client.AuthenticateCustomAsync(MainPlayer.player_id, MainPlayer.player_name);
        await Client.UpdateAccountAsync(Session, playerName, playerName);

        Socket = Nakama.Socket.From(Client);
        await Socket.ConnectAsync(Session);

        Socket.ReceivedMatchPresence += onMatchPresence;
        Socket.ReceivedMatchState += onMatchState;

        Match = await Socket.CreateMatchAsync();
        RoomId = Match.Id;
        //GD.Print($"Created Match with ID : {Match.Id}");

        await Socket.JoinMatchAsync(Match.Id);
        //GD.Print($"Joined Match with ID : {Match.Id}");


        //EmitSignal(nameof(MatchJoined), Match.Id);
    }

    public async Task JoinGame(string playerName, string roomId)
    {
        IsHost = false;
        MainPlayer = new Player(playerName, false);

        Session = await Client.AuthenticateCustomAsync(MainPlayer.player_id, MainPlayer.player_name);
        await Client.UpdateAccountAsync(Session, playerName, playerName);

        Socket = Nakama.Socket.From(Client);
        await Socket.ConnectAsync(Session);

        Socket.ReceivedMatchPresence += onMatchPresence;
        Socket.ReceivedMatchState += onMatchState;

        Match = await Socket.JoinMatchAsync(roomId);
        GD.Print($"Joined Match with ID : {Match.Id}");

        var data = Encoding.UTF8.GetBytes(MainPlayer.player_name);
        await Socket.SendMatchStateAsync(Match.Id, 0, data);
        //EmitSignal(nameof(MatchJoined), Match.Id);
    }


    public async Task SendMessage(string message)
    {
        var array = new string[] { MainPlayer.player_name, message };
        var json = JsonConvert.SerializeObject(array);
        var data = Encoding.UTF8.GetBytes(json);
        await Socket.SendMatchStateAsync(Match.Id, 1, data);
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
                PlayerJoiningInfo(data); // Host add new player
                break;
            case 1: // Chats
                content = JsonConvert.DeserializeObject<string[]>(data);
                GD.Print($"Received data from user {content[0]} : {content[1]} ");
                CallDeferred(nameof(ChatMessages), content);
                break;
            case 2: // Start Game
                CallDeferred(nameof(EmitReadytostart), data);
                NumberOfPlayers = int.Parse(data);
                break;
            case 3: // Set Player turn
                string[] input = data.Split(',');
                if (input[0] == MainPlayer.player_name)
                {
                    MainPlayer.player_turn = int.Parse(input[1]);
                    GD.Print($"{MainPlayer.player_name} assigned {MainPlayer.player_turn}");
                }
                break;
            case 4: // Update tile
                content = JsonConvert.DeserializeObject<string[]>(data);
                GD.Print($"Received data from user {content[0]} : {content[1]} ");
                CallDeferred(nameof(EmitSyncTiles), content);
                break;
        }
    }

    private void ChatMessages(string[] content)
    {
        //TLabel.Text = content[1];
        GD.Print($"I {MainPlayer.player_name} received from {content[0]}");
    }


    public void EmitPlayerJoinGameSignal(string data)
    {
        EmitSignal(SignalName.PlayerJoinGame, data);
    }

    public void EmitReadytostart(string data)
    {
        EmitSignal(nameof(PlayerReadyGame), data);
    }


    public void EmitSyncTiles(string[] content)
    {
        EmitSignal(nameof(PlayerTileUpdate), content);
    }

    public async void SyncData(string data, int opcode)
    {
        await Socket.SendMatchStateAsync(Match.Id, opcode, data);
    }

    private async void PlayerJoiningInfo(string data)
    {
        if(!IsHost)
            return;

        var player = new Player(data, false);
        CurrentPlayers++;

        string x = player.player_name + "," + CurrentPlayers;
        await Socket.SendMatchStateAsync(Match.Id, 3, x);
        PlayerList.Add(player);

        GD.Print($" {MainPlayer.player_name} says {player.player_name} Joined. Current Player count {CurrentPlayers}");
        //NakamaStore("PlayerList", Hostname, playerList);

        if (CurrentPlayers == NumberOfPlayers)
        {
            //GameStart();
            CallDeferred(nameof(EmitReadytostart), CurrentPlayers.ToString());
            //EmitSignal(nameof(PlayerReadyGame), MainPlayer.player_name);
            await Socket.SendMatchStateAsync(Match.Id, 2, CurrentPlayers.ToString());
        }
    }

    // Store data to Nakama
    protected async void NakamaStore(string collection, string key, Object classtype, int permissionread = 1, int permissionwrite = 1)
    {
        WriteStorageObject writeStorageObject = new WriteStorageObject
        {
            Collection = collection,
            Key = key,
            Value = Nakama.TinyJson.JsonWriter.ToJson(classtype),
            PermissionRead = permissionread,  //0 is everyone can , 1 is owner and serveronly
            PermissionWrite = permissionwrite
        };

        await Client.WriteStorageObjectsAsync(Session, new[] { writeStorageObject });
        GD.Print("NakamaStore Completed!!");

    }

    // Get data from Nakama
    protected async void NakamaGetStorageObject(string collection, string key)
    {
        var storageobject = new StorageObjectId
        {
            Collection = collection,
            Key = key,
            UserId = Session.UserId
        };
        var result = await Client.ReadStorageObjectsAsync(Session, new[] { storageobject });
        if (result.Objects.Any())
        {
            var obj = result.Objects.First();
            GD.Print($"data received is {obj.Value} {JsonParser.FromJson<Player>(obj.Value)}");
        }

    }

    public List<int> GetTurnOrder(int myId, int numPlayers)
    {
        List<int> players = Enumerable.Range(1, numPlayers).ToList();
        int startIndex = players.IndexOf(myId);

        // Rotate the list starting from myId
        List<int> rotated = players.Skip(startIndex).Concat(players.Take(startIndex)).ToList();
        return rotated;
    }

}
