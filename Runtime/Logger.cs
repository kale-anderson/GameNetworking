namespace Fall2025GameClient.GameNetworking.Runtime
{
    internal class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }
    }
}


