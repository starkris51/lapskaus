using Godot;
using System;
using System.Text.Json;

public partial class ServerBrowser : Control
{
    [Export]
    PacketPeerUdp broadcaster = new();
    [Export]
    PacketPeerUdp listener = new();
    [Export]
    int listenPort = 8080;
    [Export]
    int hostPort = 8081;
    [Export]
    string broadcastAdress = "192.168.1.255";

    Timer broadcastTimer;

    ServerInfo serverInfo;

    public override void _Ready()
    {
        broadcastTimer = GetNode<Timer>("BroadcastTimer");
        SetUpListener();
    }

    private void SetUpListener()
    {
        var ok = listener.Bind(listenPort);

        if (ok == Error.Ok)
        {
            GD.Print("Bound to listen port" + listenPort.ToString());
        }
        else
        {
            GD.Print("Failed to bound listen port");
        }
    }

    private void SetUpBroadcast(string name)
    {
        serverInfo = new ServerInfo()
        {
            Name = name,
            PlayerCount = 34 //insert player count from gameManager
        };

        broadcaster.SetBroadcastEnabled(true);
        broadcaster.SetDestAddress(broadcastAdress, listenPort);

        var ok = broadcaster.Bind(hostPort);
        if (ok == Error.Ok)
        {
            GD.Print("Bound to broadcast port" + hostPort.ToString());
        }
        else
        {
            GD.Print("Failed to bound broadcast port");
        }

        broadcastTimer.Start();
    }

    public override void _Process(double delta)
    {
        if (listener.GetAvailablePacketCount() > 0)
        {
            string serverIP = listener.GetPacketIP();
            int serverPort = listener.GetPacketPort();
            byte[] bytes = listener.GetPacket();
            ServerInfo info = JsonSerializer.Deserialize<ServerInfo>(bytes.GetStringFromAscii());
            GD.Print(serverIP, serverPort, info);
        }
    }

    private void _on_broadcast_timer_timeout()
    {
        GD.Print("Broadcasting💀");
        serverInfo.PlayerCount += 1; //insert player count from gameManager

        string json = JsonSerializer.Serialize(serverInfo);
        var packet = json.ToAsciiBuffer();

        broadcaster.PutPacket(packet);
    }

}
