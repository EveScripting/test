extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace EVESharpCore.Framework
{
    public enum SystemMenuButtonType
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

    public class DirectSystemMenu : DirectWindow //Note, this is not considered a window?! might need to remove DirectWindow inheritance
    {
        public List<DirectIndustryWindowButton> Buttons { get; set; } = new List<DirectIndustryWindowButton>();
        public List<DirectIndustryWindowSingleLineEditInteger> SingleLineEditIntegerControls { get; set; } = new List<DirectIndustryWindowSingleLineEditInteger>();
        public int JobStatus { get; internal set; } //0 is not started, 1 is started, 101 is Succeeded (job is done)
        public int materialEfficiency { get; internal set; }
        public int timeEfficiency { get; internal set; }
        public int maxProductionLimit { get; internal set; }
        public long solarSystemID { get; internal set; }
        public int? BlueprintTypeID { get; internal set; }
        public long? BlueprintItemID { get; internal set; }
        public bool completed { get; internal set; }
        public int distance { get; internal set; }
        public int OutputTypeID { get; internal set; }
        //public int OutputQuantity { get; internal set; }
        public DirectItem OutputItem { get; internal set; }
        //public double JobCost { get; internal set; }
        public Dictionary<DirectItem, int> InputMaterials { get; internal set; }
        public int BlueprintRunsRemaining { get; internal set; }
        public int max_slots { get; internal set; }
        public int max_distance { get; internal set; }
        public int? isImpounded { get; internal set; }
        public long? jobID { get; internal set; }
        public int maxRuns { get; internal set; }
        public int OwnerID { get; internal set; }
        public int runs { get; internal set; } //runs in the job
        public float probability { get; internal set; }
        public int slots { get; internal set; }
        public int used_slots { get; internal set; }
        public double total_cost { get; internal set; }
        public Dictionary<DirectItem, long> OptionalMaterials { get; internal set; }
        public Dictionary<DirectItem, long> AvailableMaterials { get; internal set; }
        public Dictionary<DirectItem, long> Materials { get; internal set; }
        public int Roles { get; internal set; }


        internal DirectSystemMenu(DirectEve directEve, PyObject pySystemMenu)
            : base(directEve, pySystemMenu)
        {
            /**
            SystemMenuButtonType GetButtonType(string s)
            {
                try
                {
                    if (string.IsNullOrEmpty(s))
                        return IndustryWindowButtonType.UNKNOWN;

                    if (s.ToLower().Contains("Deliver all jobs".ToLower()))
                        return IndustryWindowButtonType.JOBS_DELIVER_ALL;

                    if (s.ToLower().Contains("Deliver selected".ToLower()))
                        return IndustryWindowButtonType.JOBS_DELIVER_SELECTED;

                    if (s.ToLower().Contains("Start".ToLower()))
                        return IndustryWindowButtonType.START;

                    if (s.ToLower().Contains("Deliver".ToLower()))
                        return IndustryWindowButtonType.DELIVER;

                    if (s.ToLower().Contains("Stop".ToLower()))
                        return IndustryWindowButtonType.STOP;

                    if (s.ToLower().Contains("upButton".ToLower()))
                        return IndustryWindowButtonType.RUNSTOMAKE_UP;

                    if (s.ToLower().Contains("downButton".ToLower()))
                        return IndustryWindowButtonType.RUNSTOMAKE_DOWN;


                    return IndustryWindowButtonType.UNKNOWN;
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine("Exception [" + ex + "]");
                    return IndustryWindowButtonType.UNKNOWN;
                }

            }
            **/

             //self.uiLayerList = [('l_hint', None, None),
             //('l_dragging', None, None),
             //('l_menu', None, None),
             //('l_mloading', None, None),
             // ('l_modal', None, [('l_loadingFill', None, None), ('l_systemmenu', SystemMenu, None)]),
             //''('l_utilmenu', None, None),
             //('l_alwaysvisible', None, None),
             //('l_abovemain', None, None),
             //('l_loading', None, None),
             //('l_videoOverlay', None, None),
             //('l_infoBubble', None, None),
             //('l_main', None, None),
             //('l_viewstate', None, None)]


            Buttons = new List<DirectIndustryWindowButton>();

            //carbonui.uicore.uicore.desktop.children._childrenObjects[5]
            //I_Modal
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[0]
            //I_loadingFill
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1]
            //I_systemmenu
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0]
            //layerCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //content
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //topCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //treeCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._cZildrenObjects 0,1,2
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Scrollbar
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //Scrollbar
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //clipCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //mainCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7,8
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //TreeViewEntry - 0
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //line - this means this TreeViewEntry is selected! this is the GUI feedback!
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //TextCustom - text: Display & Graphics- this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //TreeViewEntry - 1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //topCont_TreeViewEntry - onclick() - this tab will be selected - test me!
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: Camera - this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //TreeViewEntry - 2
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: Audio - this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //TreeViewEntry - 3
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: User Interface - this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4]
            //TreeViewEntry - 4
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: Gameplay - this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5]
            //TreeViewEntry - 5
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: Shortcuts- this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6]
            //TreeViewEntry - 6
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: Reset Settings - this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7]
            //TreeViewEntry - 7
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: Language - this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8]
            //TreeViewEntry - 8
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects[0]
            //topCont_TreeViewEntry
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom - text: About EVE - this can go from 1 to 2 when the tab is selected as "line" becomes 0, icon gets pushed to 1 and this becomes 2!

            //
            //
            //
            //
            //
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //panelCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //bottomCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //centerCont
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //TextCustom
            //carbonui.uicore.uicore.desktop.children._childrenObjects[5].uiwindow.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //TextCustom


        }
    }
}