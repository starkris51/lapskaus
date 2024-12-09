using Godot;
using System;

public partial class MultiplayerController : Control
{
    [Export]
    private int port = 8080;

    public override void _Ready()
    {
        EventManager.HostButtonPressed += HostGame;
    }

    public override void _ExitTree()
    {
        // Unsubscribe from the global event to avoid memory leaks
        EventManager.HostButtonPressed -= HostGame;
    }

    private void HostGame(string serverName)
    {
        ENetMultiplayerPeer peer = new();
        var error = peer.CreateServer(port, 2);
        if (error != Error.Ok)
        {
            GD.Print("could not host");
            return;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

        Multiplayer.MultiplayerPeer = peer;
        GD.Print("waiting for players");
        GetNode<ServerBrowser>("ServerBrowser").SetUpBroadcast(serverName);
    }

    private void JoinGame()
    {
        ENetMultiplayerPeer peer = new();

    }

}
