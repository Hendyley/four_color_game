using FourColors;
using Godot;
using System;

public partial class MainMenu : Control
{
	
	[Export] private PackedScene startgamescene;
	[Export] private PackedScene multiplayerscene;
    [Export] private PackedScene tutorialgamescene;
    [Export] private PackedScene storegamescene;

    private Label PointLabel;
	private Panel bg_panel;
	private AudioStreamPlayer bgm;
    private AudioStreamPlayer sfxm;

    public override void _Ready()
	{
		Button startButton = (Button) FindChild("StartButton");
		// startButton.Pressed += _on_start_button_pressed;
		Button multiButton = (Button) FindChild("MultiButton");
		Button storeButton = (Button) FindChild("StoreButton");
		Button quitButton = (Button) FindChild("StoreButton");

		PointLabel = (Label)FindChild("PointLabel");

        bg_panel = (Panel)FindChild("Panel");
        var stylebox = bg_panel.GetThemeStylebox("panel") as StyleBoxTexture;
        if (stylebox != null)
        {
            stylebox = (StyleBoxTexture)stylebox.Duplicate();
            bg_panel.AddThemeStyleboxOverride("panel", stylebox);

            stylebox.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Background/{NakamaSingleton.Instance.BGThemeEquiped}.png");
        }

		bgm = (AudioStreamPlayer)FindChild("BGM");
		sfxm = (AudioStreamPlayer)FindChild("SFXM");

		var stream = GD.Load<AudioStream>($"res://art/4_Color_Game/Music/Piki - A New Day (freetouse.com).mp3");
		if (stream != null)
		{
			bgm.Stream = stream;
			bgm.VolumeDb = -10;
			bgm.Play();
            ((AudioStreamMP3)bgm.Stream).Loop = true;

        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		PointLabel.Text = $"Accumulated Points : {NakamaSingleton.Instance.SD.Points} ";
	}
	
	private void _on_start_button_pressed()
	{
		LoggerManager.Info("Start button pressed");
		StartGamePage startmenu = (StartGamePage)startgamescene.Instantiate();
		bgm.Stop();
		AddChild(startmenu);
	}

	private void _on_multi_button_pressed()
	{
		LoggerManager.Info("Multi button pressed");
		MultiplayerPage startmenu = (MultiplayerPage)multiplayerscene.Instantiate();
		bgm.Stop();
		AddChild(startmenu);
		
	}

	private void _on_tutorial_button_pressed()
	{
        LoggerManager.Info("Tutorial button pressed");
        bgm.Stop();

        var player = new Player("You", true);
        player.player_turn = 1;
        NakamaSingleton.Instance.MainPlayer = player;
        NakamaSingleton.Instance.PlayerList[1] = player;
        Node tutorial = (Node)tutorialgamescene.Instantiate();
        NakamaSingleton.Instance.NumberOfPlayers = 2;
        NakamaSingleton.Instance.MainPlayerTurn = 1;
        NakamaSingleton.Instance.Gamemode = "SinglePlayer";


        GetTree().Root.AddChild(tutorial);
        //AddChild(tutorial);
        GetTree().CurrentScene = tutorial;
    }


    private void _on_store_button_pressed()
	{
		LoggerManager.Info("Store button pressed");
        GameStore startmenu = (GameStore)storegamescene.Instantiate();
        bgm.Stop();
        AddChild(startmenu);
    }

	private void _on_quit_button_pressed()
	{
		LoggerManager.Info("Quit button pressed");
		GetTree().Quit(); 
	}
}
