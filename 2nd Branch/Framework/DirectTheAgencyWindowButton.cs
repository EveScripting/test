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

using EVESharpCore.Cache;
using EVESharpCore.Lookup;
using SC::SharedComponents.Py;
using System;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectTheAgencyWindowButton : DirectObject
    {
        #region Fields

        private readonly PyObject _pyBtn;
        //private readonly PyObject _pycardTitleLabel;

        #endregion Fields

        #region Constructors

        internal DirectTheAgencyWindowButton(DirectEve directEve, PyObject pyBtn)
            : base(directEve)
        {
            _pyBtn = pyBtn;
        }

        #endregion Constructors

        #region Properties

        public string ButtonName { get; internal set; }

        public string Text { get; set; }

        public string solarSystemNameText { get; set; }

        private DirectSolarSystem _solarSystem = null;
        public DirectSolarSystem SolarSystem
        {
            get
            {
                if (_solarSystem != null)
                    return _solarSystem;

                if (!string.IsNullOrEmpty(Text))
                {
                    //Take the string Text and parse it to get the system name
                    //example: Text: Niyabainen <color='0xFF2C75E2'>1.0</color>
                    //strip the any HTML tags from Text and store in solarSystemNameText
                    solarSystemNameText = Text;
                    //remove any positive or negative decimal numbers from systemNameText that start with 0 or 1 using a regex pattern
                    solarSystemNameText = System.Text.RegularExpressions.Regex.Replace(solarSystemNameText, @"\b[01]\.\d+\b", String.Empty);
                    //remove any "-" inside any HTML tags and ONLY inside HTML tags from systemNameText using regex pattern
                    //solarSystemNameText = System.Text.RegularExpressions.Regex.Replace(solarSystemNameText, "-.*?>", String.Empty);
                    //remove any HTML tags from systemNameText using regex pattern
                    solarSystemNameText = System.Text.RegularExpressions.Regex.Replace(solarSystemNameText, "<.*?>", String.Empty);
                    //if the last character is a - remove it
                    solarSystemNameText = solarSystemNameText.TrimEnd('-');
                    //remove any "." from systemNameText
                    //solarSystemNameText = solarSystemNameText.Replace(".", "");
                    //strip any spaces from systemNameText
                    solarSystemNameText = solarSystemNameText.Trim();
                    //find the first SolarSystem in ESCache.Instance.DirectEve.SolarSystems that matches solarsystemNameText
                    //
                    DirectSolarSystem thisSolarSystem = ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Value.Name.ToLower() == solarSystemNameText.ToLower()).Value;
                    if (thisSolarSystem != null)
                    {
                        _solarSystem = thisSolarSystem;
                        return _solarSystem;
                    }
                }

                return null;
            }
        }

        public TheAgencyWindowButtonType Type { get; internal set; }

        #endregion Properties

        #region Methods

        public bool Click()
        {
            if (DateTime.UtcNow < Time.Instance.LastWindowInteraction.AddSeconds(3))
                return false;

            if (DirectEve.ThreadedCall(_pyBtn.Attribute("OnClick")))
            {
                Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        #endregion Methods
    }
}