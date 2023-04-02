using HasoVM.Core.Helper;
using System;

namespace HasoVM.Core.Helper
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string t, string m)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            var time = DateTime.Now;
            Console.WriteLine($"-> {time.Hour:00}:{time.Minute:00}:{time.Second:00}.{time.Millisecond:000} [{t}]: {m}");
        }
        public void Error(string t, string m)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            var time = DateTime.Now;
            Console.WriteLine($"-> {time.Hour:00}:{time.Minute:00}:{time.Second:00}.{time.Millisecond:000} [{t}]: {m}");
        }
    }
}