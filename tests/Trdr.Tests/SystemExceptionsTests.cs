namespace Trdr.Tests;

internal sealed class SystemExceptionsTests
{
    [Test]
    [TestCaseSource(nameof(RoundCases))]
    public void Can_round_time_span(TimeSpan input, TimeSpan round, TimeSpan expected)
    {
        var result = input.Round(round);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCaseSource(nameof(RoundUpCases))]
    public void Can_round_up_time_span(TimeSpan input, TimeSpan round, TimeSpan expected)
    {
        var result = input.RoundUp(round);
        Assert.That(result, Is.EqualTo(expected));
    }

    public static object[] RoundCases =
    [
        new object[] { TimeSpan.Parse("01:01:01.012"), TimeSpan.Parse("00:00:00.01"), TimeSpan.Parse("01:01:01.01") },
        new object[] { TimeSpan.Parse("01:01:01.015"), TimeSpan.Parse("00:00:00.01"), TimeSpan.Parse("01:01:01.02") },
        new object[] { TimeSpan.Parse("01:01:01.020"), TimeSpan.Parse("00:00:00.01"), TimeSpan.Parse("01:01:01.02") },
        new object[] { TimeSpan.Parse("01:01:01.020"), TimeSpan.Parse("00:00:00.10"), TimeSpan.Parse("01:01:01.00") },
    ];

    public static object[] RoundUpCases =
    [
        new object[] { TimeSpan.Parse("01:01:01.012"), TimeSpan.Parse("00:00:00.01"), TimeSpan.Parse("01:01:01.02") },
        new object[] { TimeSpan.Parse("01:01:01.015"), TimeSpan.Parse("00:00:00.01"), TimeSpan.Parse("01:01:01.02") },
        new object[] { TimeSpan.Parse("01:01:01.020"), TimeSpan.Parse("00:00:00.01"), TimeSpan.Parse("01:01:01.02") },
        new object[] { TimeSpan.Parse("01:01:01.020"), TimeSpan.Parse("00:00:00.10"), TimeSpan.Parse("01:01:01.10") },
        new object[] { TimeSpan.Parse("01:01:01.020"), TimeSpan.Parse("01:00:00.00"), TimeSpan.Parse("02:00:00.00") },
    ];
}