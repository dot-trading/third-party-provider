using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class RedisSettings
{
    [Required]
    public string Host { get; set; } = string.Empty;
    
    [Range(1, 65535)]
    public int Port { get; set; } = 6379;
    
    public string ConnectionString => $"{Host}:{Port}";
}
