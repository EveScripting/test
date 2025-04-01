using SharedComponents.EveMarshal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Extensions
{
    public static class PyRepEvePacketParsingExtensions
    {
        public static IEnumerable<long> GetDestructedEntityIds(this PyRep t)
        {
            var terminalDestuctions = t.Descendents()
                .Where(d => d is PyString && d.StringValue.Equals("TerminalPlayDestructionEffect"));
            foreach (var terminalDestuction in terminalDestuctions)
            {
                foreach (var parent in terminalDestuction.Parents)
                {
                    if (parent.Children.Count < 2)
                        continue;
                    var child = parent.Children[1];
                    if (child is PyTuple && child.Children[0] is PyIntegerVar)
                    {
                        yield return child.Children[0].IntValue;
                    }
                }
            }
        }
        /// <summary>
        /// Returns a list of tuples of (launcherID, inventoryID) for all wrecked launchers in the given PyRep
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static IEnumerable<(long, long)> GetAllWreckLauncherIdsAndInventoryIds(this PyRep t)
        {
            var wreckTuples = t.Descendents()
                .Where(d => d is PyString && d.StringValue.Equals("UI/Inflight/WreckNameShipName"));

            foreach (var wreckTuple in wreckTuples)
            {
                foreach (var parent in wreckTuple.Parents)
                {
                    foreach (var pa in parent.Parents)
                    {
                        if (pa is PyDict dict)
                        {
                            var value = dict["launcherID"];
                            if (value != null && value is PyIntegerVar)
                            {
                                var invId = pa.Parents?[0]?.Parents?[0]?.Children?[0];
                                if (invId != null && invId is PyIntegerVar)
                                {
                                    yield return (value.IntValue, invId.IntValue);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
