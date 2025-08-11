using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using FourColors;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Diagnostics.Eventing.Reader;
using four_color_game.scripts.FourColors;

public partial class Tutorialplay : Node
{
    [Export] public PackedScene tileScene;

    public List<string> deck = new List<string>();

    private Dictionary<int, List<string>> playersHands = new Dictionary<int, List<string>>();
    private Dictionary<int, HBoxContainer> playersContainers = new Dictionary<int, HBoxContainer>();
    private Dictionary<int, Player> PlayerSeats = new Dictionary<int, Player>(); // S -> W -> N -> E
    private Dictionary<int, bool> PlayerCastleStatus = new Dictionary<int, bool>();

    private bool throwhand;
    public Godot.Container tabletilescontainer;
    private List<string> tabletiles = new();
    private Button pickButton, castleButton, backButton;
    private Tile lastTableTile, lastDrawnTile, showstile;
    private Label deckcounterLabel, currentturnLabel, turnlabel, timerview, debuglb, debuglb2;
    private ItemList l2;
    private bool _castleFocusGiven = false;
    private bool gameEnded = false;
    private int turnpass = 0;

    private ConfirmationDialog windec, windec2, win_popup;
    private bool decisionMade = false;
    private bool takeDecision = false;
    private AcceptDialog autoMessageBox;
    private Panel bg_panel;
    private AudioStreamPlayer bgm;
    private AudioStreamPlayer sfxm;
    private string mp3tilediscard = "Tile_Discard";
    private string mp3tiledraw = "Tile_Draw";
    private string mp3win = "Win";
    private string mp3castle = "Castle";

    StyleBoxFlat highlightStyle = new StyleBoxFlat();

