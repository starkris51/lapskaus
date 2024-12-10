using System;

public static class EventManager
{
    public static event Action<string> HostButtonPressed;
    public static event Action<string> JoinButtonPressed;

    public static void EmitHostPressed(string serverName)
    {
        HostButtonPressed?.Invoke(serverName);
    }

    public static void EmitJoinPressed(string ip)
    {
        JoinButtonPressed?.Invoke(ip);
    }
}