using FluentValidation;

namespace Fulfillment.Application.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public  CreateCustomerCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c=>c.Phone).MaximumLength(30);
        RuleFor(c=>c.Address).MaximumLength(500);
    }
}