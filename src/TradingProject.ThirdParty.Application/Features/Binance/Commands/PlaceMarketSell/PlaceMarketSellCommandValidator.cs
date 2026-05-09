using FluentValidation;

namespace TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketSell;

public class PlaceMarketSellCommandValidator : AbstractValidator<PlaceMarketSellCommand>
{
    public PlaceMarketSellCommandValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Symbol is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}
