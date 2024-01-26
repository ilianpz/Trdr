namespace Trdr;

public static class SystemExtensions
{
    /// <summary>
    /// Rounds a given <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="ts">The <see cref="TimeSpan"/> to round.</param>
    /// <param name="rnd">The "digit" to round to.</param>
    public static TimeSpan Round(this TimeSpan ts, TimeSpan rnd)
    {
        // Originally from https://stackoverflow.com/a/41193749/324260
        if (rnd == TimeSpan.Zero)
            return ts;

        var (major, minor, rndTicks) = GetRoundingComponents(ts, rnd);
        var minorRounded = minor + Math.Sign(ts.Ticks) * rndTicks / 2;
        return TimeSpan.FromTicks(major + minorRounded - minorRounded % rndTicks);
    }

    /// <summary>
    /// Rounds up a given <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="ts">The <see cref="TimeSpan"/> to round.</param>
    /// <param name="rnd">The "digit" to round to.</param>
    public static TimeSpan RoundUp(this TimeSpan ts, TimeSpan rnd)
    {
        var (major, minor, rndTicks) = GetRoundingComponents(ts, rnd);
        var minorRounded = minor - 1 + Math.Sign(ts.Ticks) * rndTicks;
        return TimeSpan.FromTicks(major + minorRounded - minorRounded % rndTicks);
    }

    public static int IndexOfNthOccurence(
        this string source, string match, int occurence,
        StringComparison stringComparison = StringComparison.CurrentCulture)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (match == null) throw new ArgumentNullException(nameof(match));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(occurence);

        int idx = source.IndexOf(match, 0, stringComparison);
        while (idx >= 0 && --occurence > 0)
            idx = source.IndexOf(match, idx + 1, stringComparison);
        return idx;
    }

    private static (long major, long minor, long rndTicks) GetRoundingComponents(TimeSpan ts, TimeSpan rnd)
    {
        var rndTicks = rnd.Ticks;
        var trunc = (long)Math.Pow(10, rndTicks.GetDigits());

        var tsTicks = ts.Ticks;
        var major = tsTicks / trunc * trunc;
        var minor = tsTicks - major;

        return (major, minor, rndTicks);
    }
}