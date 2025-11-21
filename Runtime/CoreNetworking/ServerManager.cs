namespace Fall2025GameClient.GameNetworking.Runtime.CoreNetworking;

internal class ServerManager
{
    public static Server StartServer(int serverPort)
    {
        Server server = new Server(serverPort);
        _ = Task.Run(server.Start);
        return server;
    }
}
