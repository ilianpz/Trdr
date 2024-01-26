namespace Trdr.Aggr.Operation;

internal sealed class FirstOperator : AggregateOperator
{
    public static FirstOperator Instance { get; } = new();

    public override decimal Do(IEnumerable<decimal> values)
    {
        return values.First();
    }

    public override DateTimeOffset Do(IEnumerable<DateTimeOffset> timeStamps)
    {
        return timeStamps.First();
    }
}