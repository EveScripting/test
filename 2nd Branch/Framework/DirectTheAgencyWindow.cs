extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Py;
using SC::SharedComponents.EVE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

namespace EVESharpCore.Framework
{
    public enum TheAgencyWindowButtonType
    {
        JOBS_DELIVER_ALL,
        JOBS_DELIVER_SELECTED,
        START,//start job
        STOP,//stop individual job
        DELIVER,//deliver individual job that is done
        UNKNOWN,
        RUNSTOMAKE_UP,
        RUNSTOMAKE_DOWN,
        None
    }

    public class DirectTheAgencyWindow : DirectWindow
    {
        public List<DirectTheAgencyWindowButton> SystemWithSignatureResultButtons { get; set; } = new List<DirectTheAgencyWindowButton>();
        //public List<DirectIndustryWindowSingleLineEditInteger> SingleLineEditIntegerControls { get; set; } = new List<DirectIndustryWindowSingleLineEditInteger>();


        //blueprint ---- activities
        //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[1]
        //blueprint ---- activities --- manufacturing
        //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[8]
        //blueprint ---- activities --- invention
        //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.all_activities
        //blueprint ---- all_activities ----  1,3,4,5,8 (all valid activities, not limited by your current facilities or skills?)
        //SetJobRuns(self, value)

        private TheAgencyWindowButtonType GetButtonType(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                    return TheAgencyWindowButtonType.UNKNOWN;

                if (s.ToLower().Contains("Deliver all jobs".ToLower()))
                    return TheAgencyWindowButtonType.JOBS_DELIVER_ALL;

                if (s.ToLower().Contains("Deliver selected".ToLower()))
                    return TheAgencyWindowButtonType.JOBS_DELIVER_SELECTED;

                if (s.ToLower().Contains("Start".ToLower()))
                    return TheAgencyWindowButtonType.START;

                if (s.ToLower().Contains("Deliver".ToLower()))
                    return TheAgencyWindowButtonType.DELIVER;

                if (s.ToLower().Contains("Stop".ToLower()))
                    return TheAgencyWindowButtonType.STOP;

                if (s.ToLower().Contains("upButton".ToLower()))
                    return TheAgencyWindowButtonType.RUNSTOMAKE_UP;

                if (s.ToLower().Contains("downButton".ToLower()))
                    return TheAgencyWindowButtonType.RUNSTOMAKE_DOWN;


                return TheAgencyWindowButtonType.UNKNOWN;
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
                return TheAgencyWindowButtonType.UNKNOWN;
            }
        }

