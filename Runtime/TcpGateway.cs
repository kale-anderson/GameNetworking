using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fall2025GameClient.GameNetworking.Runtime;

internal class TcpGateway : IDisposable
{
    private CancellationTokenSource cts = new();
    private SemaphoreSlim sendLock = new(1, 1);

    private TcpClient tcpClient;
    private NetworkStream stream;
    private IPEndPoint remoteEndPoint;

    private const uint maxBufferSize = 4096;
    private bool listening = false;
    public bool connected { get; private set; } = false;

    public event EventHandler? OnDisconnected;

    public TcpGateway(TcpClient client)
    {
        tcpClient = client ?? throw new ArgumentNullException(nameof(client));
        stream = tcpClient.GetStream();
        connected = true;
        remoteEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint
                ?? throw new NullReferenceException("TcpClient RemoteEndPoint cannot be null");
    }

    public async Task SendAsync(NetworkMessage message)
    {
        await sendLock.WaitAsync();
        try
        {
            byte[] messageBuffer = message.ToByteArray();
            await stream.WriteAsync(messageBuffer);
        }
        catch (ObjectDisposedException)
        {
            Logger.Log("Trying to send on disposed stream. Ensure client is disconnected properly");
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

    private async Task ReceiveLoop()
    {
        byte[] receiveBuffer = new byte[maxBufferSize];
        List<byte> buffer = new();
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length, cts.Token);
                }
                catch (Exception e) when (e is IOException || e is SocketException)
                { break; }
                if (bytesRead == 0) break;

                buffer.AddRange(receiveBuffer.Take(bytesRead));
                while (buffer.Count >= NetworkMessage.lengthFieldSize)
                {
                    ushort length = BitConverter.ToUInt16(buffer.ToArray(), 0);
                    if (buffer.Count < length + NetworkMessage.lengthFieldSize) break;

                    byte[] messageData = buffer.GetRange(0, length + NetworkMessage.lengthFieldSize).ToArray();
                    buffer.RemoveRange(0, length + NetworkMessage.lengthFieldSize);

                    NetworkMessageManager.HandleMessage(messageData, remoteEndPoint);
                }
            }
        }
        catch (OperationCanceledException)
        { Logger.Log("TCP receive loop cancelled."); }
        Logger.Log("TCP connection closed.");
        Dispose();
    }

    public void Dispose()
    {
        connected = false;
        OnDisconnected?.Invoke(this, EventArgs.Empty);
        cts.Cancel();
        stream?.Dispose();
        tcpClient?.Dispose();
    }
}
