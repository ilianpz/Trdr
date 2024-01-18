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

        var rndTicks = rnd.Ticks;
        var ansTicks = ts.Ticks + Math.Sign(ts.Ticks) * rndTicks / 2;
        return TimeSpan.FromTicks(ansTicks - ansTicks % rndTicks);
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
}