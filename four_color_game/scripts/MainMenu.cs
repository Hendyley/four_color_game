using Godot;
using System;

public partial class MainMenu : Control
{
	
	[Export] public PackedScene startgamescene;
	[Export] public PackedScene multiplayerscene;

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

            stylebox.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Background/{NakamaSingleton.Instance.BGThemeEquiped}");
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
		PointLabel.Text = $"Accumulated Points : {NakamaSingleton.Instance.Point} ";
	}
	
	private void _on_start_button_pressed()
	{
		GD.Print("Start button pressed"); // Use GD.Print for Godot console
		// GetTree().ChangeSceneToFile("res://scenes/gameplay.tscn");
		StartGamePage startmenu = (StartGamePage)startgamescene.Instantiate();
		bgm.Stop();
		AddChild(startmenu);
	}

	private void _on_multi_button_pressed()
	{
		GD.Print("Multi button pressed");
		MultiplayerPage startmenu = (MultiplayerPage)multiplayerscene.Instantiate();
		bgm.Stop();
		AddChild(startmenu);
		
	}

	private void _on_store_button_pressed()
	{
		GD.Print("Store button pressed");
	}

	private void _on_quit_button_pressed()
	{
		GD.Print("Quit button pressed");
		GetTree().Quit(); 
	}
}
