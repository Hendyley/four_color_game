using Godot;
using System;
using System.Threading;

public partial class Tile : Control
{
	private TextureRect TileImage;
	private Vector2 _startPosition;
	private bool _isDragging = false;
    private bool _isDraggingTile = false;
    private const float DragThreshold = 20f; // Minimum movement to count as a drag
	private bool _isPressed = false; // To track if it's been pressed and released
	private bool _hasTriggered = false;
	private Vector2 _dragOffset;

	[Signal] // Declare the signal
	public delegate void TileClickedEventHandler(Tile tile);

	[Signal] // Declare the signal for table tile clicked
	public delegate void TableTileClickedEventHandler(Tile tile);

	public String Tileid{ get; set;}
 	public int Playerid{ get; set;}
	public String Tiletype{ get; set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// GD.Print("Tile _Ready called!");
		TileImage = (TextureRect) FindChild("TextureRect");
		TileImage = GetChild<TextureRect>(0);
        MouseFilter = MouseFilterEnum.Stop; // Ensure mouse events are captured
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
        if (inputEvent is InputEventMouseButton scrollEvent &&
            (scrollEvent.ButtonIndex == MouseButton.WheelUp || scrollEvent.ButtonIndex == MouseButton.WheelDown))
            return;

        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _startPosition = mouseEvent.Position;
                _dragOffset = mouseEvent.Position;
                _isDragging = false;
                _isPressed = true;
            }
            else
            {
                if (_isPressed && !_isDragging)
                    TileSelect(Tileid);

                // Reordering logic after dragging
                if (_isDraggingTile)
                {
                    Control container = GetParent<Control>();
                    int newIndex = 0;
                    float dropX = GlobalPosition.X;

                    for (int i = 0; i < container.GetChildCount(); i++)
                    {
                        Node child = container.GetChild(i);
                        if (child == this) continue;

                        Control tile = child as Control;
                        if (tile != null && dropX > tile.GlobalPosition.X)
                            newIndex++;
                    }

                    container.MoveChild(this, newIndex);
                }

                _isDragging = false;
                _isPressed = false;
                _isDraggingTile = false;
            }

        }
        else if (inputEvent is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed)
            {
                _startPosition = touchEvent.Position;
                _isDragging = false;
                _isPressed = true;
            }
            else
            {
                if (_isPressed && !_isDragging)
                    TileSelect(Tileid);

                // Reordering logic after dragging
                if (_isDraggingTile)
                {
                    Control container = GetParent<Control>();
                    int newIndex = 0;
                    float dropX = GlobalPosition.X;

                    for (int i = 0; i < container.GetChildCount(); i++)
                    {
                        Node child = container.GetChild(i);
                        if (child == this) continue;

                        Control tile = child as Control;
                        if (tile != null && dropX > tile.GlobalPosition.X)
                            newIndex++;
                    }

                    container.MoveChild(this, newIndex);
                    this.TopLevel = false;
                }

                _isDragging = false;
                _isPressed = false;
                _isDraggingTile = false;
            }

        }
        else if (_isPressed && inputEvent is InputEventMouseMotion mouseMotion)
        {
            if (!_isDragging && (_startPosition - mouseMotion.Position).Length() > DragThreshold)
            {
                _isDragging = true;
                _isDraggingTile = true;
            }

            if (_isDraggingTile)
            {
                Vector2 globalMousePos = GetViewport().GetMousePosition();
                Vector2 newGlobalPos = globalMousePos - _dragOffset;


                this.TopLevel = true;
                // Get the container rect boundaries in global space
                Control container = GetParent<Control>();
                Vector2 containerGlobalPos = container.GetGlobalPosition();
                Vector2 containerSize = container.Size;

                // Clamp the tile position within the container's global rect
                float clampedX = Mathf.Clamp(newGlobalPos.X, containerGlobalPos.X, containerGlobalPos.X + containerSize.X - Size.X);
                float clampedY = Mathf.Clamp(newGlobalPos.Y, containerGlobalPos.Y, containerGlobalPos.Y + containerSize.Y - Size.Y);

                GlobalPosition = new Vector2(clampedX, clampedY);
            }

        }
    }
}
