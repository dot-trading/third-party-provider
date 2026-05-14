using MediatR;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;

namespace TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketSell;

public record PlaceMarketSellCommand(string Symbol, decimal Quantity) : IRequest<BinanceOrderResultDto>;

public class PlaceMarketSellCommandHandler(IBinanceService binanceService)
    : IRequestHandler<PlaceMarketSellCommand, BinanceOrderResultDto>
{
    public async Task<BinanceOrderResultDto> Handle(PlaceMarketSellCommand request, CancellationToken cancellationToken)
    {
        var result = await binanceService.PlaceMarketSellAsync(request.Symbol, request.Quantity, cancellationToken);
        return new BinanceOrderResultDto(result.OrderId.ToString(), result.ExecutedQty, result.CumulativeQuoteQty, result.Price);
    }
}
