using System.Globalization;
using Trdr.Aggr.Operation;
using Trdr.App;

namespace Trdr.Aggr.Type;

internal sealed class TimestampType : DataType
{
    private readonly List<DateTimeOffset> _timestamps = new();

    public override void OnNext(string value, UnaryOperator @operator)
    {
        var dt =
            DateTimeOffset.ParseExact(
                value, Application.TimestampFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        dt = @operator.Do(dt);
        _timestamps.Add(dt);
    }

    public override bool AreLastTwoValuesEqual()
    {
        return Type.AreLastTwoValuesEqual.Do(_timestamps);
    }

    public override string FlushFirst()
    {
        var result = _timestamps[0].ToString(Application.TimestampFormat, CultureInfo.InvariantCulture);
        _timestamps.Clear();
        return result;
    }

    public override string FlushAggregate(AggregateOperator @operator)
    {
        var result = @operator.Do(_timestamps).ToString(Application.TimestampFormat, CultureInfo.InvariantCulture);
        _timestamps.Clear();
        return result;
    }
}