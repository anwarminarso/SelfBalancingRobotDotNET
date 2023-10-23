using System.Diagnostics;

namespace SelfBalancingRobot.WebUI;

public class Utils
{
    private const long TicksPerSecond = TimeSpan.TicksPerSecond;
    private const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;
    private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
    private static readonly double s_tickFrequency = (double)TicksPerSecond / Stopwatch.Frequency;

    private static void InternalDelay(TimeSpan time, bool allowThreadYield)
    {
        long start = Stopwatch.GetTimestamp();
        long delta = (long)(time.Ticks / s_tickFrequency);
        long target = start + delta;

        if (!allowThreadYield)
        {
            do
            {
                Thread.SpinWait(1);
            }
            while (Stopwatch.GetTimestamp() < target);
        }
        else
        {
            SpinWait spinWait = new();
            do
            {
                spinWait.SpinOnce();
            }
            while (Stopwatch.GetTimestamp() < target);
        }
    }

    /// <summary>
    /// Delay for at least the specified <paramref name="microseconds"/>.
    /// </summary>
    /// <param name="microseconds">The number of microseconds to delay.</param>
    /// <param name="allowThreadYield">
    /// True to allow yielding the thread. If this is set to false, on single-proc systems
    /// this will prevent all other code from running.
    /// </param>
    public static void DelayMicroseconds(int microseconds, bool allowThreadYield)
    {
        var time = TimeSpan.FromTicks(microseconds * TicksPerMicrosecond);
        InternalDelay(time, allowThreadYield);
    }

    /// <summary>
    /// Delay for at least the specified <paramref name="milliseconds"/>
    /// </summary>
    /// <param name="milliseconds">The number of milliseconds to delay.</param>
    /// <param name="allowThreadYield">
    /// True to allow yielding the thread. If this is set to false, on single-proc systems
    /// this will prevent all other code from running.
    /// </param>
    public static void Delay(int milliseconds, bool allowThreadYield)
    {
        /* We have this as a separate method for now to make calling code clearer
         * and to allow us to add additional logic to the millisecond wait in the
         * future. If waiting only 1 millisecond we still have ample room for more
         * complicated logic. For 1 microsecond that isn't the case.
         */

        var time = TimeSpan.FromTicks(milliseconds * TicksPerMillisecond);
        InternalDelay(time, allowThreadYield);
    }
}
