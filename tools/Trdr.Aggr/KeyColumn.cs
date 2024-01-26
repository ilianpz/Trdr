using Trdr.Aggr.Operation;
using Trdr.Aggr.Type;

namespace Trdr.Aggr;

internal sealed class KeyColumn
{
    private readonly Func<IReadOnlyList<string>, string> _selector;
    private readonly DataType _dataType;
    private readonly UnaryOperator _operator;

    public KeyColumn(Func<IReadOnlyList<string>, string> selector, DataType dataType, UnaryOperator @operator)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        _operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
    }

    public bool OnNext(IReadOnlyList<string> values)
    {
        var selected = _selector(values);
        _dataType.OnNext(selected, _operator);
        return !_dataType.AreLastTwoValuesEqual();
    }

    public string Flush()
    {
        return _dataType.FlushFirst();
    }
}