using System.Runtime.InteropServices;

namespace Trdr.Windows;

public static class Kernel32
{
    public const uint TimerInfinite = 0xFFFFFFFF;
    public delegate void TimerCompleteDelegate();

    [Flags]
    public enum TimerAccessFlags : int
    {
        TimerAllAccess   = 0x1F0003,
        TimerModifyState = 0x0002,
        TimerQueryState  = 0x0001
    }

    [Flags]
    public enum TimerCreateFlags : int
    {
        CreateWaitableTimerManualReset    = 0x00000001,
        CreateWaitableTimerHighResolution = 0x00000002
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LargeIntegerSplitPart
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct LargeInteger
    {
        [FieldOffset(0)]
        public LargeIntegerSplitPart u;
        [FieldOffset(0)]
        public long QuadPart;
    }

    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern IntPtr CreateWaitableTimerEx(
        IntPtr securityAttributes,
        string? timerName,
        TimerCreateFlags flags,
        TimerAccessFlags desiredAccess);

    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern bool SetWaitableTimerEx(
        IntPtr hTimer,
        ref LargeInteger dueTime,
        long period,
        TimerCompleteDelegate? pfnCompletionRoutine,
        IntPtr lpArgToCompletionRoutine,
        IntPtr wakeContext,
        uint tolerableDelay);

    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
}