    public async override void _Ready()
    {
        string logPath = "logs/app.log";
        long maxSizeInBytes = 50 * 1024 * 1024; // 50 MB

        if (File.Exists(logPath))
        {
            FileInfo logInfo = new FileInfo(logPath);
            if (logInfo.Length > maxSizeInBytes)
            {
                File.Delete(logPath);
            }
        }

        tileScene = (PackedScene)ResourceLoader.Load("res://scenes/Tile.tscn");
        tabletilescontainer = (Godot.Container)FindChild("TableTiles");
        pickButton = (Button)FindChild("pickupbutton");
        castleButton = (Button)FindChild("castlebutton");
        backButton = (Button)FindChild("backbutton");

        deckcounterLabel = (Label)FindChild("DeckCounter");
        currentturnLabel = (Label)FindChild("CurrentTurn");
        turnlabel = (Label)FindChild("TurnLabel");
        timerview = (Label)FindChild("TimerView");

        bg_panel = (Panel)FindChild("Panel");
        var stylebox = bg_panel.GetThemeStylebox("panel") as StyleBoxTexture;
        if (stylebox != null)
        {
            stylebox = (StyleBoxTexture)stylebox.Duplicate();
            bg_panel.AddThemeStyleboxOverride("panel", stylebox);

            stylebox.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Background/{NakamaSingleton.Instance.BGThemeEquiped}.png");
        }

        bgm = (AudioStreamPlayer)FindChild("BGM");
        sfxm = (AudioStreamPlayer)FindChild("SFXM");

        var stream = GD.Load<AudioStream>($"res://art/4_Color_Game/Music/Piki - Monogatari (freetouse.com).mp3");
        if (stream != null)
        {
            bgm.Stream = stream;
            bgm.VolumeDb = -10;
            bgm.Play();
            ((AudioStreamMP3)bgm.Stream).Loop = true;
        }

        debuglb = (Label)GetNode("Debug");
        debuglb2 = (Label)GetNode("Debug2");

        windec = (ConfirmationDialog)FindChild("windec");
        windec.Confirmed += () =>
        {
            decisionMade = true;
            takeDecision = true;
        };
        windec.Canceled += () =>
        {
            decisionMade = true;
            takeDecision = false;
        };

        win_popup = (ConfirmationDialog)FindChild("win_popup");
        win_popup.Confirmed += OnWinPopupConfirmed;
        win_popup.Canceled += OnWinPopupCanceled;

        autoMessageBox = (AcceptDialog)FindChild("windec_c");
        autoCloseTimer = (Timer)FindChild("Timer");
        autoCloseTimer.Timeout += () =>
        {
            autoMessageBox.Hide();
        };


        turnTimer = (Timer)FindChild("TurnTimer");
        turnTimer.WaitTime = 1.0;
        turnTimer.Timeout += OnTurnTick;

        l2 = (ItemList)FindChild("l2");

        NakamaSingleton.Instance.CurrentTurn = 1;
        castleButton.Disabled = false;
        pickButton.Disabled = true;

        List<string> seatsvar;
        if (NakamaSingleton.Instance.NumberOfPlayers == 2)
            seatsvar = new List<string>() { "S", "N" };
        else if (NakamaSingleton.Instance.NumberOfPlayers == 3)
            seatsvar = new List<string>() { "S", "W", "N" };
        else if (NakamaSingleton.Instance.NumberOfPlayers == 4)
            seatsvar = new List<string>() { "S", "W", "N", "E" };
        else
            seatsvar = new List<string>() { "S", "W", "N", "E" };

        PlayerSeats = NakamaSingleton.Instance.PlayerList;
        ///////////////////////////////
        List<int> turnsequence = NakamaSingleton.Instance.GetTurnOrder(NakamaSingleton.Instance.MainPlayer.player_turn, NakamaSingleton.Instance.NumberOfPlayers);

        for (int i = 0; i < seatsvar.Count; i++)
        {
            string containerName = $"HBOX_P_{seatsvar[i]}";
            HBoxContainer handContainer = (HBoxContainer)FindChild(containerName);
            playersHands[turnsequence[i]] = new List<string>();
            if (turnsequence[i] != NakamaSingleton.Instance.MainPlayerTurn)
            {
                NakamaSingleton.Instance.PlayerList[turnsequence[i]] = new Player(NakamaSingleton.Instance.GenerateRandomName(), false);
            }                
            string PlayerPWName = $"P{seatsvar[i]}";
            Label PlayerPW = (Label)FindChild(PlayerPWName);
            PlayerPW.Text = NakamaSingleton.Instance.PlayerList[turnsequence[i]].player_name;

            if (handContainer != null)
            {
                playersContainers[turnsequence[i]] = handContainer;
                LoggerManager.Info($"Player {turnsequence[i]} hand assigned to {containerName}");
            }
            else
            {
                GD.PrintErr($"Could not find container: {containerName}");
            }
        }

        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer" || NakamaSingleton.Instance.IsHost)
            StartGame();

        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer")
        {

            DrawTile(2, "C1_Green");
            DrawTile(2, "C1_Red");
            DrawTile(2, "C1_Yellow");
            DrawTile(2, "C2_Green");
            DrawTile(2, "C2_Red");
            DrawTile(2, "C2_Yellow");
            DrawTile(2, "C2");
            DrawTile(2, "C1_Green");
            DrawTile(2, "C3_Green");
            DrawTile(2, "C4_Green");
            DrawTile(2, "C6_Green");
            DrawTile(2, "C6_Red");
            DrawTile(2, "C6_Yellow");
            DrawTile(2, "C6");
            DrawTile(2, "C7");

            
            DrawTile(1, "C1_Green");
            DrawTile(1, "C1_Red");
            DrawTile(1, "C1_Yellow");

            DrawTile(1, "C2_Green");
            DrawTile(1, "C2_Red");
            DrawTile(1, "C2_Yellow");
            DrawTile(1, "C2");

            DrawTile(1, "C1_Green");
            DrawTile(1, "C3_Green");
            DrawTile(1, "C4_Green");

            DrawTile(1, "C6_Green");
            DrawTile(1, "C6_Red");
            DrawTile(1, "C6_Yellow");
            DrawTile(1, "C6");

            DrawTile(1, "C7");



            // Each player start with 15 tiles
            // The Goal of game is to form color sets and honor sets
            // Color sets refer to combination of the same rank with different color (3 or 4 tiles)
            /*
             Honor sets refer to combination of either:
             - King, Queen, Bishop with the same color
             - Horse, Rock, Cannon with the same color
             - 3 or 4 Tiles of Pawns with different color
             */


            DrawTile(1, "C4");
            // The First player will draw an extra tile
            StartTurnTimer();
            // Timer will then start to count down.

            // The player will then decide a tile to discard to the table (random tile will be choose to discard if player did not pick a tile in given time).
            // King tile cannot be discard.
            // White Cannon doesnt form new color or honor set, hence can be choose to be discard





        }

        throwhand = true;
    }

