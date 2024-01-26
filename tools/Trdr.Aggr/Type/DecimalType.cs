using System.Globalization;
using Trdr.Aggr.Operation;

namespace Trdr.Aggr.Type;

internal sealed class DecimalType : DataType
{
    private readonly List<decimal> _values = new();

    public override void OnNext(string value, UnaryOperator @operator)
    {
        var dec = decimal.Parse(value, CultureInfo.InvariantCulture);
        dec = @operator.Do(dec);
        _values.Add(dec);
    }

    public override bool AreLastTwoValuesEqual()
    {
        return Type.AreLastTwoValuesEqual.Do(_values);
    }

    public override string FlushFirst()
    {
        var result = _values[0].ToString(CultureInfo.InvariantCulture);
        _values.Clear();
        return result;
    }

    public override string FlushAggregate(AggregateOperator @operator)
    {
        var result = @operator.Do(_values).ToString(CultureInfo.InvariantCulture);
        _values.Clear();
        return result;
    }
}