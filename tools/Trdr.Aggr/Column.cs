using Trdr.Aggr.Operation;
using Trdr.Aggr.Type;

namespace Trdr.Aggr;

internal static class Column
{
    public static KeyColumn ParseKeyColumn(string value)
    {
        var args = value.Split(",", StringSplitOptions.TrimEntries);
        int columnIndex = int.Parse(args[0]);
        string dataType = args[1];

        UnaryOperator @operator;
        if (args.Length > 2)
            @operator = Operator.ParseUnary(args[2]);
        else
            @operator = IdentityOperator.Instance;

        return new KeyColumn(CreateSelector(columnIndex), DataType.Parse(dataType), @operator);
    }

    public static OtherColumn ParseOtherColumn(string value)
    {
        var args = value.Split(",", StringSplitOptions.TrimEntries);
        int columnIndex = int.Parse(args[0]);
        string dataType = args[1];

        AggregateOperator @operator;
        if (args.Length > 2)
            @operator = Operator.ParseAggregate(args[2]);
        else
            @operator = FirstOperator.Instance;

        return new OtherColumn(CreateSelector(columnIndex), DataType.Parse(dataType), @operator);
    }

    private static Func<IReadOnlyList<string>, string> CreateSelector(int idx)
    {
        return values => values[idx];
    }
}