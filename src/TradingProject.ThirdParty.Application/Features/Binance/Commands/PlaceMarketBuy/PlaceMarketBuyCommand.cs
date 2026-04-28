using MediatR;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models;

namespace TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;

public record PlaceMarketBuyCommand(string Symbol, double QuoteOrderQty) : IRequest<OrderResult>;

public class PlaceMarketBuyCommandHandler(IBinanceService binanceService)
    : IRequestHandler<PlaceMarketBuyCommand, OrderResult>
{
    public Task<OrderResult> Handle(PlaceMarketBuyCommand request, CancellationToken cancellationToken)
        => binanceService.PlaceMarketBuyAsync(request.Symbol, request.QuoteOrderQty, cancellationToken);
}
