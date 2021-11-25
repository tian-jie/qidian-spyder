using System;
using System.Threading;

namespace BookFinderFullSolution
{
    public class ConsoleLogger
    {
#nullable enable
        public static void Info(string format, params object?[]? arg)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            var format1 = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} [INFO] - [{Thread.CurrentThread.ManagedThreadId}] - " + format;
            Console.WriteLine(format1, arg);
        }

        public static void Debug(string format, params object?[]? arg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            var format1 = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} [DEBUG] - [{Thread.CurrentThread.ManagedThreadId}] - " + format;
            Console.WriteLine(format1, arg);
        }

        public static void Warning(string format, params object?[]? arg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            var format1 = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} [WARN] - [{Thread.CurrentThread.ManagedThreadId}] - " + format;
            Console.WriteLine(format1, arg);
        }

        public static void Error(string format, params object?[]? arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            var format1 = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} [ERROR] - [{Thread.CurrentThread.ManagedThreadId}] - " + format;
            Console.WriteLine(format1, arg);
        }

        public static void Critical(string format, params object?[]? arg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            var format1 = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} [CRITICAL] - [{Thread.CurrentThread.ManagedThreadId}] - " + format;
            Console.WriteLine(format1, arg);
        }
#nullable enable
    }


}
