using FluentValidation;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;

namespace ProductManagement.Application.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

        When(x => x.ParentId.HasValue, () =>
        {
            RuleFor(x => x.ParentId!.Value)
                .MustAsync(async (id, ct) => await unitOfWork.Categories.ExistsAsync(id, ct))
                .WithMessage(x => $"Parent category with id '{x.ParentId}' does not exist.");
        });
    }
}
