namespace Trdr.Aggr.Operation;

internal sealed class MeanOperator : AggregateOperator
{
    public static MeanOperator Instance { get; } = new();

    public override decimal Do(IEnumerable<decimal> values)
    {
        decimal sum = 0;
        int count = 0;
        foreach (var value in values)
        {
            sum += value;
            ++count;
        }

        return sum / count;
    }

    public override DateTimeOffset Do(IEnumerable<DateTimeOffset> timeStamps)
    {
        throw new NotImplementedException();
    }
}