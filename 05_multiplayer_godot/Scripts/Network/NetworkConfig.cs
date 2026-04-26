namespace SwarmSurvivor;

public static class NetworkConfig
{
    public const int DefaultPort = 7777;
    public const string DefaultAddress = "127.0.0.1";
    public const int MaxPlayers = 4;
    public const int ServerPeerId = 1;

    public const float SnapshotRateHz = 20f;
    public const float SnapshotInterval = 1f / SnapshotRateHz;

    public const int ChannelLobby = 0;
    public const int ChannelSwarm = 1;
    public const int ChannelPlayer = 2;
    public const int ChannelInput = 3;
}