        internal DirectTheAgencyWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            //
            // if we dont need the agency window for the controller we are using, return
            //
            if (ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.ExplorationNoWeaponsController) &&
                ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController))
                return;

            SystemWithSignatureResultButtons = new List<DirectTheAgencyWindowButton>();
            if (DirectEve.Interval(8000)) Log.WriteLine("TheAgencyWindow");
            if (!DirectEve.Session.IsInSpace && !DirectEve.Session.IsInDockableLocation)
                return;

            if (Time.Instance.LastJumpAction.AddSeconds(10) > DateTime.UtcNow)
                return;

            //carbonui.uicore.uicore.registry.windows[7]
            //AgencyWndNew
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[0]
            //windows_controls_cont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[1]
            //Resizer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2]
            //content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[0]
            //__loadingParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //TabNavigationWindowHeader
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //DividerLine
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //_main_cont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //ContainerAutoSize - more here?
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //mainTabGroup
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //line
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //tabsCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //1
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //labelClipper
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ActivityIndicator
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //blinkDrop
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //11
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].label - Agents and Missions
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //labelClipper
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //ActivityIndicator
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //blinkDrop
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //12
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].label - Encounters
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //labelClipper
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //ActivityIndicator
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2]
            //blinkDrop
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3]
            //13
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].label - Exploration
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4]
            //14
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].label - Resource Harvesting
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5]
            //67
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].label - Fleet Up!
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6]
            //69
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].label - Help
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //TrailingCont
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //extra
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //BookmarksBar - more here!
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
            //ContentGroupBrowser
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects 0,1,2,3,4
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //loadingWheel
            //display (bool)
            //
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1]
            //headerContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //navigationButtonCont - ??? The Agency, Agents and Missions, Encounters, Exploration, resource Harvesting, Fleet up!, Help Section ???
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ContainerAutosize
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //goBackBtn
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //ContainerAutosize
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //goForwardBtn
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //breadCrumbLabelContainer - more here!
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[2]
            //ruleContainer
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[3]
            //ruleContainer
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4]
            //contentPageCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
            //HomeContentGroupPage
            //OR
            //BaseContentGroupPage if on other tabs
            //uniqueUiName = unique_ui_agency_page_Missions
            //uniqueUiName = unique_ui_agency_page_Encounters
            //uniqueUiName = unique_ui_agency_page_Exploration
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //clippingParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //rightFadeCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //leftFadeCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //LayoutGrid
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1,2,3,4,5,6
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //Row0_Col0
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //CombatAnomaliesNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //Row0_Col1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1

            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //CosmicSignaturesNavigationCard
            //onclick
            //
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2]
            //Row0_Col2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //EscalationsNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3]
            //Row0_Col3
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ProjectDiscoveryNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4]
            //Row0_Col4
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //TriglavianSpaceNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[5]
            //Row0_Col5
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[5].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //EncounterSurveillanceSystemNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[6]
            //Row0_Col6
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[6].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ZarzakhNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //
            //
            //
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1]
            //fill
            //
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
            //uniqueUiName = unique_ui_agency_page_Signatures
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //leftCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //informationContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Header
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Container
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Button - Join Chat
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //DescriptionIconLabel
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //DescriptionIconLabel
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //BaseFiltersCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Header
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //buttonCont - more here!
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //filtersCont - more here!
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1]
            //scrollSection - showing xx results
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Header
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //scrollContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Scrollbar
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //Scrollbar
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //clipCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4, etc each one is a result: more results more numbers...
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //SignatureSystemContentCard
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //cornerTriSmall
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //iconCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //mainCont
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //cardTitleLabel
            //text - Maurasi <color='0xFF3A9AEB'>0.9</color>
            //onclick
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //cardTextLabel
            //text - 1 Jump
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //bottomCont - X Signatures in system
            //
            //



            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //loadingWheel
            //display (bool)
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2]
            //SignatureInfoContainer
            //
            //
            //uniqueUiName = unique_ui_agency_page_ResourceHarvesting
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //clippingParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //LayoutGrid
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Row0_Col0
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //AsteroidBeltsNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //Row0_Col1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //OreAnomaliesNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //Row0_Col2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //IceBeltsNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //Row0_Col3
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //PlanetaryIndustryNavigationCard
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContentGroupInfoContainer - little question mark icon
            //
            //
            //
            //
            //
            //uniqueUiName = unique_ui_agency_page_FleetUp
            //uniqueUiName = unique_ui_agency_page_Help
            //
            //

            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[3]
            //underlay

            //carbonui.uicore.uicore.registry.windows[8]
            //AgencyWndNew
            //


            //Exploration
            //Full Path
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].label
            //Path broken down
            //carbonui.uicore.uicore.registry.windows[8]
            //.children._childrenObjects[2] - Content2
            //.children._childrenObjects[1] - headerParent
            //.children._childrenObjects[0] - TabNavigationWindowHeader
            //.children._childrenObjects[1] - _main_cont
            //.children._childrenObjects[1] - mainTabGroup
            //.children._childrenObjects[1] - ContainerAutoSize210111
            //.children._childrenObjects[0] - tabsCont
            //.children._childrenObjects[3] - 13
            //.label - Exploration
            //

            //CosmicSignaturesNavigationCard
            //Full Path
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //path broken down
            //carbonui.uicore.uicore.registry.windows[8]
            //.children._childrenObjects[2] - Content2
            //.children._childrenObjects[2] - main
            //.children._childrenObjects[1] - ContentGroupBrowser
            //.children._childrenObjects[4] - contentPageCont
            //.children._childrenObjects[0] - BaseContentGroupPage  //uniqueUiName = unique_ui_agency_page_Exploration
            //.children._childrenObjects[0] - clippingParent
            //.children._childrenObjects[2] - LayoutGrid
            //.children._childrenObjects[1] - Row0_Col1
            //.children._childrenObjects[0] - Content221400200
            //.children._childrenObjects[0] - ContentGroupCardContainer
            //.children._childrenObjects[0] - CosmicSignaturesNavigationCard


            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
            //BaseContentGroupPage
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //clippingParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //LayoutGrid
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //Row0_Col1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ContentGroupCardContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1

            //cardTitleLabel - text - Maurasi <color='0xFF3A9AEB'>0.9</color>
            //Full Path
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //cardTitleLabel
            //text - Maurasi <color='0xFF3A9AEB'>0.9</color>
            //path broken down
            //carbonui.uicore.uicore.registry.windows[8]
            //.children._childrenObjects[2] - Content2
            //.children._childrenObjects[2] - main
            //.children._childrenObjects[1] - ContentGroupBrowser
            //.children._childrenObjects[4] - contentPageCont
            //.children._childrenObjects[0] - BaseContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
            //.children._childrenObjects[0] - leftCont
            //.children._childrenObjects[1] - BaseFiltersCont
            //.children._childrenObjects[0] - Header
            //.children._childrenObjects[2] - clipCont
            //.children._childrenObjects[0] - mainCont2214011020
            //.children._childrenObjects[0] - SignatureSystemContentCard
            //.children._childrenObjects[2] - mainCont221401102002
            //.children._childrenObjects[0] - cardTitleLabel

            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //leftCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //BaseFiltersCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Header
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //clipCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //SignatureSystemContentCard
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //cardTitleLabel

            try
            {
                //CosmicSignaturesNavigationCard
                //Full Path
                //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                //path broken down
                //carbonui.uicore.uicore.registry.windows[8]
                //.children._childrenObjects[2] - Content2
                //.children._childrenObjects[2] - main
                //.children._childrenObjects[1] - ContentGroupBrowser
                //.children._childrenObjects[4] - contentPageCont
                //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Exploration
                //.children._childrenObjects[0] - clippingParent
                //.children._childrenObjects[2] - LayoutGrid
                //.children._childrenObjects[1] - Row0_Col1
                //.children._childrenObjects[0] - Content221400200
                //.children._childrenObjects[0] - ContentGroupCardContainer
                //.children._childrenObjects[0] - CosmicSignaturesNavigationCard

                try
                {
                    //.children._childrenObjects[2] - Content2
                    var pyContent2 = pyWindow.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                    if (!pyContent2.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Content not found");
                        return;
                    }

                    if (pyContent2.Attribute("name").IsValid && pyContent2.Attribute("name").ToUnicodeString().ToLower() != "Content".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Content");
                        return;
                    }
                    else
                    {
                        //.children._childrenObjects[2] - main
                        var pymain = pyContent2.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                        if (!pymain.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: main not found");
                            return;
                        }

                        if (pymain.Attribute("name").IsValid && pymain.Attribute("name").ToUnicodeString().ToLower() != "main".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != main");
                            return;
                        }
                        else
                        {
                            //.children._childrenObjects[1] - ContentGroupBrowser
                            var pyContentGroupBrowser = pymain.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                            if (!pyContentGroupBrowser.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContentGroupBrowser not found");
                                return;
                            }

                            if (pyContentGroupBrowser.Attribute("name").IsValid && pyContentGroupBrowser.Attribute("name").ToUnicodeString().ToLower() != "ContentGroupBrowser".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != ContentGroupBrowser");
                                return;
                            }
                            else
                            {
                                //Full Path
                                //carbonui.uicore.uicore.registry.windows[14].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4]
                                //path broken down
                                //.children._childrenObjects[4] - contentPageCont
                                var pycontentPageCont = pyContentGroupBrowser.Attribute("children").Attribute("_childrenObjects").GetItemAt(4);
                                if (!pycontentPageCont.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: contentPageCont not found");
                                    return;
                                }

                                if (pycontentPageCont.Attribute("name").IsValid && pycontentPageCont.Attribute("name").ToUnicodeString().ToLower() != "contentPageCont".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != contentPageCont");
                                    return;
                                }
                                else
                                {
                                    //Full Path
                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
                                    //path broken down
                                    //.children._childrenObjects[0]

                                    //HomeContentGroupPage  //uniqueUiName = unique_ui_agency_page_Home
                                    var pyHomeContentGroupPage = pycontentPageCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                    if (!pyHomeContentGroupPage.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: HomeContentGroupPage not found");
                                        return;
                                    }

                                    if (pyHomeContentGroupPage.Attribute("name").IsValid && pyHomeContentGroupPage.Attribute("name").ToUnicodeString().ToLower() == "HomeContentGroupPage".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name == HomeContentGroupPage");
                                    }
                                    else
                                    {
                                        //Full Path
                                        //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
                                        //path broken down
                                        //.children._childrenObjects[0] - BaseContentGroupPage || ContentPage*  //uniqueUiName = unique_ui_agency_page_Exploration

                                        var pyContentGroupPage = pyHomeContentGroupPage;
                                        if (!pyContentGroupPage.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: BaseContentGroupPage not found");
                                            return;
                                        }

                                        //
                                        // this is a tab we shouldnt have selected by default: someone manually clicked it? Its for the seasonal events, in this case, Capsuleer Day 2024. if its selected just bail out.. fix me?
                                        //
                                        if (pyContentGroupPage.Attribute("name").IsValid && (pyContentGroupPage.Attribute("name").ToUnicodeString().ToLower() == "ContentPageSeasons".ToLower()))
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name [" + pyContentGroupPage.Attribute("name").ToUnicodeString() + "]");
                                            return;
                                        }

                                        if (pyContentGroupPage.Attribute("name").IsValid && (pyContentGroupPage.Attribute("name").ToUnicodeString().ToLower() != "BaseContentGroupPage".ToLower() && !pyContentGroupPage.Attribute("name").ToUnicodeString().ToLower().Contains("ContentPage".ToLower())))
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name [" + pyContentGroupPage.Attribute("name").ToUnicodeString() + "] didnt match the expected value(s)");
                                        }
                                        else
                                        {
                                            //uniqueUiName = unique_ui_agency_page_Missions
                                            //uniqueUiName = unique_ui_agency_page_Encounters
                                            //uniqueUiName = unique_ui_agency_page_Exploration
                                            //uniqueUiName = unique_ui_agency_page_Signatures
                                            //uniqueUiName = unique_ui_agency_page_Anomalies
                                            //uniqueUiName = unique_ui_agency_page_Ore
                                            //uniqueUiName = unique_ui_agency_page_ResourceHarvesting
                                            var pyuniqueUiName = pyContentGroupPage.Attribute("uniqueUiName");
                                            if (!pyuniqueUiName.IsValid)
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName not found");
                                                return;
                                            }
                                            else if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName [" + pyuniqueUiName.ToUnicodeString() + "]");

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Exploration".ToLower()) //Home / Exploration
                                            {
                                                try
                                                {
                                                    if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Exploration");
                                                    //.children._childrenObjects[0] - clippingParent
                                                    var pyclippingParent = pyContentGroupPage.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                    if (!pyContentGroupBrowser.IsValid)
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: clippingParent not found");
                                                        return;
                                                    }

                                                    if (pyclippingParent.Attribute("name").IsValid && pyclippingParent.Attribute("name").ToUnicodeString().ToLower() != "clippingParent".ToLower())
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != clippingParent");
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        //.children._childrenObjects[2] - LayoutGrid
                                                        var pyLayoutGrid = pyclippingParent.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                                                        if (!pyContentGroupBrowser.IsValid)
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: LayoutGrid not found");
                                                            return;
                                                        }

                                                        if (pyLayoutGrid.Attribute("name").IsValid && pyLayoutGrid.Attribute("name").ToUnicodeString().ToLower() != "LayoutGrid".ToLower())
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != LayoutGrid");
                                                            return;
                                                        }
                                                        else
                                                        {
                                                            //.children._childrenObjects[1] - Row0_Col1
                                                            var pyRow0_Col1 = pyLayoutGrid.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                                            if (!pyContentGroupBrowser.IsValid)
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Row0_Col1 not found");
                                                                return;
                                                            }

                                                            if (pyRow0_Col1.Attribute("name").IsValid && pyRow0_Col1.Attribute("name").ToUnicodeString().ToLower() != "Row0_Col1".ToLower())
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Row0_Col1");
                                                                return;
                                                            }
                                                            else
                                                            {
                                                                //.children._childrenObjects[0] - Content221400200
                                                                var pyContent221400200 = pyRow0_Col1.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                if (!pyContentGroupBrowser.IsValid)
                                                                {
                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Content not found");
                                                                    return;
                                                                }

                                                                if (pyContent221400200.Attribute("name").IsValid && pyContent221400200.Attribute("name").ToUnicodeString().ToLower() != "Content".ToLower())
                                                                {
                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Content");
                                                                    return;
                                                                }
                                                                else
                                                                {
                                                                    //.children._childrenObjects[0] - ContentGroupCardContainer
                                                                    var pyContentGroupCardContainer = pyContent221400200.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                    if (!pyContentGroupBrowser.IsValid)
                                                                    {
                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContentGroupCardContainer not found");
                                                                        return;
                                                                    }

                                                                    if (pyContentGroupCardContainer.Attribute("name").IsValid && pyContentGroupCardContainer.Attribute("name").ToUnicodeString().ToLower() != "ContentGroupCardContainer".ToLower())
                                                                    {
                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != ContentGroupCardContainer");
                                                                        return;
                                                                    }
                                                                    else
                                                                    {
                                                                        //.children._childrenObjects[0] - CosmicSignaturesNavigationCard
                                                                        var pyCosmicSignaturesNavigationCard = pyContentGroupCardContainer.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                        if (!pyCosmicSignaturesNavigationCard.IsValid)
                                                                        {
                                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: CosmicSignaturesNavigationCard not found");
                                                                            return;
                                                                        }

                                                                        if (pyCosmicSignaturesNavigationCard.Attribute("name").IsValid && pyCosmicSignaturesNavigationCard.Attribute("name").ToUnicodeString().ToLower() != "CosmicSignaturesNavigationCard".ToLower())
                                                                        {
                                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != CosmicSignaturesNavigationCard");
                                                                            return;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController) ||
                                                                                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.ExplorationNoWeaponsController))
                                                                            {
                                                                                if (DirectEve.Interval(5000, 7000, WindowId))
                                                                                {
                                                                                    DirectEve.ThreadedCall(pyCosmicSignaturesNavigationCard.Attribute("OnClick"));
                                                                                    Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Cosmic Signatures not yet selected: OnClick");
                                                                                    return;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.WriteLine("Exception [" + ex + "]");
                                                }
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Anomalies".ToLower()) //Home / Exploration / Anomalies
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Anomalies");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Signatures".ToLower()) //Home / Exploration / Signatures
                                            {
                                                try
                                                {
                                                    if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(90000, 90000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Signatures");
                                                    //Description: left column where the filters are located
                                                    //
                                                    //.children._childrenObjects[0] - BaseContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                    //.children._childrenObjects[0] - leftCont
                                                    //.children._childrenObjects[1] - BaseFiltersCont
                                                    //.children._childrenObjects[0] - Header
                                                    //.children._childrenObjects[2] - clipCont
                                                    //.children._childrenObjects[0] - mainCont2214011020
                                                    //.children._childrenObjects[0] - SignatureSystemContentCard
                                                    //.children._childrenObjects[2] - mainCont221401102002
                                                    //.children._childrenObjects[0] - cardTitleLabel

                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
                                                    //leftCont
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
                                                    //BaseFiltersCont
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
                                                    //Header
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
                                                    //clipCont
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
                                                    //mainCont
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
                                                    //SignatureSystemContentCard
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
                                                    //mainCont
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
                                                    //cardTitleLabel

                                                    //.children._childrenObjects[0] - leftCont
                                                    var pyleftCont = pyContentGroupPage.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                    if (!pyleftCont.IsValid)
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: leftCont not found");
                                                        return;
                                                    }

                                                    if (pyleftCont.Attribute("name").IsValid && pyleftCont.Attribute("name").ToUnicodeString().ToLower() != "leftCont".ToLower())
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != leftCont");
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        //Full Path
                                                        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
                                                        //Path broken down
                                                        //
                                                        //.children._childrenObjects[1] - BaseFiltersCont
                                                        var pyBaseFiltersCont = pyleftCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                                        if (!pyBaseFiltersCont.IsValid)
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: BaseFiltersCont not found");
                                                            return;
                                                        }

                                                        if (pyBaseFiltersCont.Attribute("name").IsValid && pyBaseFiltersCont.Attribute("name").ToUnicodeString().ToLower() != "BaseFiltersCont".ToLower())
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != BaseFiltersCont");
                                                            return;
                                                        }
                                                        else
                                                        {
                                                            //Description: left column where the filters are header info: "Filters"
                                                            //Full Path
                                                            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
                                                            //path broken down
                                                            //carbonui.uicore.uicore.registry.windows[9]
                                                            //.children._childrenObjects[2] - Content2
                                                            //.children._childrenObjects[2] - main
                                                            //.children._childrenObjects[1] - ContentGroupBrowser
                                                            //.children._childrenObjects[4] - contentPageCont
                                                            //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                            //.children._childrenObjects[1] - leftCont
                                                            //.children._childrenObjects[1]
                                                            //.children._childrenObjects[0]
                                                            //
                                                            //.children._childrenObjects[0] - Header
                                                            var pyHeader = pyBaseFiltersCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                            if (!pyHeader.IsValid)
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Header not found");
                                                                return;
                                                            }

                                                            if (pyHeader.Attribute("name").IsValid && pyHeader.Attribute("name").ToUnicodeString().ToLower() != "Header".ToLower())
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Header");
                                                                return;
                                                            }
                                                            else
                                                            {

                                                            }
                                                        }
                                                    }

                                                    var pyscrollSection = pyContentGroupPage.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                                    if (!pyleftCont.IsValid)
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: scrollSection not found");
                                                        return;
                                                    }

                                                    if (pyscrollSection.Attribute("name").IsValid && pyscrollSection.Attribute("name").ToUnicodeString().ToLower() != "scrollSection".ToLower())
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != scrollSection");
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        //Full Path
                                                        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
                                                        //mainCont

                                                        //carbonui.uicore.uicore.registry.windows[9]
                                                        //.children._childrenObjects[2] - Content2
                                                        //.children._childrenObjects[2] - main
                                                        //.children._childrenObjects[1] - ContentGroupBrowser
                                                        //.children._childrenObjects[4] - contentPageCont
                                                        //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                        //.children._childrenObjects[1] - scrollSection
                                                        //.children._childrenObjects[1] - mainCont

                                                        //.children._childrenObjects[0] - mainCont2214011020
                                                        var pymainCont2214011 = pyscrollSection.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                                        if (!pymainCont2214011.IsValid)
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: mainCont not found");
                                                            return;
                                                        }

                                                        if (pymainCont2214011.Attribute("name").IsValid && pymainCont2214011.Attribute("name").ToUnicodeString().ToLower() != "mainCont".ToLower())
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != mainCont");
                                                            return;
                                                        }
                                                        else
                                                        {
                                                            //Full Path
                                                            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
                                                            //Path Broken Down
                                                            //
                                                            //carbonui.uicore.uicore.registry.windows[9]
                                                            //.children._childrenObjects[2] - Content2
                                                            //.children._childrenObjects[2] - main
                                                            //.children._childrenObjects[1] - ContentGroupBrowser
                                                            //.children._childrenObjects[4] - contentPageCont
                                                            //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                            //.children._childrenObjects[1] - scrollSection
                                                            //.children._childrenObjects[1] - mainCont
                                                            //.children._childrenObjects[0] - scrollContainer
                                                            //
                                                            var pyscrollContainer22140110 = pymainCont2214011.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                            if (!pyscrollContainer22140110.IsValid)
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: scrollContainer not found");
                                                                return;
                                                            }

                                                            if (pyscrollContainer22140110.Attribute("name").IsValid && pyscrollContainer22140110.Attribute("name").ToUnicodeString().ToLower() != "scrollContainer".ToLower())
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != scrollContainer");
                                                                return;
                                                            }
                                                            else
                                                            {
                                                                //Full Path
                                                                //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
                                                                //Path Broken Down
                                                                //
                                                                //carbonui.uicore.uicore.registry.windows[9]
                                                                //.children._childrenObjects[2] - Content2
                                                                //.children._childrenObjects[2] - main
                                                                //.children._childrenObjects[1] - ContentGroupBrowser
                                                                //.children._childrenObjects[4] - contentPageCont
                                                                //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                                //.children._childrenObjects[1] - scrollSection
                                                                //.children._childrenObjects[1] - mainCont
                                                                //.children._childrenObjects[0] - scrollContainer
                                                                //.children._childrenObjects[2] - clipCont
                                                                var pyclipCont221401102 = pyscrollContainer22140110.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                                                                if (!pyclipCont221401102.IsValid)
                                                                {
                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: clipCont not found");
                                                                    return;
                                                                }

                                                                if (pyclipCont221401102.Attribute("name").IsValid && pyclipCont221401102.Attribute("name").ToUnicodeString().ToLower() != "clipCont".ToLower())
                                                                {
                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != clipCont");
                                                                    return;
                                                                }
                                                                else
                                                                {
                                                                    //Full Path
                                                                    //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
                                                                    //Path Broken Down
                                                                    //
                                                                    //carbonui.uicore.uicore.registry.windows[9]
                                                                    //.children._childrenObjects[2] - Content2
                                                                    //.children._childrenObjects[2] - main
                                                                    //.children._childrenObjects[1] - ContentGroupBrowser
                                                                    //.children._childrenObjects[4] - contentPageCont
                                                                    //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                                    //.children._childrenObjects[1] - scrollSection
                                                                    //.children._childrenObjects[1] - mainCont
                                                                    //.children._childrenObjects[0] - scrollContainer
                                                                    //.children._childrenObjects[2] - clipCont
                                                                    //.children._childrenObjects[0] - mainCont
                                                                    var pymainCont2214011020 = pyclipCont221401102.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                    if (!pymainCont2214011020.IsValid)
                                                                    {
                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: mainCont not found");
                                                                        return;
                                                                    }

                                                                    if (pymainCont2214011020.Attribute("name").IsValid && pymainCont2214011020.Attribute("name").ToUnicodeString().ToLower() != "mainCont".ToLower())
                                                                    {
                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != mainCont");
                                                                        return;
                                                                    }
                                                                    else
                                                                    {

                                                                        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects
                                                                        //
                                                                        //
                                                                        //Full Path
                                                                        //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects
                                                                        //path broken down
                                                                        //carbonui.uicore.uicore.registry.windows[7]
                                                                        //.children._childrenObjects[2] - Content2
                                                                        //.children._childrenObjects[2] - main
                                                                        //.children._childrenObjects[1] - ContentGroupBrowser
                                                                        //.children._childrenObjects[4] - contentPageCont
                                                                        //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                                        //.children._childrenObjects[1] - scrollSection
                                                                        //.children._childrenObjects[1] - mainCont
                                                                        //.children._childrenObjects[0] - scrollContainer
                                                                        //.children._childrenObjects[2] - clipCont
                                                                        //.children._childrenObjects[0] - mainCont
                                                                        //.children._childrenObjects
                                                                        //list of results - this will vary in length depending on the number of results returned
                                                                        //
                                                                        if (!pymainCont2214011020.Attribute("children").Attribute("_childrenObjects").IsValid)
                                                                        {
                                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: pymainCont2214011.Attribute(\"children\").Attribute(\"_childrenObjects\") not found");
                                                                            return;
                                                                        }

                                                                        List<PyObject> ListOfSystems = pymainCont2214011020.Attribute("children").Attribute("_childrenObjects").ToList();
                                                                        //if (DirectEve.Interval(10000, 10000, WindowId)) Log.WriteLine("ListOfSystems.Count [" + ListOfSystems.Count + "]");
                                                                        int intCount = 0;
                                                                        foreach (PyObject pySignatureSystemContentCard in ListOfSystems)
                                                                        {
                                                                            intCount++;
                                                                            //if (DirectEve.Interval(10000, 10000, WindowId)) Log.WriteLine("pySignatureSystemContentCard [" + intCount + "]");
                                                                            //Full Path
                                                                            //example: carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
                                                                            //.children._childrenObjects[0] - SignatureSystemContentCard
                                                                            //var pySignatureSystemContentCard = pymainCont2214011020.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                            if (!pySignatureSystemContentCard.IsValid)
                                                                            {
                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: SignatureSystemContentCard not found");
                                                                                continue;
                                                                            }

                                                                            if (pySignatureSystemContentCard.Attribute("name").IsValid && pySignatureSystemContentCard.Attribute("name").ToUnicodeString().ToLower() != "SignatureSystemContentCard".ToLower())
                                                                            {
                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name [" + pySignatureSystemContentCard.Attribute("name").ToUnicodeString() + "] != SignatureSystemContentCard");
                                                                                continue;
                                                                            }
                                                                            else
                                                                            {
                                                                                //full path
                                                                                //example: carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
                                                                                //path broken down
                                                                                //.children._childrenObjects[2] - mainCont221401102002
                                                                                //
                                                                                // display (bool)
                                                                                //
                                                                                var pymainCont221401102002 = pySignatureSystemContentCard.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                                                                                if (!pymainCont221401102002.IsValid)
                                                                                {
                                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: mainCont not found");
                                                                                    return;
                                                                                }

                                                                                if (pymainCont221401102002.Attribute("name").IsValid && pymainCont221401102002.Attribute("name").ToUnicodeString().ToLower() != "mainCont".ToLower())
                                                                                {
                                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != mainCont");
                                                                                    return;
                                                                                }
                                                                                else
                                                                                {
                                                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
                                                                                    //cardTitleLabel
                                                                                    //text - Maurasi <color='0xFF3A9AEB'>0.9</color>
                                                                                    //onclick
                                                                                    //
                                                                                    //.children._childrenObjects[0] - cardTitleLabel
                                                                                    var pycardTitleLabel = pymainCont221401102002.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                                    if (!pycardTitleLabel.IsValid)
                                                                                    {
                                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: cardTitleLabel not found");
                                                                                        return;
                                                                                    }

                                                                                    if (pycardTitleLabel.Attribute("name").IsValid && pycardTitleLabel.Attribute("name").ToUnicodeString().ToLower() != "cardTitleLabel".ToLower())
                                                                                    {
                                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != cardTitleLabel");
                                                                                        return;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        try
                                                                                        {
                                                                                            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController) ||
                                                                                                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.ExplorationNoWeaponsController))
                                                                                            {
                                                                                                //if (DirectEve.Interval(5000)) Log.WriteLine("if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController))");
                                                                                                //if (DirectEve.Interval(30000, 30000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: text == Start");
                                                                                                if (pycardTitleLabel.Attribute("display").ToBool())
                                                                                                {
                                                                                                    DirectTheAgencyWindowButton SystemWithSignatureResult = new DirectTheAgencyWindowButton(directEve, pySignatureSystemContentCard);
                                                                                                    SystemWithSignatureResult.Text = (string)pycardTitleLabel.Attribute("text");

                                                                                                    if (!string.IsNullOrEmpty(SystemWithSignatureResult.Text))
                                                                                                    {
                                                                                                        if (SystemWithSignatureResult.SolarSystem != null)
                                                                                                        {
                                                                                                            //if (DirectEve.Interval(10000, 10000, WindowId)) Log.WriteLine("SystemWithSignatureResult.Text [" + SystemWithSignatureResult.Text + "] SystemWithSignatureResult.SolarSystem [" + SystemWithSignatureResult.SolarSystem.Name ?? "null" + "]");
                                                                                                            SystemWithSignatureResultButtons.Add(SystemWithSignatureResult);
                                                                                                            continue;
                                                                                                        }

                                                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Log.WriteLine("! SystemWithSignatureResult.Text [" + SystemWithSignatureResult.Text + "] SystemWithSignatureResult.solarSystemNameText [" + SystemWithSignatureResult.solarSystemNameText + "]");
                                                                                                        continue;
                                                                                                    }
                                                                                                    else if (DirectEve.Interval(10000, 10000, WindowId)) Log.WriteLine("if (!string.IsNullOrEmpty(SystemWithSignatureResult.Text))");
                                                                                                }
                                                                                                else if (DirectEve.Interval(10000, 10000, WindowId)) Log.WriteLine("SystemWithSignatureResult: if (pycardTitleLabel.Attribute(\"display\").ToBool())");
                                                                                            }
                                                                                            else if (DirectEve.Interval(10000, 10000, WindowId)) Log.WriteLine("SystemWithSignatureResult: if (ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController))");
                                                                                        }
                                                                                        catch (Exception ex)
                                                                                        {
                                                                                            Log.WriteLine("Exception [" + ex + "]");
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

                                                    //Full Path
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2]
                                                    //Path broken down
                                                    //carbonui.uicore.uicore.registry.windows[8]
                                                    //.children._childrenObjects[2] - Content2
                                                    //.children._childrenObjects[2] - main
                                                    //.children._childrenObjects[1] - ContentGroupBrowser
                                                    //.children._childrenObjects[4] - contentPageCont
                                                    //.children._childrenObjects[0] - ContentGroupPage  //uniqueUiName = unique_ui_agency_page_Signatures
                                                    //.children._childrenObjects[2] - SignatureInfoContainer
                                                    //
                                                    //SignatureInfoContainer
                                                    var pySignatureInfoContainer = pyContentGroupPage.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                                    if (!pyleftCont.IsValid)
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: SignatureInfoContainer not found");
                                                        return;
                                                    }

                                                    if (pySignatureInfoContainer.Attribute("name").IsValid && pySignatureInfoContainer.Attribute("name").ToUnicodeString().ToLower() != "SignatureInfoContainer".ToLower())
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != SignatureInfoContainer");
                                                        return;
                                                    }
                                                    else
                                                    {

                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.WriteLine("Exception [" + ex + "]");
                                                }
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Escalations".ToLower()) //Home / Exploration / Escalations
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Escalations");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_ProjectDiscovery".ToLower()) //Home / Exploration / Project Discovery
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_ProjectDiscovery");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_TriglavianSpace".ToLower()) //Home / Exploration / Triglavian Space
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_TriglavianSpace");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_EncounterSurveillanceSystem".ToLower()) //Home / Exploration / Encounter Surveillance System
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_EncounterSurveillanceSystem");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Zarzakh".ToLower()) //Home / Exploration / Zarzakh
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Zarzakh");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Encounters".ToLower()) //Home / Encounters
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Encounters");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_HomefrontSites".ToLower()) //Home / Encounters / Homefront Operations
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_HomefrontSites");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Incursions".ToLower()) //Home / Encounters / Incursions
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Incursions");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_PirateInsurgencies".ToLower()) //Home / Encounters / Pirate Insurgencies
                                            {
                                                //ContentPagePirateIncursionsHome - todo
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_FactionalWarfare".ToLower()) //Home / Encounters / Factional Warfare
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_FactionalWarfare");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_PirateStrongholds".ToLower()) //Home / Encounters / Pirate Strongholds
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_PirateStrongholds");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_AbyssalDeadspace".ToLower()) //Home / Encounters / AbyssalDeadspace
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_AbyssalDeadspace");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_ResourceHarvesting".ToLower()) //Home / Resource Harvesting
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_ResourceHarvesting");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Ore".ToLower()) //Home / Resource Harvesting / Ore
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Ore");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_Ice".ToLower()) //Home / Resource Harvesting / Ice
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_Ice");
                                            }

                                            if (pyuniqueUiName.IsValid && pyuniqueUiName.ToUnicodeString().ToLower() == "unique_ui_agency_page_AsteroidBelts".ToLower()) //Home / Resource Harvesting / Asteroid Belts
                                            {
                                                if (DebugConfig.DebugHighSecCombatSignaturesBehavior && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: uniqueUiName == unique_ui_agency_page_AsteroidBelts");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                try
                {
                    //
                    // Navigation bat at the top of the Agency window: The Agency, Agents, Missions, Encounters, Exploration, etc
                    //
                    //.children._childrenObjects[2] - Content2
                    var pyContent2 = pyWindow.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                    if (!pyContent2.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: main not found");
                        return;
                    }

                    if (pyContent2.Attribute("name").IsValid && pyContent2.Attribute("name").ToUnicodeString().ToLower() != "Content".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Content");
                        return;
                    }
                    else
                    {
                        //.children._childrenObjects[1] - headerParent
                        var pyHeaderParent = pyContent2.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                        if (!pyHeaderParent.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: headerParent not found");
                            return;
                        }

                        if (pyHeaderParent.Attribute("name").IsValid && pyHeaderParent.Attribute("name").ToUnicodeString().ToLower() != "headerParent".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != headerParent");
                            return;
                        }
                        else
                        {
                            try
                            {
                                //.children._childrenObjects[0] - TabNavigationWindowHeader
                                var pyTabNavigationWindowHeader = pyHeaderParent.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                if (!pyHeaderParent.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: TabNavigationWindowHeader not found");
                                    return;
                                }

                                if (pyTabNavigationWindowHeader.Attribute("name").IsValid && pyTabNavigationWindowHeader.Attribute("name").ToUnicodeString().ToLower() != "TabNavigationWindowHeader".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != TabNavigationWindowHeader");
                                    return;
                                }
                                else
                                {
                                    //.children._childrenObjects[1] - _main_cont
                                    var pyMainCont = pyTabNavigationWindowHeader.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                    if (!pyMainCont.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: _main_cont not found");
                                        return;
                                    }

                                    if (pyMainCont.Attribute("name").IsValid && pyMainCont.Attribute("name").ToUnicodeString().ToLower() != "_main_cont".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != _main_cont");
                                        return;
                                    }
                                    else
                                    {
                                        //.children._childrenObjects[1] - mainTabGroup
                                        var pymainTabGroup = pyMainCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                        if (!pymainTabGroup.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: mainTabGroup not found");
                                            return;
                                        }

                                        if (pymainTabGroup.Attribute("name").IsValid && pymainTabGroup.Attribute("name").ToUnicodeString().ToLower() != "mainTabGroup".ToLower())
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != mainTabGroup");
                                            return;
                                        }
                                        else
                                        {
                                            //.children._childrenObjects[1] - ContainerAutoSize210111
                                            var pyContainerAutoSize210111 = pymainTabGroup.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                            if (!pyContainerAutoSize210111.IsValid)
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize not found");
                                                return;
                                            }

                                            if (pyContainerAutoSize210111.Attribute("name").IsValid && pyContainerAutoSize210111.Attribute("name").ToUnicodeString().ToLower() != "ContainerAutoSize".ToLower())
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != ContainerAutoSize");
                                                return;
                                            }
                                            else
                                            {
                                                //.children._childrenObjects[0] - tabsCont
                                                var pyTabsCont = pyContainerAutoSize210111.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                if (!pyTabsCont.IsValid)
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: tabsCont not found");
                                                    return;
                                                }

                                                if (pyTabsCont.Attribute("name").IsValid && pyTabsCont.Attribute("name").ToUnicodeString().ToLower() != "tabsCont".ToLower())
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != tabsCont");
                                                    return;
                                                }
                                                else
                                                {
                                                    //The Agency
                                                    //Full Path
                                                    ////carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
                                                    var py1TheAgency = pyTabsCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(3);
                                                    if (!py1TheAgency.IsValid)
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: 1 not found");
                                                        return;
                                                    }

                                                    if (py1TheAgency.Attribute("text").IsValid && py1TheAgency.Attribute("text").ToUnicodeString().ToLower() != "The Agency".ToLower())
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != The Agency");
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        if (py1TheAgency.Attribute("_selected").ToBool())
                                                        {
                                                            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController) ||
                                                                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.ExplorationNoWeaponsController))
                                                            {
                                                                //Do something here?
                                                            }
                                                        }
                                                    }

                                                    //Exploration
                                                    //Full Path
                                                    //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].label
                                                    //Path broken down
                                                    //.children._childrenObjects[3] - 13
                                                    var py13Exploration = pyTabsCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(3);
                                                    if (!py13Exploration.IsValid)
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: 13 not found");
                                                        return;
                                                    }

                                                    if (py13Exploration.Attribute("text").IsValid && py13Exploration.Attribute("text").ToUnicodeString().ToLower() != "Exploration".ToLower())
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Exploration");
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        if (!py13Exploration.Attribute("_selected").ToBool())
                                                        {
                                                            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController) ||
                                                                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.ExplorationNoWeaponsController))
                                                            {
                                                                if (DirectEve.Interval(5000, 7000, WindowId))
                                                                {
                                                                    DirectEve.ThreadedCall(py13Exploration.Attribute("OnClick"));
                                                                    Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Exploration tab not selected: OnClick");
                                                                    return;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (DirectEve.Interval(5000, 7000, WindowId))
                                                            {
                                                                Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Exploration tab is selected");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }
    }
}