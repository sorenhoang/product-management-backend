using FluentAssertions;
using Moq;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.Validators;

namespace ProductManagement.UnitTests.Validators;

public class CreateVariantRequestValidatorTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IProductRepository> _mockProductRepo;
    private readonly CreateVariantRequestValidator _validator;

    public CreateVariantRequestValidatorTests()
    {
        _mockProductRepo = new Mock<IProductRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockUow.Setup(x => x.Products).Returns(_mockProductRepo.Object);

        // Default: SKU does not exist
        _mockProductRepo
            .Setup(x => x.SkuExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _validator = new CreateVariantRequestValidator(_mockUow.Object);
    }

    private static CreateVariantRequest ValidRequest() => new()
    {
        Sku        = "ABC-123",
        Price      = 9.99m,
        Stock      = 10,
        Attributes = new Dictionary<string, string> { ["color"] = "red" }
    };

    [Fact]
    public async Task Valid_Request_Passes()
    {
        var result = await _validator.ValidateAsync(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Sku_Fails()
    {
        var request = new CreateVariantRequest
        {
            Sku        = "",
            Price      = 9.99m,
            Stock      = 10,
            Attributes = new Dictionary<string, string> { ["color"] = "red" }
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "SKU is required.");
    }

    [Fact]
    public async Task Sku_With_Lowercase_Letters_Fails()
    {
        var request = new CreateVariantRequest
        {
            Sku        = "abc-123",
            Stock      = 10,
            Attributes = new Dictionary<string, string> { ["color"] = "red" }
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "SKU must contain only uppercase letters, numbers, and hyphens.");
    }

    [Fact]
    public async Task Negative_Price_Fails()
    {
        var request = new CreateVariantRequest
        {
            Sku        = "ABC-123",
            Price      = -1m,
            Stock      = 10,
            Attributes = new Dictionary<string, string> { ["color"] = "red" }
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Price must be greater than 0.");
    }

    [Fact]
    public async Task Negative_Stock_Fails()
    {
        var request = new CreateVariantRequest
        {
            Sku        = "ABC-123",
            Stock      = -1,
            Attributes = new Dictionary<string, string> { ["color"] = "red" }
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Stock cannot be negative.");
    }

    [Fact]
    public async Task Empty_Attributes_Dict_Fails()
    {
        var request = new CreateVariantRequest
        {
            Sku        = "ABC-123",
            Stock      = 10,
            Attributes = []
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "At least one attribute is required.");
    }

    [Fact]
    public async Task Attribute_Key_Is_Whitespace_Fails()
    {
        var request = new CreateVariantRequest
        {
            Sku        = "ABC-123",
            Stock      = 10,
            Attributes = new Dictionary<string, string> { ["   "] = "value" }
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Attribute keys cannot be empty or whitespace.");
    }

    [Fact]
    public async Task Duplicate_Sku_Fails()
    {
        _mockProductRepo
            .Setup(x => x.SkuExistsAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _validator.ValidateAsync(ValidRequest());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "SKU 'ABC-123' already exists.");
    }
}
