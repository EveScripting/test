using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SharedComponents.EVE.DatabaseSchemas
{

    public class AbyssStatEntry
    {

        [AutoIncrement]
        public int Index { get; set; }

        public int AStarErrors { get; set; }

        public int TotalSeconds { get; set; }

        public bool Died { get; set; }

        public DateTime StartedDate { get; set; }
        public double LowestStructure1 { get; set; }

        public double LowestArmor1 { get; set; }

        public double LowestShield1 { get; set; }

        public double LowestStructure2 { get; set; }

        public double LowestArmor2 { get; set; }

        public double LowestShield2 { get; set; }

        public double LowestStructure3 { get; set; }

        public double LowestArmor3 { get; set; }

        public double LowestShield3 { get; set; }

        public double LowestCap { get; set; }

        public int SmallDronesLost { get; set; }

        public int MediumDronesLost { get; set; }

        public int LargeDronesLost { get; set; }

        public int LostDronesRoom1 { get; set; }

        public int LostDronesRoom2 { get; set; }

        public int LostDronesRoom3 { get; set; }

        public bool SingleGateAbyss { get; set; }

        public double Room1Hp { get; set; }

        public double Room2Hp { get; set; }

        public double Room3Hp { get; set; }

        public int Room1Seconds { get; set; }

        public int Room2Seconds { get; set; }

        public int Room3Seconds { get; set; }

        public int FilamentTypeId { get; set; }

        public bool OverheatRoom1 { get; set; }

        public bool OverheatRoom2 { get; set; }

        public bool OverheatRoom3 { get; set; }

        public bool DrugsUsedRoom1 { get; set; }

        public bool DrugsUsedRoom2 { get; set; }

        public bool DrugsUsedRoom3 { get; set; }

        public string Room1Dump { get; set; }

        public string Room2Dump { get; set; }

        public string Room3Dump { get; set; }

        public string PylonsClounds1 { get; set; }

        public string PylonsClounds2 { get; set; }

        public string PylonsClounds3 { get; set; }

        public int Room1Neuts { get; set; }
        public int Room2Neuts { get; set; }
        public int Room3Neuts { get; set; }

        public double Room1NeutGJs { get; set; }
        public double Room2NeutGJs { get; set; }
        public double Room3NeutGJs { get; set; }

        public int Room1CacheMiss { get; set; }
        public int Room2CacheMiss { get; set; }
        public int Room3CacheMiss { get; set; }

        public double ClearDoneGateDist1 { get; set; }

        public double ClearDoneGateDist2 { get; set; }

        public double ClearDoneGateDist3 { get; set; }

        public int DroneRecallsStage1 { get; set; }

        public int DroneRecallsStage2 { get; set; }

        public int DroneRecallsStage3 { get; set; }

        public int DroneEngagesStage1 { get; set; }

        public int DroneEngagesStage2 { get; set; }

        public int DroneEngagesStage3 { get; set; }

        public double DronePercOptimal1 { get; set; }

        public double DronePercOptimal2 { get; set; }

        public double DronePercOptimal3 { get; set; }

        public bool MTULost { get; set; }

        public float LootValueRoom1 { get; set; }

        public float LootValueRoom2 { get; set; }

        public float LootValueRoom3 { get; set; }

        public float LootValueTotal => LootValueRoom1 + LootValueRoom2 + LootValueRoom3;

        public float MilISKPerHour => TotalSeconds == 0 ? 0 : (float)Math.Round(((60 * 60 / TotalSeconds) * LootValueTotal), 2);

        public double PenaltyStrength { get; set; }

        public string LootTableRoom1 { get; set; }

        public string LootTableRoom2 { get; set; }

        public string LootTableRoom3 { get; set; }


    }
}

