using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

public partial class Gameplay : Node
{
	[Export] public PackedScene tileScene;

	public int numberOfplayers; 

	public List<string> deck = new List<string>();  

	private Dictionary<int, List<string>> playersHands = new Dictionary<int, List<string> >();

	private Dictionary<int, HBoxContainer> playersContainers = new Dictionary<int, HBoxContainer>();

	private Dictionary<int, string> playersnames = new Dictionary<int, string>();


	public bool castlestatus;
	public Container tabletiles;
	private Label deckcounterLabel;
	private Button pickButton;
	private Button castleButton;
	private Button backButton;

	// public PackedScene tile = (PackedScene)GD.Load("res://scenes/Tile.tscn");
	public int current_turn;
	private TextureRect currentturn;
	private Tile lastTableTile;




	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		numberOfplayers = (int)GetMeta("numberofplayers",2);
		GD.Print("NOP "+numberOfplayers.ToString());

		tileScene = (PackedScene)ResourceLoader.Load("res://scenes/Tile.tscn");

		current_turn = 1;
		currentturn = (TextureRect) FindChild($"Turn{current_turn}");
		currentturn.Show();

		castlestatus = false;

		for (int i = 1; i <= numberOfplayers; i++)
		{
			string containerName = $"HBOX_P{i}";
			HBoxContainer handContainer = (HBoxContainer) FindChild(containerName);
			playersHands[i] = new List<string>();

			if (handContainer != null)
			{
				playersContainers[i] = handContainer;
				GD.Print($"Player {i} hand assigned to {containerName}");
			}
			else
			{
				GD.PrintErr($"Could not find container: {containerName}");
			}
		}

		tabletiles = (Container) FindChild("TableTiles");
		deckcounterLabel = (Label) FindChild("DeckCounter");
		pickButton = (Button) GetNode("pickupbutton");
		castleButton = (Button) GetNode("castlebutton");
		backButton = (Button) GetNode("backbutton");
		
		StartGame();

		for (int i=1; i<=numberOfplayers; i++){
			for(int j=1; j <= 15; j++){
				DrawTile(i);
			}
			SortTiles(i);
		}

