using four_color_game.scripts.FourColors;
using FourColors;
using Godot;
using Nakama;
using Nakama.TinyJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using System.Text.Json;
using Newtonsoft.Json;

public partial class MultiplayerPage : Node
{
	private Button HostButton, JoinButton, WatchButton, CopyRoomID;
	private LineEdit PlayernameInput, JoinRoomIdInput;
	private Label RoomLabel, GeneratedRoomID, TLabel;
	private OptionButton RoomSizeInput;
	private Node MainTable;

	[Export] public PackedScene gameplayScene;
	public override void _Ready()
	{
		// UI setup
		HostButton = (Button)FindChild("Hostbutton");
		JoinButton = (Button)FindChild("JoinButton");
		WatchButton = (Button)FindChild("WatchButton");
		CopyRoomID = (Button)FindChild("CopyRoomID");

		PlayernameInput = (LineEdit)FindChild("Playername");
		RoomSizeInput = (OptionButton)FindChild("Roomsize");
		JoinRoomIdInput = (LineEdit)FindChild("Joinroomid");

		RoomLabel = (Label)FindChild("RoomLabel");
		GeneratedRoomID = (Label)FindChild("GeneratedRoomID");
		TLabel = (Label)FindChild("TestingLabel");

		// Signals
		HostButton.Pressed += OnHostButtonPressed;
		JoinButton.Pressed += OnJoinButtonPressed;

		RoomSizeInput.AddItem("1");
		RoomSizeInput.AddItem("2");
		RoomSizeInput.AddItem("3");
		RoomSizeInput.AddItem("4");
		RoomSizeInput.ItemSelected += onOptionSelected;

		NakamaSingleton.Instance.Connect(nameof(NakamaSingleton.PlayerReadyGame), new Callable(this, nameof(StartGame)));   // Sync Startgame

    }

    private void StartGame(string playernum)
    {
		GD.Print($"{NakamaSingleton.Instance.MainPlayer.player_name} Plays. Total {playernum} Game start");
        
		Gameplay gameplayInstance = (Gameplay)gameplayScene.Instantiate();
		NakamaSingleton.Instance.Gamemode = "Multiplayer";

        GetTree().Root.AddChild(gameplayInstance);
        //GetTree().CurrentScene.Free();  // Remove the previous scene
        GetTree().CurrentScene = gameplayInstance;

    }

    private void onOptionSelected(long index)
	{
		NakamaSingleton.Instance.NumberOfPlayers = int.Parse(RoomSizeInput.GetItemText((int)index));
	}

	public override void _Process(double delta)
	{
	}

	private async void OnHostButtonPressed()
	{
		string name = PlayernameInput.Text;
		int maxPlayers = int.Parse(RoomSizeInput.GetItemText(RoomSizeInput.Selected));

		await NakamaSingleton.Instance.HostGame(name, maxPlayers);

		RoomLabel.Text = "Created Room with ID:              ";
		GeneratedRoomID.Text = NakamaSingleton.Instance.RoomId;
		CopyRoomID.Visible = true;
	}

	private async void OnJoinButtonPressed()
	{
		string name = PlayernameInput.Text;
		string inputRoomId = JoinRoomIdInput.Text.Trim();
		
		await NakamaSingleton.Instance.JoinGame(name, inputRoomId);
	}

	public async void _on_button_pressed()
	{
		var tes = (LineEdit)FindChild("testfield");
		var array = new string[] { NakamaSingleton.Instance.MainPlayer.player_name, tes.Text };
		string arrayjson = JsonConvert.SerializeObject(array);
		var data = Encoding.UTF8.GetBytes(arrayjson);
		await NakamaSingleton.Instance.Socket.SendMatchStateAsync(NakamaSingleton.Instance.Match.Id, 1, data);

	}

	public void _on_copy_room_id_pressed()
	{
		DisplayServer.ClipboardSet(GeneratedRoomID.Text);
	}

	private void DisableButtons()
	{
		HostButton.Disabled = true;
		JoinButton.Disabled = true;
		WatchButton.Disabled = true;
	}


    /////////////////////////// RPC Calling Funtions
}
