extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace EVESharpCore.Framework
{
    public enum IndustryWindowButtonType
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

    public class DirectIndustryWindow : DirectWindow
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


        //blueprint ---- activities
        //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[1]
        //blueprint ---- activities --- manufacturing
        //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[8]
        //blueprint ---- activities --- invention
        //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.all_activities
        //blueprint ---- all_activities ----  1,3,4,5,8 (all valid activities, not limited by your current facilities or skills?)
        //SetJobRuns(self, value)


        internal DirectIndustryWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            if (DebugConfig.ProcessIndustryJobs)
                return;

            IndustryWindowButtonType GetButtonType(string s)
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

            Buttons = new List<DirectIndustryWindowButton>();

            //carbonui.uicore.uicore.registry.windows[7]
            //industryWnd
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[0]
            //window_controls_cont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[1]
            //Resizer
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2]
            //content
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[0]
            //__loadingparent
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //topCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //BaseView
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //topCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0, 1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //UtilMenu
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //bgCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //Container
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //BlueprintCenter
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData
            //.jobData
            //
            // will be null if there is no blueprint loaded?!
            //
            //materials ---- 0,1,2,3,4,5,6,7,8,9,10 (one for each material, varies based on the blueprint!)
            //total_cost - Cost to submit the job, with taxes and such
            //runs - runs (int)
            //probability - probability (float)
            //optional_materials - list of optional materials (like what?!) (list)
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.output[0]
            //output
            //output ---- quantity
            //output ---- typeID
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.activity
            //activity
            //activity -- time
            //activity -- skills
            //activity -- products
            //activity -- materials
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.available
            //available - this is a list of all materials available (missing items would need to be calculated!)
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.slots
            //slots - does this show the slots using this blueprint? not sure
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.status
            //status - status of the job (int) - 0 is not started, 1 is started, what are the other values?
            //used_slots - used slots (int) - is this total or just this blueprint?
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.roles
            //roles - (int) - what are the possible values?
            //
            //blueprint
            //blueprint ---- blueprintTypeID
            //blueprint ---- original (bool)
            //blueprint ---- productTypeID
            //blueprint ---- runsRemaining
            //blueprint ---- singleton (bool)
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities
            //blueprint ---- activities
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[1]
            //blueprint ---- activities --- manufacturing
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[8]
            //blueprint ---- activities --- invention
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.all_activities
            //blueprint ---- all_activities ----  1,3,4,5,8 (all valid activities, not limited by your current facilities or skills?)
            //SetJobRuns(self, value)

            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.materials 0,1,2,3
            //materials
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.materials[0]
            //typeID
            //quantity
            //missing (int)
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.materials[1]
            //typeID
            //quantity
            //missing (int)
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.materials[2]
            //typeID
            //quantity
            //missing (int)
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.materials[3]
            //typeID
            //quantity
            //missing (int)
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.materials[4] - ???
            //typeID
            //quantity
            //missing (int)
            //

            //max_slots
            //max_distance
            //max_runs
            //materialEfficiency
            //output
            //outputLocation
            //product
            //products
            //skills
            //slots
            //startDate
            //status
            //timeEfficiency
            //used_slots
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7,8,9,10,11
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //BlueprintItemIcon
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //ErrorFrame
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //bgFrame
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3]
            //blueprintGbFill
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4]
            //dashesCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5]
            //bgCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0]
            //ContainerAutoSize

            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0]
            //runsCaption - Job runs - OnDownKeyPressed, OnUpKeyPressed
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[1]
            //SingleLineEditInteger
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2]
            //runsPerCopyCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //runsPerCopyCaption - Runs per copy
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2].children._childrenObjects[1]
            //SingleLineEditInteger
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects 0,1,2
            //numericControlsCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1]
            //__caption
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[2]
            //_textClipper
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7]
            //SkillIcon
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[8]
            //ContainerME - ME stands for material efficiency
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[9]
            //ContainerTE - TE stands for time efficiency
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[10]
            //bpCopyCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[11]
            //gauge
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //MaterialGroups (left side of the window)
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1,2,3,4,5,6,7,8,9,10
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //noItemsLabelCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //groupBackground
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2]
            //MaterialGroup - Is this the upper left?
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0]
            //materialsCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ItemIcon - typeID is a property here. ex: 48112
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //Label - text is a property here: ex 400
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //bgGlow
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //bgFrame
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4]
            //valueBg
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[5]
            //Gauge
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[6]
            //bgFill
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[7]
            //patternNotReady
            //
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2,3,4,5,6,7
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //ItemIcon - typeID is a property here. ex: 48112
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //Label - text is a property here: ex 400
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //bgGlow
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[3]
            //bgFrame
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4]
            //valueBg
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[5]
            //Gauge
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[6]
            //bgFill
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[7]
            //patternNotReady
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //IndustryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //DashedCircle
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[1]
            //icon
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3]
            //MaterialGroup - Is this the center left?
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0]
            //materialsCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7,8,9,10
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[2]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[3]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[4]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[5]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[6]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[7]
            //IndustryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[8]
            //IndustryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[9]
            //IndustryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[10]
            //DashedCircle
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[1]
            //icon
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4]
            //MaterialGroup - Is this the bottom left?
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
            //materialsCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1]
            //Material
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2]
            //IndustryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[3]
            //DashedCircle
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[1]
            //icon
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[5]
            //bigConnectorCircle
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[6]
            //industryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[7]
            //industryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[8]
            //industryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[9]
            //industryLineTrace
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[10]
            //fromLine
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //OutputCont (right side of the window) - more here!
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[3]
            //bgTransform
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //JobsStrip
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //JobsSummary
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Row0_Col0
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Label - text == Manufacturing Jobs
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //Row0_Col1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Label - text == 1 / <color=0xffff9900>1</color>
            //
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //ErrorFrame
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //Row1_Col0 - Control Range
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3]
            //Row1_Col1 - Current System (or whatver your range is!)
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //AllJobsSummary
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1,2,3,4,5
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //Row0_Col0
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Content
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Label -text == Manufacturing Jobs
            //
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //Row0_Col1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].name
            //Content
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Label
            //1 / <color=0xFFFF9900>1</color>
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2]
            //Row1_Col0
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3]
            //Row1_Col1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[4]
            //Row2_Col0
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[5]
            //Row2_Col1
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //ActivityTabs
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0]
            //myToggleBtnGroup
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //Button_1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //Button_4 ME Button uniqueUiName == unique_UI_materialEfficiencyBtn
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //Button_3 TE Button uniqueUiName == unique_UI_timeEfficiencyBtn
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //Button_5 Copy Button uniqueUiName == unique_UI_industrycopyBtn
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4]
            //Button_8 Invention Button uniqueUiName == unique_UI_InventionBtn
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5]
            //Button_9
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[3]
            //SubmitButton, text == Start, enabled (bool)
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
            //bottomCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //browserCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //tabgroup
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //line
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //DividerLine
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //tabsCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //blueprints
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //facilities
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2]
            //jobs - has on onclick method!
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0]
            //labelClipper
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[1]
            //ActivityIndicator
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[2]
            //blinkDrop
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3]
            //research
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //TrailingContainer - what is in this container? I cant see anything....
            //
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //BrowserBlueprints
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //BrowserFacilities
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3]
            //BrowserJobs - more here!
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //topPanel
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1]
            //ButtonGroup
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1].children._childrenObjects._childrenObjects[0]
            //btns
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[0]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Deliver selected_Btn, text == Deliver selected, enabled (bool), display (bool)
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[1]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Deliver all jobs_Btn, text == Deliver all jobs, enabled (bool), display (bool)
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[2]
            //OverflowButton
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[2]
            //scroll
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4]
            //BrowserResearch
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1]
            //expandBottomBtn - UI element to collapse the bottom part of the window
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2]
            //historyArrowCont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[3]
            //underlay

            //
            // Misc stuff at top level window!?!
            //
            //bottomCont
            //browserBlueprints
            //browserCont
            //browserFacilities
            //browserJobs
            //browserresearch
            //content
            //expandBottomBtn
            //ExpandBrowser
            //ExpandView
            //expandViewBtn
            //goBackBtn
            //goForwrdBtn
            //jobData
            //jobsStrip
            //OpenOrShowBlueprint(cls, blueprintID, blueprintTypeID,bpData)
            //sr
            //tabs
            //

            //Deliver all jobs_Btn, text == Deliver all jobs, enabled (bool), display (bool)
            //carbonui.uicore.uicore.registry.windows[7] - industryWnd
            //.children._childrenObjects[2] - Content
            //.children._childrenObjects[2] - main
            //.children._childrenObjects[1] - bottomCont
            //.children._childrenObjects[0] - browserCont
            //.children._childrenObjects[3] - browserJobs
            //.children._childrenObjects[1] - ButtonGroup
            //.children._childrenObjects._childrenObjects[0] - btns
            //.children._childrenObjects[1] - ButtonWrapper
            //.children._childrenObjects[0] - Deliver selected_Btn, text == Deliver selected, enabled (bool), display (bool)

            //
            //jobs (tab!)- has on onclick method!, _selected (bool)
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2]
            //carbonui.uicore.uicore.registry.windows[7]
            //.children._childrenObjects[2] - Content*
            //.children._childrenObjects[2] - main*
            //.children._childrenObjects[1] - bottomCont*
            //.children._childrenObjects[0] - browserCont*
            //.children._childrenObjects[0] - tabgroup
            //.children._childrenObjects[1] - ContainerAutoSize
            //.children._childrenObjects[1] - tabsCont
            //.children._childrenObjects[2] - Jobs

            //carbonui.uicore.uicore.registry.windows[7]
            //.children._childrenObjects[2] - Content*
            //.children._childrenObjects[2] - main*
            //.children._childrenObjects[0] - topCont
            //.children._childrenObjects[0] - BaseView
            //.children._childrenObjects[1] - Container
            //.children._childrenObjects[0] - BlueprintCenter
            //.jobData
            //
            // will be null if there is no blueprint loaded?!
            //
            //blueprint
            //blueprint ---- blueprintTypeID
            //blueprint ---- original (bool)
            //blueprint ---- productTypeID
            //blueprint ---- runsRemaining
            //blueprint ---- singleton (bool)
            //materials ---- 0,1,2,3,4,5,6,7,8,9,10 (one for each material, varies based on the blueprint!)


            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.errors
            //jobData ---- errors
            //tells us EXACTLY what is wrong with the job! (if anything)
            //needs to be fleshed out to see how useful this is! probably VERY useful!
            //
            //There are buttons in the UI to change from Manufacturing to Invention etc...
            //Can we do invention?
            //Can we do reaction(s)?

            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //JobsStrip
            //carbonui.uicore.uicore.registry.windows[7]
            //.children._childrenObjects[2] - Content*
            //.children._childrenObjects[2] - main*
            //.children._childrenObjects[0] - topCont
            //.children._childrenObjects[1] - JobsStrip


            //Start Button
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[3]
            //carbonui.uicore.uicore.registry.windows[7]
            //.children._childrenObjects[2] - Content*
            //.children._childrenObjects[2] - main*
            //.children._childrenObjects[0] - topCont
            //.children._childrenObjects[1] - JobsStrip
            //.children._childrenObjects[3] - SubmitButton, text == Start, enabled (bool)

            //if (DirectEve.Interval(2000, 5000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Creating DirectIndustryWindow Object");

            //JobsSummary - jobs in use / total jobs
            //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Label
            //1 / <color=0xFFFF9900>1</color>
            //
            //carbonui.uicore.uicore.registry.windows[9]
            //.children._childrenObjects[2] - Content*
            //.children._childrenObjects[2] - main*
            //.children._childrenObjects[0] - topCont
            //.children._childrenObjects[1] - JobsStrip
            //.children._childrenObjects[1] - JobsSummary
            //.children._childrenObjects[1] - Row0_Col1
            //.children._childrenObjects[0] - Content
            //.children._childrenObjects[0] - Label


            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //JobsSummary

            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0]
            //SingleLineInteger - Job runs - OnDownKeyPressed, OnUpKeyPressed
            //carbonui.uicore.uicore.registry.windows[7]
            //.children._childrenObjects[2] - Content*
            //.children._childrenObjects[2] - main*
            //.children._childrenObjects[0] - topCont*
            //.children._childrenObjects[0] - BaseView*
            //.children._childrenObjects[1] - Container
            //.children._childrenObjects[0] - BlueprintCenter
            //.children._childrenObjects[6] - ContainerAutoSize
            //.children._childrenObjects[1] - SingleLineInteger

            try
            {
                var pyMain = pyWindow.Attribute("children").Attribute("_childrenObjects").GetItemAt(2).Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                if (!pyMain.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: main not found");
                    return;
                }

                if (pyMain.Attribute("name").IsValid && pyMain.Attribute("name").ToUnicodeString().ToLower() != "main".ToLower())
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != main");
                    return;
                }
                else
                {
                    //this object
                    //bottomCont - .children._childrenObjects[1]
                    //path to this object: 2201
                    //.children._childrenObjects[2] - Content
                    //.children._childrenObjects[2] - main
                    //.children._childrenObjects[1] - bottomCont
                    var pyBottomCont221 = pyMain.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                    if (!pyBottomCont221.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: bottomCont not found");
                        return;
                    }

                    if (!pyBottomCont221.Attribute("name").IsValid && pyBottomCont221.Attribute("name").ToUnicodeString().ToLower() != "bottomCont".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != bottomCont");
                        return;
                    }
                    else
                    {
                        //this object
                        //topCont - .children._childrenObjects[0]
                        //path to this object: 220
                        //.children._childrenObjects[2] - Content
                        //.children._childrenObjects[2] - main
                        //.children._childrenObjects[0] - topCont
                        var pyTopCont220 = pyMain.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                        if (!pyTopCont220.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: topCont not found");
                            return;
                        }

                        if (!pyTopCont220.Attribute("name").IsValid && pyTopCont220.Attribute("name").ToUnicodeString().ToLower() != "topCont".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != topCont");
                            return;
                        }
                        else
                        {
                            //this object
                            //BaseView - .children._childrenObjects[0]
                            //path to this object: 2200
                            //.children._childrenObjects[2] - Content
                            //.children._childrenObjects[2] - main
                            //.children._childrenObjects[0] - topCont
                            //.children._childrenObjects[0] - BaseView
                            var pyBaseView2200 = pyTopCont220.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                            if (!pyBaseView2200.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: BaseView not found");
                                return;
                            }

                            if (!pyBaseView2200.Attribute("name").IsValid && pyBaseView2200.Attribute("name").ToUnicodeString().ToLower() != "BaseView".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != BaseView");
                                return;
                            }
                            else
                            {
                                //this object
                                //Container - .children._childrenObjects[1] //Note 22001 is the path to this Container because that name isnt unique enough without the numbers
                                //path to this object: 22001
                                //.children._childrenObjects[2] - Content
                                //.children._childrenObjects[2] - main
                                //.children._childrenObjects[0] - topCont
                                //.children._childrenObjects[0] - BaseView
                                //.children._childrenObjects[1] - Container
                                var pyContainer22001 = pyBaseView2200.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                if (!pyContainer22001.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Container not found");
                                    return;
                                }

                                if (!pyContainer22001.Attribute("name").IsValid && pyContainer22001.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != Container");
                                    return;
                                }
                                else
                                {
                                    //this object
                                    //BlueprintCenter - .children._childrenObjects[0]
                                    //path to this object 220010
                                    //.children._childrenObjects[2] - Content
                                    //.children._childrenObjects[2] - main
                                    //.children._childrenObjects[0] - topCont
                                    //.children._childrenObjects[0] - BaseView
                                    //.children._childrenObjects[1] - Container
                                    //.children._childrenObjects[0] - BlueprintCenter
                                    var pyBlueprintCenter220010 = pyContainer22001.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                    if (!pyBlueprintCenter220010.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: BlueprintCenter not found");
                                        return;
                                    }

                                    if (!pyBlueprintCenter220010.Attribute("name").IsValid && pyBlueprintCenter220010.Attribute("name").ToUnicodeString().ToLower() != "BlueprintCenter".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != BlueprintCenter");
                                        return;
                                    }
                                    else
                                    {
                                        var pyContainerAutoSize2200106 = pyBlueprintCenter220010.Attribute("children").Attribute("_childrenObjects").GetItemAt(6);
                                        if (!pyContainerAutoSize2200106.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize not found. Note: 2200106");
                                            return;
                                        }

                                        if (!pyContainerAutoSize2200106.Attribute("name").IsValid && pyContainerAutoSize2200106.Attribute("name").ToUnicodeString().ToLower() != "ContainerAutoSize".ToLower())
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ContainerAutoSize");
                                            return;
                                        }
                                        else
                                        {
                                            //if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(5000)) Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize found");
                                            var SingleLineEditInteger22001061 = pyContainerAutoSize2200106.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                            if (!SingleLineEditInteger22001061.IsValid)
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: SingleLineEditInteger not found");
                                                return;
                                            }

                                            if (!SingleLineEditInteger22001061.Attribute("name").IsValid && SingleLineEditInteger22001061.Attribute("name").ToUnicodeString().ToLower() != "SingleLineEditInteger".ToLower())
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != SingleLineEditInteger");
                                                return;
                                            }
                                            else
                                            {
                                                //if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(5000)) Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: SingleLineEditInteger22001061 found");
                                                //downButton - OnClick
                                                //
                                                //path to this object: 220010.downButton - OnClick
                                                //
                                                //carbonui.uicore.uicore.registry.windows[10]
                                                //.children._childrenObjects[2] - Content
                                                //.children._childrenObjects[2] - main
                                                //.children._childrenObjects[0] - topCont
                                                //.children._childrenObjects[0] - BaseView
                                                //.children._childrenObjects[1] - Container
                                                //.children._childrenObjects[0] - BlueprintCenter
                                                //.children._childrenObjects[6] - ContainerAutoSize
                                                //.children._childrenObjects[1] - SingleLineEditInteger.downButton
                                                //
                                                //downButton - OnClick
                                                //

                                                //DirectIndustryWindowSingleLineEditInteger
                                                try
                                                {
                                                    DirectIndustryWindowSingleLineEditInteger JobRunsToMake = new DirectIndustryWindowSingleLineEditInteger(directEve, SingleLineEditInteger22001061);
                                                    if (JobRunsToMake != null)
                                                    {
                                                        //Log.WriteLine("JobRunsToMake is not null");
                                                        JobRunsToMake.Name = "JobRunsToMake";
                                                        //JobRunsToMake.Text = SingleLineEditInteger22001061.Attribute("text").ToUnicodeString();
                                                        //Log.WriteLine("JobRunsToMake.Name [" + JobRunsToMake.Name + "] Text [" + JobRunsToMake.Text + "] IntValue [" + JobRunsToMake.IntValue + "]");
                                                        SingleLineEditIntegerControls = new List<DirectIndustryWindowSingleLineEditInteger>();
                                                        SingleLineEditIntegerControls.Add(JobRunsToMake);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.WriteLine("Exception [" + ex + "]");
                                                }

                                                /**
                                                var downButton22001061 = SingleLineEditInteger22001061.Attribute("downButton");
                                                if (!downButton22001061.IsValid)
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: downButton not found");
                                                    return;
                                                }

                                                if (!downButton22001061.Attribute("name").IsValid && downButton22001061.Attribute("name").ToUnicodeString().ToLower() != "downButton".ToLower())
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != downButton");
                                                    return;
                                                }
                                                else
                                                {
                                                    //if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(5000)) Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: downButton22001061 found");
                                                    DirectIndustryWindowButton RunsToMake_Down_button = new DirectIndustryWindowButton(directEve, downButton22001061);
                                                    RunsToMake_Down_button.Text = downButton22001061.Attribute("name").ToString();
                                                    RunsToMake_Down_button.Type = GetButtonType(RunsToMake_Down_button.Text);
                                                    RunsToMake_Down_button.ButtonName = (string)downButton22001061.Attribute("name");
                                                    if (DebugConfig.DebugWindows)
                                                    {
                                                        //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("button Text [" + RunsToMake_Down_button.Text + "] Type [" + RunsToMake_Down_button.Type + "] ButtonName [" + RunsToMake_Down_button.ButtonName + "]");
                                                    }

                                                    if (RunsToMake_Down_button.Type == IndustryWindowButtonType.RUNSTOMAKE_DOWN)
                                                        Buttons.Add(RunsToMake_Down_button);
                                                }

                                                var upButton22001061 = SingleLineEditInteger22001061.Attribute("upButton");
                                                if (!upButton22001061.IsValid)
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: upButton not found");
                                                    return;
                                                }

                                                if (!upButton22001061.Attribute("name").IsValid && upButton22001061.Attribute("name").ToUnicodeString().ToLower() != "upButton".ToLower())
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != upButton");
                                                    return;
                                                }
                                                else
                                                {
                                                    //if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(5000)) Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: upButton22001061 found");
                                                    DirectIndustryWindowButton RunsToMake_Up_button = new DirectIndustryWindowButton(directEve, upButton22001061);
                                                    RunsToMake_Up_button.Text = (string)upButton22001061.Attribute("name");
                                                    //if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(5000)) Log.WriteLine("RunsToMake_Up_button text [" + RunsToMake_Up_button.Text + "]");
                                                    RunsToMake_Up_button.Type = GetButtonType(RunsToMake_Up_button.Text);
                                                    //if (DebugConfig.DebugIndustryBehavior) if (DirectEve.Interval(5000)) Log.WriteLine("RunsToMake_Up_button text [" + RunsToMake_Up_button.Type.ToString() + "]");
                                                    RunsToMake_Up_button.ButtonName = (string)upButton22001061.Attribute("name");
                                                    if (DebugConfig.DebugWindows)
                                                    {
                                                        //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("button Text [" + RunsToMake_Up_button.Text + "] Type [" + RunsToMake_Up_button.Type + "] ButtonName [" + RunsToMake_Up_button.ButtonName + "]");
                                                    }

                                                    if (RunsToMake_Up_button.Type == IndustryWindowButtonType.RUNSTOMAKE_UP)
                                                        Buttons.Add(RunsToMake_Up_button);
                                                }
                                                **/
                                            }

                                        }

                                        if (pyBlueprintCenter220010.Attribute("jobData").IsValid)
                                        {
                                            //.jobData
                                            //
                                            // will be null if there is no blueprint loaded?!
                                            //
                                            //materials ---- 0,1,2,3,4,5,6,7,8,9,10 (one for each material, varies based on the blueprint!)
                                            //total_cost - Cost to submit the job, with taxes and such
                                            //runs - runs (int)
                                            //probability - probability (float)
                                            //optional_materials - list of optional materials (like what?!) (list)
                                            //
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.output[0]
                                            //output
                                            //output ---- quantity
                                            //output ---- typeID
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.activity
                                            //activityID
                                            //activity
                                            //activity -- time
                                            //activity -- skills
                                            //activity -- products
                                            //activity -- materials
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.available
                                            //available - this is a list of all materials available (missing items would need to be calculated!)
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.slots
                                            //slots - does this show the slots using this blueprint? not sure
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.status
                                            //status - status of the job (int) - 0 is not started, 1 is started, what are the other values?
                                            //used_slots - used slots (int) - is this total or just this blueprint?
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.roles
                                            //roles - (int) - what are the possible values?
                                            //
                                            //blueprint
                                            //blueprint ---- blueprintTypeID
                                            //blueprint ---- original (bool)
                                            //blueprint ---- productTypeID
                                            //blueprint ---- runsRemaining
                                            //blueprint ---- singleton (bool)
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities
                                            //blueprint ---- activities
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[1]
                                            //blueprint ---- activities --- manufacturing
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[8]
                                            //blueprint ---- activities --- invention
                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.all_activities
                                            //blueprint ---- all_activities ----  1,3,4,5,8 (all valid activities, not limited by your current facilities or skills?)
                                            //SetJobRuns(self, value)

                                            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData
                                            //jobData
                                            var pyJobData = pyBlueprintCenter220010.Attribute("jobData");
                                            if (pyJobData.IsValid)
                                            {
                                                if (pyJobData.Attribute("status").IsValid)
                                                {
                                                    JobStatus = pyJobData.Attribute("status").ToInt(); //0 is not started, 1 is stated
                                                }

                                                if (pyJobData.Attribute("completed").IsValid) //itemID!
                                                {
                                                    completed = pyJobData.Attribute("completed").ToBool();
                                                }

                                                if (pyJobData.Attribute("total_cost").IsValid) //itemID!
                                                {
                                                    total_cost = pyJobData.Attribute("total_cost").ToDouble();
                                                }

                                                if (pyJobData.Attribute("distance").IsValid)
                                                {
                                                    distance = pyJobData.Attribute("distance").ToInt(); //0 is closest (in station?), 1 jumps?, 2 jumps?, 3 jumps? - needs more info
                                                }

                                                if (pyJobData.Attribute("errors").IsValid)
                                                {
                                                    //total_cost = pyJobData.Attribute("errors").ToDouble();
                                                }

                                                if (pyJobData.Attribute("facility").IsValid)
                                                {
                                                    //total_cost = pyJobData.Attribute("errors").ToDouble();
                                                }

                                                if (pyJobData.Attribute("facilityID").IsValid)
                                                {
                                                    //total_cost = pyJobData.Attribute("errors").ToDouble();
                                                }

                                                if (pyJobData.Attribute("jobID").IsValid)
                                                {
                                                    try
                                                    {
                                                        if (pyJobData.Attribute("jobID") != null && pyJobData.Attribute("jobID").ToLong() > 0)
                                                        {
                                                            jobID = pyJobData.Attribute("jobID").ToLong();
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Logging.Log.WriteLine("Exception [" + ex + "]");
                                                    }
                                                }

                                                if (pyJobData.Attribute("isImpounded").IsValid)
                                                {
                                                    isImpounded = pyJobData.Attribute("isImpounded").ToInt();
                                                }

                                                if (pyJobData.Attribute("materials").IsValid)
                                                {
                                                    //total_cost = jobData.Attribute("errors").ToDouble();
                                                }

                                                if (pyJobData.Attribute("max_distance").IsValid)
                                                {
                                                    max_distance = pyJobData.Attribute("max_distance").ToInt();
                                                }

                                                if (pyJobData.Attribute("max_slots").IsValid)
                                                {
                                                    max_slots = pyJobData.Attribute("max_slots").ToInt();
                                                }

                                                if (pyJobData.Attribute("maxRuns").IsValid)
                                                {
                                                    maxRuns = pyJobData.Attribute("maxRuns").ToInt();
                                                }

                                                if (pyJobData.Attribute("optional_materials").IsValid)
                                                {
                                                    //maxRuns = jobData.Attribute("optional_materials").ToInt();
                                                }

                                                if (pyJobData.Attribute("ownerID").IsValid)
                                                {
                                                    OwnerID = pyJobData.Attribute("ownerID").ToInt();
                                                }

                                                if (pyJobData.Attribute("productTypeID").IsValid)
                                                {
                                                    OutputTypeID = pyJobData.Attribute("productTypeID").ToInt();
                                                    if (OutputTypeID > 0)
                                                    {
                                                        OutputItem = new DirectItem(DirectEve);
                                                        OutputItem.TypeId = OutputTypeID;
                                                    }
                                                }

                                                if (pyJobData.Attribute("probability").IsValid)
                                                {
                                                    probability = pyJobData.Attribute("probability").ToInt();
                                                }

                                                if (pyJobData.Attribute("roles").IsValid)
                                                {
                                                    Roles = pyJobData.Attribute("roles").ToInt();
                                                }

                                                if (pyJobData.Attribute("runs").IsValid)
                                                {
                                                    //# of runs to manufacture, the nuber listed in the Industry window under the blueprint!
                                                    runs = pyJobData.Attribute("runs").ToInt();
                                                }

                                                if (pyJobData.Attribute("slots").IsValid)
                                                {
                                                    //slots = jobData.Attribute("slots").ToInt();
                                                }

                                                //output 0,1
                                                //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.output[0]
                                                //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.output[1]

                                                if (pyJobData.Attribute("used_slots").IsValid)
                                                {
                                                    used_slots = pyJobData.Attribute("used_slots").ToInt();
                                                }

                                                if (pyJobData.Attribute("available").IsValid)
                                                {
                                                    //available - this is a list of all materials available (missing items would need to be calculated!)
                                                }

                                                if (pyJobData.Attribute("blueprint").IsValid)
                                                {
                                                    var pyBlueprint = pyJobData.Attribute("blueprint");
                                                    if (pyBlueprint.IsValid)
                                                    {
                                                        if (pyBlueprint.Attribute("blueprintTypeID").IsValid)
                                                        {
                                                            BlueprintTypeID = pyBlueprint.Attribute("blueprintTypeID").ToInt();

                                                            if (pyBlueprint.Attribute("available").IsValid)
                                                            {
                                                                //AvailableMaterials = pyBlueprint.Attribute("available").To();
                                                                //this is a list of all materials available (missing items would need to be calculated!)
                                                            }

                                                            if (pyBlueprint.Attribute("activities").IsValid)
                                                            {
                                                                //valid activities, limited by your current facilities and skills?
                                                                //1 = manufacturing?
                                                                //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].jobData.blueprint.activities[1]
                                                            }

                                                            if (pyBlueprint.Attribute("all_activities").IsValid)
                                                            {
                                                                //all valid activities, not limited by your current facilities or skills?
                                                            }

                                                            if (pyBlueprint.Attribute("blueprintID").IsValid) //same as ItemID?
                                                            {
                                                                BlueprintItemID = pyBlueprint.Attribute("blueprintID").ToLong();
                                                            }

                                                            if (pyBlueprint.Attribute("materialEfficiency").IsValid)
                                                            {
                                                                materialEfficiency = pyBlueprint.Attribute("materialEfficiency").ToInt();
                                                            }

                                                            if (pyBlueprint.Attribute("timeEfficiency").IsValid)
                                                            {
                                                                timeEfficiency = pyBlueprint.Attribute("timeEfficiency").ToInt();
                                                            }

                                                            if (pyBlueprint.Attribute("runsRemaining").IsValid)
                                                            {
                                                                BlueprintRunsRemaining = pyBlueprint.Attribute("runsRemaining").ToInt();
                                                                if (BlueprintRunsRemaining == -1)
                                                                    BlueprintRunsRemaining = 9999;
                                                            }

                                                            if (pyBlueprint.Attribute("maxProductionLimit").IsValid)
                                                            {
                                                                maxProductionLimit = pyBlueprint.Attribute("maxProductionLimit").ToInt();
                                                            }

                                                            if (pyBlueprint.Attribute("solarSystemID").IsValid)
                                                            {
                                                                solarSystemID = pyBlueprint.Attribute("solarSystemID").ToLong();
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (DirectEve.Interval(60000, 60000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] No jobs found - Please install blueprint");
                                        }
                                    }
                                }
                            }

                            //this object
                            //JobsStrip - .children._childrenObjects[1]
                            //path to this object: 2201
                            //carbonui.uicore.uicore.registry.windows[9]
                            //.children._childrenObjects[2] - Content*
                            //.children._childrenObjects[2] - main*
                            //.children._childrenObjects[0] - topCont
                            //.children._childrenObjects[1] - JobsStrip
                            var pyJobsStrip2201 = pyTopCont220.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                            if (!pyJobsStrip2201.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: JobsStrip not found");
                                return;
                            }

                            if (!pyJobsStrip2201.Attribute("name").IsValid && pyJobsStrip2201.Attribute("name").ToUnicodeString().ToLower() != "JobsStrip".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != JobsStrip");
                                return;
                            }
                            else
                            {
                                //this object
                                //JobsSummary - .children._childrenObjects[1]
                                //path to this object: 22011
                                //.children._childrenObjects[2] - Content*
                                //.children._childrenObjects[2] - main*
                                //.children._childrenObjects[0] - topCont
                                //.children._childrenObjects[1] - JobsStrip
                                //.children._childrenObjects[1] - JobsSummary
                                var pyJobsSummary22011 = pyJobsStrip2201.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                if (!pyJobsSummary22011.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: JobsSummary not found");
                                    return;
                                }

                                if (!pyJobsSummary22011.Attribute("name").IsValid && pyJobsSummary22011.Attribute("name").ToUnicodeString().ToLower() != "JobsSummary".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != JobsSummary");
                                    return;
                                }
                                else
                                {
                                    //this object
                                    //.children._childrenObjects[1] - Row0_Col1
                                    //path to this object: 220111
                                    //.children._childrenObjects[2] - Content*
                                    //.children._childrenObjects[2] - main*
                                    //.children._childrenObjects[0] - topCont
                                    //.children._childrenObjects[1] - JobsStrip
                                    //.children._childrenObjects[1] - JobsSummary
                                    //.children._childrenObjects[1] - Row0_Col1
                                    var pyRow0_Col1_220111 = pyJobsSummary22011.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                    if (!pyRow0_Col1_220111.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Row0_Col1 not found");
                                        return;
                                    }

                                    if (!pyRow0_Col1_220111.Attribute("name").IsValid && pyRow0_Col1_220111.Attribute("name").ToUnicodeString().ToLower() != "Row0_Col1".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != Row0_Col1");
                                        return;
                                    }
                                    else
                                    {
                                        //this object
                                        //.children._childrenObjects[0] - Content
                                        //path to this object: 2201110
                                        //.children._childrenObjects[2] - Content
                                        //.children._childrenObjects[2] - main
                                        //.children._childrenObjects[0] - topCont
                                        //.children._childrenObjects[1] - JobsStrip
                                        //.children._childrenObjects[1] - JobsSummary
                                        //.children._childrenObjects[1] - Row0_Col1
                                        //.children._childrenObjects[0] - Content
                                        var pyContent2201110 = pyRow0_Col1_220111.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                        if (!pyContent2201110.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Content not found");
                                            return;
                                        }

                                        if (!pyContent2201110.Attribute("name").IsValid && pyContent2201110.Attribute("name").ToUnicodeString().ToLower() != "Content".ToLower())
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != Content");
                                            return;
                                        }
                                        else
                                        {
                                            //.children._childrenObjects[0] - Label
                                            //path to this object: 22011100
                                            //.children._childrenObjects[2] - Content
                                            //.children._childrenObjects[2] - main
                                            //.children._childrenObjects[0] - topCont
                                            //.children._childrenObjects[1] - JobsStrip
                                            //.children._childrenObjects[1] - JobsSummary
                                            //.children._childrenObjects[1] - Row0_Col1
                                            //.children._childrenObjects[0] - Content
                                            //.children._childrenObjects[0] - Label
                                            var pyLabel = pyContent2201110.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                            if (!pyLabel.IsValid)
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Label not found");
                                                return;
                                            }

                                            if (!pyLabel.Attribute("name").IsValid && pyLabel.Attribute("name").ToUnicodeString().ToLower() != "Label".ToLower())
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != Label");
                                                return;
                                            }
                                            else
                                            {
                                                //Label - 1 / <color=0xFFFF9900>1</color>
                                                if (pyLabel.Attribute("text").IsValid)
                                                {
                                                    string tempLabelText = pyLabel.Attribute("text").ToUnicodeString();
                                                    if (!string.IsNullOrEmpty(tempLabelText))
                                                    {
                                                        //take tempLabelText and split it on the / character, taking the right side of the split
                                                        string tempLabelTextRight = tempLabelText.Split('/')[1];
                                                        if (!string.IsNullOrEmpty(tempLabelTextRight))
                                                        {
                                                            //strip all XML tags from tempLabelTextRight
                                                            tempLabelTextRight = Regex.Replace(tempLabelTextRight, "<.*?>", string.Empty);
                                                            //strip any non-numeric characters using regex
                                                            tempLabelTextRight = Regex.Replace(tempLabelTextRight, "[^0-9]", "");
                                                            //if (DirectEve.Interval(5000)) Log.WriteLine("tempLabelRight [" + tempLabelTextRight + "] aka max_slots");
                                                            //if tempLabelTextRight is numeric
                                                            try
                                                            {
                                                                if (!string.IsNullOrEmpty(tempLabelTextRight))
                                                                {
                                                                    //convert tempLabelTextRight to an integer
                                                                    max_slots = Convert.ToInt32(tempLabelTextRight);
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.WriteLine("Exception [" + ex + "]");
                                                            }

                                                            //take the first two numeric characters in tempLabelText
                                                            string tempLabelTextLeft = tempLabelText.Substring(0, 2);
                                                            //strip any spaces
                                                            tempLabelTextLeft = tempLabelTextLeft.Trim();
                                                            //strip any non-numeric characters using regex
                                                            tempLabelTextLeft = Regex.Replace(tempLabelTextLeft, "[^0-9]", "");

                                                            try
                                                            {
                                                                if (!string.IsNullOrEmpty(tempLabelTextLeft))
                                                                {
                                                                    used_slots = Convert.ToInt32(tempLabelTextLeft);
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                if (DirectEve.Interval(5000)) Log.WriteLine("tempLabelLeft [" + tempLabelTextLeft + "] aka used_slots");
                                                                Log.WriteLine("Exception [" + ex + "]");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                //SubmitButton - .children._childrenObjects[3] - SubmitButton, text == Start, enabled (bool)
                                //path to this object: 22013
                                //.children._childrenObjects[2] - Content
                                //.children._childrenObjects[2] - main
                                //.children._childrenObjects[0] - topCont
                                //.children._childrenObjects[1] - JobsStrip
                                //.children._childrenObjects[3] - SubmitButton
                                var pySubmitButton22013 = pyJobsStrip2201.Attribute("children").Attribute("_childrenObjects").GetItemAt(3);
                                if (!pySubmitButton22013.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: SubmitButton !IsValid");
                                }
                                //StartButton - Start new job
                                else if (pySubmitButton22013.Attribute("enabled").ToBool() && pySubmitButton22013.Attribute("display").ToBool())
                                {
                                    //if (DirectEve.Interval(30000, 30000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: text == Start");
                                    if (pySubmitButton22013.Attribute("text").IsValid && pySubmitButton22013.Attribute("text").ToUnicodeString().ToLower() == "Start".ToLower())
                                    {
                                        DirectIndustryWindowButton START_button = new DirectIndustryWindowButton(directEve, pySubmitButton22013);
                                        START_button.Text = (string)pySubmitButton22013.Attribute("text");
                                        START_button.Type = GetButtonType(START_button.Text);
                                        START_button.ButtonName = (string)pySubmitButton22013.Attribute("name");
                                        if (DebugConfig.DebugWindows)
                                        {
                                            if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("button Text [" + START_button.Text + "] Type [" + START_button.Type + "] ButtonName [" + START_button.ButtonName + "]");
                                        }

                                        if (START_button.Type == IndustryWindowButtonType.START)
                                            Buttons.Add(START_button);
                                    }

                                    //Deliver selected_Btn - Deliver selected job
                                    if (pySubmitButton22013.Attribute("text").IsValid && pySubmitButton22013.Attribute("text").ToUnicodeString().ToLower() == "Deliver".ToLower())
                                    {
                                        DirectIndustryWindowButton DELIVER_button = new DirectIndustryWindowButton(directEve, pySubmitButton22013);
                                        DELIVER_button.Text = (string)pySubmitButton22013.Attribute("text");
                                        DELIVER_button.Type = GetButtonType(DELIVER_button.Text);
                                        DELIVER_button.ButtonName = (string)pySubmitButton22013.Attribute("name");
                                        if (DebugConfig.DebugWindows)
                                        {
                                            if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("button Text [" + DELIVER_button.Text + "] Type [" + DELIVER_button.Type + "] ButtonName [" + DELIVER_button.ButtonName + "]");
                                        }

                                        if (DELIVER_button.Type == IndustryWindowButtonType.DELIVER)
                                            Buttons.Add(DELIVER_button);
                                    }

                                    //StopButton - Stop existing (selected) job
                                    if (pySubmitButton22013.Attribute("text").IsValid && pySubmitButton22013.Attribute("text").ToUnicodeString().ToLower() == "Stop".ToLower())
                                    {
                                        DirectIndustryWindowButton STOP_button = new DirectIndustryWindowButton(directEve, pySubmitButton22013);
                                        STOP_button.Text = (string)pySubmitButton22013.Attribute("text");
                                        STOP_button.Type = GetButtonType(STOP_button.Text);
                                        STOP_button.ButtonName = (string)pySubmitButton22013.Attribute("name");
                                        if (DebugConfig.DebugWindows)
                                        {
                                            if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("button Text [" + STOP_button.Text + "] Type [" + STOP_button.Type + "] ButtonName [" + STOP_button.ButtonName + "]");
                                        }

                                        if (STOP_button.Type == IndustryWindowButtonType.STOP)
                                            Buttons.Add(STOP_button);
                                    }
                                }
                            }
                        }

                        //browserCont - .children._childrenObjects[0]
                        //path to this object: 2210
                        //carbonui.uicore.uicore.registry.windows[7]
                        //.children._childrenObjects[2] - Content
                        //.children._childrenObjects[2] - main
                        //.children._childrenObjects[1] - bottomCont
                        //.children._childrenObjects[0] - browserCont
                        var pyBrowserCont2201 = pyBottomCont221.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                        if (!pyBrowserCont2201.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: browserCont not found");
                            return;
                        }

                        if (!pyBrowserCont2201.Attribute("name").IsValid && pyBrowserCont2201.Attribute("name").ToUnicodeString().ToLower() != "browserCont".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != browserCont");
                            return;
                        }
                        else
                        {
                            //tabgroup - .children._childrenObjects[0]
                            //path to this object: 22100
                            //carbonui.uicore.uicore.registry.windows[7]
                            //.children._childrenObjects[2] - Content
                            //.children._childrenObjects[2] - main
                            //.children._childrenObjects[1] - bottomCont
                            //.children._childrenObjects[0] - browserCont
                            //.children._childrenObjects[0] - tabgroup
                            var pyTabgroup22100 = pyBrowserCont2201.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                            if (!pyTabgroup22100.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: tabgroup not found");
                                return;
                            }

                            if (!pyTabgroup22100.Attribute("name").IsValid && pyTabgroup22100.Attribute("name").ToUnicodeString().ToLower() != "tabgroup".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != tabgroup");
                                return;
                            }
                            else
                            {
                                //ContainerAutoSize - .children._childrenObjects[1]
                                //path to this object: 221001
                                //carbonui.uicore.uicore.registry.windows[7]
                                //.children._childrenObjects[2] - Content
                                //.children._childrenObjects[2] - main
                                //.children._childrenObjects[1] - bottomCont
                                //.children._childrenObjects[0] - browserCont
                                //.children._childrenObjects[0] - tabgroup
                                //.children._childrenObjects[1] - ContainerAutoSize
                                var pyContainerAutoSize221001 = pyTabgroup22100.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                if (!pyContainerAutoSize221001.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize not found");
                                    return;
                                }

                                if (!pyContainerAutoSize221001.Attribute("name").IsValid && pyContainerAutoSize221001.Attribute("name").ToUnicodeString().ToLower() != "ContainerAutoSize".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ContainerAutoSize");
                                    return;
                                }
                                else
                                {
                                    //tabsCont - .children._childrenObjects[1]
                                    //path to this object: 2210011
                                    //carbonui.uicore.uicore.registry.windows[7]
                                    //.children._childrenObjects[2] - Content
                                    //.children._childrenObjects[2] - main
                                    //.children._childrenObjects[1] - bottomCont
                                    //.children._childrenObjects[0] - browserCont
                                    //.children._childrenObjects[0] - tabgroup
                                    //.children._childrenObjects[1] - ContainerAutoSize
                                    //.children._childrenObjects[1] - tabsCont
                                    var pyTabsCont2210011 = pyContainerAutoSize221001.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                    if (!pyTabsCont2210011.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: tabsCont not found");
                                        return;
                                    }

                                    if (!pyTabsCont2210011.Attribute("name").IsValid && pyTabsCont2210011.Attribute("name").ToUnicodeString().ToLower() != "tabsCont".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != tabsCont");
                                    }
                                    else
                                    {
                                        //Jobs - .children._childrenObjects[2]
                                        //path to this object: 22100112
                                        //carbonui.uicore.uicore.registry.windows[7]
                                        //.children._childrenObjects[2] - Content
                                        //.children._childrenObjects[2] - main
                                        //.children._childrenObjects[1] - bottomCont
                                        //.children._childrenObjects[0] - browserCont
                                        //.children._childrenObjects[0] - tabgroup
                                        //.children._childrenObjects[1] - ContainerAutoSize
                                        //.children._childrenObjects[1] - tabsCont
                                        //.children._childrenObjects[2] - Jobs
                                        var pyJobs22100112 = pyTabsCont2210011.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                                        if (!pyJobs22100112.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Jobs not found");
                                            return;
                                        }

                                        if (!pyJobs22100112.Attribute("name").IsValid && pyJobs22100112.Attribute("name").ToUnicodeString().ToLower() != "Jobs".ToLower())
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != Jobs");
                                        }

                                        if (!pyJobs22100112.Attribute("_selected").ToBool())
                                        {
                                            if (ESCache.Instance.Paused)
                                                return;

                                            if (State.CurrentIndustryBehaviorState == IndustryBehaviorState.JobsDeliverAll)
                                            {
                                                if (DirectEve.Interval(5000, 7000, WindowId))
                                                {
                                                    DirectEve.ThreadedCall(pyJobs22100112.Attribute("OnClick"));
                                                    Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Jobs tab not selected");
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            //BrowserJobs - .children._childrenObjects[3]
                            //path to this object: 22103
                            //carbonui.uicore.uicore.registry.windows[7]
                            //.children._childrenObjects[2] - Content
                            //.children._childrenObjects[2] - main
                            //.children._childrenObjects[1] - bottomCont
                            //.children._childrenObjects[0] - browserCont
                            //.children._childrenObjects[3] - BrowserJobs
                            var pyBrowserJobs22103 = pyBrowserCont2201.Attribute("children").Attribute("_childrenObjects").GetItemAt(3);
                            if (!pyBrowserJobs22103.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: browserCont not found");
                                return;
                            }

                            if (!pyBrowserJobs22103.Attribute("name").IsValid && pyBrowserJobs22103.Attribute("name").ToUnicodeString().ToLower() != "browserCont".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != browserCont");
                                return;
                            }
                            else
                            {
                                //ButtonGroup - .children._childrenObjects[1]
                                //path to this object: 221031
                                //carbonui.uicore.uicore.registry.windows[7]
                                //.children._childrenObjects[2] - Content
                                //.children._childrenObjects[2] - main
                                //.children._childrenObjects[1] - bottomCont
                                //.children._childrenObjects[0] - browserCont
                                //.children._childrenObjects[3] - BrowserJobs
                                //.children._childrenObjects[1] - ButtonGroup
                                var pyButtonGroup221031 = pyBrowserJobs22103.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                if (!pyButtonGroup221031.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ButtonGroup not found - is the jobs tab selected?");
                                    return;
                                }

                                if (!pyButtonGroup221031.Attribute("name").IsValid && pyButtonGroup221031.Attribute("name").ToUnicodeString().ToLower() != "ButtonGroup".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ButtonGroup");
                                    return;
                                }
                                else
                                {
                                    //btns - .children._childrenObjects._childrenObjects[0]
                                    //path to this object: 2210310
                                    //carbonui.uicore.uicore.registry.windows[7]
                                    //.children._childrenObjects[2] - Content
                                    //.children._childrenObjects[2] - main
                                    //.children._childrenObjects[1] - bottomCont
                                    //.children._childrenObjects[0] - browserCont
                                    //.children._childrenObjects[3] - BrowserJobs
                                    //.children._childrenObjects[1] - ButtonGroup
                                    //.children._childrenObjects._childrenObjects[0] - btns
                                    var pyBtns2210310 = pyButtonGroup221031.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").GetItemAt(0);
                                    if (!pyBtns2210310.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: btns not found");
                                        return;
                                    }

                                    if (!pyBtns2210310.Attribute("name").IsValid && pyBtns2210310.Attribute("name").ToUnicodeString().ToLower() != "btns".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != btns");
                                        return;
                                    }
                                    else
                                    {
                                        //ButtonWrapper - .children._childrenObjects[1]
                                        //path to this object: 22103101
                                        //carbonui.uicore.uicore.registry.windows[7]
                                        //.children._childrenObjects[2] - Content
                                        //.children._childrenObjects[2] - main
                                        //.children._childrenObjects[1] - bottomCont
                                        //.children._childrenObjects[0] - browserCont
                                        //.children._childrenObjects[3] - BrowserJobs
                                        //.children._childrenObjects[1] - ButtonGroup
                                        //.children._childrenObjects._childrenObjects[0] - btns
                                        //.children._childrenObjects[1] - ButtonWrapper
                                        var pyButtonWrapper22103101 = pyBtns2210310.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                        if (!pyButtonWrapper22103101.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ButtonWrapper not found");
                                            return;
                                        }

                                        if (!pyButtonWrapper22103101.Attribute("name").IsValid && pyButtonWrapper22103101.Attribute("name").ToUnicodeString().ToLower() != "ButtonWrapper".ToLower())
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ButtonWrapper");
                                            return;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                //Deliver all jobs_Btn - .children._childrenObjects[0]
                                                //path to this object: 221031010
                                                //carbonui.uicore.uicore.registry.windows[7]
                                                //.children._childrenObjects[2] - Content
                                                //.children._childrenObjects[2] - main
                                                //.children._childrenObjects[1] - bottomCont
                                                //.children._childrenObjects[0] - browserCont
                                                //.children._childrenObjects[3] - BrowserJobs
                                                //.children._childrenObjects[1] - ButtonGroup
                                                //.children._childrenObjects._childrenObjects[0] - btns
                                                //.children._childrenObjects[1] - ButtonWrapper
                                                //.children._childrenObjects[0] - Deliver all jobs_Btn
                                                var pyDeliverAllJobs_Btn221031010 = pyButtonWrapper22103101.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                if (!pyDeliverAllJobs_Btn221031010.IsValid)
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Deliver all jobs_Btn not found");
                                                    return;
                                                }

                                                if (!pyDeliverAllJobs_Btn221031010.Attribute("name").IsValid || pyDeliverAllJobs_Btn221031010.Attribute("name").ToUnicodeString().ToLower() != "Deliver all jobs_Btn".ToLower())
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != Deliver all jobs_Btn");
                                                    return;
                                                }
                                                else
                                                {
                                                    //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("pyJOBS_DELIVER_ALL_button IsValid: enabled [" + pyDeliverAllJobs_Btn.Attribute("enabled").ToBool() + "] display [" + pyDeliverAllJobs_Btn.Attribute("display").ToBool() + "]");
                                                    if (pyDeliverAllJobs_Btn221031010.Attribute("enabled").ToBool() && pyDeliverAllJobs_Btn221031010.Attribute("display").ToBool())
                                                    {
                                                        //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("pyJOBS_DELIVER_ALL_button enabled (true) and display (true)");
                                                        DirectIndustryWindowButton JOBS_DELIVER_ALL_button = new DirectIndustryWindowButton(directEve, pyDeliverAllJobs_Btn221031010);
                                                        JOBS_DELIVER_ALL_button.Text = (string)pyDeliverAllJobs_Btn221031010.Attribute("text");
                                                        //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("JOBS_SELIVER_ALL_button.Text [" + JOBS_DELIVER_ALL_button.Text + "]");
                                                        JOBS_DELIVER_ALL_button.Type = GetButtonType(JOBS_DELIVER_ALL_button.Text);
                                                        //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("JOBS_SELIVER_ALL_button.Type [" + JOBS_DELIVER_ALL_button.Type.ToString() + "]");
                                                        JOBS_DELIVER_ALL_button.ButtonName = (string)pyDeliverAllJobs_Btn221031010.Attribute("name");
                                                        //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("JOBS_SELIVER_ALL_button.Name [" + JOBS_DELIVER_ALL_button.ButtonName + "]");
                                                        if (DebugConfig.DebugIndustryBehavior)
                                                        {
                                                            //if (DirectEve.Interval(15000, 15000, WindowId)) if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("JOBS_DELIVER_ALL_button Text [" + JOBS_DELIVER_ALL_button.Text + "] Type [" + JOBS_DELIVER_ALL_button.Type + "] ButtonName [" + JOBS_DELIVER_ALL_button.ButtonName + "]");
                                                        }

                                                        if (JOBS_DELIVER_ALL_button.Type == IndustryWindowButtonType.JOBS_DELIVER_ALL)
                                                        {
                                                            State.CurrentIndustryBehaviorState = IndustryBehaviorState.JobsDeliverAll;
                                                            Buttons.Add(JOBS_DELIVER_ALL_button);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logging.Log.WriteLine("Exception [" + ex + "]");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                /**
                //ClaimButton
                var ClaimButton = Container.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                if (!ClaimButton.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ClaimButton not found");
                }

                if (!ClaimButton.Attribute("name").IsValid || ClaimButton.Attribute("name").ToUnicodeString().ToLower() != "ClaimButton".ToLower())
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ClaimButton");
                }

                if (!ClaimButton.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("ClaimButton !IsValid");
                }

                //CloseButton
                var CloseButton = Container.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                if (!CloseButton.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: CloseButton not found");
                }

                if (!CloseButton.Attribute("name").IsValid || CloseButton.Attribute("name").ToUnicodeString().ToLower() != "CloseButton".ToLower())
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != CloseButton");
                }

                if (!CloseButton.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("CloseButton !IsValid");
                }
                **/

                //carbonui.uicore.uicore.registry.windows[9].buttonGroup.children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]



                /**
                if (CloseButton.Attribute("name").IsValid && CloseButton.Attribute("name").ToUnicodeString().ToLower() == "CloseButton".ToLower())
                {
                    if (CloseButton.Attribute("enabled").ToBool() && ClaimButton.Attribute("display").ToBool())
                    {
                        DirectLoginRewardButton close_button = new DirectLoginRewardButton(directEve, CloseButton);
                        close_button.Text = (string)ClaimButton.Attribute("text");
                        close_button.Type = GetButtonType(close_button.Text);
                        close_button.ButtonName = (string)ClaimButton.Attribute("name");
                        if (DebugConfig.DebugWindows)
                        {
                            if (DirectEve.Interval(15000, 15000, WindowId)) Logging.Log.WriteLine("button Text [" + close_button.Text + "] Type [" + close_button.Type + "] ButtonName [" + close_button.ButtonName + "]");
                        }

                        if (close_button.Type != IndustryWindowButtonType.UNKNOWN)
                            Buttons.Add(close_button);

                }
                **/
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
            }
        }
    }
}