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
using SharpDX.Direct2D1;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectLayers : DirectObject
    {
        #region Fields

        #endregion Fields

        #region Constructors

        internal DirectLayers(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     The character selection screen is open
        /// </summary>
        public bool AtCharacterSelection => (bool)CharSelectLayer.Attribute("display") && ((bool)CharSelectLayer.Attribute("isopen") || (bool)CharSelectLayer.Attribute("isopening"));

        /// <summary>
        ///     The login screen is open
        /// </summary>
        public bool AtLogin => (bool)LoginLayer.Attribute("display") && ((bool)LoginLayer.Attribute("isopen") || (bool)LoginLayer.Attribute("isopening"));





        //
        // Other viewstates
        //

        public List<PyObject> DefinedLayers
        {
            get
            {
                return new List<PyObject>
                {
                    AboveMainLayer,
                    ActivityTrackerLayer,
                    AlwaysVisibleLayer,
                    BlinkLayer,
                    BracketLayer,
                    CareerPortalLayer,
                    CharacterCreationLayer,
                    CharSelectLayer,
                    DraggingLayer,
                    HangarLayer,
                    HintLayer,
                    InflightLayer,
                    InfoBubbleLayer,
                    LoadingLayer,
                    LoadingFillLayer,
                    LoginLayer,
                    MainLayer,
                    MenuLayer,
                    MLoadingLayer,
                    ModalLayer,
                    PaintToolLayer,
                    PlanetLayer,
                    SensorSuiteLayer,
                    ShipTreeLayer,
                    ShipUILayer,
                    SidePanelsLayer,
                    SkillPlanLayer,
                    SkillTreeLayer,
                    SpaceUILayer,
                    SpaceTutorialLayer,
                    StarMapLayer,
                    StarMapNewLayer,
                    StructureLayer,
                    SystemMapLayer,
                    SystemMapNewLayer,
                    StarMapBracketsLayer,
                    SystemMenuLayer,
                    TacticalLayer,
                    TargetLayer,
                    UtilMenuLayer,
                    VgsAboveSuppressLayer,
                    VgsSuppressLayer,
                    VideoOverlayLayer,
                    VirtualGoodsStoreLayer,
                    ActiveMapLayer,
                    //DockPanelLayer
                    };
            }
        }

        public List<PyObject> LayersInUse
        {
            get
            {
                return DefinedLayers.FindAll(layer => (bool)layer.Attribute("isopen") || (bool)layer.Attribute("isopening"));
            }
        }

        public PyObject CurrentLayer
        {
            get
            {
                return LayersInUse.FirstOrDefault(layer => (bool)layer.Attribute("display"));
            }
        }

        //
        // worth noting that right now any window that is FULL SCREEN will make it do we cant see any windows behind it NOR currently interact with the layer that is in front, not sure how to see the contents of that full screen window!
        //

        //carbonui.uicore.uicore.layer.viewstate.children._childrenObjects[18] //inflight - display(bool)
        //
        public PyObject AboveMainLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("abovemain");
        //carbonui.uicore.uicore.layer.activitytracker.uiwindow
        public PyObject ActivityTrackerLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("activitytracker");
        public PyObject AlwaysVisibleLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("alwaysvisible");
        public PyObject BlinkLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("blink");
        public PyObject BracketLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("bracket");
        public PyObject CareerPortalLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("career_portal");
        public PyObject CharacterCreationLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("charactercreation");
        public PyObject CharSelectLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("charsel");
        public PyObject DraggingLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("dragging");
        public PyObject HangarLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("hangar");
        public PyObject HintLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("hint");
        public PyObject InflightLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("inflight"); //in space, flying around
        public PyObject InfoBubbleLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("infobubble");
        public PyObject LoadingLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("loading");
        public PyObject LoadingFillLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("loadingfill");
        public PyObject LoginLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("login");
        public PyObject MainLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("main");
        public PyObject MenuLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("menu");
        public PyObject MLoadingLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("mloading");
        public PyObject ModalLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("modal");
        public PyObject PaintToolLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("paint_tool");
        public PyObject PlanetLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("planet");

        public PyObject SensorSuiteLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("sensorsuite");
        public PyObject ShipTreeLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("shiptree");
        public PyObject ShipUILayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("shipui"); //Ship user interface buttons and such for making your ship do things
        public PyObject SidePanelsLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("sidepanels");
        public PyObject SkillPlanLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("skill_plan");
        public PyObject SkillTreeLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("skill_tree");
        public PyObject SpaceUILayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("space_ui");
        public PyObject SpaceTutorialLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("spacetutorial");
        public PyObject StarMapLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("starmap");
        public PyObject StarMapNewLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("starmap_new");
        public PyObject StructureLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("structure");
        public PyObject SystemMapLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("systemmap"); //full screen system map for probing
        public PyObject SystemMapNewLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("systemmap_new"); //full screen system map for probing
        public PyObject StarMapBracketsLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("starmapbrackets");
        public PyObject SystemMenuLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("systemmenu");
        public PyObject TacticalLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("tactical");
        public PyObject TargetLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("target");
        public PyObject UtilMenuLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("utilmenu");
        public PyObject VgsAboveSuppressLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("vgsabovesuppress"); //Vgs = Virtual Goods Store
        public PyObject VgsSuppressLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("vgssuppress"); //Vgs = Virtual Goods Store
        public PyObject VideoOverlayLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("videooverlay");
        //public PyObject ViewOverlaysLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("view_overlays"); //this is a list of overlays!
        public PyObject VirtualGoodsStoreLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("virtual_goods_store");

        public PyObject ActiveMapLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("activemap");

        //public PyObject DockPanelLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("dockpanel");






        #endregion Properties
    }
}