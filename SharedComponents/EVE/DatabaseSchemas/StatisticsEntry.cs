/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 21.05.2016
 * Time: 00:14
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using ServiceStack.DataAnnotations;
using System;

namespace SharedComponents.EVE
{
    public class StatisticsEntry
    {
        #region Fields

        private static int ISKperLP = 500;

        private decimal? _ISKperHour;

        private decimal? _MillionISKperHour;

        #endregion Fields

        #region Properties

        [AutoIncrement]
        public int Index { get; set; }

        public long AfterMissionsalvageTime { get; set; }

        public long AmmoConsumption { get; set; }

        public long AmmoValue { get; set; }

        public string Charname { get; set; }

        public DateTime Date { get; set; }

        public long DroneRecalls { get; set; }

        public string DungeonID { get; set; }

        public string Faction { get; set; }

        public decimal FactionStanding { get; set; }


        public long Isk { get; set; }
        public long ISKLootHangarItems { get; set; }

        public decimal? ISKperHour
        {
            get
            {
                if (_ISKperHour != null)
                    return _ISKperHour;

                if (Time <= 0)
                {
                    _ISKperHour = 0;
                    return _ISKperHour;
                }

                var Isk = this.Isk + IskReward + Loot + LP * ISKperLP;
                _ISKperHour = Isk * 60 / Time;
                return _ISKperHour;
            }
        }

        public int IskReward { get; set; }
        public decimal ISKWallet { get; set; }
        public long Loot { get; set; }
        public long LostDrones { get; set; }
        public long LowestArmor { get; set; }
        public long LowestCap { get; set; }
        public long LowestShield { get; set; }
        public long LP { get; set; }

        public decimal? MillionISKperHour
        {
            get
            {
                if (_MillionISKperHour != null)
                    return _MillionISKperHour;
                _MillionISKperHour = ISKperHour / 1000000;
                return _MillionISKperHour;
            }
        }

        public decimal MinStandingAgentCorpFaction { get; set; }
        public string Mission { get; set; }

        public bool MissionXMLAvailable { get; set; }
        public long OutOfDronesCount { get; set; }
        public long Panics { get; set; }
        public long RepairCycles { get; set; }
        public string SolarSystem { get; set; }
        public int Time { get; set; }
        public long TotalLP { get; set; }
        public long TotalMissionTime { get; set; }

        #endregion Properties

        #region Methods

        public static int GetISKperLP()
        {
            return ISKperLP;
        }

        public static void SetISKperLP(int iskPerLP)
        {
            ISKperLP = iskPerLP;
        }

        #endregion Methods

        public override string ToString()
        {
            return $"{nameof(_ISKperHour)}: {_ISKperHour}, {nameof(_MillionISKperHour)}: {_MillionISKperHour}, {nameof(Index)}: {Index}, {nameof(AfterMissionsalvageTime)}: {AfterMissionsalvageTime}, {nameof(AmmoConsumption)}: {AmmoConsumption}, {nameof(AmmoValue)}: {AmmoValue}, {nameof(Charname)}: {Charname}, {nameof(Date)}: {Date}, {nameof(DroneRecalls)}: {DroneRecalls}, {nameof(DungeonID)}: {DungeonID}, {nameof(Faction)}: {Faction}, {nameof(FactionStanding)}: {FactionStanding}, {nameof(Isk)}: {Isk}, {nameof(ISKLootHangarItems)}: {ISKLootHangarItems}, {nameof(ISKperHour)}: {ISKperHour}, {nameof(IskReward)}: {IskReward}, {nameof(ISKWallet)}: {ISKWallet}, {nameof(Loot)}: {Loot}, {nameof(LostDrones)}: {LostDrones}, {nameof(LowestArmor)}: {LowestArmor}, {nameof(LowestCap)}: {LowestCap}, {nameof(LowestShield)}: {LowestShield}, {nameof(LP)}: {LP}, {nameof(MillionISKperHour)}: {MillionISKperHour}, {nameof(MinStandingAgentCorpFaction)}: {MinStandingAgentCorpFaction}, {nameof(Mission)}: {Mission}, {nameof(MissionXMLAvailable)}: {MissionXMLAvailable}, {nameof(OutOfDronesCount)}: {OutOfDronesCount}, {nameof(Panics)}: {Panics}, {nameof(RepairCycles)}: {RepairCycles}, {nameof(SolarSystem)}: {SolarSystem}, {nameof(Time)}: {Time}, {nameof(TotalLP)}: {TotalLP}, {nameof(TotalMissionTime)}: {TotalMissionTime}";
        }
    }
}