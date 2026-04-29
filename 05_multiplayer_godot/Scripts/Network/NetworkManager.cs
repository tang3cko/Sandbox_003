namespace SwarmSurvivor;

using Godot;

public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    [Signal] public delegate void ServerStartedEventHandler();
    [Signal] public delegate void ClientConnectedEventHandler();
    [Signal] public delegate void ConnectionFailedEventHandler();
    [Signal] public delegate void ServerDisconnectedEventHandler();
    [Signal] public delegate void PeerJoinedEventHandler(long peerId);
    [Signal] public delegate void PeerLeftEventHandler(long peerId);

    public bool IsServer => Multiplayer.MultiplayerPeer != null
        && Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected
        && Multiplayer.IsServer();

    public bool IsClient => Multiplayer.MultiplayerPeer != null
        && Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected
        && !Multiplayer.IsServer();

    public bool IsHeadless { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;

        IsHeadless = OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless";
        if (IsHeadless)
        {
            Callable.From(StartHeadlessServer).CallDeferred();
        }
    }

    public Error StartServer(int port = NetworkConfig.DefaultPort, int maxClients = NetworkConfig.MaxPlayers)
    {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, maxClients);
        if (error != Error.Ok)
        {
            NetLog.Error($"[NetworkManager] Failed to create server on port {port}: {error}");
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        NetLog.Info($"[NetworkManager] Server started on port {port} (max clients: {maxClients})");
        EmitSignal(SignalName.ServerStarted);
        return Error.Ok;
    }

    public Error StartClient(string address, int port = NetworkConfig.DefaultPort)
    {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(address, port);
        if (error != Error.Ok)
        {
            NetLog.Error($"[NetworkManager] Failed to create client to {address}:{port}: {error}");
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        NetLog.Info($"[NetworkManager] Connecting to {address}:{port}...");
        return Error.Ok;
    }

    public void Disconnect()
    {
        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
            Multiplayer.MultiplayerPeer = null;
            NetLog.Info("[NetworkManager] Disconnected");
        }
    }

    private void StartHeadlessServer()
    {
        NetLog.Info("[NetworkManager] Headless mode detected, starting dedicated server");
        var error = StartServer();
        if (error != Error.Ok) return;

        GetTree().ChangeSceneToFile("res://Scenes/MainMultiplayer.tscn");
    }

    private void OnPeerConnected(long peerId)
    {
        NetLog.Info($"[NetworkManager] Peer joined: {peerId}");
        EmitSignal(SignalName.PeerJoined, peerId);
    }

    private void OnPeerDisconnected(long peerId)
    {
        NetLog.Info($"[NetworkManager] Peer left: {peerId}");
        EmitSignal(SignalName.PeerLeft, peerId);
    }

    private void OnConnectedToServer()
    {
        NetLog.Info("[NetworkManager] Connected to server");
        EmitSignal(SignalName.ClientConnected);
    }

    private void OnConnectionFailed()
    {
        NetLog.Error("[NetworkManager] Connection failed");
        Multiplayer.MultiplayerPeer = null;
        EmitSignal(SignalName.ConnectionFailed);
    }

    private void OnServerDisconnected()
    {
        NetLog.Info("[NetworkManager] Server disconnected");
        Multiplayer.MultiplayerPeer = null;
        EmitSignal(SignalName.ServerDisconnected);
    }
}
