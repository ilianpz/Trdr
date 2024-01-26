using System.Globalization;

namespace Trdr.Aggr.Operation;

internal sealed class RoundOperator : UnaryOperator
{
    private readonly TimeSpan? _round;

    public RoundOperator(string? format)
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
            value.TimeOfDay.Round(_round.Value);
        return new DateTimeOffset(dt);
    }

    public override decimal Do(decimal value)
    {
        throw new NotImplementedException();
    }
}