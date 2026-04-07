using FluentValidation;
using ProductManagement.Application.DTOs.Requests;

namespace ProductManagement.Application.Validators;

public class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .NotEqual(0).WithMessage("Quantity cannot be zero.")
            .InclusiveBetween(-10000, 10000)
                .WithMessage("Quantity must be between -10,000 and 10,000.");

        When(x => x.Reason != null, () =>
        {
            RuleFor(x => x.Reason)
                .MaximumLength(200).WithMessage("Reason must not exceed 200 characters.");
        });
    }
}
