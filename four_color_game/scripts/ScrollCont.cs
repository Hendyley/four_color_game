using Godot;
using System;

public partial class ScrollCont : ScrollContainer
{
	private bool isDragging = false;
	private Vector2 lastMousePos;

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButtonEvent)
		{
			if (mouseButtonEvent.ButtonIndex == MouseButton.Left && GetGlobalRect().HasPoint(mouseButtonEvent.Position))
			{
				isDragging = mouseButtonEvent.Pressed;
				lastMousePos = mouseButtonEvent.Position;
			}
		}
		else if (@event is InputEventMouseMotion mouseMotionEvent && isDragging)
		{
			if (GetGlobalRect().HasPoint(mouseMotionEvent.Position)) // Ensure only this container scrolls
			{
				Vector2 delta = mouseMotionEvent.Position - lastMousePos;
				ScrollHorizontal -= (int)delta.X;
				ScrollVertical -= (int)delta.Y;
				lastMousePos = mouseMotionEvent.Position;
			}
		}
	}
}
