namespace Fall2025GameClient.KaleNetworking;

internal class ServerClient : IDisposable
{
    private static UdpGateway udpGateway;
    private static ConcurrentDictionary<uint, IPEndPoint> udpEpMapping = new ConcurrentDictionary<uint, IPEndPoint>();

    private uint clientId;
    private TcpGateway tcpGateway;

    public delegate void DisconnectedEventHandler(uint clientId);
    public event DisconnectedEventHandler? OnDisconnected;

    public ServerClient(TcpClient tcpClient, uint clientId)
    {
        this.clientId = clientId;
        tcpGateway = new TcpGateway(tcpClient);
        tcpGateway.OnDisconnected += (sender, e)
            =>
        { OnDisconnected?.Invoke(clientId); };
        Task.Run(() => tcpGateway.SendAsync(new ClientIdMessage(clientId)));
        Logger.Log("Sent client ID to client.");
    }

    static ServerClient()
    {
        udpGateway = new UdpGateway(new IPEndPoint(IPAddress.Any, NetworkInfo.serverUdpPort));
    }

    public void SetUdpEndPoint(IPEndPoint clientEp)
    {
        udpEpMapping[clientId] = clientEp;
    }

    public void StartListening()
    {
        tcpGateway.StartListening();
        udpGateway.StartListening();
    }

    public async Task SendTcpAsync(NetworkMessage message)
        => await tcpGateway.SendAsync(message);

    public static async Task SendUdpAsync(uint clientId, NetworkMessage message)
    {
        if (udpEpMapping.TryGetValue(clientId, out IPEndPoint? clientEp))
            await udpGateway.SendAsync(message, clientEp);
    }

    public void Dispose()
    {
        tcpGateway.Dispose();
    }
}
