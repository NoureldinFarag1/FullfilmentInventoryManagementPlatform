using FluentValidation;

namespace Fulfillment.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public  CreateOrderCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}