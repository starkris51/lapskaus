using Godot;
using System;

public partial class ServerCreator : Control
{
    public override void _Ready()
    {
        Button hostButton = GetNode<Button>("%Host");
        TextEdit serverName = GetNode<TextEdit>("%ServerName");

        hostButton.Pressed += () =>
        {
            EventManager.EmitHostPressed(serverName.Text);
        };
    }
}