    public async override void _Process(double delta)
    {
        if (gameEnded)
            return;

        deckcounterLabel.Text = deck.Count.ToString();
        currentturnLabel.Text = NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name;
        GameLogic.Deck = deck;
        GameLogic.Table = tabletiles;

        debuglb.Text = "";
        debuglb.Text = $"Tile P!: {playersHands[1].Count}\n";
        var sub = playersHands[1].ToList();
        sub.Sort();
        playersHands[1].Sort();
        foreach (var v in sub)
        {
            debuglb.Text = debuglb.Text + GameLogic.TileName(v) + " "+ v + " \n";
        }

        debuglb2.Text = "";
        debuglb2.Text = $"Tile P2 : {playersHands[2].Count}\n";
        sub = playersHands[2].ToList();
        sub.Sort();
        playersHands[2].Sort();
        foreach (var v in sub)
        {
            debuglb2.Text = debuglb2.Text + GameLogic.TileName(v) +" "+ v + " \n";
        }

        if (l2.ItemCount > 0)
        {
            l2.EnsureCurrentIsVisible(); // ensure selected is visible
            l2.Select(l2.ItemCount - 1); // select last item to trigger scroll
        }

        /////////////////////////////////////////////////////////////////////////////////  "SinglePlayer"
        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer")
        {
            if (GameLogic.CheckCastle(playersHands[NakamaSingleton.Instance.MainPlayerTurn]))
            {
                if (!PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
                {
                    if (!_castleFocusGiven)
                    {
                        castleButton.GrabFocus();
                        _castleFocusGiven = true;
                    }
                }
                else
                {
                    castleButton.ReleaseFocus();
                    _castleFocusGiven = false;
                }
            }
            
            // Check if winning or not (after castle = true)
            if (NakamaSingleton.Instance.CurrentTurn != 1 && PlayerCastleStatus[NakamaSingleton.Instance.CurrentTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.CurrentTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.CurrentTurn);

                }
            }
            else if (NakamaSingleton.Instance.CurrentTurn == 1 && PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.MainPlayerTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.MainPlayerTurn);

                }
            }

        }
    }

    private void StartGame()
    {
        LoggerManager.Info("Game Start");

        deck = CreateDeck();
        ShuffleDeck();
        for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
        {
            playersHands[i].Clear();
            foreach (Node child in playersContainers[i].GetChildren())
                child.QueueFree();
        }

    }

    private List<string> CreateDeck()
    {
        LoggerManager.Info("Create Deck");

        List<string> newDeck = new List<string>();
        String x;
        for (int s = 0; s < 4; s++)
        {
            for (int i = 1; i <= 7; i++) // 7 Pieces
            {
                x = $"C{i}";
                newDeck.Add(x);
                newDeck.Add(x + "_Green");
                newDeck.Add(x + "_Red");
                newDeck.Add(x + "_Yellow");
            }
        }
        LoggerManager.Info("Amount in the Deck: " + newDeck.Count);

        return newDeck;
    }

    private void ShuffleDeck()
    {
        Random rng = new Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (deck[k], deck[n]) = (deck[n], deck[k]);
        }
    }

    private void DrawTile(int playerid, string tileid = "")
    {
        PlaySoundEffect(mp3tiledraw);

        if (lastTableTile != null)
            lastTableTile.CallDeferred("UpdateHighlightVisual", false);

        if (deck.Count > 0)
        {
            if (tileid=="")
            {
                string tilevalue = deck[0];
                deck.RemoveAt(0);
            }
            else
            {
                string tilevalue = tileid;
                deck.Remove(tileid);
            }

            playersHands[playerid].Add(tilevalue);

            LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} Drawn Card Value: " + GameLogic.TileName(tilevalue));
            l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} Drawn Card Value: " + GameLogic.TileName(tilevalue));

            Tile newTile = (Tile)tileScene.Instantiate();
            playersContainers[playerid].AddChild(newTile);
            newTile.Tileid = tilevalue;

            newTile.SetTile(playerid, tilevalue);
            //newTile.CallDeferred("SetTile", playerid ,tilevalue);
            newTile.Connect("TileClicked", new Callable(this, "OnTileClicked"));

            if (playersContainers[playerid].Name != "HBOX_P_S")
                newTile.SetCover(true);
            else
                newTile.Mainuser = true;

            lastDrawnTile = newTile;

            throwhand = true;
        }
        else
        {
            LoggerManager.Info("Deck is empty! No card drawn.");
            l2.AddItem("Deck is empty! No card drawn.");
        }
    }

    private void TakeTile(int playerid = 1, string tilevalue = "C1")
    {
        PlaySoundEffect(mp3tiledraw);

        lastTableTile.CallDeferred("UpdateHighlightVisual", false);
        //SortTiles(NakamaSingleton.Instance.CurrentTurn);

        playersHands[playerid].Add(tilevalue);
        Tile newTile = (Tile)tileScene.Instantiate();
        newTile.Tileid = tilevalue;
        playersContainers[playerid].AddChild(newTile);

        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} take tile: " + GameLogic.TileName(tilevalue));
        l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} take tile: " + GameLogic.TileName(tilevalue));

        newTile.SetTile(playerid, tilevalue);
        //newTile.CallDeferred("SetTile", playerid ,tilevalue);
        newTile.Connect("TileClicked", new Callable(this, "OnTileClicked"));

        if (playersContainers[playerid].Name != "HBOX_P_S")
            newTile.SetCover(true);     //newTile.CallDeferred("SetCover", true);
        else
            newTile.Mainuser = true;

        lastTableTile = null;

        tabletiles.Remove(tilevalue);
        pickButton.Disabled = true;
        throwhand = true;

    }

    public async void DiscardTile(int playerid, string tilevalue)
    {
        if (!throwhand)
            return;

        PlaySoundEffect(mp3tilediscard);

        l2.AddItem($"player {NakamaSingleton.Instance.PlayerList[playerid].player_name} Discard {GameLogic.TileName(tilevalue)}");

        var children = playersContainers[playerid].GetChildren();

        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is Tile tile)
            {
                if (tile.Tileid == tilevalue)
                {
                    playersContainers[playerid].RemoveChild(tile);
                    tile.QueueFree();
                    playersHands[playerid].Remove(tilevalue);
                    await AddtoTables(tilevalue);
                    break;
                }
            }
        }

        //SortTiles(playerid);
        await AddTurn(); ////////////////////////////////////////////////////////////////////////////// Turn Change here
        await autoplay();
        return;
    }

    private async Task<bool> AddtoTables(string tilevalue)
    {
        LoggerManager.Info($"Add {GameLogic.TileName(tilevalue)} to the table ");

        if (lastTableTile != null)
        {
            lastTableTile.Disconnect("TableTileClicked", new Callable(this, "OnTableTileClicked"));
        }

        Tile newTile = (Tile)tileScene.Instantiate();
        tabletilescontainer.AddChild(newTile);
        tabletiles.Add(tilevalue);
        newTile.Tileid = tilevalue;
        lastTableTile = newTile;

        //newTile.CallDeferred("UpdateHighlightVisual", true);
        //newTile.CallDeferred("SetTableTile", tilevalue);
        //newTile.Connect("TableTileClicked", new Callable(this,"OnTableTileClicked"));
        newTile.UpdateHighlightVisual(true);
        newTile.SetTableTile(tilevalue);
        newTile.Connect("TableTileClicked", new Callable(this, "OnTableTileClicked"));

        Random rand = new Random();
        Vector2 containerSize = tabletilescontainer.GetRect().Size;
        Vector2 tileSize = newTile.GetRect().Size;

        float randomX = (float)(rand.NextDouble() * (containerSize.X - tileSize.X));
        float randomY = (float)(rand.NextDouble() * (containerSize.Y - tileSize.Y));
        newTile.Position = new Vector2(randomX, randomY);
        float randomRotation = (float)(rand.NextDouble() * 360);
        newTile.RotationDegrees = randomRotation;


        throwhand = false;

        return true;
    }

    private void OnTileClicked(Tile clickedTile)
    {
        if (clickedTile.Tileid.Contains("C7"))
        {
            LoggerManager.Info("Cannot discard King tile !!!");
            ShowAutoMessage("Cannot discard King tile !!!", 5000);
            return;
        }
        DiscardTile(clickedTile.Playerid, clickedTile.Tileid);

    }

    private void OnTableTileClicked(Tile clickedTile)
    {
        foreach (Node child in tabletilescontainer.GetChildren())
        {
            if (child is Tile tile)
            {
                if (tile.Tileid == clickedTile.Tileid)
                {
                    tabletilescontainer.RemoveChild(tile);
                    tile.QueueFree();
                    break;
                }
            }
            else
            {
                LoggerManager.Info("Warning: Child is not a Tile node!");
            }
        }
        LoggerManager.Info($"Player {clickedTile.Playerid} Taking Tile: " + clickedTile.Tileid);
        l2.AddItem($"Player {clickedTile.Playerid} Taking Tile: " + clickedTile.Tileid);
        TakeTile(NakamaSingleton.Instance.CurrentTurn, clickedTile.Tileid);
    }



    private void buttondisable()
    {
        pickButton.Disabled = true;
        castleButton.Disabled = true;
    }


    private void _on_pickupbutton_pressed()
    {
        LoggerManager.Info("pickup button pressed");
        DrawTile(NakamaSingleton.Instance.CurrentTurn);
        pickButton.Disabled = true;
    }

    private void _on_castlebutton_pressed()
    {
        PlaySoundEffect(mp3castle);
        LoggerManager.Info("castle button pressed");
        if (!GameLogic.CheckCastle(playersHands[NakamaSingleton.Instance.MainPlayerTurn]))
        {
            ShowAutoMessage("You Cannot Castle now.", 5000);
            castleButton.ReleaseFocus();
            return;
        }
        castleButton.ReleaseFocus();
        PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn] = true;
        ShowAutoMessage($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} Castle", 5000);
        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} Castle");
    }

    private void _on_back_button_pressed()
    {
        LoggerManager.Info("back button pressed");
        bgm.Stop();
        sfxm.Stop();
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }

    private void _on_sortbutton_pressed()
    {
        SortTiles(NakamaSingleton.Instance.MainPlayerTurn);

    }

    private void EndGame(int winnerId)
    {
        bgm.Stop();
        PlaySoundEffect(mp3win);

        gameEnded = true;

        pickButton.Disabled = true;
        castleButton.Disabled = true;

        if (NakamaSingleton.Instance.MainPlayerTurn == winnerId)
            NakamaSingleton.Instance.Point += GameLogic.CheckScore(playersHands[NakamaSingleton.Instance.MainPlayerTurn]);
        else
            NakamaSingleton.Instance.Point += 10;

        win_popup.DialogText = $"🎉 {NakamaSingleton.Instance.PlayerList[winnerId].player_name} wins! What would you like to do?";
        win_popup.PopupCentered();
        GameLogic.Savepoint(NakamaSingleton.Instance.Point);
    }



    private void OnWinPopupConfirmed()
    {
        LoggerManager.Info("OK (Play Again) selected");
        RestartGame();
    }

    private void OnWinPopupCanceled()
    {
        LoggerManager.Info("Cancel (Go Back) selected");
        bgm.Stop();
        sfxm.Stop();
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }


    private void RestartGame()
    {
        LoggerManager.Info("Restarting game...");

        var TutorialplayScene = GD.Load<PackedScene>("res://scenes/Tutorialplay.tscn");
        var newGame = TutorialplayScene.Instantiate();

        GetTree().Root.AddChild(newGame);

        // SAFELY queue the old scene for deletion after the new one is added and running
        GetTree().CurrentScene.CallDeferred("free");

        // Set the new scene as current
        GetTree().CurrentScene = newGame;
    }

    public async Task<bool> AddTurn()
    {
        turnpass++;
        if (NakamaSingleton.Instance.CurrentTurn == NakamaSingleton.Instance.MainPlayerTurn)
        {
            StartTurnTimer();
            if (!PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn] && GameLogic.CheckCastle(playersHands[NakamaSingleton.Instance.MainPlayerTurn]))
            {
                ShowAutoMessage("You Can Castle.", 5000);
            }
        }

        if (NakamaSingleton.Instance.CurrentTurn < NakamaSingleton.Instance.NumberOfPlayers)
        {
            NakamaSingleton.Instance.CurrentTurn++;
            while (playersContainers[NakamaSingleton.Instance.CurrentTurn] == null)
            {
                NakamaSingleton.Instance.CurrentTurn++;
                if (NakamaSingleton.Instance.CurrentTurn > NakamaSingleton.Instance.NumberOfPlayers)
                    NakamaSingleton.Instance.CurrentTurn = 1;
            }
        }
        else
        {
            NakamaSingleton.Instance.CurrentTurn = 1;
        }

        if (NakamaSingleton.Instance.CurrentTurn == NakamaSingleton.Instance.MainPlayerTurn)
        {
            pickButton.Disabled = false;
            if (lastTableTile!= null)
                lastTableTile.Mainuser = true;
        }

        if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
        {
            if (lastTableTile != null)
                lastTableTile.Mainuser = true;
        }

        return true;

    }

    private async Task<bool> autoplay()
    {
        /////////////////////////////////////////////////////////////////////////////////  "SinglePlayer"
        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer")
        {
            int thisturn = NakamaSingleton.Instance.CurrentTurn;
            if (NakamaSingleton.Instance.CurrentTurn != 1)
            {
                var gs = new GameLogic.GameState();
                gs.Hand = playersHands[NakamaSingleton.Instance.CurrentTurn].ToList(); //Current Hand

                var gs1 = new GameLogic.GameState();
                gs1.Hand = playersHands[NakamaSingleton.Instance.CurrentTurn].ToList(); //Current hand + take tile from table
                gs1.Hand.Add(lastTableTile.Tileid);

                if (GameLogic.EvaluateState(gs) >= GameLogic.EvaluateState(gs1))
                {
                    l2.AddItem($"player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} draw {GameLogic.EvaluateState(gs)} vs take {GameLogic.EvaluateState(gs1)}");
                    DrawTile(NakamaSingleton.Instance.CurrentTurn);

                    if (PlayerCastleStatus[NakamaSingleton.Instance.CurrentTurn]) // Check Ai Self Win
                    {
                        if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.CurrentTurn]) == "WIN")
                        {
                            l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                            LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                            EndGame(NakamaSingleton.Instance.CurrentTurn);
                            return true;
                        }
                    }

                    if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn]) //Check player Draw
                    {
                        var x = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList();
                        x.Add(lastDrawnTile.Tileid);

                        if (GameLogic.WinCondition(x) == "WIN")
                        {
                            decisionMade = false;
                            takeDecision = false;
                            windec.DialogText = windec.DialogText + $" {lastDrawnTile.Tileid}";
                            windec.PopupCentered();

                            int timeoutMs = 50000;
                            int waitedMs = 0;
                            int pollInterval = 100;

                            while (!decisionMade && waitedMs < timeoutMs)
                            {
                                await Task.Delay(pollInterval);
                                waitedMs += pollInterval;
                            }

                            if (!decisionMade)
                            {
                                takeDecision = false;
                                decisionMade = true;
                                windec.Hide();
                            }

                            if (takeDecision)
                            {
                                l2.AddItem("Player chose to Take");
                                TakeTile(NakamaSingleton.Instance.MainPlayerTurn, lastDrawnTile.Tileid);
                            }
                            else
                            {
                                l2.AddItem("Player chose to Pass");
                                DrawTile(NakamaSingleton.Instance.MainPlayerTurn);
                            }
                        }
                    }
                    gs.Hand = playersHands[NakamaSingleton.Instance.CurrentTurn].ToList();
                    if (PlayerCastleStatus[NakamaSingleton.Instance.CurrentTurn]) // Check Ai Self Win
                    {
                        if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.CurrentTurn]) == "WIN")
                        {
                            l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                            LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                            EndGame(NakamaSingleton.Instance.CurrentTurn);
                            return true;
                        }
                    }
                    gs.Hand.Add(lastDrawnTile.Tileid);
                    DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs));
                }
                else
                {
                    OnTableTileClicked(lastTableTile);
                    if (PlayerCastleStatus[NakamaSingleton.Instance.CurrentTurn]) // Check Ai Self Win
                    {
                        if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.CurrentTurn]) == "WIN")
                        {
                            l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                            LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                            EndGame(NakamaSingleton.Instance.CurrentTurn);
                            return true;
                        }
                    }
                    DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs1));
                }

                if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn]) //Player Discard
                {
                    var x = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList();
                    if (lastTableTile != null)
                        x.Add(lastTableTile.Tileid);

                    if (GameLogic.WinCondition(x) == "WIN")
                    {
                        decisionMade = false;
                        takeDecision = false;
                        windec.DialogText = windec.DialogText + $" {GameLogic.TileName(lastTableTile.Tileid)}";
                        windec.PopupCentered();

                        int timeoutMs = 50000;
                        int waitedMs = 0;
                        int pollInterval = 100;

                        while (!decisionMade && waitedMs < timeoutMs)
                        {
                            await Task.Delay(pollInterval);
                            waitedMs += pollInterval;
                        }

                        if (!decisionMade)
                        {
                            takeDecision = false;
                            decisionMade = true;
                            windec.Hide();
                        }

                        if (takeDecision)
                        {
                            l2.AddItem("Player chose to Take and Win");
                            OnTableTileClicked(lastTableTile);
                        }
                        else
                        {
                            l2.AddItem("Player chose to Pass");
                        }
                    }
                }

                if (GameLogic.CheckCastle(playersHands[thisturn]) && !PlayerCastleStatus[thisturn])
                {
                    PlayerCastleStatus[thisturn] = true;
                    PlaySoundEffect(mp3castle);
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[thisturn].player_name} CASTLE !!!!");
                    ShowAutoMessage($"Player {NakamaSingleton.Instance.PlayerList[thisturn].player_name} CASTLE !!!!", 5000);
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[thisturn].player_name} CASTLE !!!!");
                }

            }
        }

        /////////////////////////////////////////////////////////////////////////////////  "Multiplayer"
        if (NakamaSingleton.Instance.Gamemode == "Multiplayer")
        {

        }

        return true;
    }

    public void StartTurnTimer()
    {
        elapsedMs = 0;
        remainingTurnTimeMs = 30_000;
        turnTimer.Start();
    }

    private int remainingTurnTimeMs = 30_000;
    private int elapsedTurnTimeMs = 0;

    private void OnTurnTick()
    {

        elapsedMs += 1000;
        remainingTurnTimeMs = Mathf.Max(0, remainingTurnTimeMs - 1000);

        int secondsLeft = remainingTurnTimeMs / 1000;
        LoggerManager.Info($"Elapsed: {elapsedMs} ms");
        timerview.Text = $"{remainingTurnTimeMs / 1000} s";

        if (NakamaSingleton.Instance.CurrentTurn != NakamaSingleton.Instance.MainPlayerTurn)
            turnTimer.Stop();

        if (remainingTurnTimeMs <= 0)
        {
            LoggerManager.Info("Turn timeout. Skipping turn.");
            turnTimer.Stop();
            SkipTurn();
        }
    }

    private void SkipTurn()
    {
        if(turnpass==1)
        {
            var c = new GameLogic.GameState();
            c.Hand = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList(); //Current Hand
            DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(c));
        }
            
        var gs = new GameLogic.GameState();
        gs.Hand = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList(); //Current Hand

        var gs1 = new GameLogic.GameState();
        gs1.Hand = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList(); //Current hand + take tile from table
        if(lastTableTile!=null)
            gs1.Hand.Add(lastTableTile.Tileid);

        if (GameLogic.EvaluateState(gs) >= GameLogic.EvaluateState(gs1))
        {
            l2.AddItem($"player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} draw {GameLogic.EvaluateState(gs)} vs take {GameLogic.EvaluateState(gs1)}");
            DrawTile(NakamaSingleton.Instance.MainPlayerTurn);

            if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.MainPlayerTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.CurrentTurn);
                }
            }

            for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
            {
                if (i == NakamaSingleton.Instance.MainPlayerTurn)
                    continue;

                if (PlayerCastleStatus[i])
                {
                    if (GameLogic.WinCondition(playersHands[i]) == "WIN")
                    {
                        l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        EndGame(i);
                    }
                }
            }

            gs.Hand.Add(lastDrawnTile.Tileid);
            DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs));
        }
        else
        {
            OnTableTileClicked(lastTableTile);

            if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.MainPlayerTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.CurrentTurn);
                }
            }

            for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
            {
                if (i == NakamaSingleton.Instance.MainPlayerTurn)
                    continue;

                if (PlayerCastleStatus[i])
                {
                    if (GameLogic.WinCondition(playersHands[i]) == "WIN")
                    {
                        l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        EndGame(i);
                    }
                }
            }

            DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs1));
        }

        

    }


    private void SortTiles(int playerid)
    {
        var container = playersContainers[playerid];

        var tiles = container.GetChildren().OfType<Tile>()
                            .OrderBy(tile => tile.Tileid)
                            .ToList();

        for (int i = 0; i < tiles.Count; i++)
        {
            container.MoveChild(tiles[i], i);
        }

        container.QueueSort(); // Ensure UI update
    }


    private Timer autoCloseTimer, turnTimer;
    private float remainingMs;
    private float elapsedMs;
    private float updateIntervalMs = 50f; // update every 50ms

    public void ShowAutoMessage(string message, int durationMs = 2000)
    {
        remainingMs = durationMs;
        elapsedMs = 0;

        // Lazy create the timer
        if (autoCloseTimer == null)
        {
            autoCloseTimer = new Timer();
            autoCloseTimer.OneShot = false;
            autoCloseTimer.WaitTime = updateIntervalMs / 1000f;
            autoCloseTimer.Timeout += () => UpdateCountdown(message);
            AddChild(autoCloseTimer);
        }

        // Hide OK button if it's AcceptDialog
        if (autoMessageBox is AcceptDialog dialog)
        {
            dialog.GetOkButton()?.Hide();
        }

        // Initial display
        UpdateCountdown(message);
        autoMessageBox.PopupCentered();
        autoCloseTimer.Start();
    }

    private void UpdateCountdown(string baseMessage)
    {
        elapsedMs += updateIntervalMs;
        int timeLeft = Mathf.Max(0, (int)(remainingMs - elapsedMs));

        string display = $"{baseMessage}\n";// Closing in {timeLeft} ms";

        if (autoMessageBox.HasNode("MessageLabel"))
        {
            var label = autoMessageBox.GetNode<Label>("MessageLabel");
            label.Text = display;
        }
        else
        {
            autoMessageBox.DialogText = display;
        }

        if (timeLeft <= 0)
        {
            autoMessageBox.Hide();
            autoCloseTimer.Stop();
        }
    }

    public async void PlaySoundEffect(string name, float duration = -1f)
    {
        var path = $"res://art/4_Color_Game/Music/{name}.mp3";
        var stream = GD.Load<AudioStream>(path);
        if (stream != null)
        {
            sfxm.Stream = stream;
            sfxm.Play();

            if (duration > 0 && duration < stream.GetLength())
            {
                await ToSignal(GetTree().CreateTimer(duration), "timeout");
                sfxm.Stop();
            }
        }
    }


}
