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
using SharpDX.Direct2D1;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectGameLogs : DirectObject
    {
        #region Fields
        public List<PyObject> pyMessages = new List<PyObject>();
        public PyObject pyLoggerService = null;
        public PyObject pyLastCombatMessage = null;

        #endregion Fields

        #region Constructors


        internal DirectGameLogs(DirectEve directEve) : base(directEve)
        {
            //__builtin__.sm.services[logger].messages - this is a list of messages
            //__builtin__.sm.services[logger].messages[0] - this is the first thus oldest message
            //__builtin__.sm.services[logger].messages[0].Attribute("0") - this is the actual text of the message: ex: Jumping from Perimeter to Jita
            //__builtin__.sm.services[logger].messages[1] - this would be a slightly newer message
            //__builtin__.sm.services[logger].messages[1].Attribute("0") - this is the actual text of the message: ex: Attempting to join a channel
            //
            //lastCombatMessage
            //

            pyLoggerService = directEve.GetLocalSvc("logger");
            pyMessages = pyLoggerService.Attribute("messages").ToList();
            pyLastCombatMessage = directEve.GetLocalSvc("logger").Attribute("lastCombatMessage");
        }

        #endregion Constructors

        #region Properties



        #endregion Properties
    }
}