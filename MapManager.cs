using Godot;
using System;

public partial class MapManager : Node
{
    PackedScene PlayerScene = (PackedScene)ResourceLoader.Load("res://Player.tscn");
    public override void _Ready()
    {
        EventManager.LoadMap += OnLoadMap;
    }

    public override void _ExitTree()
    {
        EventManager.LoadMap -= OnLoadMap;
    }

    private void OnLoadMap(string mapPath)
    {
        PackedScene mapScene = (PackedScene)ResourceLoader.Load(mapPath);
        Node mapInstance = mapScene.Instantiate();

        Node currentScene = GetTree().CurrentScene;
        if (currentScene != null)
        {
            currentScene.QueueFree();
        }

        GetTree().Root.AddChild(mapInstance);
        GetTree().CurrentScene = mapInstance;
    }
}
