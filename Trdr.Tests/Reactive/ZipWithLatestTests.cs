using System.Reactive.Linq;
using System.Reactive.Subjects;
using Trdr.Reactive;

namespace Trdr.Tests.Reactive;

public sealed class ZipWithLatestTests
{
    [Test]
    public async Task Can_zip_with_latest()
    {
        var subject1 = new Subject<int>();
        var subject2 = new Subject<string>();

        var connectable = subject1.ZipWithLatest(subject2)
            .Replay();

        using var connection = connectable.Connect();

        subject1.OnNext(1);
        subject1.OnNext(2);
        subject1.OnNext(3);

        subject2.OnNext("1");
        subject2.OnNext("2");
        subject2.OnNext("3");

        subject1.OnNext(1);
        subject1.OnNext(2);

        subject2.OnNext("1");

        subject1.OnCompleted();
        subject2.OnCompleted();

        var list = await connectable.ToAsyncEnumerable().ToListAsync();
        Assert.That(list.SequenceEqual(new[] { (3, "1"), (1, "3"), (2, "1") }));
    }
}