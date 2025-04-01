extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public class DirectMapViewWindow : DirectWindow
    {
        #region Fields

        #endregion Fields

        #region Constructors

        internal DirectMapViewWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            //carbonui.uicore.uicore.registry.windows[10]
            //solar_system_map_panel //This is the probe scanner window showing the graphical representation of the solar system and probe positions
            //
            //Attribute("mapView")
            //MapViewSolarSystem
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0]
            //HorizontalDragContainer
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //dragArea
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ResizeHandle
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Sprite
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //PanelsContainer
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //dragArea
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //mainCont
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //firstPanelMainCont
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ScannerPaletteHeader
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //ProbeScannerPalette
            //primaryButton
            //primaryButtonController
            //Analyze
            //display (bool)
            //filterCont
            //mapButton
            //probeStateCont
            //reconnectButton
            //refreshTimer
            //resultScroll
            //scanResultsContainer
            //solarSystemView
            //state (int)
            //bonusesContainer
            //bottomContainer
            //centerOnSelfFormationButton
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //filterCont
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //BottomContainer
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //probeStateCont - No probes in launcher, No probes launched, # of probes in space
            //More here, including the recover probes button and stats about scan strength and scan duration, etc
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[1]
            //FormationBottonsContainer
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[2]
            //AnalyzeButtonContainer
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0]
            //sliderCont
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //sliderLabel
            //text - example 4AU
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1]
            //SizeSlider
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //barCont
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //handle
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //ticksCont
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //bgCont
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //ScanResultsContainer
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0]
            //resultScroll
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //ScrollColumnHeader - more here!
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //maincontainer
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Scrollbar
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //ScrollHandle
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //fill
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //fill
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //__clipper
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //__content
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //scan results here!
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //columnContainer
            //columns
            //display (bool)
            //groupColumn
            //hint
            //idColumn
            //pickState (int)
            //pickRadius (int)
            //signalColumn
            //state (int)
            //statusBar
            //warpButton
            //WarpToAction
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //EveLabelMedium
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //warpButton
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2,3,4
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Sprite
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //EveLabelMedium - Distance
            //text - EX: 5.22 AU
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0]
            //EveLabelMedium - ID
            //text - EX: ZZZ-123
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[3]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[3].children._childrenObjects[0]
            //EveLabelMedium - Name of site: EX: Guristas Burrow
            //text
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4]
            //Container
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
            //EveLabelMedium - Type of site: EX: Combat Site
            //text - EX: Combat Site
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //statusBar
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //scanAnimator
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //secondPanelsContainer
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[1]
            //Container
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[1]
            //infoLayer
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[2]
            //MapViewScannerNavigation
            //
            //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[3]
            //MapViewSceneContainer
            //
            //





            //
            // Alternate method to get the Directional Scanner window
            //
            //carbonui.uicore.uicore.registry.windows[11]
            //probeScannerWindow
            //
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
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //ProbeScannerPalette
            //
            // the rest is the same as above, paths needs to be adjusted to be relative to this new root
            //
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[3]
            //underlay
            //
            //

            PyWindow = pyWindow;
            try
            {
                //ProbeScannerPalette
                //
                //
                try
                {
                    if (PyWindow != null)
                    {
                        if (PyWindow.Attribute("name").IsValid && PyWindow.Attribute("name").ToUnicodeString().ToLower() == "solar_system_map_panel".ToLower())
                        {
                            //.mapView                      - mapview
                            var pymapView = PyWindow.Attribute("mapView");
                            if (!pymapView.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: MapViewSolarSystem not found");
                                return;
                            }

                            if (pymapView.Attribute("name").IsValid && pymapView.Attribute("name").ToUnicodeString().ToLower() != "MapViewSolarSystem".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != MapViewSolarSystem");
                                return;
                            }
                            else
                            {
                                //.children._childrenObjects[0] - mainCont
                                foreach (var pyMainCont in pymapView.Attribute("children").Attribute("_childrenObjects").ToList().Where(i => i.Attribute("name").IsValid && i.Attribute("name").ToUnicodeString().ToLower() == "mainCont".ToLower()))
                                {
                                    if (!pyMainCont.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: mainCont not found");
                                        return;
                                    }

                                    if (pyMainCont.Attribute("name").IsValid && pyMainCont.Attribute("name").ToUnicodeString().ToLower() != "mainCont".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name [" + pyMainCont.Attribute("name").ToUnicodeString() + "] != mainCont");
                                        return;
                                    }
                                    else
                                    {
                                        //Full Path
                                        //carbonui.uicore.uicore.registry.windows[7].mapView.children._childrenObjects[0].children._childrenObjects[0]
                                        //.children._childrenObjects[0] - HorizontalDragContainer
                                        var pyHorizontalDragContainer00 = pyMainCont.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                        if (!pyHorizontalDragContainer00.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: HorizontalDragContainer not found");
                                            return;
                                        }

                                        if (pyHorizontalDragContainer00.Attribute("name").IsValid && pyHorizontalDragContainer00.Attribute("name").ToUnicodeString().ToLower() != "HorizontalDragContainer".ToLower())
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != HorizontalDragContainer");
                                            return;
                                        }
                                        else
                                        {
                                            foreach (var pyObject in pyHorizontalDragContainer00.Attribute("children").Attribute("_childrenObjects").ToList().Where(i => i.Attribute("name").IsValid && i.Attribute("name").ToUnicodeString().ToLower() == "mainCont".ToLower()))
                                            {
                                                //Full Path - example:
                                                //carbonui.uicore.uicore.registry.windows[7].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
                                                //.children._childrenObjects[0] - mainCont
                                                if (!pyObject.IsValid)
                                                {
                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: mainCont not found");
                                                    return;
                                                }

                                                if (pyObject.Attribute("name").IsValid && pyObject.Attribute("name").ToUnicodeString().ToLower() != "mainCont".ToLower())
                                                {
                                                    if (DebugConfig.DebugProbeScanner && DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name [" + pyObject.Attribute("name").ToUnicodeString() + "] != mainCont");
                                                    return;
                                                }
                                                else
                                                {
                                                    //Full Path - example:
                                                    //carbonui.uicore.uicore.registry.windows[7].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
                                                    //.children._childrenObjects[0] - PanelsContainer
                                                    var pyPanelsContainer0010 = pyObject.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                    if (!pyPanelsContainer0010.IsValid)
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: PanelsContainer not found");
                                                        return;
                                                    }

                                                    if (pyPanelsContainer0010.Attribute("name").IsValid && pyPanelsContainer0010.Attribute("name").ToUnicodeString().ToLower() != "PanelsContainer".ToLower())
                                                    {
                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != PanelsContainer");
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        //Full Path - example:
                                                        //carbonui.uicore.uicore.registry.windows[7].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
                                                        //.children._childrenObjects[1] - mainCont
                                                        var pymainContainer00101 = pyPanelsContainer0010.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                                        if (!pymainContainer00101.IsValid)
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: mainCont not found");
                                                            return;
                                                        }

                                                        if (pymainContainer00101.Attribute("name").IsValid && pymainContainer00101.Attribute("name").ToUnicodeString().ToLower() != "mainCont".ToLower())
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != mainCont");
                                                            return;
                                                        }
                                                        else
                                                        {
                                                            //Full Path - example:
                                                            //carbonui.uicore.uicore.registry.windows[7].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
                                                            //.children._childrenObjects[0] - firstPanelMainCont
                                                            foreach (var pyfirstPanelMainCont001010 in pymainContainer00101.Attribute("children").Attribute("_childrenObjects").ToList().Where(i => i.Attribute("name").IsValid && i.Attribute("name").ToUnicodeString().ToLower() == "firstPanelMainCont".ToLower()))
                                                            {
                                                                if (!pyfirstPanelMainCont001010.IsValid)
                                                                {
                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: firstPanelMainCont not found");
                                                                    return;
                                                                }

                                                                if (pyfirstPanelMainCont001010.Attribute("name").IsValid && pyfirstPanelMainCont001010.Attribute("name").ToUnicodeString().ToLower() != "firstPanelMainCont".ToLower())
                                                                {
                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != firstPanelMainCont");
                                                                    return;
                                                                }
                                                                else
                                                                {
                                                                    //Full Path - example:
                                                                    //carbonui.uicore.uicore.registry.windows[7].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
                                                                    //.children._childrenObjects[1] - probeScannerPalette
                                                                    var pyprobeScannerPalette0010101 = pyfirstPanelMainCont001010.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                                                                    if (!pyprobeScannerPalette0010101.IsValid)
                                                                    {
                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ProbeScannerPalette not found");
                                                                        return;
                                                                    }

                                                                    if (pyprobeScannerPalette0010101.Attribute("name").IsValid && pyprobeScannerPalette0010101.Attribute("name").ToUnicodeString().ToLower() != "ProbeScannerPalette".ToLower())
                                                                    {
                                                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != ProbeScannerPalette");
                                                                        return;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (DebugConfig.DebugProbeScanner && DirectEve.Interval(60000)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ProbeScannerPalette found");
                                                                        pyProbeScannerPalette = pyprobeScannerPalette0010101;

                                                                        //Full path
                                                                        //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
                                                                        //Path broken down
                                                                        //
                                                                        //.children._childrenObjects[0] - ProbeScannerPalette
                                                                        //.children._childrenObjects[2] - ScanResultsContainer
                                                                        //.children._childrenObjects[0] - resultScroll
                                                                        //.children._childrenObjects[1] - maincontainer
                                                                        //.children._childrenObjects[1] - __clipper
                                                                        //.children._childrenObjects[0] - __content
                                                                        //.children._childrenObjects.ToList() - scan results here!
                                                                        var pyScanResultsContainer2202 = pyProbeScannerPalette.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                                                                        if (!pyScanResultsContainer2202.IsValid)
                                                                        {
                                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ScanResultsContainer not found");
                                                                            return;
                                                                        }

                                                                        if (pyScanResultsContainer2202.Attribute("name").IsValid && pyScanResultsContainer2202.Attribute("name").ToUnicodeString().ToLower() != "ScanResultsContainer".ToLower())
                                                                        {
                                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != ScanResultsContainer");
                                                                            return;
                                                                        }
                                                                        else
                                                                        {
                                                                            //full path example:
                                                                            //carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0]
                                                                            //
                                                                            //relative path:
                                                                            //.children._childrenObjects[0] - ProbeScannerPalette
                                                                            //.children._childrenObjects[2] - ScanResultsContainer
                                                                            //.children._childrenObjects[0] - resultScroll
                                                                            var pyresultScroll22020 = pyScanResultsContainer2202.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                            if (!pyresultScroll22020.IsValid)
                                                                            {
                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: resultScroll not found");
                                                                                return;
                                                                            }

                                                                            if (pyresultScroll22020.Attribute("name").IsValid && pyresultScroll22020.Attribute("name").ToUnicodeString().ToLower() != "resultScroll".ToLower())
                                                                            {
                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != resultScroll");
                                                                                return;
                                                                            }
                                                                            else
                                                                            {
                                                                                List<PyObject> ListOfPyObjectsUnderResultScroll = pyresultScroll22020.Attribute("children").Attribute("_childrenObjects").ToList();
                                                                                foreach (var PyObjectUnderResultScroll220201 in ListOfPyObjectsUnderResultScroll)
                                                                                {
                                                                                    //full path example:
                                                                                    //carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
                                                                                    //
                                                                                    //relative path:
                                                                                    //.children._childrenObjects[0] - ProbeScannerPalette
                                                                                    //.children._childrenObjects[2] - ScanResultsContainer
                                                                                    //.children._childrenObjects[0] - resultScroll
                                                                                    //.children._childrenObjects[1] - maincontainer
                                                                                    if (PyObjectUnderResultScroll220201.Attribute("name").IsValid && PyObjectUnderResultScroll220201.Attribute("name").ToUnicodeString().ToLower() == "maincontainer".ToLower())
                                                                                    {
                                                                                        var pymaincontainer = PyObjectUnderResultScroll220201;
                                                                                        //.children._childrenObjects[1] - __clipper
                                                                                        List<PyObject> ListOfPyObjectsUnderMainContainer = pymaincontainer.Attribute("children").Attribute("_childrenObjects").ToList();
                                                                                        foreach (var PyObjectUnderMainContainer in ListOfPyObjectsUnderMainContainer)
                                                                                        {
                                                                                            //full path example:
                                                                                            //carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
                                                                                            //
                                                                                            //relative path:
                                                                                            //.children._childrenObjects[0] - ProbeScannerPalette
                                                                                            //.children._childrenObjects[2] - ScanResultsContainer
                                                                                            //.children._childrenObjects[0] - resultScroll
                                                                                            //.children._childrenObjects[1] - maincontainer
                                                                                            //.children._childrenObjects[1] - __clipper
                                                                                            if (PyObjectUnderMainContainer.Attribute("name").IsValid && PyObjectUnderMainContainer.Attribute("name").ToUnicodeString().ToLower() == "__clipper".ToLower())
                                                                                            {
                                                                                                var py__clipper2202011 = PyObjectUnderMainContainer;
                                                                                                //full path example:
                                                                                                //carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
                                                                                                //
                                                                                                //relative path:
                                                                                                //.children._childrenObjects[0] - ProbeScannerPalette
                                                                                                //.children._childrenObjects[2] - ScanResultsContainer
                                                                                                //.children._childrenObjects[0] - resultScroll
                                                                                                //.children._childrenObjects[1] - maincontainer
                                                                                                //.children._childrenObjects[1] - __clipper
                                                                                                //.children._childrenObjects[0] - __content
                                                                                                var py__content = py__clipper2202011.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                                                                if (!py__content.IsValid)
                                                                                                {
                                                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: __content not found");
                                                                                                    return;
                                                                                                }

                                                                                                if (py__content.Attribute("name").IsValid && py__content.Attribute("name").ToUnicodeString().ToLower() != "__content".ToLower())
                                                                                                {
                                                                                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != __content");
                                                                                                    return;
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    //full path example:
                                                                                                    //carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects
                                                                                                    //
                                                                                                    //relative path:
                                                                                                    //.children._childrenObjects[0] - ProbeScannerPalette
                                                                                                    //.children._childrenObjects[2] - ScanResultsContainer
                                                                                                    //.children._childrenObjects[0] - resultScroll
                                                                                                    //.children._childrenObjects[1] - maincontainer
                                                                                                    //.children._childrenObjects[1] - __clipper
                                                                                                    //.children._childrenObjects[0] - __content
                                                                                                    //.children._childrenObjects.ToList() - scan results here!
                                                                                                    List<PyObject> ListOfPyObjectScanResults = py__content.Attribute("children").Attribute("_childrenObjects").ToList();
                                                                                                    foreach (var PyObjectScanResult in ListOfPyObjectScanResults)
                                                                                                    {
                                                                                                        DirectProbeScannerWindowScanResult newProbeScannerWindowScanResult = new DirectProbeScannerWindowScanResult(directEve);
                                                                                                        //
                                                                                                        //columns
                                                                                                        //
                                                                                                        if (PyObjectScanResult.Attribute("groupColumn").IsValid)
                                                                                                        {
                                                                                                            PyObject pygroupColumn = PyObjectScanResult.Attribute("groupColumn");
                                                                                                            if (pygroupColumn.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                                                                                                            {
                                                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Container");
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                if (pygroupColumn.Attribute("label").IsValid && pygroupColumn.Attribute("label").Attribute("display").ToBool())
                                                                                                                {
                                                                                                                    //text == Combat Site
                                                                                                                    //text == Wormhole
                                                                                                                    newProbeScannerWindowScanResult.GroupName = pygroupColumn.Attribute("label").Attribute("text").ToUnicodeString();
                                                                                                                }
                                                                                                            }
                                                                                                        }

                                                                                                        if (PyObjectScanResult.Attribute("idColumn").IsValid)
                                                                                                        {
                                                                                                            PyObject pyidColumn = PyObjectScanResult.Attribute("idColumn");
                                                                                                            if (pyidColumn.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                                                                                                            {
                                                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Container");
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                if (pyidColumn.Attribute("label").IsValid && pyidColumn.Attribute("label").Attribute("display").ToBool())
                                                                                                                {
                                                                                                                    //text == Combat Site
                                                                                                                    //text == Wormhole
                                                                                                                    newProbeScannerWindowScanResult.ID = pyidColumn.Attribute("label").Attribute("text").ToUnicodeString();
                                                                                                                }
                                                                                                            }
                                                                                                        }

                                                                                                        if (PyObjectScanResult.Attribute("distanceColumn").IsValid)
                                                                                                        {
                                                                                                            PyObject pydistanceColumn = PyObjectScanResult.Attribute("distanceColumn");
                                                                                                            if (pydistanceColumn.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                                                                                                            {
                                                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Container");
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                if (pydistanceColumn.Attribute("label").IsValid && pydistanceColumn.Attribute("label").Attribute("display").ToBool())
                                                                                                                {
                                                                                                                    //text == Combat Site
                                                                                                                    //text == Wormhole
                                                                                                                    newProbeScannerWindowScanResult.Distance = pydistanceColumn.Attribute("label").Attribute("text").ToUnicodeString();
                                                                                                                }
                                                                                                            }
                                                                                                        }

                                                                                                        if (PyObjectScanResult.Attribute("nameColumn").IsValid)
                                                                                                        {
                                                                                                            PyObject pynameColumn = PyObjectScanResult.Attribute("nameColumn");
                                                                                                            if (pynameColumn.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                                                                                                            {
                                                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Container");
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                if (pynameColumn.Attribute("label").IsValid && pynameColumn.Attribute("label").Attribute("display").ToBool())
                                                                                                                {
                                                                                                                    //text == Combat Site
                                                                                                                    //text == Wormhole
                                                                                                                    newProbeScannerWindowScanResult.Name = pynameColumn.Attribute("label").Attribute("text").ToUnicodeString();
                                                                                                                }
                                                                                                            }
                                                                                                        }

                                                                                                        if (PyObjectScanResult.Attribute("signalColumn").IsValid)
                                                                                                        {
                                                                                                            PyObject pysignalColumn = PyObjectScanResult.Attribute("signalColumn");
                                                                                                            if (pysignalColumn.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                                                                                                            {
                                                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Container");
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                if (pysignalColumn.Attribute("label").IsValid && pysignalColumn.Attribute("label").Attribute("display").ToBool())
                                                                                                                {
                                                                                                                    //text == Combat Site
                                                                                                                    //text == Wormhole
                                                                                                                    //newProbeScannerWindowScanResult.Color = pysignalColumn.Attribute("label").Attribute("text").ToUnicodeString();
                                                                                                                }
                                                                                                            }
                                                                                                        }

                                                                                                        if (PyObjectScanResult.Attribute("colorColumn").IsValid)
                                                                                                        {
                                                                                                            PyObject pycolorColumn = PyObjectScanResult.Attribute("colorColumn");
                                                                                                            if (pycolorColumn.Attribute("name").ToUnicodeString().ToLower() != "Container".ToLower())
                                                                                                            {
                                                                                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Container");
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                if (pycolorColumn.Attribute("label").IsValid && pycolorColumn.Attribute("label").Attribute("display").ToBool())
                                                                                                                {
                                                                                                                    //text == Combat Site
                                                                                                                    //text == Wormhole
                                                                                                                    //newProbeScannerWindowScanResult.Color = pysignalColumn.Attribute("label").Attribute("text").ToUnicodeString();
                                                                                                                }
                                                                                                            }
                                                                                                        }

                                                                                                        Scanner.ProbeScannerWindowScanResults.Add(newProbeScannerWindowScanResult);
                                                                                                    }

                                                                                                    //
                                                                                                    // carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                                                                                                    // Container
                                                                                                    // carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                                                                                                    // Container
                                                                                                    // carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
                                                                                                    // EveLabelMedium
                                                                                                    // carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
                                                                                                    // Container
                                                                                                    //
                                                                                                    // carbonui.uicore.uicore.registry.windows[12].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
                                                                                                    // statusBar
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

        #endregion Constructors

        public PyObject pyProbeScannerPalette { get; internal set; }

        #region Properties

        #endregion Properties

        #region Methods

        #endregion Methods
    }
}