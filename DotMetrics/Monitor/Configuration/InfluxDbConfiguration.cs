namespace DotMetrics.Monitor.Configuration
{
    public class InfluxDbConfiguration
    {
        public string Url { get; }
        public string Username { get; }
        public string Password { get; }
        public string Database { get; }

        public InfluxDbConfiguration(string url, string username, string password, string database)
        {
            Url = url;
            Username = username;
            Password = password;
            Database = database;
        }
    }
}