namespace Palantir.Generators.Abstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class QueryBindableAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class QueryParamNameAttribute : Attribute
{
    public string Name { get; }
    public QueryParamNameAttribute(string name) => Name = name;
}
