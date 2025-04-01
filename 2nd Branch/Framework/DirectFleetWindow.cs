extern alias SC;

using SC::SharedComponents.Py;
using System;
using System.Linq;

namespace EVESharpCore.Framework
{
    public class DirectFleetWindow : DirectWindow
    {
        #region Constructors

        internal DirectFleetWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {

        }

        #endregion Constructors

        #region Properties


        #endregion Properties
    }
}