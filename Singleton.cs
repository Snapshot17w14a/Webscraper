namespace Webscraper
{
    internal class Singleton<T>(T instance) where T : class
    {
        public readonly T Instance = instance;
    }
}
