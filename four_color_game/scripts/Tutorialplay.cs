using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using FourColors;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Diagnostics.Eventing.Reader;
using four_color_game.scripts.FourColors;

public partial class Tutorialplay : Node
{
    [Export] public PackedScene tileScene;

    public List<string> deck = new List<string>();

    private Dictionary<int, List<string>> playersHands = new Dictionary<int, List<string>>();
    private Dictionary<int, HBoxContainer> playersContainers = new Dictionary<int, HBoxContainer>();
    private Dictionary<int, Player> PlayerSeats = new Dictionary<int, Player>(); // S -> W -> N -> E
    private Dictionary<int, bool> PlayerCastleStatus = new Dictionary<int, bool>();

    private bool throwhand;
    public Godot.Container tabletilescontainer;
    private List<string> tabletiles = new();
    private Button pickButton, castleButton, backButton;
    private Tile lastTableTile, lastDrawnTile, showstile;
    private Label deckcounterLabel, currentturnLabel, turnlabel, timerview, debuglb, debuglb2;
    private ItemList l2;
    private bool _castleFocusGiven = false;
    private bool gameEnded = false;
    private int turnpass = 0;

    private ConfirmationDialog windec, windec2, win_popup;
    private bool decisionMade = false;
    private bool takeDecision = false;
    private AcceptDialog autoMessageBox, guidewindow;
    private Timer autoCloseTimer, turnTimer;
    private float remainingMs;
    private float elapsedMs;
    private float updateIntervalMs = 50f; // update every 50ms

    private Panel bg_panel, TurnPanel;
    private AudioStreamPlayer bgm;
    private AudioStreamPlayer sfxm;
    private string mp3tilediscard = "Tile_Discard";
    private string mp3tiledraw = "Tile_Draw";
    private string mp3win = "Win";
    private string mp3castle = "Castle";

    private string allowedtile = "";
    private string allowedmove = "";

    private bool _clicked = true;

    StyleBoxFlat highlightStyle = new StyleBoxFlat();
    StyleBoxTexture D_dec, POD_dec;

