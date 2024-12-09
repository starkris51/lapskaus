using System;

public static class EventManager
{
    public static event Action<string> HostButtonPressed;

    public static void EmitHostPressed(string serverName)
    {
        HostButtonPressed?.Invoke(serverName);
    }
}