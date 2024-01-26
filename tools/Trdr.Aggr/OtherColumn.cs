using Trdr.Aggr.Operation;
using Trdr.Aggr.Type;

namespace Trdr.Aggr;

internal sealed class OtherColumn
{
    private readonly Func<IReadOnlyList<string>, string> _selector;
    private readonly DataType _dataType;
    private readonly AggregateOperator _operator;

    public OtherColumn(Func<IReadOnlyList<string>, string> selector, DataType dataType, AggregateOperator @operator)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        _operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
    }

    public void OnNext(IReadOnlyList<string> values)
    {
        var selected = _selector(values);
        _dataType.OnNext(selected, IdentityOperator.Instance);
    }

    public string Flush()
    {
        return _dataType.FlushAggregate(_operator);
    }
}