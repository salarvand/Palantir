using System.Linq.Expressions;

namespace Palantir.SharedKernel;

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
}

public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);
    protected void AddInclude(string includeString) => IncludeStrings.Add(includeString);

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) => OrderBy = orderByExpression;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression) => OrderByDescending = orderByDescExpression;
}

public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> specification) where T : class
    {
        var query = inputQuery;

        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current);

        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}

/// <summary>
/// Encapsulates a single reusable validation rule evaluable in domain guards and HTTP filters.
/// </summary>
public interface IValidationSpecification<in T>
{
    string RuleName { get; }
    string ErrorMessage { get; }
    bool IsSatisfiedBy(T entity);
}

/// <summary>
/// Base class for composable validation specifications.
/// </summary>
public abstract class ValidationSpecification<T> : IValidationSpecification<T>
{
    public abstract string RuleName { get; }
    public abstract string ErrorMessage { get; }
    public abstract bool IsSatisfiedBy(T entity);
}

/// <summary>
/// Composite AND specification requiring both operands to be satisfied.
/// </summary>
public sealed class AndSpecification<T>(
    IValidationSpecification<T> left,
    IValidationSpecification<T> right) : ValidationSpecification<T>
{
    public override string RuleName => $"{left.RuleName} AND {right.RuleName}";
    public override string ErrorMessage => $"{left.ErrorMessage} {right.ErrorMessage}";
    public override bool IsSatisfiedBy(T entity) => left.IsSatisfiedBy(entity) && right.IsSatisfiedBy(entity);
}

/// <summary>
/// Composite OR specification requiring at least one operand to be satisfied.
/// </summary>
public sealed class OrSpecification<T>(
    IValidationSpecification<T> left,
    IValidationSpecification<T> right) : ValidationSpecification<T>
{
    public override string RuleName => $"{left.RuleName} OR {right.RuleName}";
    public override string ErrorMessage => $"{left.ErrorMessage} or {right.ErrorMessage}";
    public override bool IsSatisfiedBy(T entity) => left.IsSatisfiedBy(entity) || right.IsSatisfiedBy(entity);
}

/// <summary>
/// Negation specification inverting the inner specification result.
/// </summary>
public sealed class NotSpecification<T>(IValidationSpecification<T> inner) : ValidationSpecification<T>
{
    public override string RuleName => $"NOT {inner.RuleName}";
    public override string ErrorMessage => $"Condition '{inner.RuleName}' must not be satisfied.";
    public override bool IsSatisfiedBy(T entity) => !inner.IsSatisfiedBy(entity);
}

public sealed record ValidationFailure(string RuleName, string ErrorMessage);

public sealed record ValidationResult(IReadOnlyList<ValidationFailure> Failures)
{
    public bool IsValid => Failures.Count == 0;

    public static ValidationResult Success() => new([]);
}

/// <summary>
/// Domain exception raised when one or more validation specifications fail.
/// </summary>
public sealed class DomainSpecificationException : Exception
{
    public IReadOnlyList<ValidationFailure> Failures { get; }

    public DomainSpecificationException(IReadOnlyList<ValidationFailure> failures)
        : base(string.Join("; ", failures.Select(f => $"{f.RuleName}: {f.ErrorMessage}")))
    {
        Failures = failures;
    }
}

public static class ValidationSpecificationExtensions
{
    public static IValidationSpecification<T> And<T>(
        this IValidationSpecification<T> left,
        IValidationSpecification<T> right) => new AndSpecification<T>(left, right);

    public static IValidationSpecification<T> Or<T>(
        this IValidationSpecification<T> left,
        IValidationSpecification<T> right) => new OrSpecification<T>(left, right);

    public static IValidationSpecification<T> Not<T>(
        this IValidationSpecification<T> spec) => new NotSpecification<T>(spec);

    /// <summary>
    /// Evaluates all supplied specifications and returns every failure without short-circuiting.
    /// </summary>
    public static ValidationResult Validate<T>(
        this T entity,
        params IValidationSpecification<T>[] specifications)
    {
        var failures = new List<ValidationFailure>();
        foreach (var spec in specifications)
        {
            if (!spec.IsSatisfiedBy(entity))
            {
                failures.Add(new ValidationFailure(spec.RuleName, spec.ErrorMessage));
            }
        }

        return failures.Count == 0 ? ValidationResult.Success() : new ValidationResult(failures);
    }

    /// <summary>
    /// Validates the entity against the specifications and throws <see cref="DomainSpecificationException"/> if any fail.
    /// </summary>
    public static void EnsureSatisfies<T>(
        this T entity,
        params IValidationSpecification<T>[] specifications)
    {
        var result = entity.Validate(specifications);
        if (!result.IsValid)
        {
            throw new DomainSpecificationException(result.Failures);
        }
    }
}
