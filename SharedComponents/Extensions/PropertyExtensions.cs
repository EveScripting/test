using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool IsAutoProperty(this PropertyInfo prop)
        {
            return prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(f => f.Name.Contains("<" + prop.Name + ">"));
        }
    }
}
