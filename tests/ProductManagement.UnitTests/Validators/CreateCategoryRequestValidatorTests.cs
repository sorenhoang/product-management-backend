using FluentAssertions;
using Moq;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.Validators;

namespace ProductManagement.UnitTests.Validators;

public class CreateCategoryRequestValidatorTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly CreateCategoryRequestValidator _validator;

    public CreateCategoryRequestValidatorTests()
    {
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockUow.Setup(x => x.Categories).Returns(_mockCategoryRepo.Object);

        // Default: parent category exists
        _mockCategoryRepo
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _validator = new CreateCategoryRequestValidator(_mockUow.Object);
    }

    [Fact]
    public async Task Valid_Request_Passes()
    {
        var request = new CreateCategoryRequest { Name = "Electronics" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Name_Fails_With_Correct_Message()
    {
        var request = new CreateCategoryRequest { Name = "" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Category name is required.");
    }

    [Fact]
    public async Task Name_Exceeding_100_Chars_Fails()
    {
        var request = new CreateCategoryRequest { Name = new string('A', 101) };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Name" &&
            e.ErrorMessage == "Category name must not exceed 100 characters.");
    }

    [Fact]
    public async Task Valid_Request_With_Existing_ParentId_Passes()
    {
        var parentId = Guid.NewGuid();
        var request = new CreateCategoryRequest { Name = "Phones", ParentId = parentId };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Non_Existent_ParentId_Fails_With_Correct_Message()
    {
        var parentId = Guid.NewGuid();
        _mockCategoryRepo
            .Setup(x => x.ExistsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateCategoryRequest { Name = "Phones", ParentId = parentId };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == $"Parent category with id '{parentId}' does not exist.");
    }
}
