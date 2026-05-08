using MediatR;
using Microsoft.Extensions.Logging;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

public record GetBalancesBySymbolQuery(string Symbol) : IRequest<ListBinanceBalanceDto?>;

public class GetBalancesBySymbolQueryHandler(
    IMediator mediator,
    IBinanceService binanceService,
    ILogger<GetBalancesBySymbolQueryHandler> logger) : IRequestHandler<GetBalancesBySymbolQuery, ListBinanceBalanceDto?>
{
    public async Task<ListBinanceBalanceDto?> Handle(GetBalancesBySymbolQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching balances for symbol {Symbol}", request.Symbol);

        // 1. Get exchange info to find base and quote assets
        var exchangeInfo = await binanceService.GetExchangeInfoAsync(request.Symbol, cancellationToken);
        if (exchangeInfo?.Symbols == null || exchangeInfo.Symbols.Length == 0)
        {
            logger.LogWarning("Exchange info not found for symbol {Symbol}", request.Symbol);
            return null;
        }

        var symbolInfo = exchangeInfo.Symbols[0];
        var assets = new HashSet<string> { symbolInfo.BaseAsset, symbolInfo.QuoteAsset };

        // 2. Get all balances (leveraging cache if available via the other query)
        var allBalances = await mediator.Send(new GetBalancesQuery(), cancellationToken);
        if (allBalances == null) return null;

        // 3. Filter for the specific assets
        var filteredBalances = allBalances.Balances
            .Where(b => assets.Contains(b.Asset))
            .ToArray();

        return allBalances with { Balances = filteredBalances };
    }
}
