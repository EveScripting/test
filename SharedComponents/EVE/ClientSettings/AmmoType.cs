using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class AmmoType
    {
        public AmmoType()
        {

        }

        public AmmoType(XElement ammo)
        {
            try
            {
                TypeId = (int)ammo.Attribute("typeId");
                DamageType = (DamageType)Enum.Parse(typeof(DamageType), (string)ammo.Attribute("damageType"));
                Range = (int)ammo.Attribute("range");
                Quantity = (int)ammo.Attribute("quantity");

            }
            catch (Exception ex)
            {
                Console.WriteLine("AmmoType exception: " + ex);
            }
        }

        public int TypeId { get; set; }
        [Browsable(false)]
        public DamageType DamageType { get; set; }
        public int Range { get; set; }
        public int Quantity { get; set; }

        public AmmoType Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (this.GetType().IsSerializable)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Binder = new CustomizedBinder();
                    formatter.Serialize(stream, this);
                    stream.Position = 0;
                    return (AmmoType)formatter.Deserialize(stream);
                }
                return null;
            }
        }

        [Serializable]
        private sealed class CustomizedBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.Contains(nameof(AmmoType)))
                    return typeof(AmmoType);
                if (typeName.Contains(nameof(ClientSettings.DamageType)))
                    return typeof(DamageType);
                return null;
            }
        }
    }
}