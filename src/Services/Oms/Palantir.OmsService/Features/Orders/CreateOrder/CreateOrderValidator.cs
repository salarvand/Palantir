using FluentValidation;
using Palantir.SharedKernel;

namespace Palantir.OmsService.Features.Orders.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("ClientId is required.");
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20).WithMessage("Symbol is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.Type is OrderType.Limit or OrderType.StopLimit)
            .WithMessage("Price must be greater than zero for Limit orders.");
    }
}
