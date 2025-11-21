using System;
using System.IO;
using System.Net.Sockets;

namespace Fall2025GameClient.GameNetworking.Runtime;

internal class Client : IDisposable
{
    private TcpGateway tcpGateway;
    private UdpGateway udpGateway;

    private bool clientIdSet = false;
    private uint clientId;

    private bool listening = false;
    private bool udpConnected = false;

    public Client(TcpClient client, int remoteUdpPort)
    {
        IPEndPoint tcpEp = client.Client.RemoteEndPoint as IPEndPoint ?? throw new NullReferenceException("TcpClient remote endpoint is null");
        IPAddress serverIp = tcpEp.Address.MapToIPv4();
        tcpGateway = new TcpGateway(client);
        tcpGateway.OnDisconnected += (sender, e) =>
        {
            Logger.Log("Disconnected from server.");
        };
        udpGateway = new UdpGateway(
            new IPEndPoint(IPAddress.Any, 0),
            new IPEndPoint(serverIp, remoteUdpPort));

        NetworkMessageManager.RegisterHandler(MessageType.ClientId, (msg, ep) =>
        {
            if (clientIdSet) return;
            clientIdSet = true;
            clientId = ((ClientIdMessage)msg).clientId;
            Logger.Log($"Received client ID: {clientId}");
            _ = Task.Run(UdpConnect).ContinueWith(t =>
                { if (t.Exception != null) Console.WriteLine(t.Exception); });
        });
        NetworkMessageManager.RegisterHandler(MessageType.UDPConnect, (msg, ep) =>
        {
            if (!((UDPConnectMessage)msg).ack) return;
            udpConnected = true;
            Logger.Log("UDP connection established.");
        });
    }

    public async Task SendTcpAsync(NetworkMessage message)
        => await tcpGateway.SendAsync(message);

    public async Task SendUdpAsync(NetworkMessage message)
        => await udpGateway.SendAsync(message);

    public void StartListening()
    {
        tcpGateway.StartListening();
        udpGateway.StartListening();
    }

    private async Task UdpConnect()
    {
        UDPConnectMessage connectMsg = new UDPConnectMessage(false, clientId);
        while (!udpConnected)
        {
            Logger.Log("sending udp connect request");
            await SendUdpAsync(connectMsg);
            await Task.Delay(10);
        }
    }

    public void Dispose()
    {
        tcpGateway.Dispose();
    }
}
