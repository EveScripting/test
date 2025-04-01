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
using System;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectChatMessage : DirectObject
    {
        #region Constructors

        internal DirectChatMessage(DirectEve directEve, PyObject message, DirectChatWindow _directChatWindow) : base(directEve)
        {
            // 0 - sender // inttype
            // 1 - text // unicode
            // 2 - timestamp // long
            // 3 - colorkey // long

            MessageText = message.GetItemAt(1).ToUnicodeString();
            CharacterId = -1;
            if (message.GetItemAt(0).GetPyType() == PyType.IntType)
            {
                CharacterId = (long) message.GetItemAt(0);
                Name = DirectEve.GetOwner(CharacterId).Name;
            }

            Time = (DateTime) message.GetItemAt(2);
            ColorKey = (int) message.GetItemAt(3);
            directChatWindow = _directChatWindow;
        }

        #endregion Constructors

        #region Properties

        public long CharacterId { get; internal set; }
        public int ColorKey { get; internal set; }
        public string MessageText { get; internal set; }
        public string Name { get; internal set; }
        public DateTime Time { get; internal set; }

        public DirectChatWindow directChatWindow { get; set; }

        #endregion Properties
    }
}