namespace Trdr.Aggr.Operation;

internal sealed class SumOperator : AggregateOperator
{
    public static SumOperator Instance { get; } = new();

    public override decimal Do(IEnumerable<decimal> values)
    {
        return values.Sum();
    }

    public override DateTimeOffset Do(IEnumerable<DateTimeOffset> timeStamps)
    {
        throw new NotImplementedException();
    }
}