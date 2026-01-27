using Godot;
using System;
using FourColors;

public partial class StartGamePage : Control
{
	[Export] public PackedScene gameplayScene;
	private HBoxContainer B1,B2,B3;
	private LineEdit Username, ModeSet;
	private OptionButton PlayerNum, TimeSet;

    public override void _Ready()
	{
		Username = (LineEdit)FindChild("Username");
        PlayerNum = (OptionButton) FindChild("PlayerNum");
		TimeSet = (OptionButton)FindChild("TimeSet");
		ModeSet = (LineEdit)FindChild("ModeSet");
        //gameplayScene = (PackedScene)ResourceLoader.Load("res://scenes/gameplay.tscn");
        // Hide();

		Username.Text = NakamaSingleton.Instance.SaveCommonSettings["Defaultname"];
		PlayerNum.Selected = int.Parse(NakamaSingleton.Instance.SaveCommonSettings["DefaultNOP"]);
		TimeSet.Selected = int.Parse(NakamaSingleton.Instance.SaveCommonSettings["Timeset"]);

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
        NakamaSingleton.Instance.SD.CommonSettings["Defaultname"] = Username.Text;
        NakamaSingleton.Instance.SD.CommonSettings["DefaultNOP"] = PlayerNum.Selected.ToString();
        NakamaSingleton.Instance.SD.CommonSettings["Timeset"] = TimeSet.Selected.ToString();
		GameLogic.SetGameSaved( NakamaSingleton.Instance.SD);

        if (Username.Text == "")
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
		if (TimeSet.Text == "∞")
            NakamaSingleton.Instance.TimingPerTurn = 10000;
        else
            NakamaSingleton.Instance.TimingPerTurn = System.Int32.Parse(TimeSet.Text);


        GetTree().Root.AddChild(gameplayInstance);
		//GetTree().CurrentScene.Free();  // Remove the previous scene
		GetTree().CurrentScene = gameplayInstance;
	}


}
