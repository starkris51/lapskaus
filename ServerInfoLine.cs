using Godot;
using System;
using System.Net;

public partial class ServerInfoLine : HBoxContainer
{
    public override void _Ready()
    {
        Button joinButton = GetNode<Button>("%Join");

        joinButton.Pressed += () =>
        {
            string ipAddress = GetNode<Label>("%IP").Text;

            if (IsValidIPAddress(ipAddress))
            {
                EventManager.EmitJoinPressed(ipAddress);
            }
            else
            {
                GD.PrintErr("Invalid IP address: " + ipAddress);
            }
        };
    }

    private bool IsValidIPAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }
}