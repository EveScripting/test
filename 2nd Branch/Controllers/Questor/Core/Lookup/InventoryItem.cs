extern alias SC;

using System;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using SC::SharedComponents.EVE.ClientSettings;

namespace EVESharpCore.Lookup
{
    public class Ammo
    {
        #region Fields

        private string _name = string.Empty;

        #endregion Fields

        #region Methods

        public Ammo Clone()
        {
            Ammo _ammo = new Ammo
            {
                TypeId = TypeId,
                DamageType = DamageType,
                Range = Range,
                Quantity = Quantity,
                Description = Description
            };
            return _ammo;
        }

        #endregion Methods

        #region Constructors

        public Ammo()
        {
        }

        public Ammo(XElement ammo)
        {
            try
            {
                TypeId = (int)ammo.Attribute("typeId");
                DamageType = (DamageType)Enum.Parse(typeof(DamageType), (string)ammo.Attribute("damageType"));
                Range = (int)ammo.Attribute("range");
                Quantity = (int)ammo.Attribute("quantity");
                Description = (string)ammo.Attribute("description") ?? (string)ammo.Attribute("typeId");

                //if (!ESCache.Instance.DirectEve.DoesInvTypeExistInTypeStorage(TypeId))
                //    Log.WriteLine("ERROR: DefinedAmmoTypes.TypeId: " + TypeId + " was NOT found in type storage. Fix your ammo type ids.");
                //else
                //    if (DebugConfig.DebugArm) Log.WriteLine("DefinedAmmoTypes.TypeId: " + TypeId + " was found in type storage");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Constructors

        #region Properties

        public DamageType DamageType { get; private set; }

        public string Description { get; set; }

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_name))
                    return _name;

                string ret = string.Empty;
                //if (!ESCache.Instance.DirectEve.DoesInvTypeExistInTypeStorage(TypeId))
                //    return ret;

                DirectInvType invType = ESCache.Instance.DirectEve.GetInvType(TypeId);

                if (invType == null)
                    return ret;

                string typeName = invType.TypeName;
                _name = typeName;
                return typeName;
            }
        }

        public int Quantity { get; set; }
        public int Range { get; private set; }
        public int TypeId { get; private set; }

        #endregion Properties
    }

    public class InventoryItem
    {
        #region Methods

        public InventoryItem Clone()
        {
            InventoryItem stuffToHaul = new InventoryItem
            {
                TypeId = TypeId,
                Quantity = Quantity,
                Description = Description,
                Priority = Priority,
                SellOrderValue = SellOrderValue,
                BuyOrderValue = BuyOrderValue
            };
            return stuffToHaul;
        }

        #endregion Methods

        #region Fields

        //
        // use priority levels to decide what to do 1st vs last, 1 being the highest priority.
        //
        public int Priority = 5;

        private string _name = string.Empty;

        #endregion Fields

        #region Constructors

        public InventoryItem()
        {
        }

        public InventoryItem(XElement xmlInventoryItem)
        {
            try
            {
                TypeId = (int)xmlInventoryItem.Attribute("typeId");
                Quantity = (int?)xmlInventoryItem.Attribute("quantity") ?? 1;
                Priority = (int?)xmlInventoryItem.Attribute("priority") ?? 5;
                SellOrderValue = (int?)xmlInventoryItem.Attribute("sellordervalue") ?? 0;
                BuyOrderValue = (int?)xmlInventoryItem.Attribute("buyordervalue") ?? 0;
                Description = (string)xmlInventoryItem.Attribute("description") ?? (string)xmlInventoryItem.Attribute("typeId");

                //if (!ESCache.Instance.DirectEve.DoesInvTypeExistInTypeStorage(TypeId))
                //    Log.WriteLine("ERROR: xmlInventoryItem.TypeId: " + TypeId + " was NOT found in type storage. Fix your xmlInventoryItem type ids.");
                //else
                //    Log.WriteLine("xmlInventoryItem.TypeId: " + TypeId + " was found in type storage");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Constructors

        #region Properties

        public int BuyOrderValue { get; set; }

        public string Description { get; set; }

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_name))
                    return _name;

                string ret = string.Empty;
                //if (!ESCache.Instance.DirectEve.DoesInvTypeExistInTypeStorage(TypeId))
                //    return ret;

                DirectInvType invType = ESCache.Instance.DirectEve.GetInvType(TypeId);

                if (invType == null)
                    return ret;

                string typeName = invType.TypeName;
                _name = typeName;
                return typeName;
            }
        }

        public int Quantity { get; set; }
        public int SellOrderValue { get; set; }
        public int TypeId { get; private set; }

        #endregion Properties
    }
}