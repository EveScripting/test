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
using System.Linq;
using System.Text.RegularExpressions;
using EVESharpCore.Cache;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum CareerAgentButtonType
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

    public enum CareerAgentWindowState
    {
        MISSION_REQUEST_WINDOW,
        MISSION_DETAIL_WINDOW,
        LOADING
    }

    public class DirectCareerAgentWindow : DirectWindow
    {
        #region Constructors

        internal DirectCareerAgentWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            //PyObject loading = pyWindow.Attribute("briefingBrowser").Attribute("_loading");
            //IsReady = loading.IsValid && !(bool)loading;
            //
            //if (pyWindow.Attribute("briefingBrowser").IsValid)
            //{
            //    loading = pyWindow.Attribute("objectiveBrowser").Attribute("_loading");
            //    IsReady &= loading.IsValid && !(bool)loading;
            //}

            AgentId = (int)pyWindow.Attribute("npc_character_id"); //npc_character_id

            Buttons = new List<DirectAgentButton>();


            //new career agent window
            //
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[0].children._childrenObjects[0]
            //DefaultWindowControls
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //WindowControls
            //
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ContainerAutoSize
            //
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[1]
            //resizer
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2]
            //content
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[0]
            //__loadingParent
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //AnimSprite
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //DefaultWindowHeader
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //main
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects //0,1
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //sprite
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //extra
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects
            //"blank"
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[3]
            //underlay
            //carbonui.uicore.uicore.registry.windows[9].content
            //main
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects //0,1,2
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[0]
            //container_npc.character //left part where the graphic showing the agent portrait is, 1 and 2 are also part of this...
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[1]
            //loadingwheel
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2]
            //container_interaction
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[0]
            //loading_wheel
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1]
            //content_cont
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects //0,1,2
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //container_buttons
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0]
            //btns
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects //0,1,2,3,4
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //AcceptMission_Button
            //
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[1]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //DeferMission_Button
            //
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[2]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //TrackJobButton
            //
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[3]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[3]
            //OverFlowButton

            //
            //carbonui.uicore.uicore.registry.windows[9].content
            //main
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects //0,1,2
            //
            //
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1]
            //line
            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[2]
            //scroll
            //DefaultWindowControls
            var content = pyWindow.Attribute("content");
            if (!content.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content not found");
                return;
            }

            if (!content.Attribute("name").ToUnicodeString().Contains("main"))
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.name [" + content.Attribute("name").ToUnicodeString() + "] != main");
                return;
            }

            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2]
            //container_interaction

            var container_interaction = content
                    .Attribute("children")
                    .Attribute("_childrenObjects")
                    .GetItemAt(2);

            if (!container_interaction.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2] not found");
                return;
            }

            if (!container_interaction.Attribute("name").ToUnicodeString().Contains("container_interaction"))
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].name [" + container_interaction.Attribute("name").ToUnicodeString() + "] != container_interaction");
                return;
            }

            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1]
            //content_cont

            var content_cont = container_interaction
                .Attribute("children")
                .Attribute("_childrenObjects")
                .GetItemAt(1);

            if (!content_cont.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].children._childrenObjects[1] not found");
                return;
            }

            if (!content_cont.Attribute("name").IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].children._childrenObjects[1].name not found");
                return;
            }

            if (!content_cont.Attribute("name").ToUnicodeString().Contains("content_cont"))
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].children._childrenObjects[1].name [" + content_cont.Attribute("name").ToUnicodeString() + "] != content_cont");
                return;
            }

            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //container_buttons

            var container_buttons = content_cont
                .Attribute("children")
                .Attribute("_childrenObjects")
                .GetItemAt(0);

            if (!container_buttons.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0] not found");
                return;
            }

            if (!container_buttons.Attribute("name").IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].name not found");
                return;
            }

            if (!container_buttons.Attribute("name").ToUnicodeString().Contains("container_buttons"))
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].name [" + container_buttons.Attribute("name").ToUnicodeString() + "] != container_buttons");
                return;
            }

            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0]
            //btns

            var btns = container_buttons
                .Attribute("children")
                .Attribute("_childrenObjects")
                .Attribute("_childrenObjects")
                .GetItemAt(0);

            if (!btns.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: btns not found");
                return;
            }

            if (!btns.Attribute("name").IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: btns.name not found");
            }

            if (!btns.Attribute("name").ToUnicodeString().Contains("btns"))
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: btns.name != main");
            }

            //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects
            //list of buttons as buttonwrapper(s) 0,1,2,3

            var buttons = btns
                .Attribute("children")
                .Attribute("_childrenObjects")
                .ToList();

            if (!buttons.Any())
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: content.children._childrenObjects[2].children._childrenObjects[1] not found");
                return;
            }

            foreach (var pyBtn in buttons)
            {
                if (!pyBtn.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow:pyBtn !IsValid");
                }

                if (pyBtn.Attribute("name").ToUnicodeString().ToLower().Contains("OverflowButton".ToLower()))
                {
                    continue;
                }

                //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0]
                //ButtonWrapper

                //pyBtn name is ButtonWrapper
                if (!pyBtn.Attribute("name").ToUnicodeString().Contains("ButtonWrapper"))
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow: WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name [" + pyBtn.Attribute("name").ToUnicodeString() + "] != ButtonWrapper");
                }

                //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                //This is the ACTUAL button

                PyObject btn = null;
                //carbonui.uicore.uicore.registry.windows[9].content.children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                if (pyBtn.Attribute("children").IsValid)
                {
                    if (pyBtn.Attribute("children").Attribute("_childrenObjects").IsValid)
                    {
                        if (pyBtn.Attribute("children").Attribute("_childrenObjects").GetItemAt(0).IsValid)
                        {
                            //normal buttons
                            btn = pyBtn.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                        }
                        else if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow:pyBtn.children._childrenObjects.GetItemAt(0) !IsValid");

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
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow:pyBtn.children._childrenObjects !IsValid");
                    }
                }
                else
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("DirectCareerAgentWindow:pyBtn.children !IsValid");
                }

                var button = new DirectAgentButton(directEve, btn);
                button.AgentId = AgentId;
                button.Text = (string)btn.Attribute("text");
                button.Type = GetButtonType(button.Text);
                button.ButtonName = (string)btn.Attribute("name");
                Buttons.Add(button);
            }

            //Briefing = (string)pyWindow.Attribute("briefingBrowser").Attribute("sr").Attribute("currentTXT");
            //Objective = (string)pyWindow.Attribute("objectiveBrowser").Attribute("sr").Attribute("currentTXT");

            //IsReady &= CareerAgentWindowState != CareerAgentWindowState.LOADING;
            IsReady = true; //hard coded!

            //if (CareerAgentWindowState == CareerAgentWindowState.MISSION_DETAIL_WINDOW)
            //{
            //    if (ObjectiveEmpty && BriefingEmpty)
            //        IsReady = false;
            //
            //    IsReady = true;
            //}

            if (DebugConfig.DebugAgentInteraction) Logging.Log.WriteLine("DirectCareerAgentWindow: IsReady [" + IsReady + "]"); // BriefingHtml [" + Briefing + "] ObjectiveHtml [" + Objective + "]");
        }

        #endregion Constructors

        #region Properties

        public DirectAgent Agent => DirectAgent.GetAgentById(DirectEve, AgentId);
        public long AgentId { get; internal set; }
        //public string Briefing { get; internal set; }
        public List<DirectAgentButton> Buttons { get; internal set; }
        public bool IsReady { get; internal set; }
        //public string Objective { get; internal set; }
        //public bool ObjectiveEmpty => Objective?.Equals("<html><body></body></html>") ?? true;
        //public bool BriefingEmpty => Briefing?.Equals("<html><body></body></html>") ?? true;
        //public bool HtmlEmpty => Html?.Equals("<html><body></body></html>") ?? true;

        /**
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
        **/

        /**
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
        **/

        /**
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
        **/

        /**
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
        **/

        public CareerAgentWindowState CareerAgentWindowState
        {
            get
            {
                if (!Buttons.Any())
                {
                    if (DebugConfig.DebugAgentInteraction)
                        Logging.Log.WriteLine("WindowState: LOADING: No Buttons Found");

                    return CareerAgentWindowState.LOADING;
                }

                if (Buttons.Any(b => b.Type == AgentButtonType.REQUEST_MISSION))
                {
                    if (DebugConfig.DebugAgentInteraction)
                        Logging.Log.WriteLine("WindowState: MISSION_REQUEST_WINDOW: REQUEST_MISSION Button Found");

                    return CareerAgentWindowState.MISSION_REQUEST_WINDOW;
                }

                if (Buttons.Any(b => b.Type == AgentButtonType.VIEW_MISSION))
                {
                    if (DebugConfig.DebugAgentInteraction)
                        Logging.Log.WriteLine("WindowState: MISSION_REQUEST_WINDOW: VIEW_MISSION Button Found");

                    return CareerAgentWindowState.MISSION_REQUEST_WINDOW;
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

                return CareerAgentWindowState.MISSION_DETAIL_WINDOW;
            }
        }

        /**
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
        **/

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