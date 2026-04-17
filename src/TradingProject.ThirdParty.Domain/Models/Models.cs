namespace TradingProject.ThirdParty.Domain.Models;

public record Balance(string Asset, double Free, double Locked);
public record TickerPrice(string Symbol, double Price);
