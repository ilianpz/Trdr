namespace Trdr.Aggr.Operation;

internal sealed class IdentityOperator : UnaryOperator
{
    public static IdentityOperator Instance { get; } = new();

    private IdentityOperator() { }

    public override DateTimeOffset Do(DateTimeOffset value) => value;

    public override decimal Do(decimal value) => value;
}