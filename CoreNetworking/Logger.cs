using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fall2025GameClient.KaleNetworking;

internal class Logger
{
    public static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now}] {message}");
    }
}
