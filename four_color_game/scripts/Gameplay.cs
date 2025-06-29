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

public partial class Gameplay : Node
{
	[Export] public PackedScene tileScene;
	
	public List<string> deck = new List<string>();  

	private Dictionary<int, List<string>> playersHands = new Dictionary<int, List<string> >();
	private Dictionary<int, HBoxContainer> playersContainers = new Dictionary<int, HBoxContainer>();
	private Dictionary<int, Player> PlayerSeats = new Dictionary<int, Player>(); // S -> W -> N -> E
	private Dictionary<int, bool> PlayerCastleStatus = new Dictionary<int, bool>();

	private bool throwhand;
	public Godot.Container tabletilescontainer;
	private List<string> tabletiles = new();
	private Button pickButton, castleButton, backButton;
	//public PackedScene tile = (PackedScene)GD.Load("res://scenes/Tile.tscn");
	//private TextureRect currentturn;
	private Tile lastTableTile, lastDrawnTile, showstile;
	private Label deckcounterLabel, currentturnLabel, turnlabel, debuglb, debuglb2;
	private ItemList l2;

	private ConfirmationDialog windec, windec2;
    private bool decisionMade = false;
    private bool takeDecision = false;
    private AcceptDialog autoMessageBox;

    StyleBoxFlat highlightStyle = new StyleBoxFlat();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

        if (File.Exists("logs/app.log"))
        {
            File.Delete("logs/app.log");
        }

        tileScene = (PackedScene)ResourceLoader.Load("res://scenes/Tile.tscn");
        tabletilescontainer = (Godot.Container)FindChild("TableTiles");
        pickButton = (Button)FindChild("pickupbutton");
        castleButton = (Button)FindChild("castlebutton");
        backButton = (Button)FindChild("backbutton");

        deckcounterLabel = (Label)FindChild("DeckCounter");
        currentturnLabel = (Label)FindChild("CurrentTurn");
        turnlabel = (Label)FindChild("TurnLabel");
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
		autoMessageBox = (AcceptDialog)FindChild("windec_c");
		autoCloseTimer = (Timer)FindChild("Timer");
        autoCloseTimer.Timeout += () =>
        {
            autoMessageBox.Hide();
        };

        l2 = (ItemList)FindChild("l2");

		NakamaSingleton.Instance.Connect(nameof(NakamaSingleton.PlayerTileUpdate), new Callable(this, nameof(TileUpdate)));

        NakamaSingleton.Instance.CurrentTurn = 1;
        //currentturn = (TextureRect)FindChild($"Turn{NakamaSingleton.Instance.CurrentTurn}");
        //currentturn.Show();

		castleButton.Disabled = false;
		pickButton.Disabled = true;

		List<string> seatsvar;
		if(NakamaSingleton.Instance.NumberOfPlayers==2)
			seatsvar = new List<string>() { "S", "N" };
		else if(NakamaSingleton.Instance.NumberOfPlayers == 3)
			seatsvar = new List<string>() { "S", "W", "N" };
		else if (NakamaSingleton.Instance.NumberOfPlayers == 4)
			seatsvar = new List<string>() { "S", "W", "N", "E" };
		else
			seatsvar = new List<string>() { "S", "W", "N", "E" };

        List<int> turnsequence = NakamaSingleton.Instance.GetTurnOrder(NakamaSingleton.Instance.MainPlayer.player_turn, NakamaSingleton.Instance.NumberOfPlayers);
        PlayerSeats[NakamaSingleton.Instance.MainPlayerTurn] = NakamaSingleton.Instance.MainPlayer;

