using MediatR;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Trading;

namespace TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;

public record PlaceMarketBuyCommand(string Symbol, double QuoteOrderQty) : IRequest<OrderResult>;

public class PlaceMarketBuyCommandHandler(IBinanceService binanceService)
    : IRequestHandler<PlaceMarketBuyCommand, OrderResult>
{
    public async Task<OrderResult> Handle(PlaceMarketBuyCommand request, CancellationToken cancellationToken)
    {
        var result = await binanceService.PlaceMarketBuyAsync(request.Symbol, request.QuoteOrderQty, cancellationToken);
        return new OrderResult(result.OrderId.ToString(), result.ExecutedQty, result.CumulativeQuoteQty, result.Price);
    }
}
