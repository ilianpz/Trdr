using Trdr.Aggr.Operation;

namespace Trdr.Aggr.Type;

internal abstract class DataType
{
    public static DataType Parse(string value)
    {
        return value switch
        {
            "d" => new DecimalType(),
            "ts" => new TimestampType(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public abstract void OnNext(string value, UnaryOperator @operator);

    public abstract bool AreLastTwoValuesEqual();

    public abstract string FlushFirst();

    public abstract string FlushAggregate(AggregateOperator @operator);
}