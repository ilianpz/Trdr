using Nito.AsyncEx;

namespace Trdr.Tests;

public sealed class WatcherTests
{
    [Test]
    public void Watch_can_succeed()
    {
        var @event = new AsyncManualResetEvent(false);
        var watcher = Watcher.Create(CreateStream());

        Task<bool> watchTask = watcher.Watch(i => i == 5);
        @event.Set();
        Assert.That(async () => await watchTask, Is.True);

        async IAsyncEnumerable<int> CreateStream()
        {
            await @event.WaitAsync();
            for (int i = 0; i < 10; ++i)
            {
                yield return i;
            }
        }
    }
    
    [Test]
    public void Watch_can_handle_completed_stream()
    {
        var @event = new AsyncManualResetEvent(false);
        var watcher = Watcher.Create(CreateStream());

        Task<bool> watchTask = watcher.Watch(_ => true);
        @event.Set();
        Assert.That(async () => await watchTask, Is.False);

#pragma warning disable CS1998 - Rider complains about "useless" async
        async IAsyncEnumerable<int> CreateStream()
        {
            yield break;
        }
#pragma warning restore CS1998
    }
    
    [Test]
    public void Watch_can_throw()
    {
        var watcher = Watcher.Create(CreateStream());

        Assert.That(async () => await watcher.Watch(_ => true), Throws.Exception);

        async IAsyncEnumerable<int> CreateStream()
        {
            await Task.Yield();
            throw new Exception();
            
#pragma warning disable CS0162
            yield break; // Compiler complains about not returning even though this is unreachable
#pragma warning restore CS0162
        }
    }
}