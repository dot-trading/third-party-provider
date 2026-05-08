using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class AlternativeMeSettings
{
    [Required, Url]
    public required string BaseUrl { get; set; }
}