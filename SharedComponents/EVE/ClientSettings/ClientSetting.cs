using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using SharedComponents.EVE.ClientSettings.Abyssal.Main;
using SharedComponents.EVE.ClientSettings.AbyssalGuard.Main;
using SharedComponents.EVE.ClientSettings.AbyssalHunter.Main;
using SharedComponents.EVE.ClientSettings.AutoBot;
using SharedComponents.EVE.ClientSettings.Global.Main;
using SharedComponents.EVE.ClientSettings.Pinata.Main;
using SharedComponents.EVE.ClientSettings.SharedComponents.EVE.ClientSettings;
using SharedComponents.Utility;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class ClientSetting
    {
        public ClientSetting()
        {

        }
        //NOTE: Default values work only for non list types due a weird behaviour of the serialization

        [TabRoot("Global")]
        public GlobalMainSetting GlobalMainSetting { get; set; } = new GlobalMainSetting();

        [TabRoot("Pinata")]
        public PinataMainSetting PinataMainSetting { get; set; } = new PinataMainSetting();

        [TabRoot("Questor")]
        public QuestorMainSetting QuestorMainSetting { get; set; } = new QuestorMainSetting();

        [TabRoot("Abyssal")]
        public AbyssalMainSetting AbyssalMainSetting { get; set; } = new AbyssalMainSetting();

        [TabRoot("AbyssalGuard")]
        public AbyssalGuardMainSetting AbyssalGuardMainSetting { get; set; } = new AbyssalGuardMainSetting();


        [TabRoot("AbyssalHunter")]
        public AbyssalHunterMainSetting AbyssalHunterMainSetting { get; set; } = new AbyssalHunterMainSetting();

        [TabRoot("AutoBot")]
        public AutoBotMainSetting AutoBotMainSetting { get; set; } = new AutoBotMainSetting();



        public QuestorMainSetting QMS => QuestorMainSetting;

        public AbyssalMainSetting AMS => AbyssalMainSetting;

        public ClientSetting Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (this.GetType().IsSerializable)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, this);
                    stream.Position = 0;
                    return (ClientSetting)formatter.Deserialize(stream);
                }
                return null;
            }
        }
    }
}