using Vogen;

namespace Palantir.SharedKernel;

[ValueObject<Guid>]
public readonly partial struct OrderId
{
    private static Validation Validate(Guid value) =>
        value != Guid.Empty ? Validation.Ok : Validation.Invalid("OrderId cannot be empty.");
}

[ValueObject<Guid>]
public readonly partial struct TradeId
{
    private static Validation Validate(Guid value) =>
        value != Guid.Empty ? Validation.Ok : Validation.Invalid("TradeId cannot be empty.");
}

[ValueObject<Guid>]
public readonly partial struct AccountId
{
    private static Validation Validate(Guid value) =>
        value != Guid.Empty ? Validation.Ok : Validation.Invalid("AccountId cannot be empty.");
}

[ValueObject<string>]
public readonly partial struct SymbolId
{
    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) ? Validation.Ok : Validation.Invalid("SymbolId cannot be empty.");
}

[ValueObject<Guid>]
public readonly partial struct UserId
{
    private static Validation Validate(Guid value) =>
        value != Guid.Empty ? Validation.Ok : Validation.Invalid("UserId cannot be empty.");
}

[ValueObject<Guid>]
public readonly partial struct ClientId
{
    private static Validation Validate(Guid value) =>
        value != Guid.Empty ? Validation.Ok : Validation.Invalid("ClientId cannot be empty.");
}
