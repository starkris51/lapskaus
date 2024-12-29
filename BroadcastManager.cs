using Godot;
using System;
using System.Text.Json;
using System.Linq;

public partial class BroadcastManager : Node
{
    [Export]
    private int listenPort = 8911;
    [Export]
    private int hostPort = 8918;
    [Export]
    private string broadcastAddress = "255.255.255.255";

    private PacketPeerUdp broadcaster;
    private Timer broadcastTimer;
    private ServerInfo serverInfo;

    public override void _Ready()
    {
        broadcastTimer = new Timer();
        AddChild(broadcastTimer);
        broadcastTimer.Connect("timeout", new Callable(this, nameof(OnBroadcastTimerTimeout)));
    }

    public void SetUpBroadcast(string name)
    {
        broadcaster = new PacketPeerUdp();
        string localIP = IP.GetLocalAddresses().FirstOrDefault(ip => ip.IsValidIPAddress()) ?? "127.0.0.1";

        serverInfo = new ServerInfo()
        {
            Name = name,
            PlayerCount = GameManager.Players.Count,
            IP = $"{localIP}:{hostPort}"
        };

        broadcaster.SetBroadcastEnabled(true);
        broadcaster.SetDestAddress(broadcastAddress, listenPort);

        var ok = broadcaster.Bind(hostPort);
        if (ok == Error.Ok)
        {
            GD.Print("Bound to broadcast port" + hostPort.ToString());
        }
        else
        {
            GD.Print("Failed to bind broadcast port");
        }
        broadcastTimer.Start();
    }

    private void OnBroadcastTimerTimeout()
    {
        GD.Print("Broadcasting💀");
        serverInfo.PlayerCount = GameManager.Players.Count;

        string json = JsonSerializer.Serialize(serverInfo);
        var packet = json.ToAsciiBuffer();

        broadcaster.PutPacket(packet);
    }

    public void CleanUp()
    {
        broadcastTimer.Stop();
        broadcaster?.Close();
    }
}