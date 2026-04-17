using MediatR;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;

public record GetPriceQuery(string Symbol) : IRequest<double>;

public class GetPriceQueryHandler(IBinanceService binanceService) : IRequestHandler<GetPriceQuery, double>
{
    public async Task<double> Handle(GetPriceQuery request, CancellationToken cancellationToken)
    {
        return await binanceService.GetCurrentPriceAsync(request.Symbol, cancellationToken);
    }
}
