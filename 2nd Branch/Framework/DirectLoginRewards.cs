// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Py;
using System.Collections.Generic;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectLoginRewards : DirectObject
    {
        #region Fields

        #endregion Fields

        #region Properties
        public bool has_claimed_today
        {
            get
            {
                PyObject pyLoginRewardService = DirectEve.GetLocalSvc("loginRewardService");
                if (pyLoginRewardService.IsValid)
                {
                    bool? temp_has_claimed_today = (bool)pyLoginRewardService.Call("has_claimed_today");
                    return temp_has_claimed_today ?? false;
                }

                return false;
            }
        }

        public bool user_has_unclaimed_rewards
        {
            get
            {
                PyObject pyLoginRewardService = DirectEve.GetLocalSvc("loginRewardService");
                if (pyLoginRewardService.IsValid)
                {
                    bool? temp_has_claimed_today = (bool)pyLoginRewardService.Call("user_has_unclaimed_rewards");
                    return temp_has_claimed_today ?? false;
                }

                return false;
            }
        }

        public bool claim_reward
        {
            get
            {
                if (!user_has_unclaimed_rewards) return false;
                if (has_claimed_today) return false;
                //if ()


                PyObject pyLoginRewardService = DirectEve.GetLocalSvc("loginRewardService");
                if (pyLoginRewardService.IsValid)
                {
                    bool? temp_claimed_reward = (bool)pyLoginRewardService.Call("claim_reward");
                    return temp_claimed_reward ?? false;
                }

                return false;
            }
        }

        #endregion Properties

        #region Constructors

        internal DirectLoginRewards(DirectEve directEve, string windowName) : base(directEve)
        {
            _windowName = windowName;
        }

        #endregion Constructors

        #region Methods

        private readonly string _windowName;
        /**
        private DirectLoginRewardWindow _window;
        public DirectLoginRewardWindow Window
        {
            get
            {
                if (_window == null && !string.IsNullOrEmpty(_windowName))
                    _window = DirectEve.Windows.OfType<DirectLoginRewardWindow>().FirstOrDefault(w => w.Name.Contains(_windowName));

                if (_window == null && _itemId != 0)
                {
                    _window = DirectEve.Windows.OfType<DirectLoginRewardWindow>().FirstOrDefault(w => (w.CurrInvIdItem == _itemId || w.GetIdsFromTree(false).Contains(_itemId)) && !w.IsPrimary());
                    if (_window == null)
                        _window = DirectEve.Windows.OfType<DirectLoginRewardWindow>().FirstOrDefault(w => w.IsPrimary() && (w.CurrInvIdItem == _itemId || w.GetIdsFromTree(false).Contains(_itemId)));
                }
                return _window;
            }
        }

        public DirectLoginRewardWindow _loginRewardWindow;
        public DirectLoginRewardWindow LoginRewardWindow
        {
            get
            {
                if (_loginRewardWindow == null)
                {

                    if (!_loginRewardWindow.IsValid)
                        return null;

                    if (_loginRewardWindow.Window == null)
                    {
                        DirectEve.ExecuteCommand(DirectCmd.OpenLoginCampaignWindow);
                        return null;
                    }
                    if (!_loginRewardWindow.IsReady)
                        return null;

                    _loginRewardWindow = shipsCargo;
                }

                return _loginRewardWindow;
            }
        }
        **/

        /// <summary>
        ///     Try to fit this fitting
        /// </summary>
        /// <returns></returns>

        #endregion Methods

        #region Properties

        #endregion Properties
    }
}