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
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace EVESharpCore.Framework
{
    /**
    public enum InsuranceQuotesRadioButtonType
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
    **/

    public class DirectInsuranceQuotesWindow : DirectWindow
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


        internal DirectInsuranceQuotesWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            /**
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
            **/

            //Buttons = new List<DirectIndustryWindowButton>();

            //carbonui.uicore.uicore.registry.windows[7]
            //InsurenaceTermsWindow
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[0]
            //windows_controls_cont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[1]
            //Resizer
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2]
            //content
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[0]
            //__loadingParent
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7,8
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //Container
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //nameLabel
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //RadioButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //text - <b>Basic</b><br>Cost <b>0.20 ISK</b> - Estimated Payout Value <b>2.00 ISK</b>
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //diode
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //RadioButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //text - <b>Standard</b><br>Cost <b>0.40 ISK</b> - Estimated Payout Value <b>2.40 ISK</b>
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1]
            //diode
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4]
            //RadioButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0]
            //text - <b>Bronze</b><br>Cost <b>0.60 ISK</b> - Estimated Payout Value <b>2.80 ISK</b>
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[1]
            //diode
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5]
            //RadioButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0]
            //text - <b>Silver</b><br>Cost <b>0.80 ISK</b> - Estimated Payout Value <b>3.20 ISK</b>
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[1]
            //diode
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6]
            //RadioButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0]
            //text - <b>Gold</b><br>Cost <b>1.00 ISK</b> - Estimated Payout Value <b>3.60 ISK</b>
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[1]
            //diode
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7]
            //RadioButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0]
            //text - <b>Platinum</b><br>Cost <b>1.20 ISK</b> - Estimated Payout Value <b>4.00 ISK</b>
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[1]
            //diode
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8]
            //ButtonGroup
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects._childrenObjects[0]
            //btns
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects._childrenObjects[0].children._childrenObjects[0]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Insure_Btn
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects._childrenObjects[0].children._childrenObjects[1]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Cancel_btn
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8].children._childrenObjects._childrenObjects[0].children._childrenObjects[2]
            //OverflowButton
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[3]
            //underlay



        }
    }
}