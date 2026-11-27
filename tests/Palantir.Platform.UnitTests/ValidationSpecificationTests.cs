using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.Platform.UnitTests;

public sealed class ValidationSpecificationTests
{
    private sealed class PositiveNumberSpec : ValidationSpecification<int>
    {
        public override string RuleName => "Positive";
        public override string ErrorMessage => "Value must be positive.";
        public override bool IsSatisfiedBy(int entity) => entity > 0;
    }

    private sealed class EvenNumberSpec : ValidationSpecification<int>
    {
        public override string RuleName => "Even";
        public override string ErrorMessage => "Value must be even.";
        public override bool IsSatisfiedBy(int entity) => entity % 2 == 0;
    }

    [Fact]
    public void CompositeSpecifications_ShouldComposeAndOrNotCorrectly()
    {
        var positiveAndEven = new PositiveNumberSpec().And(new EvenNumberSpec());
        var positiveOrEven = new PositiveNumberSpec().Or(new EvenNumberSpec());
        var notEven = new EvenNumberSpec().Not();

        positiveAndEven.IsSatisfiedBy(4).ShouldBeTrue();
        positiveAndEven.IsSatisfiedBy(3).ShouldBeFalse();
        positiveOrEven.IsSatisfiedBy(-2).ShouldBeTrue();
        notEven.IsSatisfiedBy(3).ShouldBeTrue();
    }

    [Fact]
    public void EnsureSatisfies_ShouldThrowDomainSpecificationException_WhenRulesFail()
    {
        var ex = Should.Throw<DomainSpecificationException>(() =>
            (-3).EnsureSatisfies(new PositiveNumberSpec(), new EvenNumberSpec()));

        ex.Failures.Count.ShouldBe(2);
    }
}
