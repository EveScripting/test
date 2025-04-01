/*
 * Created by huehue.
 * User: duketwo
 * Date: 01.05.2017
 * Time: 18:31
 *
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework.Events;
using EVESharpCore.Cache;
using EVESharpCore.Questor.Lookup;
using SC::SharedComponents.Events;


namespace EVESharpCore.Controllers
{
    public class UILootController : BaseController
    {
        public UILootController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            ControllerManager.Instance.SetPause(true);
            Form = new UILootControllerForm(this);
        }

        public override bool EvaluateDependencies(List<BaseController> controllerList)
        {
            var loginController = controllerList.FirstOrDefault(c => c.GetType() == typeof(LoginController));
            if (loginController == null || !loginController.IsWorkDone)
                return false;
            return true;
        }

        public override void DoWork()
        {
            if (!CheckSessionValid("UILootController"))
                return;

            if (DateTime.UtcNow < QCache.Instance.EveAccount.LastSessionChange.AddSeconds(15))
                return;

            if (DebugConfig.DebugUnloadLoot) Log("UILootController pulse.");
        }
    }
}