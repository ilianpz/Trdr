using Microsoft.Win32.SafeHandles;

namespace Trdr.Windows;

public sealed class HighResolutionSleep : IDisposable
{
    private readonly SafeAccessTokenHandle _handle;

    public HighResolutionSleep()
    {
#pragma warning disable CA1416
        _handle = new SafeAccessTokenHandle(
            Kernel32.CreateWaitableTimerEx(
                IntPtr.Zero, null,
                Kernel32.TimerCreateFlags.CreateWaitableTimerManualReset |
                    Kernel32.TimerCreateFlags.CreateWaitableTimerHighResolution,
                Kernel32.TimerAccessFlags.TimerAllAccess));

        if (_handle.IsInvalid)
            throw new InvalidOperationException("Unable to create high resolution timer.");
#pragma warning restore CA1416
    }

    public void Wait(TimeSpan interval)
    {
        Wait((long)(interval.TotalMicroseconds * 10));
    }

    public void Wait(long hundredNanoseconds)
    {
        var largeInt = new Kernel32.LargeInteger { QuadPart = -hundredNanoseconds };
        if (!Kernel32.SetWaitableTimerEx(_handle.DangerousGetHandle(), ref largeInt, 0,
                null, IntPtr.Zero, IntPtr.Zero,
                0 /* Not supported by high-resolution timer */))
        {
            throw new InvalidOperationException("Unable to wait.");
        }

        if (Kernel32.WaitForSingleObject(_handle.DangerousGetHandle(), Kernel32.TimerInfinite) != 0)
        {
            throw new InvalidOperationException("Unexpected error while waiting.");
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
    }
}