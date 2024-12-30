using Godot;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;

public partial class ServerBrowser : Control
{
    [Export]
    PacketPeerUdp listener = new();
    [Export]
    int listenPort = 8911;
    [Export]
    PackedScene ServerInfo;

    public override void _Ready()
    {
        SetUpListener();
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

    public override void _Process(double delta)
    {
        if (listener.GetAvailablePacketCount() > 0)
        {
            string serverIP = listener.GetPacketIP();
            int serverPort = listener.GetPacketPort();
            byte[] bytes = listener.GetPacket();

            if (serverIP == "") { return; }

            ServerInfo info = new();
            try
            {
                info = JsonSerializer.Deserialize<ServerInfo>(bytes);
                info.IP = serverIP;
                GD.Print($"Server Name: {info.Name}, ServerIP: {serverIP}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to deserialize JSON: {ex.Message}");
            }

            VBoxContainer browserContainer = GetNode<VBoxContainer>("%BrowserContainer");
            ServerInfoLine currentNode = browserContainer.GetChildren()
                .OfType<ServerInfoLine>()
                .FirstOrDefault(x => x.serverInfo.IP == serverIP && x.serverInfo.Name == info.Name);

            if (currentNode != null)
            {
                currentNode.serverInfo.Name = info.Name;
                currentNode.serverInfo.PlayerCount = info.PlayerCount;
                return;
            }

            ServerInfoLine serverInfo = ServerInfo.Instantiate<ServerInfoLine>();
            serverInfo.serverInfo = info;
            browserContainer.AddChild(serverInfo);
        }
    }
}
