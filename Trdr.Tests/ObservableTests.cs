using Nito.AsyncEx;

namespace Trdr.Tests;

public class Tests
{
    [Test]
    public void Wait_can_succeed()
    {
        var @event = new AsyncManualResetEvent(false);
        var observable = Observable.Create(CreateStream());

        Task<bool> obsTask = observable.Wait(i => i == 5);
        @event.Set();
        Assert.That(async () => await obsTask, Is.True);

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
    public void Wait_can_handle_completed_stream()
    {
        var @event = new AsyncManualResetEvent(false);
        var observable = Observable.Create(CreateStream());

        Task<bool> obsTask = observable.Wait(_ => true);
        @event.Set();
        Assert.That(async () => await obsTask, Is.False);

#pragma warning disable CS1998 - Rider complains about "useless" async
        async IAsyncEnumerable<int> CreateStream()
        {
            yield break;
        }
#pragma warning restore CS1998
    }
    
    [Test]
    public void Wait_can_throw()
    {
        var observable = Observable.Create(CreateStream());
        
        Assert.That(async () => await observable.Wait(_ => true), Throws.Exception);

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