		int first = new Random().Next(current_turn, numberOfplayers);
		DrawTile(first);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		deckcounterLabel.Text = deck.Count.ToString();
		// GD.Print($"Player 1 win is {WinCondition(1)}");
		
	}

	private void StartGame()
	{
		GD.Print("Game Start");
		
		deck = CreateDeck();
		ShuffleDeck();
		for(int i = 1; i <= numberOfplayers; i++){
			playersHands[i].Clear();
			foreach (Node child in playersContainers[i].GetChildren())
				child.QueueFree();
		}

	}

	private List<string> CreateDeck()
	{
		GD.Print("Create Deck");
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
		//newDeck.ForEach(GD.Print);
		GD.Print("Amount in the Deck: "+newDeck.Count);

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
		// PackedScene packedScene = GD.Load<PackedScene>("res://scenes/Tile.tscn");
		GD.Print($"Player {playerid} Attempting to draw a card...");

		if (deck.Count > 0)
		{
			string tilevalue = deck[0];
			deck.RemoveAt(0);
			playersHands[playerid].Add(tilevalue);
			GD.Print($"Player {playerid} Drawn Card Value: " + tilevalue);

			// Tile newCard = packedScene.Instantiate<Tile>();
			// Tile newTile = tileScene.Instantiate<Tile>();
			// newTile.GlobalPosition = new Vector2(174,500);
			// CallDeferred("add_child", newTile);
			
			Tile newTile = (Tile)tileScene.Instantiate();
			playersContainers[playerid].AddChild(newTile);
			newTile.Tileid = tilevalue;

			// Call SetCard deferred to ensure TileImage is initialized first
			newTile.CallDeferred("SetTile", playerid ,tilevalue);
			newTile.Connect("TileClicked", new Callable(this,"OnTileClicked"));

			if(castlestatus == true){
				GD.Print($"Castle detected!!!!    Drawn value is {tilevalue}");
				for(int i=1;i<=numberOfplayers;i++){
					GD.Print($"Player {i} win is {WinCondition(i,tilevalue)}");;
				}
			}

			SortTiles(current_turn);
			
		}
		else
		{
			GD.Print("Deck is empty! No card drawn.");
		}
	}

	private void TakeTile(int playerid=1, string tilevalue="C1")
	{
		playersHands[playerid].Add(tilevalue);
		Tile newTile = (Tile)tileScene.Instantiate();
		newTile.Tileid = tilevalue;
		playersContainers[playerid].AddChild(newTile);
		
		GD.Print($"Player {playerid} take tile: " + tilevalue);
		newTile.CallDeferred("SetTile", playerid ,tilevalue);
		newTile.Connect("TileClicked", new Callable(this,"OnTileClicked"));
		lastTableTile = null;
		SortTiles(current_turn);

	}


	public void DiscardTile(int playerid, string tilevalue){

		
		foreach (Node child in playersContainers[playerid].GetChildren())
		{
			if (child is Tile tile)
			{
		
				if (tile.Tileid == tilevalue)  
				{
					playersContainers[playerid].RemoveChild(tile);
					tile.QueueFree();
					playersHands[playerid].Remove(tilevalue);
					// GD.Print("Removed card: " + tileValue);
					AddtoTables(tilevalue);
					break;
				}
			}
			else
			{
				GD.Print("Warning: Child is not a Tile node!");
			}
		}
		
		return;
	}

	private void AddtoTables(string tilevalue)
	{
		GD.Print($"Add {tilevalue} to the table ");

		if (lastTableTile != null)
		{
			lastTableTile.Disconnect("TableTileClicked", new Callable(this, "OnTableTileClicked"));
		}

		Tile newTile = (Tile)tileScene.Instantiate();
		tabletiles.AddChild(newTile);
		newTile.CallDeferred("SetTableTile", tilevalue);
		newTile.Connect("TableTileClicked", new Callable(this,"OnTableTileClicked"));
		lastTableTile = newTile;


		Random rand = new Random();

		// Get the size of the container (tabletiles)
		Vector2 containerSize = tabletiles.GetRect().Size; // Correct way to get container size

		// Get the size of the new tile
		Vector2 tileSize = newTile.GetRect().Size; // Correct way to get tile size

		// Random position, ensuring the tile stays inside the container
		float randomX = (float)(rand.NextDouble() * (containerSize.X - tileSize.X));
		float randomY = (float)(rand.NextDouble() * (containerSize.Y - tileSize.Y));

		// Set the position of the tile
		newTile.Position = new Vector2(randomX, randomY);

		// Randomize rotation between 0 and 360 degrees
		float randomRotation = (float)(rand.NextDouble() * 360); // Rotation in degrees
		newTile.RotationDegrees = randomRotation;


	}

	private void OnTileClicked(Tile clickedTile)
	{
		if(clickedTile.Tileid.Contains("C5"))
		{
			GD.Print("Cannot discard King tile !!!");
			return;
		}
		GD.Print($"Player {clickedTile.Playerid} Discarding Tile: " + clickedTile.Tileid);
		DiscardTile(clickedTile.Playerid, clickedTile.Tileid);
		AddTurn(); ////////////////////////////////////////////////////////////////////////////// Turn Change here
		GD.Print($"Player {current_turn} win is {WinCondition(current_turn,clickedTile.Tileid)}");
	}

	private void OnTableTileClicked(Tile clickedTile)
	{
		foreach (Node child in tabletiles.GetChildren())
		{
			// GD.Print("Checking child: " + child.Name);

			// Check if the child is a Tile node
			if (child is Tile tile)
			{
				// GD.Print("Tile ID: " + tile.Tileid);  // Debugging output

				if (tile.Tileid == clickedTile.Tileid)  
				{
					tabletiles.RemoveChild(tile);
					tile.QueueFree(); 
				}
			}
			else
			{
				GD.Print("Warning: Child is not a Tile node!");
			}
		}
		GD.Print($"Player {clickedTile.Playerid} Taking Tile: " + clickedTile.Tileid);
		TakeTile(current_turn, clickedTile.Tileid);
	}


	



	private void _on_pickupbutton_pressed()
	{
		GD.Print("pickup button pressed");
		DrawTile(current_turn);
	}

	private void _on_castlebutton_pressed()
	{
		GD.Print("castle button pressed");
		castlestatus = !castlestatus;
		DrawTile(current_turn);
	}

	private void _on_back_button_pressed()
	{
		GD.Print("back button pressed");
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}

	private void EndGame()
	{
		pickButton.Disabled = true;
		castleButton.Disabled = true;
	}

	private void _on_control_gui_input(InputEvent inputEvent){

	}

	////////////////// Game Winning Conditions
	public string WinCondition(int x, string thistile="")
	{
		if(thistile!=""){
			playersHands[x].Add(thistile);
		}

		List<string> focustile = new List<string>(playersHands[x]);
		List<List<string>> H = GetHonourSets(focustile);
		List<List<string>> CH = GetCombinations(H);

		if(CH.Count == 0)
		{
			return "No Honour";
		}

		foreach (var h in CH)
		{
			focustile = new List<string>(playersHands[x]);
			Console.WriteLine("Combination : " + String.Join(",", h));
			foreach (var item in h)
			{
				focustile.Remove(item);
			}
			
			Console.WriteLine("Remainers : "+String.Join(",",focustile));
			
			if (CheckColorSets(focustile)) { return "WIN"; }
		}

		return "x";
	}

	public bool CheckColorSets(List<string> remainer)
	{
		if (remainer.Count == 0)
		{
			return true;
		}

		//Remove Kings
		remainer.RemoveAll(item => item.Contains("C5"));

		//Check C1
		List<string> C1List = new List<string> { "C1", "C1_Green", "C1_Red", "C1_Yellow" };
		if (RemovesetsItems(ref remainer, C1List)) { return CheckColorSets(remainer); }
		//Check C2
		List<string> C2List = new List<string> { "C2", "C2_Green", "C2_Red", "C2_Yellow" };
		if (RemovesetsItems(ref remainer, C2List)) { return CheckColorSets(remainer); }
		//Check C3
		List<string> C3List = new List<string> { "C3", "C3_Green", "C3_Red", "C3_Yellow" };
		if (RemovesetsItems(ref remainer, C3List)) { return CheckColorSets(remainer); }
		//Check C4
		List<string> C4List = new List<string> { "C4", "C4_Green", "C4_Red", "C4_Yellow" };
		if (RemovesetsItems(ref remainer, C4List)) { return CheckColorSets(remainer);}
		//Check C6
		List<string> C6List = new List<string> { "C6", "C6_Green", "C6_Red", "C6_Yellow" };
		if (RemovesetsItems(ref remainer, C6List)) { return CheckColorSets(remainer); }
		//Check C7
		List<string> C7List = new List<string> { "C7", "C7_Green", "C7_Red", "C7_Yellow" };
		if (RemovesetsItems(ref remainer, C7List)) { return CheckColorSets(remainer); }

		
		return false;
	}

	static bool RemovesetsItems(ref List<string> v, List<string> presetList)
	{
		// Count occurrences of each matching item in v
		var matchCounts = v.GroupBy(x => x)
						   .Where(g => presetList.Contains(g.Key))
						   .ToDictionary(g => g.Key, g => g.Count());

		if (matchCounts.Count >= 3)
		{
			foreach (var key in matchCounts.Keys.ToList())
			{
				v.Remove(key); 
			}
			return true;
		} else {  
			return false; 
		}

	}


	// Honor sets
	private List<List<string>> GetHonourSets(List<string> tile)
	{
		List<List<string>> honourSets = new List<List<string>>();
		List<List<string>> possibleHonours = new List<List<string>>()
		{
			new List<string> { "C2", "C5", "C6" },  // KQB
			new List<string> { "C2_Green", "C5_Green", "C6_Green" },
			new List<string> { "C2_Yellow", "C5_Yellow", "C6_Yellow" },
			new List<string> { "C2_Red", "C5_Red", "C6_Red" },

			new List<string> { "C1", "C3", "C4" },  // HRC
			new List<string> { "C1_Green", "C3_Green", "C4_Green" },
			new List<string> { "C1_Yellow", "C3_Yellow", "C4_Yellow" },
			new List<string> { "C1_Red", "C3_Red", "C4_Red" },

			new List<string> { "C6", "C6_Green", "C6_Red", "C6_Yellow" },  // All C6
			new List<string> { "C6", "C6_Green", "C6_Red" },
			new List<string> { "C6", "C6_Green", "C6_Yellow" },
			new List<string> { "C6", "C6_Red", "C6_Yellow" },
			new List<string> { "C6_Green", "C6_Red", "C6_Yellow" },
		};

		List<string> TrackTiles = new List<string>(tile);

		foreach (var honour in possibleHonours)
		{
			while (honour.All(TrackTiles.Contains))
			{
				honourSets.Add(new List<string>(honour));

				foreach (var item in honour)
				{
					TrackTiles.Remove(item);
				}
			}
		}

		return honourSets;
	}

	public static List<List<string>> GetCombinations(List<List<string>> lists)
	{
		List<List<string>> result = new List<List<string>>();

		for (int i = 1; i <= lists.Count; i++)
		{
			var subsets = GetSubsets(lists, i);
			result.AddRange(subsets);
		}

		return result;
	}

	public static List<List<string>> GetSubsets(List<List<string>> lists, int subsetSize)
	{
		var result = new List<List<string>>();
		var indices = new int[subsetSize];

		for (int i = 0; i < subsetSize; i++)
		{
			indices[i] = i;
		}

		while (indices[0] <= lists.Count - subsetSize)
		{
			var subset = new List<string>();
			foreach (var index in indices)
			{
				subset.AddRange(lists[index]);
			}
			result.Add(subset);

			int i = subsetSize - 1;
			while (i >= 0 && indices[i] == lists.Count - subsetSize + i)
			{
				i--;
			}
			if (i >= 0)
			{
				indices[i]++;
				for (int j = i + 1; j < subsetSize; j++)
				{
					indices[j] = indices[j - 1] + 1;
				}
			}
			else
			{
				break;
			}
		}

		return result;
	}
	///////////////////////////////////////////////////////////////////////////////////

	public void AddTurn(){

		currentturn = (TextureRect) FindChild($"Turn{current_turn}");
		currentturn.Hide();
		if(current_turn < numberOfplayers){
			current_turn++;
		} else {
			current_turn = 1;
		}
		currentturn = (TextureRect) FindChild($"Turn{current_turn}");
		currentturn.Show();
	}

	private void SortTiles(int playerid)
	{
		var container = playersContainers[playerid];

		// Get and sort all Tile nodes inside the container by their Tileid
		var tiles = container.GetChildren().OfType<Tile>()
							.OrderBy(tile => tile.Tileid)
							.ToList();

		// Print the sorted Tileids to debug
		// GD.Print("Sorted Tile IDs:");
		// foreach (var tile in tiles)
		// {
		// 	GD.Print(tile.Tileid);
		// }

		// Move each sorted tile to its correct position
		for (int i = 0; i < tiles.Count; i++)
		{
			container.MoveChild(tiles[i], i);
		}

		container.QueueSort(); // Ensure UI update
	}
	

}
