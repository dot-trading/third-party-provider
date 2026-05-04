namespace TradingProject.ThirdParty.Domain.Models.Trading;

public record OrderResult(string OrderId, double ExecutedQty, double CummulativeQuoteQty, double Price);