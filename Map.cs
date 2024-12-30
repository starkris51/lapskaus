using Godot;
using System;

public partial class Map : Node3D
{
    public override void _Ready()
    {
        CallDeferred(nameof(DeferredReady));
    }

    private void DeferredReady()
    {
        EventManager.EmitMapLoaded();
    }
}
