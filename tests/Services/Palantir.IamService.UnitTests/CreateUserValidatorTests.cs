using Palantir.IamService.Features.Users.Create;
using Shouldly;
using Xunit;

namespace Palantir.IamService.UnitTests;

public sealed class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var request = new CreateUserRequest(
            "trader1",
            "trader1@palantir.local",
            "Alex",
            "Trader",
            "Secret123!",
            "Trader",
            3,
            10m,
            50_000m);

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidEmail_ShouldFail()
    {
        var request = new CreateUserRequest(
            "trader1",
            "not-an-email",
            "Alex",
            "Trader",
            "Secret123!",
            "Trader",
            3,
            10m,
            50_000m);

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }
}