        for (int i = 0; i < seatsvar.Count; i++)
        {
			
            string containerName = $"HBOX_P_{seatsvar[i]}";
            HBoxContainer handContainer = (HBoxContainer)FindChild(containerName);

            playersHands[turnsequence[i]] = new List<string>();

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
		
        StartGame();

        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer")
        {
            for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
            {
				PlayerCastleStatus[i] = false;
                for (int j = 1; j <= 15; j++)
                {
                    DrawTile(i);
                }
                SortTiles(i);
            }
        }
        else if (NakamaSingleton.Instance.Gamemode == "MultiPlayer")
        {
            for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
            {
                PlayerCastleStatus[i] = false;
                for (int j = 1; j <= 15; j++)
                {
                    DrawTile(i);
                }
                SortTiles(i);
            }

        }

        //int first = new Random().Next(NakamaSingleton.Instance.CurrentTurn, NakamaSingleton.Instance.NumberOfPlayers);
        DrawTile(NakamaSingleton.Instance.CurrentTurn);
		throwhand = true;

	}

   

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		deckcounterLabel.Text = deck.Count.ToString();
        currentturnLabel.Text = NakamaSingleton.Instance.CurrentTurn.ToString();

		debuglb.Text = "";
		debuglb.Text = $"Tile P!: {playersHands[1].Count}\n";
		var sub = playersHands[1].ToList();
		sub.Sort();
		//playersHands[1].Sort();
		foreach (var v in sub)
		{
			debuglb.Text = debuglb.Text + v + " \n";
		}

		debuglb2.Text = "";
		debuglb2.Text = $"Tile P2 : {playersHands[2].Count}\n";
		sub = playersHands[2].ToList();
		sub.Sort();
		//playersHands[2].Sort();
		foreach (var v in sub)
		{
			debuglb2.Text = debuglb2.Text + v + " \n";
		}

