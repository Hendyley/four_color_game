using Godot;
using System;

public partial class StartGamePage : Control
{
	[Export] public PackedScene gameplayScene;
	private HBoxContainer B1,B2,B3;
	private LineEdit B1le, B2le, B3le;

	public override void _Ready()
	{
		B1le = (LineEdit) FindChild("B1LineEdit");
		//gameplayScene = (PackedScene)ResourceLoader.Load("res://scenes/gameplay.tscn");
		// Hide();
	}

	public override void _Process(double delta)
	{
		// testEsc();	
	}

	public void ShowPopup()
	{
		Show();
	}

	public void pause(){
		GetTree().Paused = true;
		
	}

	public void resume(){
		GetTree().Paused = false;
	}

	public void testEsc(){
		if (Input.IsActionJustPressed("esc") && !GetTree().Paused ){
			pause();
		}
		if (Input.IsActionJustPressed("esc") && GetTree().Paused ){
			resume();
		}
	}

	private void _on_b_1_button_pressed(){
		resume();
	}

	private void _on_b_2_button_pressed(){
		resume();
		GetTree().ReloadCurrentScene();
	}

	private void _on_b_3_button_pressed(){
		GetTree().Quit();
	}

	private void _on_confirm_button_pressed()
	{
		string numberOfplayers = B1le.Text;
		Node gameplayInstance = (Node)gameplayScene.Instantiate();
		gameplayInstance.SetMeta("numberofplayers", int.Parse(numberOfplayers));  
		// gameplayInstance.SetMeta("player_name", "Hendy");
		GetTree().Root.AddChild(gameplayInstance);  // Add the scene to the root
		// GetTree().CurrentScene.Free();  // Remove the previous scene
		GetTree().CurrentScene = gameplayInstance; // Set the new scene as active
		// GetTree().ChangeSceneToFile("res://scenes/gameplay.tscn");
	}


}
