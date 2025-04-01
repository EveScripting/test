using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void ReloadAllAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                    return;

                Log.WriteLine("Reload All Action"); // reload ammo
                //if (!Combat.Combat.ReloadAll()) return; // reload ammo
                if (!Combat.Combat.BoolReloadWeaponsAsap && ESCache.Instance.Weapons.Any(i => !i.IsCivilianWeapon))
                {
                    Log.WriteLine("ReloadAllAction: BoolReloadWeaponsAsap = true");
                    Combat.Combat.BoolReloadWeaponsAsap = true;
                }

                Log.WriteLine("Done queing a reload"); // reload ammo
                NextAction(myMission, myAgent, true);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}