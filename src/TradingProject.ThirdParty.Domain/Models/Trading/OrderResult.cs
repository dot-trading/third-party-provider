namespace TradingProject.ThirdParty.Domain.Models.Trading;

public record OrderResult(string OrderId, double ExecutedQty, double CumulativeQuoteQty, double Price);