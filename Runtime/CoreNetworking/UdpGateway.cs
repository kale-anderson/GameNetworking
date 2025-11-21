using System;
using System.ComponentModel.Design;

namespace Fall2025GameClient.GameNetworking.Runtime.CoreNetworking;

// TODO
// - UDP packet ordering (plus duplicate handling)
internal class UdpGateway
{
    private UdpClient udpClient;
    private CancellationTokenSource cts = new();
    private SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

    private bool listening = false;
    private bool connected = false;

    public UdpGateway(IPEndPoint localEp, IPEndPoint? remoteEp = null)
    {
        connected = remoteEp != null;
        udpClient = new UdpClient(localEp);

        if (connected)
        {
            if (remoteEp == null)
                throw new ArgumentNullException(nameof(remoteEp), "Remote endpoint must be provided to connect UDP client.");
            udpClient.Connect(remoteEp);
        }
    }

    public async Task SendAsync(NetworkMessage message, IPEndPoint? ep = null)
    {
        if (ep == null && !connected)
            throw new ArgumentNullException(nameof(ep), "Endpoint must be provided when UDP client is not connected.");
        else if (ep != null && connected)
            throw new ArgumentException("Endpoint should not be provided when UDP client is connected.", nameof(ep));

        await sendLock.WaitAsync();
        try
        {
            byte[] messageBuffer = message.ToByteArray();
            if (ep == null)
                await udpClient.SendAsync(messageBuffer, messageBuffer.Length);
            else
                await udpClient.SendAsync(messageBuffer, messageBuffer.Length, ep);
        }
        finally
        {
            sendLock.Release();
        }
    }

    public void StartListening()
    {
        if (listening) return;
        listening = true;
        _ = Task.Run(ReceiveLoop).ContinueWith(t =>
            { if (t.Exception != null) Console.WriteLine(t.Exception); });
    }

    public async Task ReceiveLoop()
    {
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
                NetworkMessageManager.HandleMessage(result.Buffer, result.RemoteEndPoint);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("UDP receive loop cancelled.");
        }
    }
}
