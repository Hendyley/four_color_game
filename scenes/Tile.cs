using Godot;
using System;
using System.Threading;

public partial class Tile : Control
{
	private TextureRect TileImage;
	private Vector2 _startPosition;
	private bool _isDragging = false;
	private const float DragThreshold = 10f; // Minimum movement to count as a drag
	private bool _isPressed = false; // To track if it's been pressed and released
	private bool _hasTriggered = false;

	[Signal] // Declare the signal
	public delegate void TileClickedEventHandler(Tile tile);

	[Signal] // Declare the signal for table tile clicked
	public delegate void TableTileClickedEventHandler(Tile tile);

	public String Tileid{
		get; set;
	}

 	public int Playerid{
		get; set;
	}

	public String Tiletype{
		get; set;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// GD.Print("Tile _Ready called!");

		TileImage = (TextureRect) FindChild("TextureRect");
		TileImage = GetChild<TextureRect>(0);

		// SetCard("C1_Green");
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void SetTile(int playerid, string value)
	{
		string imagePath = $"res://art/4_Color_Game/Chess/Removed_BG/{value}.png";
		Texture2D loadedTexture = (Texture2D) GD.Load(imagePath);
		TileImage.Texture = loadedTexture;
		//TileImage.Position = new Vector2(831,767);
		TileImage.Size = new Vector2(50, 200);
		Tileid = value;
		Playerid = playerid;
		Tiletype = "player";
		// GD.Print("Set = " + Tileid);
	}

	public void SetTableTile(string value)
	{
		string imagePath = $"res://art/4_Color_Game/Chess/Removed_BG/{value}.png";
		Texture2D loadedTexture = (Texture2D) GD.Load(imagePath);
		TileImage.Texture = loadedTexture;
		//TileImage.Position = new Vector2(831,767);
		TileImage.Size = new Vector2(50, 200);
		Tileid = value;
		Tiletype = "table";
		// GD.Print("Set = " + Tileid);
	}

	public void TileSelect(string value)
	{
		GD.Print("Selected "+value+" type "+Tiletype);
		switch(Tiletype)
			{
				case "player":
					EmitSignal("TileClicked",this);
					break;
				case "table":
					EmitSignal("TableTileClicked", this); 
					break;

			} 
	}

	

	private void _on_gui_input(InputEvent inputEvent)
	{
		// Check if it's a mouse event or a touch event
		if (inputEvent is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.Pressed)
			{
				_startPosition = mouseEvent.Position;
				_isDragging = false; // Reset dragging state
				_isPressed = true; // Mark it as pressed
			}
			// Detect release (only if pressed and not dragging)
			else if (!mouseEvent.Pressed && _isPressed && !_isDragging)
			{
				// GD.Print("Mouse says: " + Tileid);
				TileSelect(Tileid);
				_isPressed = false; // Reset after release
			}
		}
		else if (inputEvent is InputEventScreenTouch touchEvent)
		{
			// Detect press
			if (touchEvent.Pressed)
			{
				_startPosition = touchEvent.Position;
				_isDragging = false; // Reset dragging state
				_isPressed = true; // Mark it as pressed
			}
			// Detect release (only if pressed and not dragging)
			else if (!touchEvent.Pressed && _isPressed && !_isDragging)
			{
				// GD.Print("Screen says: " + Tileid);
				TileSelect(Tileid);
				_isPressed = false; // Reset after release
			}
		}

		// Detect dragging (mouse or touch)
		if (inputEvent is InputEventMouseMotion mouseMotion && (_startPosition - mouseMotion.Position).Length() > DragThreshold)
		{
			_isDragging = true;
		}
		else if (inputEvent is InputEventScreenDrag touchDrag && (_startPosition - touchDrag.Position).Length() > DragThreshold)
		{
			_isDragging = true;
		}
	}

}
