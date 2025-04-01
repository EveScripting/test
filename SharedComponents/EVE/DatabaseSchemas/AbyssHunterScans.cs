using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SharedComponents.EVE.DatabaseSchemas
{
    public class AbyssHunterScans
    {
        [AutoIncrement] 
        public int Index { get; set; }
        public long SolarSystemId { get; set; }
        public string SolarSystemName { get; set; }
        public string SignatureId { get; set; }
        public long BookmarkId { get; set; }
        public string BookmarkName { get; set; }
        public int FilamentTypeId { get; set; }
        public string FilamentTypeName { get; set; }
        public int GameMode { get; set; }
        public string AbyssalRunnerCharacterName { get; set; }
        public int AbyssalRunnerCharacterId { get; set; }
        public int AbyssalRunnerShipTypeId { get; set; }
        public int AbyssalRunnerSigRadius { get; set; }
        public int AbyssalRunnerMass { get; set; }
        public DateTime Added { get; set; }
        public bool AreWeSeen { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

    }
}
