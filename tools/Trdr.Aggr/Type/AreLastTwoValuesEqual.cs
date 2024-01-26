namespace Trdr.Aggr.Type;

internal static class AreLastTwoValuesEqual
{
    public static bool Do<T>(IReadOnlyList<T> values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));

        if (values.Count < 2)
            return true;

        return Equals(values[^1], values[^2]);
    }
}