		if (l2.ItemCount > 0)
        {
            l2.EnsureCurrentIsVisible(); // ensure selected is visible
            l2.Select(l2.ItemCount - 1); // select last item to trigger scroll
        }

        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer")
		{

            GameLogic.Deck = deck;
            GameLogic.Table = tabletiles;
            if (GameLogic.CheckCastle(playersHands[1]))
            {
                if (!PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
                    castleButton.GrabFocus();
                else
                    castleButton.ReleaseFocus();
            }

            if (NakamaSingleton.Instance.CurrentTurn != 1)
			{ 
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.CurrentTurn]) == "WIN")
				{
					turnlabel.Text = $"Player {NakamaSingleton.Instance.CurrentTurn} WIN";
					EndGame();
				}
			}
			else if (NakamaSingleton.Instance.CurrentTurn == 1)
			{
				if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
				{
					if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.MainPlayer.player_turn]) == "WIN")
					{
						turnlabel.Text = $"Player {NakamaSingleton.Instance.MainPlayer.player_turn} WIN";
						EndGame();
					}
				}
			}
		}

	}

	private async void TileUpdate(string tile)
	{
        var array = new string[] { NakamaSingleton.Instance.MainPlayer.player_turn.ToString(), tile };
        string arrayjson = JsonConvert.SerializeObject(array);
        var data = Encoding.UTF8.GetBytes(arrayjson);
        await NakamaSingleton.Instance.Socket.SendMatchStateAsync(NakamaSingleton.Instance.Match.Id, 4, data);
    }

	private void StartGame()
	{
		LoggerManager.Info("Game Start");

		deck = CreateDeck();
		ShuffleDeck();
		for(int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++){
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
			  x=$"C{i}";
			  newDeck.Add(x);
			  newDeck.Add(x+"_Green");
			  newDeck.Add(x+"_Red");
			  newDeck.Add(x+"_Yellow");
		  }
		}
		LoggerManager.Info("Amount in the Deck: "+newDeck.Count);

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

	private void DrawTile(int playerid)
	{
		if (lastTableTile != null)
			lastTableTile.CallDeferred("UpdateHighlightVisual", false);
        LoggerManager.Info($"Player {playerid} Attempting to draw a card...");

		if (deck.Count > 0)
		{
			string tilevalue = deck[0];
			deck.RemoveAt(0);
			playersHands[playerid].Add(tilevalue);
			LoggerManager.Info($"Player {playerid} Drawn Card Value: " + tilevalue);
			
			Tile newTile = (Tile)tileScene.Instantiate();
			playersContainers[playerid].AddChild(newTile);
			newTile.Tileid = tilevalue;

			// Call SetCard deferred to ensure TileImage is initialized first
			newTile.CallDeferred("SetTile", playerid ,tilevalue);
			newTile.Connect("TileClicked", new Callable(this,"OnTileClicked"));

			if (playersContainers[playerid].Name != "HBOX_P_S")
				newTile.CallDeferred("SetCover", true);
			else
				newTile.Mainuser = true;

			lastDrawnTile = newTile;

			throwhand = true;
		}
		else
		{
			LoggerManager.Info("Deck is empty! No card drawn.");
		}
	}

	private void TakeTile(int playerid=1, string tilevalue="C1")
	{
		lastTableTile.CallDeferred("UpdateHighlightVisual", false);
        //SortTiles(NakamaSingleton.Instance.CurrentTurn);

        playersHands[playerid].Add(tilevalue);
		Tile newTile = (Tile)tileScene.Instantiate();
		newTile.Tileid = tilevalue;
		playersContainers[playerid].AddChild(newTile);
		
		LoggerManager.Info($"Player {playerid} take tile: " + tilevalue);
		newTile.CallDeferred("SetTile", playerid ,tilevalue);
		newTile.Connect("TileClicked", new Callable(this,"OnTileClicked"));

        if (playersContainers[playerid].Name != "HBOX_P_S")
            newTile.CallDeferred("SetCover", true);
        else
            newTile.Mainuser = true;

        lastTableTile = null;

		tabletiles.Remove(tilevalue);
		pickButton.Disabled = true;
		throwhand = true;

	}

	public void DiscardTile(int playerid, string tilevalue)
	{
		if(!throwhand)
			return;

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
                    AddtoTables(tilevalue);
                    break;
                }
            }
        }

        //SortTiles(playerid);
        //await ToSignal(GetTree(), "idle_frame");
        AddTurn(); ////////////////////////////////////////////////////////////////////////////// Turn Change here
		return;
	}

	private void AddtoTables(string tilevalue)
	{
		LoggerManager.Info($"Add {tilevalue} to the table ");

		if (lastTableTile != null)
		{
			lastTableTile.Disconnect("TableTileClicked", new Callable(this, "OnTableTileClicked"));
		}

		Tile newTile = (Tile)tileScene.Instantiate();
        tabletilescontainer.AddChild(newTile);
        tabletiles.Add(tilevalue);

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
	}

	private void OnTileClicked(Tile clickedTile)
	{
		if(clickedTile.Tileid.Contains("C7"))
		{
			LoggerManager.Info("Cannot discard King tile !!!");
			return;
		}
		LoggerManager.Info($"Player {clickedTile.Playerid} Discarding Tile: " + clickedTile.Tileid);
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
		LoggerManager.Info("castle button pressed");
        if (!GameLogic.CheckCastle(playersHands[1]))
        {
			turnlabel.Text = "False Castle";
            castleButton.ReleaseFocus();
            return;
        }
        castleButton.ReleaseFocus();
        PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn] = true;
		l2.AddItem("CASTLE!!!");
	}

	private void _on_back_button_pressed()
	{
		LoggerManager.Info("back button pressed");
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}

	private void _on_sortbutton_pressed()
	{
		SortTiles(NakamaSingleton.Instance.MainPlayerTurn);
        
    }

    private void EndGame()
	{
		pickButton.Disabled = true;
		castleButton.Disabled = true;
	}

	private void _on_control_gui_input(InputEvent inputEvent)
	{
	}	

	public async Task AddTurn()
	{

        if (NakamaSingleton.Instance.CurrentTurn == NakamaSingleton.Instance.MainPlayerTurn)
        {
            if(!PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn] && GameLogic.CheckCastle(playersHands[NakamaSingleton.Instance.MainPlayerTurn]) )
			{
                ShowAutoMessage("You Can Castle.", 2500);
            }
        }

        if (NakamaSingleton.Instance.CurrentTurn < NakamaSingleton.Instance.NumberOfPlayers)
        {
			NakamaSingleton.Instance.CurrentTurn++;
		} 
		else 
		{
            NakamaSingleton.Instance.CurrentTurn = 1;
		}

		if (NakamaSingleton.Instance.CurrentTurn == NakamaSingleton.Instance.MainPlayerTurn)
		{
            pickButton.Disabled = false;
            lastTableTile.Mainuser = true;
        }

		if(PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
		{
			lastTableTile.Mainuser = true;
		}

		if(NakamaSingleton.Instance.Gamemode == "SinglePlayer")
		{
            if (NakamaSingleton.Instance.CurrentTurn != 1)
            {
                var gs = new GameLogic.GameState();
                gs.Hand = playersHands[NakamaSingleton.Instance.CurrentTurn].ToList(); //Current Hand

                var gs1 = new GameLogic.GameState();
                gs1.Hand = playersHands[NakamaSingleton.Instance.CurrentTurn].ToList(); //Current hand + take tile from table
                gs1.Hand.Add(lastTableTile.Name);

                if (GameLogic.EvaluateState(gs) >= GameLogic.EvaluateState(gs1))
                {
                    l2.AddItem($"player {NakamaSingleton.Instance.CurrentTurn} Previous Higher {GameLogic.EvaluateState(gs)} vs {GameLogic.EvaluateState(gs1)}");
                    DrawTile(NakamaSingleton.Instance.CurrentTurn);
					
                    if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn]) //Check player Draw
					{
                        var x = playersHands[1].ToList();
                        x.Add(lastDrawnTile.Tileid);

						if( GameLogic.WinCondition(x) == "WIN" )
						{
                            decisionMade = false;
                            takeDecision = false;
							windec.DialogText = windec.DialogText + $" {lastDrawnTile.Tileid}";
                            windec.PopupCentered();

                            int timeoutMs = 7000;
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
                            }
                        }

                        
                    }
                    gs.Hand = playersHands[NakamaSingleton.Instance.CurrentTurn].ToList();
                    l2.AddItem($"player {NakamaSingleton.Instance.CurrentTurn} Discard {GameLogic.MAX_AI_DISCARD(gs)}");
                    DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs));
                }
                else
                {
                    l2.AddItem($"player {NakamaSingleton.Instance.CurrentTurn} take tile {lastTableTile.Name}");
                    TakeTile(NakamaSingleton.Instance.CurrentTurn, lastTableTile.Name);
                    l2.AddItem($"player {NakamaSingleton.Instance.CurrentTurn} Discard {GameLogic.MAX_AI_DISCARD(gs1)}");
                    DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs1));
                }

				if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn]) //Player Discard
				{
                    var x = playersHands[1].ToList();
					x.Add(lastTableTile.Tileid);

					if (GameLogic.WinCondition(x) == "WIN")
					{
                        decisionMade = false;
                        takeDecision = false;
                        windec.DialogText = windec.DialogText + $" {lastTableTile.Tileid}";
                        windec.PopupCentered();

                        int timeoutMs = 7000;
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
                            TakeTile(NakamaSingleton.Instance.MainPlayerTurn, lastTableTile.Tileid);
                        }
                        else
                        {
                            l2.AddItem("Player chose to Pass");
                        }
                    }
				}

			}
        }
	}

	private void SortTiles(int playerid)
	{
		var container = playersContainers[playerid];

		// Get and sort all Tile nodes inside the container by their Tileid
		var tiles = container.GetChildren().OfType<Tile>()
							.OrderBy(tile => tile.Tileid)
							.ToList();

		// Move each sorted tile to its correct position
		for (int i = 0; i < tiles.Count; i++)
		{
			container.MoveChild(tiles[i], i);
		}

		container.QueueSort(); // Ensure UI update
	}


    private Timer autoCloseTimer;
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

        string display = $"{baseMessage}\nClosing in {timeLeft} ms";

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
}
