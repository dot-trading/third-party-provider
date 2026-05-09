using MediatR;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;

namespace TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;

public record PlaceMarketBuyCommand(string Symbol, double QuoteOrderQty) : IRequest<BinanceOrderResultDto>;

public class PlaceMarketBuyCommandHandler(IBinanceService binanceService)
    : IRequestHandler<PlaceMarketBuyCommand, BinanceOrderResultDto>
{
    public async Task<BinanceOrderResultDto> Handle(PlaceMarketBuyCommand request, CancellationToken cancellationToken)
    {
        var result = await binanceService.PlaceMarketBuyAsync(request.Symbol, request.QuoteOrderQty, cancellationToken);
        return new BinanceOrderResultDto(result.OrderId.ToString(), result.ExecutedQty, result.CumulativeQuoteQty, result.Price);
    }
}
