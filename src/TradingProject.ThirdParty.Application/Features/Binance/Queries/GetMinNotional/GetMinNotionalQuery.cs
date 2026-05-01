using MediatR;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;

public record GetMinNotionalQuery(string Symbol) : IRequest<double>;

public class GetMinNotionalQueryHandler(IBinanceService binanceService) : IRequestHandler<GetMinNotionalQuery, double>
{
    public async Task<double> Handle(GetMinNotionalQuery request, CancellationToken cancellationToken)
    {
        return await binanceService.GetMinNotionalAsync(request.Symbol, cancellationToken);
    }
}
