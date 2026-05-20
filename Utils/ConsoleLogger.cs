using System.Runtime.CompilerServices;
using Webscraper.Interfaces;

namespace Webscraper.Utils
{
    internal class ConsoleLogger : ILogger<ConsoleLogger>
    {
        public void Error(string message, [CallerMemberName] string caller = "Unknown")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("[Error - {0}]: {1}", caller, message));
            Console.ResetColor();
        }

        public void Info(string message, [CallerMemberName] string caller = "Unknown")
        {
            Console.ResetColor();
            Console.WriteLine(string.Format("[Info - {0}]: {1}", caller, message));
        }

        public void Warn(string message, [CallerMemberName] string caller = "Unknown")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(string.Format("[Warn - {0}]: {1}", caller, message));
            Console.ResetColor();
        }
    }
}
