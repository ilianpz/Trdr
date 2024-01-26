namespace Trdr.Aggr.Tests;

public class RowsTests
{
    [Test]
    public void Can_aggregate()
    {
        var rows = Rows.Create(new[] { "0,ts,rndup{00:00:00.01}", "1,d,mean", "2,d,sum" });

        Assert.That(rows.OnNext("2024-01-26T01:01:01.1111111,5,1").Count(), Is.EqualTo(0));
        Assert.That(rows.OnNext("2024-01-26T01:01:01.1122222,5,1").Count(), Is.EqualTo(0));
        Assert.That(rows.OnNext("2024-01-26T01:01:01.1133333,5,1").Count(), Is.EqualTo(0));
        Assert.That(rows.OnNext("2024-01-26T01:01:01.1144444,5,1").Count(), Is.EqualTo(0));
        var row = rows.OnNext("2024-01-26T01:01:01.1200001,3,2").ToList();
        Assert.That(row, Has.Count.EqualTo(3));
        Assert.That(row[0], Is.EqualTo("2024-01-26T01:01:01.1200000"));
        Assert.That(row[1], Is.EqualTo("5"));
        Assert.That(row[2], Is.EqualTo("4"));
        Assert.That(rows.OnNext("2024-01-26T01:01:01.1200002,3,2").Count(), Is.EqualTo(0));
        Assert.That(rows.OnNext("2024-01-26T01:01:01.1230001,3,2").Count(), Is.EqualTo(0));
        Assert.That(rows.OnNext("2024-01-26T01:01:01.1300000,3,2").Count(), Is.EqualTo(0));
        row = rows.OnNext("2024-01-26T01:01:01.1300001,4,3").ToList();
        Assert.That(row[0], Is.EqualTo("2024-01-26T01:01:01.1300000"));
        Assert.That(row[1], Is.EqualTo("3"));
        Assert.That(row[2], Is.EqualTo("8"));
        row = rows.Flush().ToList();
        Assert.That(row[0], Is.EqualTo("2024-01-26T01:01:01.1400000"));
        Assert.That(row[1], Is.EqualTo("4"));
        Assert.That(row[2], Is.EqualTo("3"));
    }
}