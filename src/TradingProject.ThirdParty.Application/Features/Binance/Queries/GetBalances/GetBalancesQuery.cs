using MediatR;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

public record GetBalancesQuery : IRequest<Dictionary<string, double>>;

public class GetBalancesQueryHandler(IBinanceService binanceService) : IRequestHandler<GetBalancesQuery, Dictionary<string, double>>
{
    public async Task<Dictionary<string, double>> Handle(GetBalancesQuery request, CancellationToken cancellationToken)
    {
        return await binanceService.GetBalancesAsync(cancellationToken);
    }
}
