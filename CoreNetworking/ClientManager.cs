using System.Net;
using System.Net.Sockets;
namespace Fall2025GameClient.KaleNetworking;

internal class ClientManager
{
    private const int connectionRetryDelayMs = 2000;

    public static async Task<Client> StartClient(string serverIp, int remoteTcpPort, int remoteUdpPort)
    {
        TcpClient tcpClient = new TcpClient();
        Logger.Log("Connecting to server...");
        while (true)
        {
            try
            {
                await tcpClient.ConnectAsync(serverIp, remoteTcpPort);
                break;
            }
            catch (SocketException e)
            {
                Logger.Log($"Connection failed: {e.Message}. Retrying in {connectionRetryDelayMs} ms...");
                await Task.Delay(connectionRetryDelayMs);
            }
        }
        Client client = new Client(tcpClient, remoteUdpPort);
        Logger.Log("Connected to server.");
        client.StartListening();
        return client;
    }
}
