using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace SharedComponents.EVE.ClientSettings
    {
        [AttributeUsage(AttributeTargets.All)]
        public class TabRootAttribute : Attribute
        {
            // Private fields.
            private string name;

            public TabRootAttribute(string name)
            {
                this.name = name;
            }

            public virtual string Name => name;
        }
    }

}
