using MediatR;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Trading;

namespace TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketSell;

public record PlaceMarketSellCommand(string Symbol, double Quantity) : IRequest<OrderResult>;

public class PlaceMarketSellCommandHandler(IBinanceService binanceService)
    : IRequestHandler<PlaceMarketSellCommand, OrderResult>
{
    public Task<OrderResult> Handle(PlaceMarketSellCommand request, CancellationToken cancellationToken)
        => binanceService.PlaceMarketSellAsync(request.Symbol, request.Quantity, cancellationToken);
}
