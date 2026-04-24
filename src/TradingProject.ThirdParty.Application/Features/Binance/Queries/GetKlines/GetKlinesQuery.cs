using MediatR;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;

public record GetKlinesQuery(string Symbol, string Interval = "1h", int Limit = 24) : IRequest<List<Kline>>;

public class GetKlinesQueryHandler(IBinanceService binanceService) : IRequestHandler<GetKlinesQuery, List<Kline>>
{
    public async Task<List<Kline>> Handle(GetKlinesQuery request, CancellationToken cancellationToken)
    {
        return await binanceService.GetKlinesAsync(request.Symbol, request.Interval, request.Limit, cancellationToken);
    }
}
