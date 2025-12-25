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
using static System.Formats.Asn1.AsnWriter;


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

    public Dictionary<int, Player> PlayerList { get; set; } = new();

    public Player MainPlayer { get; set; }
    public int MainPlayerTurn {  get; set; }
    public string Gamemode { get; set; }
    private string HostName;
    private int PlayerId = -1;
    public bool IsHost { get; private set; } = false;
    public int NumberOfPlayers { get; set; } = 4;
    public int TimingPerTurn { get; set; } = 30;
    private string[] content;
    public bool BGMPlay { get; set; } = true;
    public string GameLanguage { get; set; } = "English";

    public SaveData SD;
    public int Point { get; set; }

    public string BGThemeEquiped { get; set; }
    public string SoundEquiped { get; set; }
    public string TileThemeEquiped { get; set; }

    public string GameToken { get; set; }

    private IApiGroup currentSelectedGroup;
    private IGroupUserListGroupUser currentlySelectedUser;
    private IChannel currentChat;

    [Signal] public delegate void MatchJoinedEventHandler(string roomId);
    [Signal] public delegate void PlayerJoinedEventHandler(string playerName);

    [Signal] public delegate void PlayerDataSyncEventHandler(Json data);
    [Signal] public delegate void PlayerJoinGameEventHandler(string data);
    [Signal] public delegate void PlayerReadyGameEventHandler(string data);
    [Signal] public delegate void PlayerGameStatusEventHandler(string data);
    [Signal] public delegate void PlayerGameTurnsEventHandler(string data);
    [Signal] public delegate void GameDeckEventHandler(string data);
    [Signal] public delegate void PlayerGameSeatsEventHandler(string data);
    [Signal] public delegate void PlayerGameHandsEventHandler(string data);
    [Signal] public delegate void GameTableEventHandler(string data);
    [Signal] public delegate void PlayerGameCastleEventHandler(string data);
    [Signal] public delegate void PlayerGameChatsEventHandler(string data);

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

        LoggerManager.Info("NakamaSingleton ready.");

        GameLogic.LoadFromFile();

        UpdateSaveData();

    }

    public void UpdateSaveData()
    {
        SD = GameLogic.GetGameSaved();
        Point = GameLogic.Loadpoint();
        BGThemeEquiped = GameLogic.GetEquippedBG();
        TileThemeEquiped = GameLogic.GetEquippedTile();
        SoundEquiped = GameLogic.GetEquippedMusic();
    }

    public override void _Notification(int what)
    {
        //  Main quit request constant in Godot C# is 17
        const int NotificationWmQuitRequest = 17;

        if (what == NotificationWmQuitRequest || what == NotificationWMCloseRequest)
        {
            GD.Print("Game is quitting, saving score: " + Point);
            GameLogic.Savepoint(Point);
        }
    }

    public override void _ExitTree()
    {
        GD.Print("Game is quitting, saving score: " + Point);
        GameLogic.Savepoint(Point);
    }



    public async Task HostGame(string playerName, int maxPlayers)
    {
        try
        {
            IsHost = true;
            NumberOfPlayers = maxPlayers;
            MainPlayer = new Player(playerName, true);
            CurrentPlayers++;
            MainPlayer.player_turn = CurrentPlayers;
            PlayerList[CurrentPlayers] = MainPlayer;


            Session = await Client.AuthenticateCustomAsync(MainPlayer.player_id, MainPlayer.player_name);
            await Client.UpdateAccountAsync(Session, playerName, playerName);

            Socket = Nakama.Socket.From(Client);
            await Socket.ConnectAsync(Session);

            Socket.ReceivedMatchPresence += onMatchPresence;
            Socket.ReceivedMatchState += onMatchState;

            Match = await Socket.CreateMatchAsync();
            RoomId = Match.Id;
            LoggerManager.Info($"Created Room with ID : {Match.Id}");

            await Socket.JoinMatchAsync(Match.Id);

        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS HostGame : "+e);
        }
    }

    public async Task JoinGame(string playerName, string roomId)
    {
        try
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
            LoggerManager.Info($"Joined Match with ID : {Match.Id}");

            var data = Encoding.UTF8.GetBytes(MainPlayer.player_name);
            await Socket.SendMatchStateAsync(Match.Id, 0, data);

        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS JoinGame : " + e);
        }

    }

    public async Task SendMessage(string message)
    {
        try
        {
            var array = new string[] { MainPlayer.player_name, message };
            var json = JsonConvert.SerializeObject(array);
            var data = Encoding.UTF8.GetBytes(json);
            await Socket.SendMatchStateAsync(Match.Id, 1, data);
        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS SendMessage : " + e);
        }
    }
  
    private void onMatchPresence(IMatchPresenceEvent @event)
    {
        //LoggerManager.Info(@event.ToString());
    }

    // For others
    private void onMatchState(IMatchState state)
    {
        try
        {
            string data = Encoding.UTF8.GetString(state.State);
            switch (state.OpCode) //content of the data sent
            {
                case 0: // Players Join
                    CallDeferred(nameof(EmitPlayerJoinGameSignal), data);
                    break;
                case 1: // Chats
                    content = JsonConvert.DeserializeObject<string[]>(data);
                    LoggerManager.Info($"Received data from user {content[0]} : {content[1]} ");
                    CallDeferred(nameof(ChatMessages), content);
                    break;
                case 2: // Start Game
                    CallDeferred(nameof(EmitReadytostart), data);
                    NumberOfPlayers = int.Parse(data);
                    break;
                case 3: // Set Player turn
                    var rjson = JsonConvert.DeserializeObject<Dictionary<int, Player>>(data);
                    foreach (var kvp in rjson)
                    {
                        PlayerList[kvp.Key] = kvp.Value;
                    }
                    break;
                case 4: // Update tile
                    content = JsonConvert.DeserializeObject<string[]>(data);
                    LoggerManager.Info($"Received data from user {content[0]} : {content[1]} ");
                    //CallDeferred(nameof(EmitSyncTiles), content);
                    break;
                case 5: // Update Deck
                    CallDeferred(nameof(EmitGameDeck), data);
                    break;

            }
        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS onMatchState {state.OpCode} : " + e);
        }
        
    }

    private void ChatMessages(string[] content)
    {
        LoggerManager.Info($"I {MainPlayer.player_name} received from {content[0]}");
    }

    public async void EmitPlayerJoinGameSignal(string data)
    {
        try
        {
            if (!IsHost)
                return;

            var player = new Player(data, false);
            CurrentPlayers++;

            string x = player.player_name + "," + CurrentPlayers;
            PlayerList[CurrentPlayers] = player;

            LoggerManager.Info($" {MainPlayer.player_name} says {player.player_name} Joined. Current Player count {CurrentPlayers}");
            //NakamaStore("PlayerList", Hostname, playerList);

            if (CurrentPlayers == NumberOfPlayers)
            {
                //GameStart();
                CallDeferred(nameof(EmitReadytostart), CurrentPlayers.ToString());  // For self startgame
                                                                                    //EmitSignal(nameof(PlayerReadyGame), MainPlayer.player_name);

                // Update Turns
                var djson = JsonConvert.SerializeObject(PlayerList);
                byte[] datas = Encoding.UTF8.GetBytes(djson);
                await Socket.SendMatchStateAsync(Match.Id, 3, datas); // For other toupdate


                await Socket.SendMatchStateAsync(Match.Id, 2, CurrentPlayers.ToString()); // For others startgame
            }
        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS EmitPlayerJoinGameSignal : " + e);
        }

    }

    public void EmitReadytostart(string data)
    {
        EmitSignal(nameof(PlayerReadyGame), data);
    }

    public void EmitPlayerGameStatus(string data)
    {
        EmitSignal(nameof(PlayerGameStatus), data);
    }
    public void EmitPlayerGameTurns(string data)
    {
        EmitSignal(nameof(PlayerGameTurns), data);
    }
    public void EmitGameDeck(string data)
    {
        EmitSignal(nameof(GameDeck), data);
    }
    public void EmitPlayerGameSeats(string data)
    {
        EmitSignal(nameof(PlayerGameSeats), data);
    }
    public void EmitPlayerGameHands(string data)
    {
        EmitSignal(nameof(PlayerGameHands), data);
    }
    public void EmitGameTable(string data)
    {
        EmitSignal(nameof(GameTable), data);
    }
    public void EmitPlayerGameCastle(string data)
    {
        EmitSignal(nameof(PlayerGameCastle), data);
    }
    public void EmitPlayerGameChats(string data)
    {
        EmitSignal(nameof(PlayerGameChats), data);
    }

    public async void SyncData(string data, int opcode)
    {
        await Socket.SendMatchStateAsync(Match.Id, opcode, data);
    }

    // Store data to Nakama
    protected async void NakamaStore(string collection, string key, Object classtype, int permissionread = 1, int permissionwrite = 1)
    {

        try
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
            LoggerManager.Info("NakamaStore Completed!!");
        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS NakamaStore : " + e);
        }
        

    }

    // Get data from Nakama
    protected async void NakamaGetStorageObject(string collection, string key)
    {
        try
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
                LoggerManager.Info($"data received is {obj.Value} {JsonParser.FromJson<Player>(obj.Value)}");
            }
        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS NakamaGetStorageObject : " + e);
        }
        

    }

    public List<int> GetTurnOrder(int myId, int numPlayers)
    {
        try
        {
            List<int> players = Enumerable.Range(1, numPlayers).ToList();
            int startIndex = players.IndexOf(myId);

            // Rotate the list starting from myId
            List<int> rotated = players.Skip(startIndex).Concat(players.Take(startIndex)).ToList();
            return rotated;
        }
        catch (Exception e)
        {
            LoggerManager.Error($"NS GetTurnOrder : " + e);
            return null;
        }
    }


    private Random rand = new Random();

    private string[] syllables = new[]
    {
    "ka", "zu", "mi", "ta", "shi", "ra", "na", "ko", "ha", "yo",
    "li", "sa", "ne", "mo", "re", "chi", "do", "fu", "ga", "ve"
    };

    public string GenerateRandomName(int syllableCount = 3)
    {
        StringBuilder name = new StringBuilder();
        for (int i = 0; i < syllableCount; i++)
        {
            string syllable = syllables[rand.Next(syllables.Length)];
            name.Append(syllable);
        }

        // Capitalize first letter
        if (name.Length > 0)
            name[0] = char.ToUpper(name[0]);

        return name.ToString();
    }



}
