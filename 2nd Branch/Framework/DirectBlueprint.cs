extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Logging;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectBlueprint : DirectItem
    {
        #region Constructors

        internal DirectBlueprint(DirectEve directEve, PyObject pyModule) : base(directEve)
        {
            PyObject GetPyModule = pyModule;
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Methods

        
        

        #endregion Methods
    }
}