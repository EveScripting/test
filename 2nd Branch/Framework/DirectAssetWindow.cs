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
    public class DirectAssetWindow : DirectWindow
    {
        #region Fields

        #endregion Fields

        #region Constructors

        internal DirectAssetWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            //carbonui.uicore.uicore.registry.windows[8]
            //assets //This is the Personal Assets window
            //
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
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //searchCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //comboCont
            //more here
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //sortcombosearch
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //bottomCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //infoIcon
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //searchButton

            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
            //scroll
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //maincontainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //scrollHeaders
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //Scrollbar
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //ScrollHandle
            //
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //__clipper
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //__content
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3, etc
            //This is where all the locations and that locations assets are listed!
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //entry_0 - Plant name and name of station?
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3, etc?
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //mainLinePar
            //nothing alse here?
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //expanderParent
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //expander
            //nothing else here?
            //
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //icon
            //
            //
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //labelClipper
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //EveLabelMedium
            //example: text = "<color=#ff61dba4>0.7</color>Akianvas III - School of Applied knowledge - 9 items - Route: 1 Jump"
            //
            //
            //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[3]
            //underlay
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[3].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[3].children._childrenObjects[0]
            //line
            //
            //carbonui.uicore.uicore.registry.windows[6].children._childrenObjects[3].children._childrenObjects[1]
            //framesprite



            PyWindow = pyWindow;
            try
            {

            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Constructors

        #region Properties


        #endregion Properties

        #region Methods


        #endregion Methods
    }
}