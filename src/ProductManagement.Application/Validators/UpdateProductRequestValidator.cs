using FluentValidation;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;

namespace ProductManagement.Application.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2,000 characters.");
        });

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Base price must not exceed 1,000,000.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.")
            .MustAsync(async (id, ct) => await unitOfWork.Categories.ExistsAsync(id, ct))
                .WithMessage(x => $"Category with id '{x.CategoryId}' does not exist.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid product status value.");
    }
}
