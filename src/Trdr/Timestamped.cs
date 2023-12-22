namespace Trdr;

public static class Timestamped
{
    public static Timestamped<T> Create<T>(DateTime timestamp, T item)
    {
        return new Timestamped<T> { Timestamp = timestamp, Value = item };
    }

    public static Timestamped<T> Timestamp<T>(T item)
    {
        return new Timestamped<T> { Timestamp = DateTime.UtcNow, Value = item };
    }
}

public readonly struct Timestamped<T>
{
    public required DateTime Timestamp { get; init; }
    public required T Value { get; init; }
}