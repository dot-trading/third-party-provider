namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class RedisSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 6379;
    
    public string ConnectionString => $"{Host}:{Port}";
}
