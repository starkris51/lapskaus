using Godot;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public partial class MultiplayerManager : Node
{
    [Export]
    private int port = 8080;
    [Signal]
    public delegate void ServerHostedEventHandler(string serverName);

    [Signal]
    public delegate void ServerJoinedEventHandler(string ip);
    BroadcastManager broadcastManager;

    private bool isMapLoaded = false;
    private bool isPlayerInfoReceived = false;

    private const string DEFAULT_MAP_PATH = "res://Maps/dm_test.tscn";

    PackedScene PlayerScene;

    public override void _Ready()
    {
        EventManager.HostButtonPressed += HostGame;
        EventManager.JoinButtonPressed += JoinGame;
        EventManager.StartButtonPressed += StartGame;

        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;
        Multiplayer.ServerDisconnected += ServerDisconnected;

        PlayerScene = (PackedScene)ResourceLoader.Load("res://player.tscn");

        broadcastManager = GetNode<BroadcastManager>("/root/BroadcastManager");

        if (OS.GetCmdlineArgs().Contains("--server"))
        {
            HostGame("test");
        }
    }

    private void ServerDisconnected()
    {
        GD.Print("Disconnected from server");
    }

    private void ConnectedToServer()
    {
        RpcId(1, nameof(sendPlayerData), "player", Multiplayer.MultiplayerPeer.GetUniqueId());
    }

    public override void _ExitTree()
    {
        EventManager.HostButtonPressed -= HostGame;
        EventManager.JoinButtonPressed -= JoinGame;
        isMapLoaded = false;
    }
    private void HostGame(string serverName)
    {
        ENetMultiplayerPeer peer = new();
        var error = peer.CreateServer(port, 5);
        if (error != Error.Ok)
        {
            GD.Print("could not host");
            return;
        }
        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

        Multiplayer.MultiplayerPeer = peer;

        broadcastManager.SetUpBroadcast(serverName);

        sendPlayerData("Player", Multiplayer.MultiplayerPeer.GetUniqueId());
    }
    public async void JoinGame(string ip)
    {
        ENetMultiplayerPeer peer = new();
        var error = peer.CreateClient(ip, port);
        if (error != Error.Ok)
        {
            GD.Print("could not join");
            return;
        }
        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

        Multiplayer.MultiplayerPeer = peer;

        int timeout = 5000;
        int elapsed = 0;
        while (Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connecting)
        {
            await Task.Delay(100);
            elapsed += 100;

            if (elapsed >= timeout)
            {
                GD.Print("Connection timed out");
                Multiplayer.MultiplayerPeer = null;
                return;
            }
        }
        if (Multiplayer.MultiplayerPeer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
        {
            GD.Print("Failed to connect to server");
            return;
        }
    }
    private void OnPlayerConnected(long id)
    {
        GD.Print($"Player {id} connected");

    }
    private void OnPlayerDisconnected(long id)
    {
        GD.Print($"Player {id} disconnected");

        var playerNode = GetTree().CurrentScene.GetNodeOrNull(id.ToString());
        playerNode?.QueueFree();

        var player = GameManager.Players.FirstOrDefault(p => p.Id == id);
        if (player != null)
        {
            GameManager.Players.Remove(player);
            Rpc(nameof(RemovePlayer), id);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RemovePlayer(long id)
    {
        var playerNode = GetTree().CurrentScene.GetNodeOrNull(id.ToString());
        playerNode?.QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void sendPlayerData(string name, int id)
    {
        PlayerInfo playerInfo = new()
        {
            Name = name,
            Id = id,
        };
        if (!GameManager.Players.Contains(playerInfo))
        {
            GameManager.Players.Add(playerInfo);
        }

        if (Multiplayer.IsServer())
        {
            foreach (var item in GameManager.Players)
            {
                Rpc(nameof(sendPlayerData), item.Name, item.Id);
            }
        }
    }

    public void StartGame()
    {
        Rpc(nameof(LoadGame));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void LoadGame()
    {
        PackedScene mapScene = (PackedScene)ResourceLoader.Load(DEFAULT_MAP_PATH);
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
