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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using EVESharpCore.Cache;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum AgentButtonType
    {
        UNKNOWN,
        ACCEPT,
        CLOSE,
        COMPLETE_MISSION,
        DECLINE,
        DELAY,
        LOCATE_CHARACTER,
        NO_JOBS_AVAILABLE,
        QUIT_MISSION,
        REQUEST_MISSION,
        VIEW_MISSION,
        None
    }

    public enum WindowState
    {
        MISSION_REQUEST_WINDOW,
        MISSION_DETAIL_WINDOW,
        LOADING
    }

    public class DirectAgentWindow : DirectWindow
    {
        #region Constructors

        internal DirectAgentWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            PyObject loading = pyWindow.Attribute("briefingBrowser").Attribute("_loading");
            IsReady = loading.IsValid && !(bool)loading;

            if (pyWindow.Attribute("briefingBrowser").IsValid)
            {
                loading = pyWindow.Attribute("objectiveBrowser").Attribute("_loading");
                IsReady &= loading.IsValid && !(bool)loading;
            }

            AgentId = (int)pyWindow.Attribute("agentID");

            Buttons = new List<DirectAgentButton>();

            //string[] buttonPathRight = { "__maincontainer", "main", "rightPane", "rightPaneBottom" };
            //string[] buttonPathLeft = { "__maincontainer", "main", "rightPaneBottom" };

            //string viewMode = (string)pyWindow.Attribute("viewMode");
            //bool isRight = viewMode != "SinglePaneView";
            //string[] buttonPath = isRight ? buttonPathRight : buttonPathLeft;
            //List<PyObject> buttons = FindChildWithPath(pyWindow, buttonPath).Attribute("children").Attribute("_childrenObjects").ToList();
            //List<PyObject> buttons = pyWindow.Attribute("buttonGroup").Attribute("children").Attribute("_childrenObjects").ToList();

            //carbonui.uicore.uicore.registry.windows[10]
            //.buttonGroup
            //.children
            //._childrenObjects
            //._childrenObjects
            //[0] //btns
            //.children
            //._childrenObjects
            //[0] //ButtonWrapper
            //.children
            //._childrenObjects
            //[0] //AcceptMission_Button

            //carbonui.uicore.uicore.registry.windows[9]
            //.buttonGroup
            //.children
            //._childrenObjects
            //._childrenObjects
            //[0]
            //.children
            //._childrenObjects
            //[0]
            //.children
            //._childrenObjects[0]
            //
            // or
            //
            //carbonui.uicore.uicore.registry.windows[5] (5 will of course vary based on the number of windows opened)
            //      name = AgentConversation
            //.buttonGroup
            //      name = ButtonGroup
            //.children
            //      name = n/a
            //._childrenObjects //1st instance of _childrenObjects
            //      name = n/a
            //._childrenObjects //2nd instance of _childrenObjects
            //      name = n/a
            //      If cast to a list, there is only 1 entry in the list
            //[0]
            //      name == btns //0 contains all the buttons
            //
            //.children
            //      name = n/a
            //._childrenObjects
            //      name = n/a
            //      If cast to a list, there one entry for each button (5?)
            //[0]
            //      name == ButtonWrapper
            //.children
            //      name = n/a
            //._childrenObjects
            //      name = n/a
            //      If cast to a list, there is only 1 entry in the list, the button!
            //[0]
            // name == AcceptMission_Button
            // name == DeclineMission_Button
            // name == QuitMission_Button
            // name == CompleteMission_Button

            var buttonGroup = pyWindow.Attribute("buttonGroup");
            if (!buttonGroup.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup not found");
                return;
            }

            if (buttonGroup.Attribute("name").IsValid && buttonGroup.Attribute("name").ToUnicodeString() != "ButtonGroup")
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.Name != ButtonGroup");
                return;
            }

            if (!buttonGroup.Attribute("children").IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.children not found");
                return;
            }

            if (!buttonGroup.Attribute("children").Attribute("_childrenObjects").IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.children._childrenObjects not found");
                return;
            }

            if (!buttonGroup.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").ToList().Any())
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.children._childrenObjects ToList() == 0");
            }

            if (!buttonGroup.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").GetItemAt(0).IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.children._childrenObjects[0] not found");
            }

            if (!buttonGroup.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").GetItemAt(0).Attribute("children").IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.children._childrenObjects[0].children not found");
            }

            if (!buttonGroup.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").GetItemAt(0).Attribute("children").Attribute("_childrenObjects").IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.children._childrenObjects[0].children._childrenObjects not found");
            }

            if (!buttonGroup.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").GetItemAt(0).Attribute("children").Attribute("_childrenObjects").ToList().Any())
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: buttonGroup.children._childrenObjects[0].children._childrenObjects is empty");
            }

            var buttons = pyWindow
                .Attribute("buttonGroup")
                .Attribute("children")
                .Attribute("_childrenObjects")
                .Attribute("_childrenObjects")
                .GetItemAt(0)
                .Attribute("children")
                .Attribute("_childrenObjects")
                .ToList();

            int icount = 0;
            foreach (var pyBtn in buttons)
            {
                icount++;
                if (!pyBtn.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow:pyBtn !IsValid");
                }

                PyObject btn = null;
                //carbonui.uicore.uicore.registry.windows[9].buttonGroup.children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                if (pyBtn.Attribute("children").IsValid)
                {
                    if (pyBtn.Attribute("children").Attribute("_childrenObjects").IsValid)
                    {
                        if (pyBtn.Attribute("children").Attribute("_childrenObjects").GetItemAt(0).IsValid)
                        {
                            //normal buttons
                            btn = pyBtn.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                        }
                        else if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow:pyBtn.children._childrenObjects.GetItemAt(0) !IsValid");

                        /**
                        if (pyBtn.Attribute("children").Attribute("_childrenObjects").GetItemAt(0).IsValid)
                        {
                            //when does this occur? window looks the same...
                            btn = pyBtn.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                        }
                        else
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow:pyBtn.children._childrenObjects.GetItemAt(0) !IsValid");
                        }
                        **/
                    }
                    else
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow:pyBtn.children._childrenObjects !IsValid");
                    }
                }
                else
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow:pyBtn.children !IsValid");
                }

                var button = new DirectAgentButton(directEve, btn);
                button.AgentId = AgentId;
                button.Text = (string)btn.Attribute("text");
                button.Type = GetButtonType(button.Text);
                button.ButtonName = (string)btn.Attribute("name");
                if (DebugConfig.DebugAgentInteractionReplyToAgent)
                {
                    if (DirectEve.Interval(15000, 15000, WindowId)) Logging.Log.WriteLine("DirectAgentWindow: button[" + icount + "] Text [" + button.Text + "] Type [" + button.Type + "] ButtonName [" + button.ButtonName + "]");
                }

                if (button.Type == AgentButtonType.UNKNOWN)
                    continue;

                if (button.Type == AgentButtonType.DELAY)
                    continue;

                Buttons.Add(button);
            }

            Briefing = (string)pyWindow.Attribute("briefingBrowser").Attribute("sr").Attribute("currentTXT");
            Objective = (string)pyWindow.Attribute("objectiveBrowser").Attribute("sr").Attribute("currentTXT");

            IsReady &= WindowState != WindowState.LOADING;

            if (WindowState == WindowState.MISSION_DETAIL_WINDOW)
            {
                if (ObjectiveEmpty && BriefingEmpty)
                    IsReady = false;

                IsReady = true;
            }

            if (DebugConfig.DebugAgentInteraction) Logging.Log.WriteLine("DirectAgentWindow: IsReady [" + IsReady + "]"); // BriefingHtml [" + Briefing + "] ObjectiveHtml [" + Objective + "]");
        }

        #endregion Constructors

        #region Properties

        public DirectAgent Agent => DirectAgent.GetAgentById(DirectEve, AgentId);
        public long AgentId { get; internal set; }
        public string Briefing { get; internal set; }
        public List<DirectAgentButton> Buttons { get; internal set; }
        public bool IsReady { get; internal set; }
        public string Objective { get; internal set; }
        public bool ObjectiveEmpty => Objective?.Equals("<html><body></body></html>") ?? true;
        public bool BriefingEmpty => Briefing?.Equals("<html><body></body></html>") ?? true;
        //public bool HtmlEmpty => Html?.Equals("<html><body></body></html>") ?? true;

        public int TotalISKReward
        {
            get
            {
                int isk = 0;
                Regex iskRegex = new Regex(@"([0-9]+)((\.([0-9]+))*) ISK", RegexOptions.Compiled);
                foreach (Match itemMatch in iskRegex.Matches(Objective))
                {
                    int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out var val);
                    isk += val;
                }
                return isk;
            }
        }

        public int TotalLPReward
        {
            get
            {
                var lps = 0;
                var lpRegex = new Regex(@"([0-9.]+) Loyalty Points", RegexOptions.Compiled);
                foreach (Match itemMatch in lpRegex.Matches(Objective))
                {
                    int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out var val);
                    lps += val;
                }
                return lps;
            }
        }

        public bool LowSecWarning
        {
            get
            {
                if (Objective.ToLower().Contains("low sec warning!".ToLower()))
                {
                    return true;
                }

                return false;
            }
        }

        public bool RouteContainsLowSecuritySystems
        {
            get
            {
                if (Objective.ToLower().Contains("contains low security systems!".ToLower()))
                {
                    return true;
                }

                return false;
            }
        }

        public WindowState WindowState
        {
            get
            {
                if (!Buttons.Any())
                {
                    if (DebugConfig.DebugAgentInteraction)
                        Logging.Log.WriteLine("WindowState: LOADING: No Buttons Found");

                    return WindowState.LOADING;
                }

                if (Buttons.Any(b => b.Type == AgentButtonType.REQUEST_MISSION))
                {
                    if (DebugConfig.DebugAgentInteraction)
                        Logging.Log.WriteLine("WindowState: MISSION_REQUEST_WINDOW: REQUEST_MISSION Button Found");

                    return WindowState.MISSION_REQUEST_WINDOW;
                }

                if (Buttons.Any(b => b.Type == AgentButtonType.VIEW_MISSION))
                {
                    if (DebugConfig.DebugAgentInteraction)
                        Logging.Log.WriteLine("WindowState: MISSION_REQUEST_WINDOW: VIEW_MISSION Button Found");

                    return WindowState.MISSION_REQUEST_WINDOW;
                }

                //var agent = Agent;
                //if (Buttons.Any(b => b.Type == ButtonType.COMPLETE_MISSION) && agent != null && agent.IsValid)
                //{
                //    var mission = agent.Mission;
                //    if (mission != null && !mission.Bookmarks.Any())
                //        return WindowState.LOADING;
                //}

                if (DebugConfig.DebugAgentInteraction)
                {
                    Logging.Log.WriteLine("WindowState: MISSION_DETAIL_WINDOW: Buttons Found");
                    int intNum = 0;
                    foreach (var Button in Buttons)
                    {
                        intNum++;
                        Logging.Log.WriteLine("WindowState: [" + intNum + "] Button: Name [" + Button.ButtonName + "] Text [" + Button.Text + "] AgentId [" + Button.AgentId + "] Type [" + Button.Type.ToString() + "]");
                    }
                }

                return WindowState.MISSION_DETAIL_WINDOW;
            }
        }

        public FactionType? GetFactionType()
        {
            if (ObjectiveEmpty)
                return null;

            Regex logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");
            Match logoMatch = logoRegex.Match(Objective);
            if (logoMatch.Success)
            {
                string id = logoMatch.Groups["factionlogo"].Value;
                if (int.TryParse(id, out var res))
                    return DirectFactions.GetFactionTypeById(res);
            }

            return FactionType.Unknown;
        }

        private AgentButtonType GetButtonType(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                    return AgentButtonType.UNKNOWN;

                //if (s.Contains("Sorry, I have no jobs available for you."))
                //    return ButtonType.NO_JOBS_AVAILABLE;

                if (Enum.TryParse<AgentButtonType>(s.ToUpper().Replace(" ", "_"), out var type))
                    return type;

                return AgentButtonType.UNKNOWN;
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
                return AgentButtonType.UNKNOWN;
            }
        }

        #endregion Properties
    }
}