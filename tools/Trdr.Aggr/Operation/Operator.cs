namespace Trdr.Aggr.Operation;

internal static class Operator
{
    public static UnaryOperator ParseUnary(string value)
    {
        var tokens = value.Split('{');
        if (tokens.Length == 1)
            return ParseUnary(tokens[0], null);

        var arg = tokens[1];
        return ParseUnary(tokens[0], arg.Substring(0, arg.IndexOf('}')));
    }

    public static AggregateOperator ParseAggregate(string value)
    {
        var tokens = value.Split('{');
        if (tokens.Length == 1)
            return ParseAggregate(tokens[0], null);

        var arg = tokens[1];
        return ParseAggregate(tokens[0], arg.Substring(0, arg.IndexOf('}')));
    }

    private static UnaryOperator ParseUnary(string op, string? arg)
    {
        return op switch
        {
            "rnd" => new RoundOperator(arg),
            "rndup" => new RoundUpOperator(arg),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static AggregateOperator ParseAggregate(string op, string? arg)
    {
        return op switch
        {
            "mean" => MeanOperator.Instance,
            "sum" => SumOperator.Instance,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}