    public async override void _Ready()
    {
        string logPath = "logs/app.log";
        long maxSizeInBytes = 50 * 1024 * 1024; // 50 MB

        if (File.Exists(logPath))
        {
            FileInfo logInfo = new FileInfo(logPath);
            if (logInfo.Length > maxSizeInBytes)
            {
                File.Delete(logPath);
            }
        }

        tileScene = (PackedScene)ResourceLoader.Load("res://scenes/Tile.tscn");
        tabletilescontainer = (Godot.Container)FindChild("TableTiles");
        pickButton = (Button)FindChild("pickupbutton");
        castleButton = (Button)FindChild("castlebutton");
        backButton = (Button)FindChild("backbutton");

        deckcounterLabel = (Label)FindChild("DeckCounter");
        currentturnLabel = (Label)FindChild("CurrentTurn");
        turnlabel = (Label)FindChild("TurnLabel");
        timerview = (Label)FindChild("TimerView");


        TurnPanel = (Panel)FindChild("TurnPanel");
        var decstyle = TurnPanel.GetThemeStylebox("panel") as StyleBoxTexture;

        if (NakamaSingleton.Instance.GameLanguage == "Chinese")
        {
            D_dec = (StyleBoxTexture)decstyle.Duplicate();
            D_dec.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Animation/Chinese/D_dec.png");
            POD_dec = (StyleBoxTexture)decstyle.Duplicate();
            POD_dec.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Animation/Chinese/POD_dec.png");

            pickButton.Icon = GD.Load<Texture2D>($"res://art/4_Color_Game/Buttons/Removed_BG/Chinese/Pick_Up.png");
            castleButton.Icon = GD.Load<Texture2D>($"res://art/4_Color_Game/Buttons/Removed_BG/Chinese/Castle.png");
        }
        else
        {
            D_dec = (StyleBoxTexture)decstyle.Duplicate();
            D_dec.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Animation/Common/D_dec.png");
            POD_dec = (StyleBoxTexture)decstyle.Duplicate();
            POD_dec.Texture = GD.Load<Texture2D>($"res://art/4_Color_Game/Animation/Common/POD_dec.png");
        }

        if (NakamaSingleton.Instance.TimingPerTurn < 10000)
            timerview.Text = $"{NakamaSingleton.Instance.TimingPerTurn} s";
        else
            timerview.Text = $" ∞ ";

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

        var stream = GD.Load<AudioStream>($"res://art/4_Color_Game/Music/Piki - Monogatari (freetouse.com).mp3");
        if (stream != null)
        {
            bgm.Stream = stream;
            bgm.VolumeDb = -10;
            if (NakamaSingleton.Instance.BGMPlay)
                bgm.Play();
            ((AudioStreamMP3)bgm.Stream).Loop = true;
        }

        debuglb = (Label)GetNode("Debug");
        debuglb2 = (Label)GetNode("Debug2");

        windec = (ConfirmationDialog)FindChild("windec");
        windec.Confirmed += () =>
        {
            decisionMade = true;
            takeDecision = true;
        };
        windec.Canceled += () =>
        {
            decisionMade = true;
            takeDecision = false;
        };

        win_popup = (ConfirmationDialog)FindChild("win_popup");
        win_popup.Exclusive = false;

        autoMessageBox = (AcceptDialog)FindChild("windec_c");
    
        autoCloseTimer = (Timer)FindChild("Timer");
        autoCloseTimer.Timeout += () =>
        {
            autoMessageBox.Hide();
        };

        guidewindow = (AcceptDialog)FindChild("GuideWindow");
        guidewindow.Exclusive = true;
        guidewindow.GetOkButton().Hide();

        turnTimer = (Timer)FindChild("TurnTimer");
        //turnTimer.WaitTime = 1.0;
        //turnTimer.Timeout += OnTurnTick;

        l2 = (ItemList)FindChild("l2");

        NakamaSingleton.Instance.CurrentTurn = 1;
        castleButton.Disabled = false;
        pickButton.Disabled = true;

        List<string> seatsvar;
        if (NakamaSingleton.Instance.NumberOfPlayers == 2)
            seatsvar = new List<string>() { "S", "N" };
        else if (NakamaSingleton.Instance.NumberOfPlayers == 3)
            seatsvar = new List<string>() { "S", "W", "N" };
        else if (NakamaSingleton.Instance.NumberOfPlayers == 4)
            seatsvar = new List<string>() { "S", "W", "N", "E" };
        else
            seatsvar = new List<string>() { "S", "W", "N", "E" };

        PlayerSeats = NakamaSingleton.Instance.PlayerList;
        ///////////////////////////////
        List<int> turnsequence = NakamaSingleton.Instance.GetTurnOrder(NakamaSingleton.Instance.MainPlayer.player_turn, NakamaSingleton.Instance.NumberOfPlayers);

        for (int i = 0; i < seatsvar.Count; i++)
        {
            string containerName = $"HBOX_P_{seatsvar[i]}";
            HBoxContainer handContainer = (HBoxContainer)FindChild(containerName);
            playersHands[turnsequence[i]] = new List<string>();
            if (turnsequence[i] != NakamaSingleton.Instance.MainPlayerTurn)
            {
                NakamaSingleton.Instance.PlayerList[turnsequence[i]] = new Player(NakamaSingleton.Instance.GenerateRandomName(), false);
            }                
            string PlayerPWName = $"P{seatsvar[i]}";
            Label PlayerPW = (Label)FindChild(PlayerPWName);
            PlayerPW.Text = NakamaSingleton.Instance.PlayerList[turnsequence[i]].player_name;

            if (handContainer != null)
            {
                playersContainers[turnsequence[i]] = handContainer;
                LoggerManager.Info($"Player {turnsequence[i]} hand assigned to {containerName}");
            }
            else
            {
                GD.PrintErr($"Could not find container: {containerName}");
            }
        }

        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer" || NakamaSingleton.Instance.IsHost)
            StartGame();

        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer")
        {
            PlayerCastleStatus[1] = false;
            PlayerCastleStatus[2] = false;
            DrawTile(2, "C1");
            DrawTile(2, "C2_Red");
            DrawTile(2, "C3_Yellow");
            DrawTile(2, "C4_Green");
            DrawTile(2, "C1");
            DrawTile(2, "C2_Red");
            DrawTile(2, "C3_Yellow");
            DrawTile(2, "C4_Green");
            DrawTile(2, "C1");
            DrawTile(2, "C2_Red");
            DrawTile(2, "C3_Yellow");
            DrawTile(2, "C4_Green");
            DrawTile(2, "C5_Yellow");
            DrawTile(2, "C5_Yellow");
            DrawTile(2, "C5_Yellow");

            
            DrawTile(1, "C1_Green");
            DrawTile(1, "C1_Red");
            DrawTile(1, "C1_Yellow");

            DrawTile(1, "C2_Green");
            DrawTile(1, "C2_Red");
            DrawTile(1, "C2_Yellow");
            DrawTile(1, "C2");

            DrawTile(1, "C1");
            DrawTile(1, "C3");
            DrawTile(1, "C4");

            DrawTile(1, "C6_Green");
            DrawTile(1, "C6_Red");
            DrawTile(1, "C6_Yellow");
            DrawTile(1, "C6");

            DrawTile(1, "C7");

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("欢迎来到游戏教程。", guidewindow, 5000, wait: true);
            else
                await ShowAutoMessage("Welcome to tutorial.", guidewindow, 5000, wait: true);

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("每位玩家初始拥有15个板块。。", guidewindow, 5000, wait: true);
            else
                await ShowAutoMessage("Each player starts with 15 tiles", guidewindow, 5000, wait: true);

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("游戏的目标是组成颜色组合和荣誉组合。", guidewindow, 5000, wait: true);
            else
                await ShowAutoMessage("The Goal of game is to form color sets and honor sets", guidewindow, 5000, wait: true);
            
            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("颜色组是指相同等级但颜色不同的牌（3 或 4 块牌）的组合。", guidewindow, 5000, wait: true);
            else
                await ShowAutoMessage("Color sets refer to combination of the same rank with different color (3 or 4 tiles)", guidewindow, 5000, wait: true);

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "荣誉组合是指以下组合：\n" +
                $"- 国王牌、王后牌、主教牌 颜色相同\t [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C5.png[/img]\n" +
                $"- 马牌、石头牌、炮牌 颜色相同\t [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C3_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C4_Red.png[/img]\n" +
                $"- 3 或 4 个不同颜色的兵牌。\t [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Yellow.png[/img]",
                guidewindow,
                8000,
                wait: true);
            }
            else
            {
                await ShowAutoMessage(
                "Honor sets refer to combination of:\n" +
                $"- King, Queen, Bishop with the same color\t [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C5.png[/img]\n" +
                $"- Horse, Rock, Cannon with the same color\t [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C3_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C4_Red.png[/img]\n" +
                $"- 3 or 4 Tiles of Pawns with different color\t [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Yellow.png[/img]",
                guidewindow,
                8000,
                wait: true);

            }
                
