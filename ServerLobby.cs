using Godot;
using System;

public partial class ServerLobby : Control
{
    Button startButton;

    [Export]
    PackedScene PlayerInfo;

    public override void _Ready()
    {
        Button startButton = GetNode<Button>("%Start");

        startButton.Pressed += () =>
        {
            EventManager.EmitStartPressed();
        };

        startButton.Visible = Multiplayer.IsServer();
    }

    public override void _Process(double delta)
    {
        VBoxContainer playerList = GetNode<VBoxContainer>("%PlayerList");

        foreach (Node child in playerList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var player in GameManager.Players)
        {
            var playerInfo = PlayerInfo.Instantiate<HBoxContainer>();
            playerInfo.GetNode<Label>("%Name").Text = player.Name;

            var kickButton = playerInfo.GetNode<Button>("%Kick");
            kickButton.Visible = Multiplayer.IsServer();

            playerList.AddChild(playerInfo);
        }
    }
}
