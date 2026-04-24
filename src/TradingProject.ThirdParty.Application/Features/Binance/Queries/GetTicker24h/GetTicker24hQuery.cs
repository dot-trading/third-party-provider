using MediatR;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;

public record GetTicker24hQuery(string Symbol) : IRequest<Ticker24h?>;

public class GetTicker24hQueryHandler(IBinanceService binanceService) : IRequestHandler<GetTicker24hQuery, Ticker24h?>
{
    public async Task<Ticker24h?> Handle(GetTicker24hQuery request, CancellationToken cancellationToken)
    {
        return await binanceService.GetTicker24hAsync(request.Symbol, cancellationToken);
    }
}
