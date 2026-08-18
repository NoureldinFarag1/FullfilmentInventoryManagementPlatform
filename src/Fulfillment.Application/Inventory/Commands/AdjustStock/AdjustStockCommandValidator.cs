using FluentValidation;

namespace Fulfillment.Application.Inventory.Commands.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0)
            .WithMessage("A stock adjustment must change the quantity.");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}