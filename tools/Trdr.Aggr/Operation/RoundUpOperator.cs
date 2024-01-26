using System.Globalization;

namespace Trdr.Aggr.Operation;

internal sealed class RoundUpOperator : UnaryOperator
{
    private readonly TimeSpan? _round;

    public RoundUpOperator(string? format)
    {
        if (format != null)
            _round = TimeSpan.Parse(format, CultureInfo.InvariantCulture);
    }

    public override DateTimeOffset Do(DateTimeOffset value)
    {
        if (_round == null)
            return value;

        var dt =
            DateTime.SpecifyKind(value.Date, DateTimeKind.Utc) +
            value.TimeOfDay.RoundUp(_round.Value);
        return new DateTimeOffset(dt);
    }

    public override decimal Do(decimal value)
    {
        throw new NotImplementedException();
    }
}