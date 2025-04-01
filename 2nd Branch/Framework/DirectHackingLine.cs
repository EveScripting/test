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

using EVESharpCore.Lookup;
using SC::SharedComponents.Py;
using System;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectHackingLine : DirectObject
    {
        #region Fields

        private readonly PyObject _pyHackingLine;

        #endregion Fields

        #region Constructors

        internal DirectHackingLine(DirectEve directEve, PyObject pyHackingLine)
            : base(directEve)
        {
            try
            {
                _pyHackingLine = pyHackingLine;
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Constructors

        #region Properties

        public DirectHackingTile TileFrom
        {
            get
            {
                return null;
            }
        }

        public DirectHackingTile TileTo
        {
            get
            {
                return null;
            }
        }

        /**
         * def GetLineType(self):
        y0 = self.tileFrom.top
        y1 = self.tileTo.top
        x0 = self.tileFrom.left
        x1 = self.tileTo.left
        if y0 == y1:
            return hackingUIConst.LINETYPE_HORIZONTAL
        if y0 > y1:
            if x0 > x1:
                return hackingUIConst.LINETYPE_INCLINE
            else:
                return hackingUIConst.LINETYPE_DECLINE
        else:
            if x0 < x1:
                return hackingUIConst.LINETYPE_INCLINE
            return hackingUIConst.LINETYPE_DECLINE

         * **/
        public int LineType //LINETYPE_HORIZONTAL, LINETYPE_INCLINE, LINETYPE_DECLINE
        {
            get
            {
                return 0;
            }
        }

        //widthFrom
        //widthTo


        public long p0 { get; internal set; }
        public long p1 { get; internal set; }

        public string ButtonName { get; internal set; }
        public string Text { get; internal set; }
        public AgentButtonType Type { get; internal set; }

        //GetLineType -LINETYPE_HORIZONTAL,LINETYPE_INCLINE, LINETYPE_DECLINE, (only 3 types!)
        //COLOR_EXPLORED, COLOR_UNEXPLORED, COLOR_BLOCKED
        //
        //
        //
        #endregion Properties

        #region Methods

        public bool Click()
        {
            if (DateTime.UtcNow < Time.Instance.LastWindowInteraction.AddSeconds(3))
                return false;

            if (DirectEve.ThreadedCall(_pyHackingLine.Attribute("OnClick")))
            {
                Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        #endregion Methods
    }
}