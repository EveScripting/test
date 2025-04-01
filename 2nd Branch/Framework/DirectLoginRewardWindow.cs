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

using EVESharpCore.Cache;
using EVESharpCore.Lookup;
using SC::SharedComponents.Py;
using SC::SharedComponents.EVE;
using System;
using System.Collections.Generic;
using System.Linq;
using SC::SharedComponents.Events;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum LoginRewardButtonType
    {
        CLAIM,
        CLOSE,
        UNKNOWN,
        None
    }

    public class DirectLoginRewardWindow : DirectWindow
    {

        public List<DirectLoginRewardButton> Buttons { get; internal set; }
        private DirectEve _directeve = null;

        internal DirectLoginRewardWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            _directeve = directEve;
            Buttons = new List<DirectLoginRewardButton>();

            if (!DebugConfig.ClaimLoginRewards)
                return;

            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            // rewardsContiner
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //bottomContainer
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //checkbox
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //innerCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //topCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ToggleButtonGroup
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Button_evergreenBtn
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //labelClipperGrid
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //campaignCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //evergreenRewardPanel
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //centerCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0, 1, 2, 3
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //OneTrackRewardBottomCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0, 1,2,3
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Container
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //Container
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //ClaimButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //label
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //underlay
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //button
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //label
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //underlay
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //upgradeBtnCont - Upgrade to Omega!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //uppSellText - Upgrade to Omega!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //contTop
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //EveLabelMedium
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //oneTrackTodaysItemTop
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //EveLabelLargeBold
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //MoreInfoIcon
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //leftCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //itemsPar
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //itemsParInner
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //itemsCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //rewardGrid
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //sideCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //todaysItem
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //countdownCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1]
            //todaysItemMain
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //previewItemEntry
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //redeemingLabel
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //bgCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //backgroundImage
            //

            //Claim Button is:
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]


            //Claim Button is:
            //carbonui.uicore.uicore.registry.windows[11]
            //.children._childrenObjects[2] content
            //.children._childrenObjects[2] main
            //.children._childrenObjects[0] rewardsContainer
            //.children._childrenObjects[1] innerCont
            //.children._childrenObjects[1] campaignCont
            //.children._childrenObjects[0] evergreenRewardPanel
            //.children._childrenObjects[0] centerCont
            //.children._childrenObjects[0] OneTrackRewardBottomCont
            //.children._childrenObjects[1] Container
            //.children._childrenObjects[0] ClaimButton


            //2nd tab?
            //carbonui.uicore.uicore.registry.windows[9]
            //.children._childrenObjects[2] content
            //.children._childrenObjects[2] main
            //.children._childrenObjects[0] rewardsContainer
            //.children._childrenObjects[1] innerCont
            //.children._childrenObjects[1] campaignCont
            //.children._childrenObjects[0] SeasonRewardPanel*
            //.children._childrenObjects[0] centerCont*
            //.children._childrenObjects[0] OneTrackRewardBottomCont*
            //.children._childrenObjects[1] Container*
            //.children._childrenObjects[0] ClaimButton*


            //Path broken down:
            //carbonui.uicore.uicore.registry.windows[9]
            //.children._childrenObjects[2] content
            //.children._childrenObjects[2] main
            var main = pyWindow.Attribute("children").Attribute("_childrenObjects").GetItemAt(2).Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
            if (!main.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: main not found");
                return;
            }

            if (main.Attribute("name").IsValid && main.Attribute("name").ToUnicodeString().ToLower() != "main".ToLower())
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != main");
                return;
            }
            else
            {
                //.children._childrenObjects[2] content
                //.children._childrenObjects[2] main
                //.children._childrenObjects[0] rewardsContainer
                var rewardsContainer = main.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                if (!rewardsContainer.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: rewardsContainer not found");
                    return;
                }

                if (!rewardsContainer.Attribute("name").IsValid && rewardsContainer.Attribute("name").ToUnicodeString().ToLower() != "rewardsContainer".ToLower())
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != rewardsContainer");
                    return;
                }
                else
                {
                    //.children._childrenObjects[2] content
                    //.children._childrenObjects[2] main
                    //.children._childrenObjects[0] rewardsContainer
                    //.children._childrenObjects[1] innerCont
                    var innerCont = rewardsContainer.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                    if (!innerCont.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: innerCont not found");
                    }

                    if (!innerCont.Attribute("name").IsValid && innerCont.Attribute("name").ToUnicodeString().ToLower() != "innerCont".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != innerCont");
                    }
                    else
                    {
                        //.children._childrenObjects[2] content
                        //.children._childrenObjects[2] main
                        //.children._childrenObjects[0] rewardsContainer
                        //.children._childrenObjects[1] innerCont
                        //.children._childrenObjects[1] campaignCont
                        var campaignCont = innerCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                        if (!campaignCont.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: campaignCont not found");
                        }

                        if (!campaignCont.Attribute("name").IsValid && campaignCont.Attribute("name").ToUnicodeString().ToLower() != "campaignCont".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != campaignCont");
                        }
                        else
                        {
                            //.children._childrenObjects[2] content
                            //.children._childrenObjects[2] main
                            //.children._childrenObjects[0] rewardsContainer
                            //.children._childrenObjects[1] innerCont
                            //.children._childrenObjects[1] campaignCont
                            //.children._childrenObjects //evergreenRewardPanel || SeasonRewardPanel
                            var ListOfPanels = campaignCont.Attribute("children").Attribute("_childrenObjects").ToList();
                            Panels(ListOfPanels);

                            foreach (var pyPanel in ListOfPanels)
                            {
                                if (pyPanel.IsValid && pyPanel.Attribute("OnClick").IsValid)
                                {
                                    if (this.Buttons.Any(i => i.Type != LoginRewardButtonType.CLAIM))
                                    {
                                        if (!pyPanel.Attribute("display").ToBool())
                                        {
                                            if (DirectEve.Interval(6000, 9000)) DirectEve.ThreadedCall(pyPanel.Attribute("OnClick"));
                                        }
                                        if (DirectEve.Interval(6000, 9000)) DirectEve.ThreadedCall(pyPanel.Attribute("OnClick"));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private LoginRewardButtonType GetButtonType(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                    return LoginRewardButtonType.UNKNOWN;

                //if (s.Contains("Sorry, I have no jobs available for you."))
                //    return ButtonType.NO_JOBS_AVAILABLE;

                if (s.ToLower().Contains("Claim".ToLower()))
                    return LoginRewardButtonType.CLAIM;

                if (s.ToLower().Contains("Close".ToLower()))
                    return LoginRewardButtonType.CLOSE;

                return LoginRewardButtonType.UNKNOWN;
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
                return LoginRewardButtonType.UNKNOWN;
            }
        }

        private bool Panels(List<PyObject> ListOfPanels)
        {
            foreach (var panel in ListOfPanels)
            {
                if (!panel.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: evergreenRewardPanel not found");
                }

                if (!panel.Attribute("name").IsValid && !panel.Attribute("name").ToUnicodeString().ToLower().Contains("RewardPanel".ToLower()))
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name [" + panel.Attribute("name").ToUnicodeString() + "] did not contain RewardPanel");
                }
                else
                {
                    //centerCont
                    var centerCont = panel.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                    if (!centerCont.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: centerCont not found");
                    }

                    if (!centerCont.Attribute("name").IsValid && centerCont.Attribute("name").ToUnicodeString().ToLower() != "centerCont".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != centerCont");
                    }
                    else
                    {
                        //OneTrackRewardBottomCont
                        var OneTrackRewardBottomCont = centerCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                        if (!OneTrackRewardBottomCont.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: OneTrackRewardBottomCont not found");
                        }

                        if (!OneTrackRewardBottomCont.Attribute("name").IsValid && OneTrackRewardBottomCont.Attribute("name").ToUnicodeString().ToLower() != "OneTrackRewardBottomCont".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != OneTrackRewardBottomCont");
                        }
                        else
                        {
                            //Container
                            var Container = OneTrackRewardBottomCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                            if (!Container.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Container not found");
                            }

                            if (!Container.Attribute("name").IsValid && Container.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != Container");
                            }
                            else
                            {
                                //ClaimButton
                                var ClaimButton = Container.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                if (!ClaimButton.IsValid)
                                {
                                    if (DebugConfig.DebugWindows) if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("ClaimButton !IsValid");
                                }

                                if (!ClaimButton.Attribute("name").IsValid || ClaimButton.Attribute("name").ToUnicodeString().ToLower() != "ClaimButton".ToLower())
                                {
                                    if (DebugConfig.DebugWindows) if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ClaimButton");
                                }
                                else //carbonui.uicore.uicore.registry.windows[9].buttonGroup.children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                                {
                                    if (ClaimButton.Attribute("enabled").ToBool() && ClaimButton.Attribute("display").ToBool())
                                    {
                                        DirectLoginRewardButton claim_button = new DirectLoginRewardButton(_directeve, ClaimButton);
                                        claim_button.Text = (string)ClaimButton.Attribute("text");
                                        claim_button.Type = GetButtonType(claim_button.Text);
                                        claim_button.ButtonName = (string)ClaimButton.Attribute("name");
                                        if (DebugConfig.DebugWindows)
                                        {
                                            if (DirectEve.Interval(15000, 15000, WindowId)) Logging.Log.WriteLine("button Text [" + claim_button.Text + "] Type [" + claim_button.Type + "] ButtonName [" + claim_button.ButtonName + "]");
                                        }

                                        if (claim_button.Type == LoginRewardButtonType.CLAIM)
                                            Buttons.Add(claim_button);

                                        if (DirectEve.Interval(6000, 8000, "ClaimButton", true))
                                        {
                                            if (DebugConfig.ClaimLoginRewards)
                                            {
                                                if (Buttons.FirstOrDefault(i => i.Type == LoginRewardButtonType.CLAIM).Click())
                                                {
                                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastLoginRewardClaim), DateTime.UtcNow);
                                                }
                                            }
                                        }
                                    }
                                }

                                //CloseButton
                                var CloseButton = Container.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                if (!CloseButton.IsValid)
                                {
                                    if (DebugConfig.DebugWindows) if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("CloseButton !IsValid");
                                }

                                if (!CloseButton.Attribute("name").IsValid || CloseButton.Attribute("name").ToUnicodeString().ToLower() != "CloseButton".ToLower())
                                {
                                    if (DebugConfig.DebugWindows) if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != CloseButton");
                                }
                                else
                                {
                                    if (CloseButton.Attribute("enabled").ToBool() && ClaimButton.Attribute("display").ToBool())
                                    {
                                        DirectLoginRewardButton close_button = new DirectLoginRewardButton(_directeve, CloseButton);
                                        close_button.Text = (string)ClaimButton.Attribute("text");
                                        close_button.Type = GetButtonType(close_button.Text);
                                        close_button.ButtonName = (string)ClaimButton.Attribute("name");
                                        if (DebugConfig.DebugWindows)
                                        {
                                            if (DirectEve.Interval(15000, 15000, WindowId)) Logging.Log.WriteLine("button Text [" + close_button.Text + "] Type [" + close_button.Type + "] ButtonName [" + close_button.ButtonName + "]");
                                        }

                                        if (close_button.Type == LoginRewardButtonType.CLOSE)
                                            Buttons.Add(close_button);

                                        if (DirectEve.Interval(7000, 8000, "CloseButton", true))
                                        {
                                            if (DebugConfig.ClaimLoginRewards)
                                            {
                                                if (Buttons.FirstOrDefault(i => i.Type == LoginRewardButtonType.CLOSE).Click())
                                                {
                                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastRewardRedeem), DateTime.UtcNow);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}