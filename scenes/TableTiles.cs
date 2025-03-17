using Godot;
using System;

public partial class TableTiles : Container
{
	// Track the tile being dragged
	private Tile _draggedTile = null;
	private Vector2 _dragOffset;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Connect the input event to the _on_gui_input method
		SetProcessInput(true);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Move the tile if it's being dragged
		if (_draggedTile != null)
		{
			var mousePosition = GetViewport().GetMousePosition();
			Vector2 newPosition = mousePosition - _dragOffset;

			// Ensure the tile stays inside the container bounds
			newPosition.X = Mathf.Clamp(newPosition.X, 0, GetRect().Size.X - _draggedTile.GetRect().Size.X);
			newPosition.Y = Mathf.Clamp(newPosition.Y, 0, GetRect().Size.Y - _draggedTile.GetRect().Size.Y);

			_draggedTile.Position = newPosition;
		}
	}

	// Detect when a mouse input happens (click or release)
	public void _on_gui_input(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.Pressed) // Mouse pressed
			{
				// Check if a tile is clicked
				foreach (Tile tile in GetChildren())
				{
					if (tile.GetRect().HasPoint(mouseEvent.Position))
					{
						_draggedTile = tile; // Start dragging the tile
						_dragOffset = mouseEvent.Position - tile.Position; // Calculate the offset from the tile's top-left corner
						break; // Exit the loop once the tile is found
					}
				}
			}
			else // Mouse released
			{
				// Stop dragging the tile
				_draggedTile = null;
			}
		}
	}
}
