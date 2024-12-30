using Godot;
using System;

public partial class Map : Node3D
{
    private PackedScene PlayerScene;

    public override void _Ready()
    {
        PlayerScene = (PackedScene)ResourceLoader.Load("res://Player.tscn");

        foreach (var player in GameManager.Players)
        {
            LoadSinglePlayer(player.Name, player.Id);
        }
    }

    private void LoadSinglePlayer(string playerName, long playerId)
    {
        var playerInstance = PlayerScene.Instantiate();
        playerInstance.Name = playerId.ToString();
        playerInstance.SetMultiplayerAuthority((int)playerId);
        AddChild(playerInstance);
    }

    private void DeferredReady()
    {
        EventManager.EmitMapLoaded();
    }
}
