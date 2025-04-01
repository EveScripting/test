extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{

    public enum SlotGroup
    {
        HiSlot = 0,
        MedSlot = 1,
        LoSlot = 2,
        SubsysSlot = 3,
        RigSlot = 4
    }

    public class DirectFittingModule : PyObject
    {
        private IntPtr _pyref;
        public bool SlotExists { get; private set; }
        public SlotGroup SlotGroup { get; private set; }

        public PyObject Module { get; private set; }

        private DirectEve DirectEve { get; set; }

        public int TypeId { get; private set; }

        public DirectInvType InvType { get; private set; }

        public bool HasItemFit { get; private set; }

        public DirectFittingModule(PySharp pySharp, IntPtr pyReference, bool newReference, DirectEve de, SlotGroup slotGroup,
            string attributeName = "") : base(pySharp, pyReference, newReference, attributeName)
        {
            _pyref = pyReference;
            SlotExists = this.Call("SlotExists").ToBool();
            SlotGroup = slotGroup;
            Module = this["dogmaModuleItem"];
            DirectEve = de;
            HasItemFit = Module.IsValid;
            if (Module.IsValid)
            {
                TypeId = Module["typeID"].ToInt();
                InvType = DirectEve.GetInvType(TypeId);
            }
        }

        public void OnlineModule()
        {
            DirectEve.ThreadedCall(this["OnlineModule"]);
        }

        public void OfflineModule()
        {
            DirectEve.ThreadedCall(this["OfflineModule"]);
        }

        // this might end up with additional modal messages
        // charges will be removed first then the module itself, so it must be called twice for modules with loaded charges
        public void Unfit()
        {
            DirectEve.ThreadedCall(this["Unfit"]);
        }

        public void FitModule(DirectItem item)
        {
            DirectEve.ThreadedCall(this["FitModule"], item.PyItem);
        }

        public bool IsOnline()
        {
            return this.Call("IsOnline").ToBool();
        }
    }
    public class DirectFittingWindow : DirectWindow
    {
        internal DirectFittingWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
            //carbonui.uicore.uicore.registry.windows[11]
            //fittingWnd
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[0]
            //windows_conreols_cont
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
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //moduleFitingModeContainer - left side of fitting window
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //leftPanelCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //xDivider
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //leftPanel - more here
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //invPanel - more here
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //rightside
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0, 1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Container
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ExpandableMenuContainer
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 01,2,3,4,5,6
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Capacitor - more here!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //Offense - more here!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //Defense - more here!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //Targeting - more here!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4]
            //Navigation - more here!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[5]
            //Drones - more here!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[6]
            //Fighters - more here!
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //LabelPriceLabelCont
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //FittingContTrue
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //7_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //6_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //5_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //4_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4]
            //3_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5]
            //2_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6]
            //1_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7]
            //0_launcherSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[8]
            //launcherSlotsLeft_Icon
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[9]
            //7_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[10]
            //6_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[11]
            //5_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[12]
            //4_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[13]
            //3_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[14]
            //2_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[15]
            //1_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[16]
            //0_turretSlotsLeft_Marker
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[17]
            //turretSlotsLeft_Icon
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18]
            //fittingBase
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects 01,2,3,4,5,6,7,8,9,10,11,12,13
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[0]
            //FeedbackLabelCont
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[1]
            //GroupAllButton
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2]
            //slotCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects 0 - 73!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[0]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[1]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[2]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[3]
            //utilicon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[4]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[5]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[6]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[7]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[8]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[9]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[10]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[11]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[12]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[13]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[14]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[15]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[16]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[17]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[18]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[19]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[20]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[21]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[22]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[23]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[24]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[25]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[26]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[27]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[28]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[29]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[30]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[31]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[32]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[33]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[34]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[35]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[36]
            //utilIcon_2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[37]
            //utilIcon_1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[38]
            //utilIcon_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[39]
            //27 - default_name: fittingSlot! - this is the slot itself - not the module (yet) - display (bool) invReady (bool) utilButtons, utilButtonsTimer, moduleSlotFill
            //.controller
            //IsOnline(self)
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[40]
            //28 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[41]
            //29 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[42]
            //30 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[43]
            //31 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[44]
            //32 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[45]
            //33 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[46]
            //34 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[47]
            //19 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[48]
            //20 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[49]
            //21 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[50]
            //22 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[51]
            //23 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[52]
            //24 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[53]
            //25 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[54]
            //26 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[55]
            //11 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[56]
            //12 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[57]
            //13 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[58]
            //14 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[59]
            //15 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[60]
            //16 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[61]
            //17 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[62]
            //18 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[63]
            //125 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[64]
            //126 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[65]
            //127 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[66]
            //128 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[67]
            //92 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[68]
            //93 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[69]
            //94 - default_name: fittingSlot!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[70]
            //StanceSlots - for stations?!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[71]
            //hiSlotRadial
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[71].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[71].children._childrenObjects[0]
            //icon
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[71].children._childrenObjects[1]
            //bgCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[72]
            //medSlotRadial - more here?
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[2].children._childrenObjects[73]
            //lowSlotRadial - more here?
            //
            //
            //



            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[3]
            //overlay
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[4]
            //calibrationGaugePreview
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[5]
            //powergridGaugePreview
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[6]
            //cpuGaugePreview
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[7]
            //calibrationGauge
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[8]
            //powergridGauge
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[9]
            //cpuGauge
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[10]
            //baseDOT
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[11]
            //baseColor
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[12]
            //baseShape
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[13]
            //ShipSceneParent
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[13].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[13].children._childrenObjects[0]
            //SceneContainerBaseNavigation
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[18].children._childrenObjects[13].children._childrenObjects[1]
            //ShipSceneContainer
            //
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //overlayCont
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2]
            //currentShipGhost - Simulated Fit!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3]
            //nameAndWarningsCont - upper left corner of fitting window
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
            //cosmeticFittingModeContainer - Personalization Tab in the fitting window!
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //cosmeticsPanel
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //leftPanelContainer
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //centerParent
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //cleanShipButton - Note this is the Personalization Tab in the fitting window!
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[3]
            //underlay
            //

        }

        public PyObject Controller => PyWindow["controller"];

        public PyObject SlotsByGroup => Controller["slotsByGroups"];

        public List<DirectFittingModule> GetModulesOfGroup(SlotGroup group, bool onlyExistingSlots = true)
        {
            var ret = new List<DirectFittingModule>();
            var slots = SlotsByGroupDictionary[(int)group].ToList<PyObject>();
            foreach (var slot in slots)
            {
                var item = new DirectFittingModule(PySharp, slot.PyRefPtr, false, DirectEve, group);
                if (onlyExistingSlots && !item.SlotExists)
                {
                    continue;
                }
                ret.Add(item);
            }
            return ret;
        }

        public List<DirectFittingModule> GetAllModules(bool onlyExistingSlots = true)
        {
            return GetModulesOfGroup(SlotGroup.HiSlot, onlyExistingSlots).Concat(GetModulesOfGroup(SlotGroup.MedSlot, onlyExistingSlots))
                .Concat(GetModulesOfGroup(SlotGroup.LoSlot, onlyExistingSlots)).Concat(GetModulesOfGroup(SlotGroup.RigSlot, onlyExistingSlots))
                .Concat(GetModulesOfGroup(SlotGroup.SubsysSlot, onlyExistingSlots)).ToList();
        }

        public Dictionary<int, PyObject> SlotsByGroupDictionary => SlotsByGroup.ToDictionary<int>();

        public PyObject DogmaLocation => Controller["dogmaLocation"];
        public Dictionary<String, PyObject> GetCurrentAttributeValues =>
            Controller.Call("GetCurrentAttributeValues").ToDictionary<string>();

        private float? _currentDroneControlRange;
        public float GetCurrentDroneControlRange => _currentDroneControlRange ??= Controller.Call("GetDroneControlRange")["value"].ToFloat();

        private float? _maxTargets;
        public float GetMaxTargets => _maxTargets ??= Controller.Call("GetMaxTargets")["value"].ToFloat();

        private float? _maxVelocity;
        public float GetMaxVelocity => _maxVelocity ??= Controller.Call("GetMaxVelocity")["value"].ToFloat();

        public bool IsFittingSimulated
        {
            get
            {
                var dogma = DirectEve.GetLocalSvc("ghostFittingSvc")["fittingDogmaLocation"];
                if (dogma.IsValid)
                {
                    var items = dogma["dogmaItems"].ToDictionary<string>(); // wait until there is at least one active module in the ghost fitting => (hopefully means) that we loaded the fitting and all values are up to date
                    foreach (var kv in items)
                    {
                        var py = kv.Value;
                        if (py.IsValid && py["IsActive"].IsCallable())
                        {
                            var ret = py.Call("IsActive").ToBool();

                            if (ret)
                            {
                                return true && Controller["isShipSimulated"].ToBool(); ;
                            }
                        }
                    }


                }

                return false;
            }
        }
        // look @ GetCurrentAttributeValues for all other attributes
    }
}
