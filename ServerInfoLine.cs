using Godot;
using System;
using System.Net;

public partial class ServerInfoLine : HBoxContainer
{
    public ServerInfo serverInfo;
    private Button joinButton;
    private Label ipLabel;
    private Label playerCount;
    public override void _Ready()
    {
        joinButton = GetNode<Button>("%Join");

        ipLabel = GetNode<Label>("%IP");

        playerCount = GetNode<Label>("%PlayerCount");


        joinButton.Pressed += () =>
        {
            if (IsValidIPAddress(serverInfo.IP))
            {
                EventManager.EmitJoinPressed(serverInfo.IP);
            }
            else
            {
                GD.PrintErr("Invalid IP address: " + serverInfo.IP);
            }
        };
    }

    public override void _Process(double delta)
    {
        ipLabel.Text = serverInfo.IP;
        playerCount.Text = serverInfo.PlayerCount.ToString();
    }

    private bool IsValidIPAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }
}