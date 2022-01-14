using System;
using Rebus.Time;

namespace Rebus.Tests.Time;

public class FakeRebusTime : IRebusTime
{
    Func<DateTimeOffset> _fakeTimeFactory = () => DateTimeOffset.Now;

    public DateTimeOffset Now => _fakeTimeFactory();

    public void FakeIt(DateTimeOffset fakeTime, bool driftSlightly = true)
    {
        var time = fakeTime;

        _fakeTimeFactory = () =>
        {
            var timeToReturn = time;
            if (driftSlightly)
            {
                time = time.AddTicks(17);
            }

            return timeToReturn;
        };
    }

    public void Reset() => _fakeTimeFactory = () => DateTimeOffset.Now;
}