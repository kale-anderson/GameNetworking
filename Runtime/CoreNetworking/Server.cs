namespace Fall2025GameClient.GameNetworking.Runtime.CoreNetworking;

internal class Server : IDisposable
{
    private TcpListener server;

    private bool started = false;
    private uint nextClientId = 1;

    private ConcurrentDictionary<uint, ServerClient> clients = new();

    public Server(int port)
    {
        server = new TcpListener(IPAddress.Any, port);

        NetworkMessageManager.RegisterHandler(MessageType.UDPConnect, (msg, ep) =>
        {
            UDPConnectMessage udpMsg = (UDPConnectMessage)msg;

            if (udpMsg.ack) return;
            if (clients.TryGetValue(udpMsg.clientId, out ServerClient? client))
            {
                client.SetUdpEndPoint(ep);
                Task.Run(() => client.SendTcpAsync(new UDPConnectMessage(true, udpMsg.clientId)));
                Logger.Log("UDP connection established.");
            }
            else
                Logger.Log("Received UDPConnectMessage from unknown client.");
        });
    }

    public async Task Start()
    {
        if (started) return;
        started = true;
        server.Start();
        Logger.Log($"Server started at {server.LocalEndpoint.ToString()}.");
        await AcceptLoop();
    }

    private async Task AcceptLoop()
    {
        while (true)
        {
            TcpClient tcpClient = await server.AcceptTcpClientAsync();
            ServerClient client = new ServerClient(tcpClient, nextClientId);
            if (!clients.TryAdd(nextClientId, client))
                throw new Exception("Failed to add new client to dictionary.");
            Logger.Log("Client connected.");
            client.StartListening();
            client.OnDisconnected += (clientId) =>
            {
                if (clients.TryRemove(clientId, out ServerClient? removedClient))
                {
                    removedClient.Dispose();
                    Logger.Log($"Client {clientId} disconnected.");
                }
            };
            ++nextClientId;
        }
    }

    public async Task SendTcpAsync(uint clientId, NetworkMessage message)
    {
        if (clients.TryGetValue(clientId, out ServerClient? client))
            await client.SendTcpAsync(message);
    }

    public async Task SendUdpAsync(uint clientId, NetworkMessage message)
    {
        if (clients.TryGetValue(clientId, out ServerClient? client))
            await ServerClient.SendUdpAsync(clientId, message);
    }

    public void Dispose()
    {
        foreach (var client in clients.Values)
            client.Dispose();
        server.Stop();
    }
}
