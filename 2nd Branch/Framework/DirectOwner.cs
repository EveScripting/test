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

namespace EVESharpCore.Framework
{
    public class DirectOwner : DirectInvType
    {
        #region Constructors

        internal DirectOwner(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Methods

        public static DirectOwner GetOwner(DirectEve directEve, long ownerId)
        {
            PyObject pyOwner = null;
            if (ownerId != -1 && ownerId != 0)
                pyOwner = directEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("eveowners").Call("GetIfExists", ownerId);

            DirectOwner owner = new DirectOwner(directEve);
            if (pyOwner != null)
            {
                owner.OwnerId = (long) pyOwner.Attribute("ownerID");
                owner.CorpId = (long)pyOwner.Attribute("corpid");
                owner.Name = (string) pyOwner.Attribute("ownerName");
                owner.TypeId = (int) pyOwner.Attribute("typeID");
                owner.TickerName = (string) pyOwner.Attribute("ticketName") ?? string.Empty;
                owner.ShortName = (string) pyOwner.Attribute("shortName") ?? string.Empty;
                return owner;
            }

            return owner;
        }

        #endregion Methods

        #region Properties

        public string Name { get; private set; } = string.Empty;
        public long CorpId { get; private set; } = -1;
        public long OwnerId { get; private set; } = -1;
        public string ShortName { get; private set; } = string.Empty;
        public string TickerName { get; private set; } = string.Empty;

        #endregion Properties
    }
}