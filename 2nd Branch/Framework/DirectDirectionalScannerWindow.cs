extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public class DirectDirectionalScannerWindow : DirectWindow
    {
        #region Fields

        #endregion Fields

        #region Constructors

        internal DirectDirectionalScannerWindow(DirectEve directEve, PyObject pyWindow)
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
                //
                //Directional Scanner as a separate window
                //
                //Full path to the probe scanner window - when in its own window
                //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
                //path broken down
                //carbonui.uicore.uicore.registry.windows[11] - directionalScannerWindow
                //.children._childrenObjects[2] - content
                //.children._childrenObjects[2] - main
                //.children._childrenObjects[0] - DirectionalScannerPalette
                //
                //DirectionalScannerPalette

                if (pyWindow.Attribute("name").IsValid && pyWindow.Attribute("name").ToUnicodeString().ToLower() == "directionalScannerWindow".ToLower())
                {
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
                        var pyMain22 = pyContent2.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                        if (!pyMain22.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: main not found");
                            return;
                        }

                        if (pyMain22.Attribute("name").IsValid && pyMain22.Attribute("name").ToUnicodeString().ToLower() != "main".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != main");
                            return;
                        }
                        else
                        {
                            var pyDirectionalScannerPalette2 = pyMain22.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                            if (!pyDirectionalScannerPalette2.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: DirectionalScannerPalette not found");
                                return;
                            }

                            if (pyDirectionalScannerPalette2.Attribute("name").IsValid && pyDirectionalScannerPalette2.Attribute("name").ToUnicodeString().ToLower() != "DirectionalScannerPalette".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != DirectionalScannerPalette");
                                return;
                            }
                            else
                            {
                                pyDirectionalScannerPalette = pyDirectionalScannerPalette2;
                            }
                        }
                    }
                }
                else
                {
                    //
                    //Probe Scanner window docked in the map
                    //
                    //Full path to the probe scanner window - when docked in the map
                    //carbonui.uicore.uicore.registry.windows[10].mapView.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
                    //path broken down
                    //carbonui.uicore.uicore.registry.windows[10] - solar_system_map_panel
                    //.mapView                      - mapview
                    //.children._childrenObjects[0] - mainCont
                    //.children._childrenObjects[0] - HorizontalDragContainer
                    //.children._childrenObjects[1] - mainCont
                    //.children._childrenObjects[0] - PanelsContainer
                    //.children._childrenObjects[1] - mainCont
                    //.children._childrenObjects[0] - firstPanelMainCont
                    //.children._childrenObjects[1] - probeScannerPalette
                    //
                    //ProbeScannerPalette
                    //
                    //
                    DirectWindow _solarSystemMapPanelWindow = ESCache.Instance.Windows.OfType<DirectWindow>().Where(x => x.Name == "solar_system_map_panel").FirstOrDefault();
                    if (_solarSystemMapPanelWindow.WindowId == WindowId)
                    {
                        _solarSystemMapPanelWindow = this;
                    }

                    if (_solarSystemMapPanelWindow != null)
                    {
                        if (_solarSystemMapPanelWindow.PyWindow.Attribute("name").IsValid && _solarSystemMapPanelWindow.PyWindow.Attribute("name").ToUnicodeString().ToLower() == "solar_system_map_panel".ToLower())
                        {
                            //.mapView                      - mapview
                            var pymapView = _solarSystemMapPanelWindow.PyWindow.Attribute("mapView");
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
                                //.directionalScannerPalette - DirectionalScannerPalette
                                var pyDirectionalScannerPalette2 = pymapView.Attribute("directionalScannerPalette");
                                if (!pyDirectionalScannerPalette2.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: DirectionalScannerPalette not found");
                                    return;
                                }

                                if (pyDirectionalScannerPalette2.Attribute("name").IsValid && pyDirectionalScannerPalette2.Attribute("name").ToUnicodeString().ToLower() != "DirectionalScannerPalette".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != DirectionalScannerPalette");
                                    return;
                                }
                                else
                                {
                                    pyDirectionalScannerPalette = pyDirectionalScannerPalette2;
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

        #endregion Constructors

        #region Properties

        public PyObject pyDirectionalScannerPalette { get; internal set; }

        private List<DirectDirectionalScanResult> _scanResults;
        public List<DirectDirectionalScanResult> DirectionalScanResults
        {
            get
            {
                var charId = DirectEve.Session.CharacterId;
                if (_scanResults == null && charId != null)
                {
                    _scanResults = new List<DirectDirectionalScanResult>();
                    foreach (var result in pyDirectionalScannerPalette.Attribute("scanresult").Attribute("lines")
                        .ToList())
                    {
                        // scan result is a list of tuples
                        var resultAsList = result.ToList();
                        _scanResults.Add(new DirectDirectionalScanResult(DirectEve, resultAsList[0].ToLong(),
                            resultAsList[1].ToInt(), resultAsList[2].ToInt()));
                    }
                }

                return _scanResults;
            }
        }

        #endregion Properties

        #region Methods

        private ActionQueueAction _autoQueuedAction;

        private DateTime waitUntil = DateTime.UtcNow.AddDays(-1);

        public void DirectionalScan()
        {
            if (!IsDirectionalScanOpen())
                return;

            if (IsDirectionalScanning())
                return;

            DirectEve.ThreadedCall(pyDirectionalScannerPalette.Attribute("DirectionalScan"));
        }

        public void UpdateRangeInput(long scanRangeKM = 0)
        {
            if (scanRangeKM == 0)
                return;

            if (!IsDirectionalScanOpen())
                return;

            if (IsDirectionalScanning())
                return;

            DirectEve.ThreadedCall(pyDirectionalScannerPalette.Attribute("UpdateRangeInput"), scanRangeKM);
        }

        public void ScanTowardsItem(long itemID = 0)
        {
            if (itemID == 0)
                return;

            if (!IsDirectionalScanOpen())
                return;

            if (IsDirectionalScanning())
                return;

            DirectEve.GetLocalSvc("directionalScanSvc").Call("ScanTowardsItem", itemID);
        }

        public bool IsDirectionalScanning()
        {
            return DirectEve.GetLocalSvc("directionalScanSvc").Attribute("isScanning").ToBool();
        }

        public bool IsDirectionalScanOpen()
        {
            return pyDirectionalScannerPalette.IsValid;
        }

        #endregion Methods
    }
}