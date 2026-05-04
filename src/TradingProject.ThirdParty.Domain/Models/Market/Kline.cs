namespace TradingProject.ThirdParty.Domain.Models.Market;

public record Kline(long OpenTime, double Open, double High, double Low, double Close, double Volume);