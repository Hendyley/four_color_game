using Godot;
using System;
using FourColors;

public partial class StartGamePage : Control
{
	[Export] public PackedScene gameplayScene;
	private HBoxContainer B1,B2,B3;
	private LineEdit Username, TimeSet, ModeSet;
	private OptionButton PlayerNum;

    public override void _Ready()
	{
		Username = (LineEdit)FindChild("Username");
        PlayerNum = (OptionButton) FindChild("PlayerNum");
		TimeSet = (LineEdit)FindChild("TimeSet");
		ModeSet = (LineEdit)FindChild("ModeSet");
        //gameplayScene = (PackedScene)ResourceLoader.Load("res://scenes/gameplay.tscn");
        // Hide();

		PlayerNum.Selected = 1;
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
		if(Username.Text == "")
        {
            Username.Text = NakamaSingleton.Instance.GenerateRandomName();
        }

        var player = new Player(Username.Text,true);
		player.player_turn = 1;
		NakamaSingleton.Instance.MainPlayer = player;
		NakamaSingleton.Instance.PlayerList[1] = player;
		Gameplay gameplayInstance = (Gameplay)gameplayScene.Instantiate();
		NakamaSingleton.Instance.NumberOfPlayers = int.Parse(PlayerNum.Text);
		NakamaSingleton.Instance.MainPlayerTurn = 1;
		NakamaSingleton.Instance.Gamemode = "SinglePlayer";


        GetTree().Root.AddChild(gameplayInstance);
		//GetTree().CurrentScene.Free();  // Remove the previous scene
		GetTree().CurrentScene = gameplayInstance;
	}


}
