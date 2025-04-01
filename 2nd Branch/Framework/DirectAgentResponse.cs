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

namespace EVESharpCore.Framework
{
    /**
    extern alias SC;

    public class DirectAgentResponse : DirectObject
    {
        #region Constructors

        internal DirectAgentResponse(DirectEve directEve, PyObject container)
            : base(directEve)
        {
            _container = container;
        }

        #endregion Constructors

        #region Methods

        public bool Say()
        {
            PyObject btn = DirectWindow.FindChildWithPath(_container, (Right ? _responseButtonsPathRight : _responseButtonsPathLeft).Concat(new[] {Button}));
            return DirectEve.ThreadedCall(btn.Attribute("OnClick"));
        }

        #endregion Methods

        #region Fields

        private readonly PyObject _container;

        private readonly string[] _responseButtonsPathLeft = {"__maincontainer", "main", "rightPaneBottom"};
        private readonly string[] _responseButtonsPathRight = {"__maincontainer", "main", "rightPane", "rightPaneBottom"};

        #endregion Fields

        #region Properties

        public long AgentId { get; internal set; }
        public string Button { get; internal set; }
        public bool Right { get; internal set; }
        public string Text { get; internal set; }

        #endregion Properties
    }
    **/
}