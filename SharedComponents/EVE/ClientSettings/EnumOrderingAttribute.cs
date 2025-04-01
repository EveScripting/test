using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EnumOrderingAttribute : Attribute
    {
        public enum EnumOrdering
        {
            None,
            Name,
            Value
        }

        public EnumOrdering Order { get; set; }

        public EnumOrderingAttribute(EnumOrdering order)
        {
            Order = order;
        }
    }
}
