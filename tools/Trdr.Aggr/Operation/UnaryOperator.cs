namespace Trdr.Aggr.Operation;

internal abstract class UnaryOperator
{
    public abstract DateTimeOffset Do(DateTimeOffset value);

    public abstract decimal Do(decimal value);
}