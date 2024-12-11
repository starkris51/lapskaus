using Godot;
using System;

public partial class MapManager : Node
{
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
        GetTree().Root.AddChild(mapInstance);
    }
}
