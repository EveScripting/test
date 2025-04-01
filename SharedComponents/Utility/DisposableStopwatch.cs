using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public class DisposableStopwatch : IDisposable
    {
        private readonly Stopwatch sw;
        private readonly Action<TimeSpan> f;

        public DisposableStopwatch(Action<TimeSpan> f)
        {
            this.f = f;
            sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            sw.Stop();
            f(sw.Elapsed);
        }
    }
}


//using (new DisposableStopwatch(t =>
//{
//Console.WriteLine($"{1000000 * t.Ticks / Stopwatch.Frequency} ns elapsed.");
//Console.WriteLine($"{(1000000 * t.Ticks / Stopwatch.Frequency) / 1000} ms elapsed.");
//}))
//{
//int a = 3;
//int b = 1;
//    for (int i = 0; i< 1000 * 1000; i++)
//{
//    var k = a == b;
//}

//}