using FluentValidation;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;

namespace ProductManagement.Application.Validators;

public class CreateVariantRequestValidator : AbstractValidator<CreateVariantRequest>
{
    public CreateVariantRequestValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100).WithMessage("SKU must not exceed 100 characters.")
            .Matches(@"^[A-Z0-9\-]+$")
                .WithMessage("SKU must contain only uppercase letters, numbers, and hyphens.")
            .MustAsync(async (sku, ct) => !await unitOfWork.Products.SkuExistsAsync(sku, ct))
                .WithMessage(x => $"SKU '{x.Sku}' already exists.");

        When(x => x.Price.HasValue, () =>
        {
            RuleFor(x => x.Price!.Value)
                .GreaterThan(0).WithMessage("Price must be greater than 0.");
        });

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.");

        RuleFor(x => x.Attributes)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Attributes are required.")
            .Must(a => a.Count > 0).WithMessage("At least one attribute is required.")
            .Must(a => a.Keys.All(k => !string.IsNullOrWhiteSpace(k)))
                .WithMessage("Attribute keys cannot be empty or whitespace.")
            .Must(a => a.Values.All(v => !string.IsNullOrWhiteSpace(v)))
                .WithMessage("Attribute values cannot be empty or whitespace.")
            .Must(a => a.Count <= 20)
                .WithMessage("A variant cannot have more than 20 attributes.");
    }
}