            DrawTile(1, "C4_Green");

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("先手玩家将额外抽取一块板块。", guidewindow, 5000, wait: true);
            else
                await ShowAutoMessage("The First player will draw an extra tile", guidewindow, 5000, wait: true);
            
            foreach (Tile t in playersContainers[1].GetChildren())
            {
                if (t.Tileid == "C1_Green" || t.Tileid == "C1_Red" || t.Tileid == "C1_Yellow")
                    t.CallDeferred("UpdateHighlightVisual", true);
                else
                    t.CallDeferred("UpdateHighlightVisual", false);
            }

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "目前，您的手牌包含\n" +
                $"马牌的颜色组合（3种不同颜色）。\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Yellow.png[/img]"
                , guidewindow,
                8000,
                wait: true
                );
            }
            else
            {
                await ShowAutoMessage(
                "Currently, your hand consist of\n" +
                "Colors set of Horse (3 different colors).\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1_Yellow.png[/img]"
                , guidewindow,
                5000,
                wait: true);
            }
                

            foreach (Tile t in playersContainers[1].GetChildren())
            {
                if (t.Tileid == "C2_Green" || t.Tileid == "C2_Red" || t.Tileid == "C2_Yellow" || t.Tileid == "C2")
                    t.CallDeferred("UpdateHighlightVisual", true);
                else
                    t.CallDeferred("UpdateHighlightVisual", false);
            }

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "和皇后牌（4 种不同颜色）\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2_Yellow.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2.png[/img]"
                , guidewindow,
                5000,
                wait: true);
            }
            else
            {
                await ShowAutoMessage(
                "and Queen (4 different colors).\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2_Yellow.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C2.png[/img]"
                , guidewindow,
                5000,
                wait: true);
            }

            foreach (Tile t in playersContainers[1].GetChildren())
            {
                if (t.Tileid == "C1" || t.Tileid == "C3" || t.Tileid == "C4")
                    t.CallDeferred("UpdateHighlightVisual", true);
                else
                    t.CallDeferred("UpdateHighlightVisual", false);
            }

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "白色荣誉套装（马牌、车牌、炮牌）\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C3.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C4.png[/img]"
                , guidewindow,
                5000,
                wait: true);
            }
            else
            {
                await ShowAutoMessage(
                "Honor set of white color (Horse, Rook, Cannon)\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C3.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C4.png[/img]"
                , guidewindow,
                5000,
                wait: true);
            }
            
            foreach (Tile t in playersContainers[1].GetChildren())
            {
                if (t.Tileid == "C6_Green" || t.Tileid == "C6_Red" || t.Tileid == "C6_Yellow" || t.Tileid == "C6")
                    t.CallDeferred("UpdateHighlightVisual", true);
                else
                    t.CallDeferred("UpdateHighlightVisual", false);
            }

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "和兵牌（4 种不同颜色）\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Yellow.png[/img]"
                , guidewindow,
                5000,
                wait: true);

            }
            else
            {
                await ShowAutoMessage(
                "and Pawn (4 different color)\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Yellow.png[/img]"
                , guidewindow,
                5000,
                wait: true);

            }


            foreach (Tile t in playersContainers[1].GetChildren())
            {
                if (t.Tileid == "C7" )
                    t.CallDeferred("UpdateHighlightVisual", true);
                else
                    t.CallDeferred("UpdateHighlightVisual", false);
            }

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "国王牌不能弃置\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Yellow.png[/img]"
                , guidewindow,
                5000,
                wait: true);

            }
            else
            {
                await ShowAutoMessage(
                "King tiles cannot be discard\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Yellow.png[/img]"
                , guidewindow,
                5000,
                wait: true);

            }

            foreach (Tile t in playersContainers[1].GetChildren())
            {
                t.CallDeferred("UpdateHighlightVisual", false);
            }

            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
            {
                await ShowAutoMessage(
                "在这种情况下，最佳选择是放弃绿炮牌。\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C4_Green.png[/img]"
                , guidewindow,
                5000,
                wait: true);

            }
            else
            {
                await ShowAutoMessage(
                "Best choice in this scenario is to discard Green Cannon\n" +
                "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C4_Green.png[/img]"
                , guidewindow,
                5000,
                wait: true);

            }

            allowedtile = "C4_Green";

        }

        throwhand = true;
    }

    public async override void _Process(double delta)
    {
        if (gameEnded)
            return;

        deckcounterLabel.Text = deck.Count.ToString();
        currentturnLabel.Text = NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name;
        GameLogic.Deck = deck;
        GameLogic.Table = tabletiles;

        /////////////////////////////////////////////////////////////////////////////////  "SinglePlayer"
        if (NakamaSingleton.Instance.Gamemode == "SinglePlayer")
        {
            if (GameLogic.CheckCastle(playersHands[NakamaSingleton.Instance.MainPlayerTurn]))
            {
                if (!PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
                {
                    if (!_castleFocusGiven)
                    {
                        castleButton.GrabFocus();
                        _castleFocusGiven = true;
                    }
                }
                else
                {
                    castleButton.ReleaseFocus();
                    _castleFocusGiven = false;
                }
            }
            
            // Check if winning or not (after castle = true)
            if (NakamaSingleton.Instance.CurrentTurn != 1 && PlayerCastleStatus[NakamaSingleton.Instance.CurrentTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.CurrentTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.CurrentTurn);

                }
            }
            else if (NakamaSingleton.Instance.CurrentTurn == 1 && PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.MainPlayerTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.MainPlayerTurn);

                }
            }
        }

        if (!guidewindow.Visible)
            return;

        if (Input.IsMouseButtonPressed(MouseButton.Left) || (Input.IsActionPressed("ui_accept")))
        {
            if(_clicked)
                TriggerOkLogic();
            _clicked = false;
        }
        else
        {
            _clicked = true;
        }
    }

    private void StartGame()
    {
        LoggerManager.Info("Game Start");

        deck = CreateDeck();
        ShuffleDeck();
        for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
        {
            playersHands[i].Clear();
            foreach (Node child in playersContainers[i].GetChildren())
                child.QueueFree();
        }

    }

    private List<string> CreateDeck()
    {
        LoggerManager.Info("Create Deck");

        List<string> newDeck = new List<string>();
        String x;
        for (int s = 0; s < 4; s++)
        {
            for (int i = 1; i <= 7; i++) // 7 Pieces
            {
                x = $"C{i}";
                newDeck.Add(x);
                newDeck.Add(x + "_Green");
                newDeck.Add(x + "_Red");
                newDeck.Add(x + "_Yellow");
            }
        }
        LoggerManager.Info("Amount in the Deck: " + newDeck.Count);

        return newDeck;
    }

    private void ShuffleDeck()
    {
        Random rng = new Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (deck[k], deck[n]) = (deck[n], deck[k]);
        }
    }

    private void DrawTile(int playerid, string tileid = "")
    {
        PlaySoundEffect(mp3tiledraw);

        if (lastTableTile != null)
            lastTableTile.CallDeferred("UpdateHighlightVisual", false);

        if (deck.Count > 0)
        {
            string tilevalue;
            if (tileid=="")
            {
                tilevalue = deck[0];
                deck.RemoveAt(0);
            }
            else
            {
                tilevalue = tileid;
                deck.Remove(tileid);
            }

            playersHands[playerid].Add(tilevalue);

            LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} Drawn Card Value: " + GameLogic.TileName(tilevalue));
            l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} Drawn Card Value: " + GameLogic.TileName(tilevalue));

            Tile newTile = (Tile)tileScene.Instantiate();
            playersContainers[playerid].AddChild(newTile);
            newTile.Tileid = tilevalue;

            newTile.SetTile(playerid, tilevalue);
            //newTile.CallDeferred("SetTile", playerid ,tilevalue);
            newTile.Connect("TileClicked", new Callable(this, "OnTileClicked"));

            if (playersContainers[playerid].Name != "HBOX_P_S")
                newTile.SetCover(true);
            else
                newTile.Mainuser = true;

            lastDrawnTile = newTile;

            throwhand = true;
        }
        else
        {
            LoggerManager.Info("Deck is empty! No card drawn.");
            l2.AddItem("Deck is empty! No card drawn.");
        }
    }

    private void TakeTile(int playerid = 1, string tilevalue = "C1")
    {
        PlaySoundEffect(mp3tiledraw);

        lastTableTile.CallDeferred("UpdateHighlightVisual", false);
        //SortTiles(NakamaSingleton.Instance.CurrentTurn);

        playersHands[playerid].Add(tilevalue);
        Tile newTile = (Tile)tileScene.Instantiate();
        newTile.Tileid = tilevalue;
        playersContainers[playerid].AddChild(newTile);

        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} take tile: " + GameLogic.TileName(tilevalue));
        l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[playerid].player_name} take tile: " + GameLogic.TileName(tilevalue));

        newTile.SetTile(playerid, tilevalue);
        //newTile.CallDeferred("SetTile", playerid ,tilevalue);
        newTile.Connect("TileClicked", new Callable(this, "OnTileClicked"));

        if (playersContainers[playerid].Name != "HBOX_P_S")
            newTile.SetCover(true);     //newTile.CallDeferred("SetCover", true);
        else
            newTile.Mainuser = true;

        lastTableTile = null;

        tabletiles.Remove(tilevalue);
        pickButton.Disabled = true;
        throwhand = true;

    }

    public async void DiscardTile(int playerid, string tilevalue)
    {
        if (!throwhand)
            return;

        if (allowedtile != tilevalue)
        {
            await ShowAutoMessage("Wrong tile, choose again", guidewindow, 0, wait:true);
            return;
        }

        PlaySoundEffect(mp3tilediscard);

        l2.AddItem($"player {NakamaSingleton.Instance.PlayerList[playerid].player_name} Discard {GameLogic.TileName(tilevalue)}");

        var children = playersContainers[playerid].GetChildren();

        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is Tile tile)
            {
                if (tile.Tileid == tilevalue)
                {
                    playersContainers[playerid].RemoveChild(tile);
                    tile.QueueFree();
                    playersHands[playerid].Remove(tilevalue);
                    await AddtoTables(tilevalue);
                    break;
                }
            }
        }

        //SortTiles(playerid);
        await AddTurn(); ////////////////////////////////////////////////////////////////////////////// Turn Change here
        await tutorialoppnext();
        return;
    }

    private async Task<bool> AddtoTables(string tilevalue)
    {
        LoggerManager.Info($"Add {GameLogic.TileName(tilevalue)} to the table ");

        if (lastTableTile != null)
        {
            lastTableTile.Disconnect("TableTileClicked", new Callable(this, "OnTableTileClicked"));
        }

        Tile newTile = (Tile)tileScene.Instantiate();
        tabletilescontainer.AddChild(newTile);
        tabletiles.Add(tilevalue);
        newTile.Tileid = tilevalue;
        lastTableTile = newTile;

        //newTile.CallDeferred("UpdateHighlightVisual", true);
        //newTile.CallDeferred("SetTableTile", tilevalue);
        //newTile.Connect("TableTileClicked", new Callable(this,"OnTableTileClicked"));
        newTile.UpdateHighlightVisual(true);
        newTile.SetTableTile(tilevalue);
        newTile.Connect("TableTileClicked", new Callable(this, "OnTableTileClicked"));

        Random rand = new Random();
        Vector2 containerSize = tabletilescontainer.GetRect().Size;
        Vector2 tileSize = newTile.GetRect().Size;

        float randomX = (float)(rand.NextDouble() * (containerSize.X - tileSize.X));
        float randomY = (float)(rand.NextDouble() * (containerSize.Y - tileSize.Y));
        newTile.Position = new Vector2(randomX, randomY);
        float randomRotation = (float)(rand.NextDouble() * 360);
        newTile.RotationDegrees = randomRotation;


        throwhand = false;

        return true;
    }

    private async void OnTileClicked(Tile clickedTile)
    {
        if (clickedTile.Tileid.Contains("C7"))
        {
            LoggerManager.Info("Cannot discard King tile !!!");
            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("不能丢弃国王牌 !!!", autoMessageBox, 5000, wait: true);
            else
                await ShowAutoMessage("Cannot discard King tile !!!" ,autoMessageBox, 5000, wait: true);
            return;
        }
        DiscardTile(clickedTile.Playerid, clickedTile.Tileid);

    }

    private void OnTableTileClicked(Tile clickedTile)
    {
        foreach (Node child in tabletilescontainer.GetChildren())
        {
            if (child is Tile tile)
            {
                if (tile.Tileid == clickedTile.Tileid)
                {
                    tabletilescontainer.RemoveChild(tile);
                    tile.QueueFree();
                    break;
                }
            }
            else
            {
                LoggerManager.Info("Warning: Child is not a Tile node!");
            }
        }
        LoggerManager.Info($"Player {clickedTile.Playerid} Taking Tile: " + clickedTile.Tileid);
        l2.AddItem($"Player {clickedTile.Playerid} Taking Tile: " + clickedTile.Tileid);
        TakeTile(NakamaSingleton.Instance.CurrentTurn, clickedTile.Tileid);
    }



    private void buttondisable()
    {
        pickButton.Disabled = true;
        castleButton.Disabled = true;
    }


    private void _on_pickupbutton_pressed()
    {
        LoggerManager.Info("pickup button pressed");
        DrawTile(NakamaSingleton.Instance.CurrentTurn);
        pickButton.Disabled = true;
    }

    private async void _on_castlebutton_pressed()
    {
        PlaySoundEffect(mp3castle);
        LoggerManager.Info("castle button pressed");
        if (!GameLogic.CheckCastle(playersHands[NakamaSingleton.Instance.MainPlayerTurn]))
        {
            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                await ShowAutoMessage("你现在无法建城堡。", autoMessageBox, 5000, wait: true);
            else
                await ShowAutoMessage("You Cannot Castle now.",autoMessageBox, 5000, wait: true);

            castleButton.ReleaseFocus();
            return;
        }
        castleButton.ReleaseFocus();
        PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn] = true;
        await ShowAutoMessage($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} Castle",autoMessageBox, 5000, wait: true);
        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.MainPlayerTurn].player_name} Castle");
    }

    private void _on_back_button_pressed()
    {
        LoggerManager.Info("back button pressed");
        bgm.Stop();
        sfxm.Stop();
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }

    private void _on_sortbutton_pressed()
    {
        SortTiles(NakamaSingleton.Instance.MainPlayerTurn);

    }

    private void EndGame(int winnerId)
    {
        bgm.Stop();
        PlaySoundEffect(mp3win);

        gameEnded = true;

        pickButton.Disabled = true;
        castleButton.Disabled = true;

        if (NakamaSingleton.Instance.MainPlayerTurn == winnerId)
            NakamaSingleton.Instance.Point += GameLogic.CheckScore(playersHands[NakamaSingleton.Instance.MainPlayerTurn]);
        else
            NakamaSingleton.Instance.Point += 10;

        if (NakamaSingleton.Instance.GameLanguage == "Chinese")
        {
            win_popup.DialogText = $"🎉 {NakamaSingleton.Instance.PlayerList[winnerId].player_name}  玩家 获胜！接下来你想做什么？";
            win_popup.CancelButtonText = "回去";
            win_popup.OkButtonText = "再玩一次";
        }
        else
        {
            win_popup.DialogText = $"🎉 {NakamaSingleton.Instance.PlayerList[winnerId].player_name} wins! What would you like to do next?";
        }
        win_popup.GetOkButton().Hide();
        win_popup.PopupCentered();
        GameLogic.Savepoint(NakamaSingleton.Instance.Point);
    }

    private void _on_win_popup_confirmed()
    {
        LoggerManager.Info("OK (Play Again) selected");
        RestartGame();
    }

    private void _on_win_popup_canceled()
    {
        LoggerManager.Info("Cancel (Go Back) selected");
        bgm.Stop();
        sfxm.Stop();
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }

    private void RestartGame()
    {
        LoggerManager.Info("Restarting game...");

        var TutorialplayScene = GD.Load<PackedScene>("res://scenes/Tutorialplay.tscn");
        var newGame = TutorialplayScene.Instantiate();

        GetTree().Root.AddChild(newGame);

        // SAFELY queue the old scene for deletion after the new one is added and running
        GetTree().CurrentScene.CallDeferred("free");

        // Set the new scene as current
        GetTree().CurrentScene = newGame;
    }

    public async Task<bool> AddTurn()
    {
        turnpass++;
        if (NakamaSingleton.Instance.CurrentTurn == NakamaSingleton.Instance.MainPlayerTurn)
        {
            //StartTurnTimer();
            if (!PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn] && GameLogic.CheckCastle(playersHands[NakamaSingleton.Instance.MainPlayerTurn]))
            {

                if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                {
                    await ShowAutoMessage(
                    "你可以选择城堡策略。当你距离胜利只差一格时。\n" +
                    "现在，你至少已经组成了一套荣誉棋子（绿、红、黄、白），你可以用其中任何一种赢得比赛。\n" +
                    "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Yellow.png[/img]" +
                    "任何一张 (国王牌) 或 (白马牌)\n" +
                    "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Yellow.png[/img] or [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1.png[/img]" +
                    "你必须先城堡才能宣布胜利。",
                    guidewindow,
                    5000, wait: true);

                }
                else
                {
                    await ShowAutoMessage(
                    "You Can Castle. when you are 1 tile away from winning.\n" +
                    "now, you had form at least 1 set of Honor (Green, Red, Yellow, and White pawns) and you can win with either \n" +
                    "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C6_Yellow.png[/img]" +
                    "any of the king tile or white horse tile\n" +
                    "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Yellow.png[/img] or [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1.png[/img]" +
                    "You must castle before you can declare your win.",
                    guidewindow,
                    5000, wait: true);

                }


                // /Block View and show only castle button
            }
        }

        if (NakamaSingleton.Instance.CurrentTurn < NakamaSingleton.Instance.NumberOfPlayers)
        {
            NakamaSingleton.Instance.CurrentTurn++;
            while (playersContainers[NakamaSingleton.Instance.CurrentTurn] == null)
            {
                NakamaSingleton.Instance.CurrentTurn++;
                if (NakamaSingleton.Instance.CurrentTurn > NakamaSingleton.Instance.NumberOfPlayers)
                    NakamaSingleton.Instance.CurrentTurn = 1;
            }
        }
        else
        {
            NakamaSingleton.Instance.CurrentTurn = 1;
        }

        if (NakamaSingleton.Instance.CurrentTurn == NakamaSingleton.Instance.MainPlayerTurn)
        {
            pickButton.Disabled = false;
            if (lastTableTile!= null)
                lastTableTile.Mainuser = true;
        }

        if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
        {
            if (lastTableTile != null)
                lastTableTile.Mainuser = true;
        }

        return true;

    }

    private async Task<bool> tutorialoppnext()
    {
        switch (turnpass)
        {
            case 1:

                if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                    await ShowAutoMessage("城堡 之后 [img=247x63]res://art/4_Color_Game/Buttons/Removed_BG/Castle.png[/img]\n you are waiting for winning hand", guidewindow, 5000, wait: true);
                else
                    await ShowAutoMessage("After Castle [img=247x63]res://art/4_Color_Game/Buttons/Removed_BG/Castle.png[/img]\n you are waiting for winning hand", guidewindow,5000,wait:true);
                
                foreach (Tile t in playersContainers[1].GetChildren())
                {
                    if (t.Tileid == "C1_Green" || t.Tileid == "C1_Red" || t.Tileid == "C1_Yellow")
                        t.CallDeferred("UpdateHighlightVisual", true);
                    else
                        t.CallDeferred("UpdateHighlightVisual", false);
                }

                if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                    await ShowAutoMessage("可能是(白马牌)\n [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1.png[/img]", guidewindow, 5000, wait: true);
                else
                    await ShowAutoMessage("It can be (white horse tile)\n [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C1.png[/img]", guidewindow, 5000, wait: true);
                
                foreach (Tile t in playersContainers[1].GetChildren())
                {
                    if (t.Tileid == "C7")
                        t.CallDeferred("UpdateHighlightVisual", true);
                    else
                        t.CallDeferred("UpdateHighlightVisual", false);
                }

                if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                {
                    await ShowAutoMessage("可以是任何颜色的 (国王牌)（白色、红色、绿色、黄色）\n" +
                    "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Yellow.png[/img]"
                    , guidewindow, 5000, wait: true);

                }
                else
                {
                    await ShowAutoMessage("It can be any king tiles (white, red, green, yellow)\n" +
                    "[img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Red.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Green.png[/img] [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/C7_Yellow.png[/img]"
                    , guidewindow, 5000, wait: true);

                }
                
                
                foreach (Tile t in playersContainers[1].GetChildren())
                {
                    t.CallDeferred("UpdateHighlightVisual", false);
                }
                DrawTile(2, "C7");
                foreach (Tile t in playersContainers[2].GetChildren())
                {
                    if (t.Tileid == "C7")
                    {
                        t.CallDeferred("UpdateHighlightVisual", true);
                        t.SetCover(false);
                    }   
                    else
                    {
                        t.CallDeferred("UpdateHighlightVisual", false);
                    }
                }

                if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                {
                    await ShowAutoMessage("点击城堡  [img=247x63]res://art/4_Color_Game/Buttons/Removed_BG/Castle.png[/img]\n 在城堡之后，你可以通过玩家在你之前弃掉的板块来赢得比赛。 \n" +
                    "或者当其他玩家抽牌时，只要该牌有助于组成获胜牌组即可。", guidewindow, 5000, wait: true);
                    await ShowAutoMessage("选择拿走牌来赢得比赛。", guidewindow, 5000, wait: true);


                }
                else
                {
                    await ShowAutoMessage("Click on castle  [img=247x63]res://art/4_Color_Game/Buttons/Removed_BG/Castle.png[/img]\n After Castle,\n you can win by tile discard by player before you \n" +
                    "or when other players drawing a tile as long as the tile contribute to the winning sets.", guidewindow, 5000, wait: true);
                    await ShowAutoMessage("Choose to win by taking the tile.", guidewindow, 5000, wait: true);


                }


                while (true)
                {
                    await Task.Delay(1000);
                    if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn] == true)
                    {
                        var x = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList();
                        x.Add(lastDrawnTile.Tileid);

                        if (GameLogic.WinCondition(x) == "WIN")
                        {
                            decisionMade = false;
                            takeDecision = false;
                            
                            RichTextLabel rtl = GetNode<RichTextLabel>("windec/windec_RTL");
                            if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                            {
                                rtl.Clear();
                                rtl.AppendText($"你想用 {lastDrawnTile.TileName} 来赢吗？?\n [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/{lastDrawnTile.Tileid}.png[/img]\n");
                                windec.OkButtonText = "赢";
                                windec.CancelButtonText = "跳过";
                            }
                            else
                            {
                                rtl.Clear();
                                rtl.AppendText($"Do you want to win with {lastDrawnTile.TileName}?\n [img=50x200]res://art/4_Color_Game/Chess/Removed_BG/{lastDrawnTile.Tileid}.png[/img]\n");
                            }
                            windec.PopupCentered();

                            int timeoutMs = 50000;
                            int waitedMs = 0;
                            int pollInterval = 100;

                            while (!decisionMade && waitedMs < timeoutMs)
                            {
                                await Task.Delay(pollInterval);
                                waitedMs += pollInterval;
                            }

                            if (!decisionMade)
                            {
                                takeDecision = false;
                                decisionMade = true;
                                windec.Hide();
                            }

                            if (takeDecision)
                            {
                                l2.AddItem("Player chose to Take");
                                TakeTile(NakamaSingleton.Instance.MainPlayerTurn, lastDrawnTile.Tileid);
                                EndGame(NakamaSingleton.Instance.MainPlayerTurn);
                            }
                            else
                            {
                                if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                                    await ShowAutoMessage("错误的选择\n", guidewindow, 2000, wait: true);
                                else
                                    await ShowAutoMessage("Wrong Choice\n", guidewindow, 2000, wait: true);
                            }
                        }
                    }
                }
            

                break;
            default:
                if (NakamaSingleton.Instance.GameLanguage == "Chinese")
                    await ShowAutoMessage($"转弯 = {turnpass}", guidewindow, 5000, wait: true);
                else
                    await ShowAutoMessage($"turnpass = {turnpass}", guidewindow, 5000, wait: true);
                break;
        }
        

        return true;
    }

    public void StartTurnTimer()
    {
        elapsedMs = 0;
        remainingTurnTimeMs = 30_000;
        turnTimer.Start();
    }

    private int remainingTurnTimeMs = 30_000;
    private int elapsedTurnTimeMs = 0;

    private void OnTurnTick()
    {

        elapsedMs += 1000;
        remainingTurnTimeMs = Mathf.Max(0, remainingTurnTimeMs - 1000);

        int secondsLeft = remainingTurnTimeMs / 1000;
        LoggerManager.Info($"Elapsed: {elapsedMs} ms");
        timerview.Text = $"{remainingTurnTimeMs / 1000} s";

        if (NakamaSingleton.Instance.CurrentTurn != NakamaSingleton.Instance.MainPlayerTurn)
            turnTimer.Stop();

        if (remainingTurnTimeMs <= 0)
        {
            LoggerManager.Info("Turn timeout. Skipping turn.");
            turnTimer.Stop();
            SkipTurn();
        }
    }

    private void SkipTurn()
    {
        if(turnpass==1)
        {
            var c = new GameLogic.GameState();
            c.Hand = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList(); //Current Hand
            DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(c));
        }
            
        var gs = new GameLogic.GameState();
        gs.Hand = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList(); //Current Hand

        var gs1 = new GameLogic.GameState();
        gs1.Hand = playersHands[NakamaSingleton.Instance.MainPlayerTurn].ToList(); //Current hand + take tile from table
        if(lastTableTile!=null)
            gs1.Hand.Add(lastTableTile.Tileid);

        if (GameLogic.EvaluateState(gs) >= GameLogic.EvaluateState(gs1))
        {
            l2.AddItem($"player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} draw {GameLogic.EvaluateState(gs)} vs take {GameLogic.EvaluateState(gs1)}");
            DrawTile(NakamaSingleton.Instance.MainPlayerTurn);

            if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.MainPlayerTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.CurrentTurn);
                }
            }

            for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
            {
                if (i == NakamaSingleton.Instance.MainPlayerTurn)
                    continue;

                if (PlayerCastleStatus[i])
                {
                    if (GameLogic.WinCondition(playersHands[i]) == "WIN")
                    {
                        l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        EndGame(i);
                    }
                }
            }

            gs.Hand.Add(lastDrawnTile.Tileid);
            DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs));
        }
        else
        {
            OnTableTileClicked(lastTableTile);

            if (PlayerCastleStatus[NakamaSingleton.Instance.MainPlayerTurn])
            {
                if (GameLogic.WinCondition(playersHands[NakamaSingleton.Instance.MainPlayerTurn]) == "WIN")
                {
                    l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[NakamaSingleton.Instance.CurrentTurn].player_name} WIN");
                    EndGame(NakamaSingleton.Instance.CurrentTurn);
                }
            }

            for (int i = 1; i <= NakamaSingleton.Instance.NumberOfPlayers; i++)
            {
                if (i == NakamaSingleton.Instance.MainPlayerTurn)
                    continue;

                if (PlayerCastleStatus[i])
                {
                    if (GameLogic.WinCondition(playersHands[i]) == "WIN")
                    {
                        l2.AddItem($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        LoggerManager.Info($"Player {NakamaSingleton.Instance.PlayerList[i].player_name} WIN");
                        EndGame(i);
                    }
                }
            }

            DiscardTile(NakamaSingleton.Instance.CurrentTurn, GameLogic.MAX_AI_DISCARD(gs1));
        }
    }

    private void SortTiles(int playerid)
    {
        var container = playersContainers[playerid];

        var tiles = container.GetChildren().OfType<Tile>()
                            .OrderBy(tile => tile.Tileid)
                            .ToList();

        for (int i = 0; i < tiles.Count; i++)
        {
            container.MoveChild(tiles[i], i);
        }

        container.QueueSort(); // Ensure UI update
    }

    private string currentBaseMessage = "";
    public async Task ShowAutoMessage(string message, AcceptDialog ad, int durationMs = 2000, bool wait = false)
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


    public async void PlaySoundEffect(string name, float duration = -1f)
    {
        var path = $"res://art/4_Color_Game/Music/{name}.mp3";
        var stream = GD.Load<AudioStream>(path);
        if (stream != null)
        {
            sfxm.Stream = stream;
            sfxm.Play();

            if (duration > 0 && duration < stream.GetLength())
            {
                await ToSignal(GetTree().CreateTimer(duration), "timeout");
                sfxm.Stop();
            }
        }
    }

    private ColorRect CreateBlocker(float px, float py, float w, float h, Color color)
    {
        var rect = new ColorRect
        {
            Position = new Vector2(px, py),
            Size = new Vector2(w, h),
            Color = color,
            MouseFilter = Control.MouseFilterEnum.Stop // block clicks here
        };
        return rect;
    }

    public void DisableMouseFiller()
    {
        var root = GetTree().CurrentScene;
        var overlay = root.GetNodeOrNull<Control>("MouseFillerOverlay");
        if (overlay != null)
            overlay.QueueFree();
    }

    private void TriggerOkLogic()
    {
        var okButton = guidewindow.GetOkButton();
        if (okButton != null)
        {
            okButton.CallDeferred("emit_signal", Button.SignalName.Pressed);
        }
    }


}
