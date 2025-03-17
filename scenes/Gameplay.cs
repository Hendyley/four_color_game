using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public partial class Gameplay : Node
{
	[Export] public PackedScene tileScene;

	public int numberOfplayers; 

	public List<string> deck = new List<string>();  

	private Dictionary<int, List<string>> playersHands = new Dictionary<int, List<string> >();

	private Dictionary<int, HBoxContainer> playersContainers = new Dictionary<int, HBoxContainer>();

	private Dictionary<int, string> playersnames = new Dictionary<int, string>();


	public Container tabletiles;
	private Label deckcounterLabel;
	private Button pickButton;
	private Button castleButton;
	private Button backButton;

	// public PackedScene tile = (PackedScene)GD.Load("res://scenes/Tile.tscn");


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		numberOfplayers = (int)GetMeta("numberofplayers",2);
		GD.Print("NOP "+numberOfplayers.ToString());

		tileScene = (PackedScene)ResourceLoader.Load("res://scenes/Tile.tscn");

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
		}
		
		int first = new Random().Next(1, numberOfplayers);
		DrawTile(first);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		deckcounterLabel.Text = deck.Count.ToString();
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

	private void CheckDeck()
	{
		GD.Print("Amount in the Deck: "+ deck.Count);

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

			// Call SetCard deferred to ensure TileImage is initialized first
			newTile.CallDeferred("SetTile", playerid ,tilevalue);
			newTile.Connect("TileClicked", new Callable(this,"OnTileClicked"));
			
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
		playersContainers[playerid].AddChild(newTile);
		
		GD.Print($"Player {playerid} Drawn Card Value: " + tilevalue);
		newTile.CallDeferred("SetTile", playerid ,tilevalue);
		newTile.Connect("TileClicked", new Callable(this,"OnTileClicked"));

	}


	public void DiscardTile(int playerid, string tilevalue){
		
		foreach (Node child in playersContainers[playerid].GetChildren())
		{
			// GD.Print("Checking child: " + child.Name);

			// Check if the child is a Tile node
			if (child is Tile tile)
			{
				// GD.Print("Tile ID: " + tile.Tileid);  // Debugging output

				if (tile.Tileid == tilevalue)  
				{
					playersContainers[playerid].RemoveChild(tile);
					tile.QueueFree();
					playersHands[playerid].Remove(tilevalue);
					// GD.Print("Removed card: " + tileValue);
					AddtoTables(tilevalue);
					return;
				}
			}
			else
			{
				GD.Print("Warning: Child is not a Tile node!");
			}
		}
	}

	private void AddtoTables(string tilevalue){
		GD.Print($"Add {tilevalue} to the table ");
		Tile newTile = (Tile)tileScene.Instantiate();
		tabletiles.AddChild(newTile);
		newTile.CallDeferred("SetTableTile", tilevalue);
		newTile.Connect("TableTileClicked", new Callable(this,"OnTableTileClicked"));

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
		GD.Print($"Player {clickedTile.Playerid} Discarding Tile: " + clickedTile.Tileid);
		DiscardTile(clickedTile.Playerid, clickedTile.Tileid);
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
		TakeTile(1, clickedTile.Tileid);
	}


	



	private void _on_pickupbutton_pressed()
	{
		GD.Print("pickup button pressed");
		DrawTile(1);
	}

	private void _on_castlebutton_pressed()
	{
		GD.Print("castle button pressed");
		DrawTile(2);
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

	

}
