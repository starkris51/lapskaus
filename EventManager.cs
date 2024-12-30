using System;

public static class EventManager
{
    public static event Action<string> HostButtonPressed;
    public static event Action<string> JoinButtonPressed;
    public static event Action StartButtonPressed;
    public static event Action<string> LoadMap;
    public static event Action MapLoaded;

    public static void EmitHostPressed(string serverName)
    {
        HostButtonPressed?.Invoke(serverName);
    }

    public static void EmitJoinPressed(string ip)
    {
        JoinButtonPressed?.Invoke(ip);
    }

    public static void EmitStartPressed()
    {
        StartButtonPressed?.Invoke();
    }

    public static void EmitLoadMap(string mapPath)
    {
        LoadMap?.Invoke(mapPath);
    }

    public static void EmitMapLoaded()
    {
        MapLoaded?.Invoke();
    }
}