// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Py;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectIndustryWindowSingleLineEditInteger : DirectObject
    {
        #region Fields

        private readonly PyObject _pySingleLineEditInteger;

        #endregion Fields

        #region Constructors

        internal DirectIndustryWindowSingleLineEditInteger(DirectEve directEve, PyObject pySingleLineEditInteger)
            : base(directEve)
        {
            _pySingleLineEditInteger = pySingleLineEditInteger;
            Text = _pySingleLineEditInteger.Attribute("text").ToUnicodeString();
        }

        #endregion Constructors

        #region Properties

        public string Name { get; set; }
        public string Text { get; set; }
        public int IntValue
        {
            get
            {
                if (!string.IsNullOrEmpty(Text))
                    return int.Parse(Text);

                return 0;
            }
        }

        #endregion Properties

        #region Methods

        public bool Down()
        {
            return DirectEve.ThreadedCall(_pySingleLineEditInteger.Attribute("OnDownKeyPressed"));
        }

        public bool Up()
        {
            return DirectEve.ThreadedCall(_pySingleLineEditInteger.Attribute("OnUpKeyPressed"));
        }

        #endregion Methods
    }
}