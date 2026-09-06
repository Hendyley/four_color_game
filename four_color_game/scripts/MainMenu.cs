using FourColors;
using Godot;
using System;
using System.Threading.Tasks;

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

    public async override void _Ready()
	{

		Button startButton = (Button) FindChild("StartButton");
		Button multiButton = (Button) FindChild("MultiButton");
		Button storeButton = (Button) FindChild("StoreButton");
		Button quitButton = (Button) FindChild("StoreButton");
        Button soundButton = (Button)FindChild("soundbutton");
        MenuButton languageSetting = (MenuButton)FindChild("languagesetting");
        TextureRect rulebook = (TextureRect)FindChild("Rulebook");

        if (NakamaSingleton.Instance.GameLanguage == "Chinese")
        {
            rulebook.Texture = GD.Load<Texture2D>("res://art/4_Color_Game/GameRules_Chinese.png");
            //startButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/.png");
            //multiButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/.png");
            //storeButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/.png");
            //quitButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/.png");
        }
        else
        {
            rulebook.Texture = GD.Load<Texture2D>("res://art/4_Color_Game/GameRules.png");
            //startButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/Start Game.png");
            //multiButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/.png");
            //storeButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/.png");
            //quitButton.Icon = GD.Load<Texture2D>("res://art/4_Color_Game/Buttons/Removed_BG/.png");
        }

        PopupMenu popup = languageSetting.GetPopup();
        popup.IdPressed += id =>
        {
            switch (id)
            {
                case 0:
                    NakamaSingleton.Instance.GameLanguage = "English";
                    NakamaSingleton.Instance.SD.CommonSettings["Language"] = "English";
                    break;
                case 1:
                    NakamaSingleton.Instance.GameLanguage = "Chinese";
                    NakamaSingleton.Instance.SD.CommonSettings["Language"] = "Chinese";
                    break;
                default:
                    break;
            }
            GameLogic.SetGameSaved(NakamaSingleton.Instance.SD);
            _Ready();
        };

        languageSetting.AboutToPopup += () =>
        {
            Callable.From(() =>
            {
                Vector2I buttonPos = (Vector2I)languageSetting.GetScreenPosition();
                Vector2I popupSize = (Vector2I)popup.Size;

                int newX = buttonPos.X;
                int newY = buttonPos.Y - popupSize.Y - 50;

                popup.Position = new Vector2I(newX, newY);

            }).CallDeferred();
        };


        if (NakamaSingleton.Instance.BGMPlay)
            soundButton.ButtonPressed = false;
        else
            soundButton.ButtonPressed = true;

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
			if (NakamaSingleton.Instance.BGMPlay)
				bgm.Play();
            ((AudioStreamMP3)bgm.Stream).Loop = true;
        }
        NakamaSingleton.Instance.UpdateSaveData();


        guidewindow = (AcceptDialog)FindChild("GuideWindow");
        guidewindow.Exclusive = true;
        guidewindow.GetOkButton().Hide();

        if (NakamaSingleton.Instance.FirstTimer)
        {
            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("欢迎来到游戏。\n", guidewindow, 5000, wait: true);
            else
                await ShowAutoMessage("Welcome to the game.\n", guidewindow, 5000, wait: true);


            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "您可以点击声音按钮来开启或关闭声音。\n" +
                $"(右下角) [img=200x200]res://art/4_Color_Game/SoundLogo.png[/img] \n",
                guidewindow,
                8000,
                wait: true);
            }
            else
            {
                await ShowAutoMessage(
                "You can turn the sound on or off by clicking sound button.\n" +
                $"(Bottom Right) [img=200x200]res://art/4_Color_Game/SoundLogo.png[/img] \n",
                guidewindow,
                8000,
                wait: true);

            }

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "点击语言图标选择语言。\n" +
                $"(右下角) [img=50x200]res://art/4_Color_Game/LanguageLogo2.png[/img] \n",
                guidewindow,
                8000,
                wait: true);
            }
            else
            {
                await ShowAutoMessage(
                "Choose language by clicking language icon.\n" +
                $"(Bottom Right) [img=50x200]res://art/4_Color_Game/LanguageLogo2.png[/img] \n",
                guidewindow,
                8000,
                wait: true);

            }
        }

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		PointLabel.Text = $"Accumulated Points : {NakamaSingleton.Instance.SD.Points} ";
	}

    private void _on_soundbutton_toggled()
    {
        if (NakamaSingleton.Instance.BGMPlay)
        {
            bgm.Stop();
            NakamaSingleton.Instance.BGMPlay = false;
            NakamaSingleton.Instance.SD.CommonSettings["Music"] = "false";
        }
        else
        {
            bgm.Play();
            NakamaSingleton.Instance.BGMPlay = true;
            NakamaSingleton.Instance.SD.CommonSettings["Music"] = "true";
        }

        GameLogic.SetGameSaved(NakamaSingleton.Instance.SD);
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
        NakamaSingleton.Instance.TimingPerTurn = 10000;
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

    private AcceptDialog guidewindow;
    private Timer autoCloseTimer, turnTimer;
    private float remainingMs;
    private float elapsedMs;
    private float updateIntervalMs = 50f; // update every 50ms
    private string currentBaseMessage = "";
    private async Task ShowAutoMessage(string message, AcceptDialog ad, int durationMs = 2000, bool wait = false)
    {
        remainingMs = durationMs;
        elapsedMs = 0;
        currentBaseMessage = message; // Store the message with the [img] tags


        RichTextLabel rtl = ad.FindChild("*", recursive: true) as RichTextLabel;
        if (rtl != null)
        {
            rtl.Clear();
            rtl.AppendText(message);
        }
        else
        {
            ad.DialogText = message;
        }


        if (autoCloseTimer == null)
        {
            autoCloseTimer = new Timer();
            autoCloseTimer.OneShot = false;
            autoCloseTimer.WaitTime = updateIntervalMs / 1000f;
            autoCloseTimer.Timeout += () => UpdateCountdown(ad);
            AddChild(autoCloseTimer);
        }

        ad.PopupCentered();
        autoCloseTimer.Start();

        if (wait) { await ToSignal(ad, "confirmed"); }
    }
    private void UpdateCountdown(AcceptDialog ad)
    {
        elapsedMs += updateIntervalMs;
        int timeLeft = Mathf.Max(0, (int)(remainingMs - elapsedMs));

        string timerText = (NakamaSingleton.Instance.GameLanguage == "Chinese")
            ? $"\n[color=yellow]收盘于: {timeLeft / 1000}s[/color]"
            : $"\n[color=yellow]Closing in: {timeLeft / 1000}s[/color]";

        // Check for your custom RichTextLabel first (Guidewindow)
        RichTextLabel rtl = ad.FindChild("*", recursive: true) as RichTextLabel;

        if (rtl != null)
        {
            rtl.Clear();
            rtl.AppendText(currentBaseMessage);
            rtl.AppendText(timerText);
        }
        else
        {
            // Fallback for autoMessageBox which uses built-in DialogText
            // Note: Built-in DialogText does NOT support [color] tags unless BBCode is enabled via theme
            string cleanTimerText = timerText.Replace("[color=yellow]", "").Replace("[/color]", "");
            ad.DialogText = currentBaseMessage + cleanTimerText;
        }

        if (timeLeft <= 0)
        {
            ad.Hide();
            autoCloseTimer.Stop();
        }
    }
}
