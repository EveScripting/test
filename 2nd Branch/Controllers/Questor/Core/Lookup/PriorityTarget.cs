// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using EVESharpCore.Cache;
using EVESharpCore.Logging;

namespace EVESharpCore.Lookup
{
    public class PriorityTarget
    {
        #region Methods

        public void ClearCache()
        {
            _entity = null;
        }

        #endregion Methods

        #region Fields

        private EntityCache _entity;

        private string _maskedID;

        #endregion Fields

        #region Properties

        public DronePriority DronePriority { get; set; }
        public EntityCache Entity => _entity ?? (_entity = ESCache.Instance.EntityById(EntityID));
        public long EntityID { get; set; }

        public string MaskedID
        {
            get
            {
                try
                {
                    int numofCharacters = EntityID.ToString().Length;
                    if (numofCharacters >= 5)
                    {
                        _maskedID = EntityID.ToString().Substring(numofCharacters - 4);
                        _maskedID = "[MaskedID]" + _maskedID;
                        return _maskedID;
                    }

                    return "!0!";
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return "!0!";
                }
            }
        }

        public string Name { get; set; }

        public PrimaryWeaponPriority PrimaryWeaponPriority { get; set; }

        #endregion Properties
    }
}