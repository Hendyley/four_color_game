using Godot;
using System;
using FourColors;

public partial class Playerfield : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void _host_set_up(){
		GD.Print("Host......");
	}

	public void _client_set_up(){
		GD.Print("client......");
	}
}
