using FluentValidation;

namespace TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;

public class PlaceMarketBuyCommandValidator : AbstractValidator<PlaceMarketBuyCommand>
{
    public PlaceMarketBuyCommandValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Symbol is required.");

        RuleFor(x => x.QuoteOrderQty)
            .GreaterThan(0).WithMessage("Quote order quantity must be greater than zero.");
    }
}
