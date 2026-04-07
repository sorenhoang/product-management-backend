using FluentAssertions;
using ProductManagement.Application.DTOs.Requests;
using ProductManagement.Application.Validators;

namespace ProductManagement.UnitTests.Validators;

public class AdjustStockRequestValidatorTests
{
    private readonly AdjustStockRequestValidator _validator = new();

    [Fact]
    public async Task Quantity_Zero_Fails_With_Correct_Message()
    {
        var request = new AdjustStockRequest { Quantity = 0 };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Quantity cannot be zero.");
    }

    [Fact]
    public async Task Quantity_Above_10000_Fails()
    {
        var request = new AdjustStockRequest { Quantity = 10001 };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Quantity must be between -10,000 and 10,000.");
    }

    [Fact]
    public async Task Quantity_Below_Minus_10000_Fails()
    {
        var request = new AdjustStockRequest { Quantity = -10001 };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Quantity must be between -10,000 and 10,000.");
    }

    [Fact]
    public async Task Valid_Positive_Quantity_Passes()
    {
        var request = new AdjustStockRequest { Quantity = 50 };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Valid_Negative_Quantity_Passes()
    {
        var request = new AdjustStockRequest { Quantity = -10 };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Reason_Exceeding_200_Chars_Fails()
    {
        var request = new AdjustStockRequest
        {
            Quantity = 5,
            Reason   = new string('X', 201)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Reason must not exceed 200 characters.");
    }

    [Fact]
    public async Task Valid_Request_With_Reason_Passes()
    {
        var request = new AdjustStockRequest { Quantity = 10, Reason = "Restock" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
