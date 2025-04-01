using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings
{
    [AttributeUsage(AttributeTargets.All)]
    public class TabPageAttribute : Attribute
    {
        // Private fields.
        private string name;

        public TabPageAttribute(string name)
        {
            this.name = name;
        }

        public virtual string Name => name;
    }
}
