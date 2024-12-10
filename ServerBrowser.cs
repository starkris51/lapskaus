using Godot;
using System;
using System.Linq;
using System.Text.Json;

public partial class ServerBrowser : Control
{
    [Export]
    PacketPeerUdp broadcaster;
    [Export]
    PacketPeerUdp listener = new();
    [Export]
    int listenPort = 8911;
    [Export]
    int hostPort = 8912;
    [Export]
    string broadcastAdress = "localhost";
    [Export]
    PackedScene ServerInfo;

    Timer broadcastTimer;

    ServerInfo serverInfo;

    MultiplayerManager multiplayerManager;

    public override void _Ready()
    {
        broadcastTimer = GetNode<Timer>("BroadcastTimer");
        SetUpListener();

        multiplayerManager = GetNode<MultiplayerManager>("/root/MultiplayerManager");
        multiplayerManager.Connect("ServerHosted", new Callable(this, nameof(OnServerHosted)));
    }

    private void SetUpListener()
    {
        var ok = listener.Bind(listenPort);
        Label bound = GetNode<Label>("%Bound");

        if (ok == Error.Ok)
        {
            GD.Print("Bound to listen port" + listenPort.ToString());
            bound.Text = "Bound to listen port" + listenPort.ToString();
        }
        else
        {
            GD.Print("Failed to bound listen port");
            bound.Text = "Failed to bound listen port";
        }
    }

    public void SetUpBroadcast(string name)
    {
        broadcaster = new PacketPeerUdp();
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

    private void OnServerHosted(string serverName)
    {
        SetUpBroadcast(serverName);
    }

    public override void _Process(double delta)
    {
        if (listener.GetAvailablePacketCount() > 0)
        {
            string serverIP = listener.GetPacketIP();
            int serverPort = listener.GetPacketPort();
            byte[] bytes = listener.GetPacket();
            ServerInfo info = new();
            try
            {
                info = JsonSerializer.Deserialize<ServerInfo>(bytes);
                GD.Print($"Server Name: {info.Name}, ServerIP: {serverIP}");

            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to deserialize JSON: {ex.Message}");
            }

            Node currentNode = GetNode<VBoxContainer>("%BrowserContainer").GetChildren().Where(x => x.Name == info.Name).FirstOrDefault();

            if (currentNode != null)
            {
                currentNode.GetNode<Label>("PlayerCount").Text = info.PlayerCount.ToString();
                currentNode.GetNode<Label>("IP").Text = serverIP;
                return;
            }

            ServerInfoLine serverInfo = ServerInfo.Instantiate<ServerInfoLine>();
            serverInfo.Name = info.Name;
            serverInfo.GetNode<Label>("%Name").Text = info.Name;
            serverInfo.GetNode<Label>("%IP").Text = serverIP;
            serverInfo.GetNode<Label>("%PlayerCount").Text = info.PlayerCount.ToString();
            GetNode("%BrowserContainer").AddChild(serverInfo);
        }
    }

    private void _on_broadcast_timer_timeout()
    {
        GD.Print("Broadcasting💀");
        serverInfo.PlayerCount = 1; //insert player count from gameManager

        string json = JsonSerializer.Serialize(serverInfo);
        var packet = json.ToAsciiBuffer();

        broadcaster.PutPacket(packet);
    }

    public void CleanUp()
    {
        listener.Close();
        broadcastTimer.Stop();
        broadcaster?.Close();
    }
}
