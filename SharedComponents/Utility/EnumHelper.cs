using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public static class EnumHelpers<TTarget>
    {
        static EnumHelpers()
        {
            Dict = Enum.GetNames(typeof(TTarget)).ToDictionary(x => x, x => (TTarget)Enum.Parse(typeof(TTarget), x), StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, TTarget> Dict;

        public static TTarget Convert(string value)
        {
            return Dict[value];
        }

        public static bool HasKey(string value)
        {
            return Dict.ContainsKey(value);
        }
    }
}
