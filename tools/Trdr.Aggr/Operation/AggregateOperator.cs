namespace Trdr.Aggr.Operation;

internal abstract class AggregateOperator
{
    public abstract decimal Do(IEnumerable<decimal> values);
    public abstract DateTimeOffset Do(IEnumerable<DateTimeOffset> timeStamps);
}