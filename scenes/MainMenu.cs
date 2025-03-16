using Godot;
using System;

public partial class MainMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Button startButton = (Button) FindChild("StartButton");
		// startButton.Pressed += _on_start_button_pressed;
		Button multiButton = (Button) FindChild("MultiButton");
		Button storeButton = (Button) FindChild("StoreButton");
		Button quitButton = (Button) FindChild("StoreButton");

		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void _on_start_button_pressed()
	{
		GD.Print("Start button pressed"); // Use GD.Print for Godot console
		GetTree().ChangeSceneToFile("res://scenes/gameplay.tscn");
	}

	private void _on_multi_button_pressed()
	{
		GD.Print("Multi button pressed");
		
	}

	private void _on_store_button_pressed()
	{
		GD.Print("Store button pressed");
	}

	private void _on_quit_button_pressed()
	{
		GD.Print("Quit button pressed");
		//GD.get_tree().quit();
		GetTree().Quit(); 
	}
}
