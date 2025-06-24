using Godot;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

public partial class Tile : Control
{
	private TextureRect TileImage;
	private Vector2 _startPosition;
    private bool _isHighlighted = false;
    private ColorRect highlightOverlay;
    private bool _isDragging = false;
    private bool _isDraggingTile = false;
    private const float DragThreshold = 20f; // Minimum movement to count as a drag
	private bool _isPressed = false; // To track if it's been pressed and released
	private bool _hasTriggered = false;
	private Vector2 _dragOffset;
    private bool cover = false;

	[Signal] // Declare the signal
	public delegate void TileClickedEventHandler(Tile tile);

	[Signal] // Declare the signal for table tile clicked
	public delegate void TableTileClickedEventHandler(Tile tile);

	public String Tileid { get; set;}
 	public int Playerid { get; set;}
	public String Tiletype { get; set;}
    public bool Mainuser { get; set; } = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        TileImage = GetNode<TextureRect>("TextureRect");
        highlightOverlay = GetNode<ColorRect>("HighlightOverlay");
        highlightOverlay.Visible = false;

        MouseFilter = MouseFilterEnum.Stop;
    }

    public void UpdateHighlightVisual(bool _isHighlighted)
    {
        if (highlightOverlay != null)
        {
            this.highlightOverlay.Visible = _isHighlighted;
        }
    }

    public void SetCover(bool cover)
    {
        this.cover = cover;
        if (!cover)
        {
            string imagePath = $"res://art/4_Color_Game/Chess/Removed_BG/{this.Tileid}.png";
            Texture2D loadedTexture = (Texture2D)GD.Load(imagePath);
            TileImage.Texture = loadedTexture;
        }
        else
        {
            string imagePath = $"res://art/4_Color_Game/Chess/Removed_BG/Covers.png";
            Texture2D loadedTexture = (Texture2D)GD.Load(imagePath);
            TileImage.Texture = loadedTexture;
        }

    }

    public void SetTile(int playerid, string value)
	{
		string imagePath = $"res://art/4_Color_Game/Chess/Removed_BG/{value}.png";
		Texture2D loadedTexture = (Texture2D) GD.Load(imagePath);
		TileImage.Texture = loadedTexture;
		TileImage.Size = new Vector2(50, 200);
		Tileid = value;
		Playerid = playerid;
		Tiletype = "player";
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
        if(!Mainuser)
            return;

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

                if (_isDraggingTile)
                {
                    Control container = GetParent<Control>();
                    int newIndex = 0;
                    float dropX = GlobalPosition.X;

                    for (int i = 0; i < container.GetChildCount(); i++)
                    {
                        if (container.GetChild(i) is Control tile && tile != this && dropX > tile.GlobalPosition.X)
                            newIndex++;
                    }

                    // Ensure the tile is still inside the container
                    if (IsInsideTree() && container.HasNode(this.GetPath()))
                    {
                        container.MoveChild(this, newIndex);
                    }

                    // Reset back to layout mode
                    TopLevel = false;
                    SetPosition(Vector2.Zero); // Reset local position; layout will override it
                    QueueRedraw(); // Optional: forces a redraw just in case
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

                if (_isDraggingTile)
                {
                    Control container = GetParent<Control>();
                    int newIndex = 0;
                    float dropX = GlobalPosition.X;

                    for (int i = 0; i < container.GetChildCount(); i++)
                    {
                        if (container.GetChild(i) is Control tile && tile != this && dropX > tile.GlobalPosition.X)
                            newIndex++;
                    }

                    if (IsInsideTree() && container.HasNode(this.GetPath()))
                    {
                        container.MoveChild(this, newIndex);
                    }

                    TopLevel = false;
                    SetPosition(Vector2.Zero); // Reset local position; layout will override it
                    QueueRedraw(); // Optional: forces a redraw just in case
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

                Control container = GetParent<Control>();
                Vector2 containerGlobalPos = container.GetGlobalPosition();
                Vector2 containerSize = container.Size;

                float clampedX = Mathf.Clamp(newGlobalPos.X, containerGlobalPos.X, containerGlobalPos.X + containerSize.X - Size.X);
                float clampedY = Mathf.Clamp(newGlobalPos.Y, containerGlobalPos.Y, containerGlobalPos.Y + containerSize.Y - Size.Y);

                TopLevel = true;
                GlobalPosition = new Vector2(clampedX, clampedY);
            }
        }
    }

}
