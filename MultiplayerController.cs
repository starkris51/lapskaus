using Godot;
using System;

public partial class MultiplayerController : Control
{
    [Export]
    int port = 8079;


    public override void _Ready()
    {
    }

    private void HostGame()
    {
        ENetMultiplayerPeer peer = new();
        var error = peer.CreateServer(port, 24);
        if (error != Error.Ok)
        {
            GD.Print("could not host");
            return;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

        Multiplayer.MultiplayerPeer = peer;

    }

    private void _on_host_button_down()
    {

    }
}
