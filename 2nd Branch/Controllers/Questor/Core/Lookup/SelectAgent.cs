using System.Xml.Linq;

namespace EVESharpCore.Lookup
{
    public class AgentsList
    {
        #region Constructors

        public AgentsList()
        {
        }

        public AgentsList(XElement agentList)
        {
            Name = (string)agentList.Attribute("name") ?? "";
            Priorit = (int)agentList.Attribute("priority");
            //var homeStationId = (long) agentList.Attribute("homestationid") > 0 ? (long) agentList.Attribute("homestationid") : 60003760;
            //HomeStationId = homeStationId;
        }

        #endregion Constructors

        #region Properties

        public string Name { get; }

        public int Priorit { get; }

        #endregion Properties

        //public long HomeStationId { get; private set; }
    }
}