namespace Fall2025GameClient.GameNetworking.Runtime.CoreNetworking;

internal class NetworkMessageManager
{
    private static readonly Dictionary<MessageType, Action<NetworkMessage, IPEndPoint>> handlers = new();

    static NetworkMessageManager()
    {
        RegisterDefaultHandlers();
    }

    public static void RegisterHandler(MessageType type, Action<NetworkMessage, IPEndPoint> handler)
    {
        if (handlers.TryGetValue(type, out var val))
            throw new ArgumentException($"Handler for message type {type} is already registered.");
        handlers[type] = handler;
    }

    private static void RegisterDefaultHandlers()
    {
        RegisterHandler(MessageType.Text, (msg, ep) =>
        {
            TextMessage text = (TextMessage)msg;
            Logger.Log($"Text message received from {ep.ToString()}: {text.GetText()}.");
        });
    }

    public static void HandleMessage(byte[] messageBytes, IPEndPoint remoteEp)
    {
        NetworkMessage message = NetworkMessage.FromBytes(messageBytes);
        if (handlers.TryGetValue((MessageType)message.Type, out var handler))
            handler(message, remoteEp);
        else
            Logger.Log($"Unhandled message type: {(MessageType)message.Type}");
    }
}
