using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Extensions
{
    public static class DoubleExtensions
    {
        public static bool AlmostEqualsWithAbsTolerance(this double a, double b, double maxAbsoluteError)
        {
            double diff = Math.Abs(a - b);

            if (a.Equals(b))
            {
                // shortcut, handles infinities
                return true;
            }

            return diff <= maxAbsoluteError;
        }
    }
}
