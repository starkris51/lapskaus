using Godot;
using System;
using System.Linq;

public partial class MultiplayerManager : Node
{
    [Export]
    private int port = 8080;
    [Signal]
    public delegate void ServerHostedEventHandler(string serverName);

    [Signal]
    public delegate void ServerJoinedEventHandler(string ip);


    public override void _Ready()
    {
        EventManager.HostButtonPressed += HostGame;
        EventManager.JoinButtonPressed += JoinGame;

        if (OS.GetCmdlineArgs().Contains("--server"))
        {
            HostGame("test");
        }
    }

    public override void _ExitTree()
    {
        EventManager.HostButtonPressed -= HostGame;
        EventManager.JoinButtonPressed -= JoinGame;
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
        EmitSignal(nameof(ServerHosted), serverName);
    }

    private void JoinGame(string ip)
    {
        ENetMultiplayerPeer peer = new();
        var error = peer.CreateClient(ip, port);
        if (error != Error.Ok)
        {
            GD.Print("could not join");
            return;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        Multiplayer.MultiplayerPeer = peer;
        EmitSignal(nameof(ServerJoined), ip);
    }

}
