using System.Runtime.CompilerServices;

namespace Webscraper.Interfaces
{
    internal interface ILogger<T> : ILogger where T : ILogger { }

    internal interface ILogger
    {
        public void Info(string message, [CallerMemberName] string caller = "Unknown");
        public void Warn(string message, [CallerMemberName] string caller = "Unknown");
        public void Error(string message, [CallerMemberName] string caller = "Unknown");
    }
}
