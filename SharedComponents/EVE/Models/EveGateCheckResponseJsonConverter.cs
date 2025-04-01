using Newtonsoft.Json;

namespace SharedComponents.EVE.Models
{
    public class EveGateCheckResponseJsonConverter : JsonConverter<EveGateCheckResponse>
    {
        public override void WriteJson(JsonWriter writer, EveGateCheckResponse value, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
        }

        public override EveGateCheckResponse ReadJson(JsonReader reader, System.Type objectType, EveGateCheckResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<Newtonsoft.Json.Linq.JObject>(reader);
            var response = new EveGateCheckResponse();
            
            foreach (var property in jObject.Properties())
            {
                if (property.Name == "premium")
                {
                    response.IsPremium = property.Value.ToObject<bool>();
                    continue;
                }
                if (property.Name == "tot_time")
                {
                    response.RequestTimeTaken = property.Value.ToObject<double>();
                    continue;
                }
                if (int.TryParse(property.Name, out var solarSystemId))
                {
                    var solarSystemKills = property.Value.ToObject<SolarSystemEntry>(serializer);
                    response.SolarSystemKills[solarSystemId] = solarSystemKills;
                }
            }
            
            return response;
        }
    }
}