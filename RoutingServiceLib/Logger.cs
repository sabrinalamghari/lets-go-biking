using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoutingServiceLib
{
    public static class Logger
    {
        public static void Info(string msg)
            => Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {msg}");

        public static void Warn(string msg)
            => Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} {msg}");

        public static void Error(string msg, Exception ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {msg}");
            if (ex != null)
                Console.WriteLine($"        → {ex.Message}");
            Console.ResetColor();
        }
    }
}
