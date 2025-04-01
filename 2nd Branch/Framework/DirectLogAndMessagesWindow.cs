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
    public class DirectLogAndMessagesWindow : DirectWindow
    {
        #region Fields

        #endregion Fields

        #region Constructors

        //of course this only shows the messages that are loaded in the Log and Messages window!
        internal DirectLogAndMessagesWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            //carbonui.uicore.uicore.registry.windows[10]
            //logger //This is the Log and Messages window
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
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //ContainerAutosize
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //MenuButtonIcon - more here! probably UI at the top that we dont need to interact with
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //ContainerAutoSize - more here! probably UI at the top that we dont need to interact with
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
            //scroll
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //mainContainer
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //scrollHeaders
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //ScrollBar
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //ScrollHandle
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //fill
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //fill
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //__clipper
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //__content
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //entry_0
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //entryLabel - this is the oldest, not the newest entry...
            //text = example: "Jumping from Perimeter to Jita"
            //
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //entry_1
            //entryLabel - the higher the entry number the newer the entry
            //text = example: "Jumping from Perimeter to Jita"
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //entry_2
            //entryLabel - the higher the entry number the newer the entry
            //text = example: "Jumping from Perimeter to Jita"
            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //entry_3
            //entryLabel - the higher the entry number the newer the entry
            //text = example: "Jumping from Perimeter to Jita"


            //
            //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[3]
            //underlay
            //



            PyWindow = pyWindow;
            try
            {
                //
                //List of log messages
                //
                //Full path to the probe scanner window - when in its own window
                //carbonui.uicore.uicore.registry.windows[11].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3
                //path broken down
                //carbonui.uicore.uicore.registry.windows[11] - logger
                //.children._childrenObjects[2] - content
                //.children._childrenObjects[2] - main
                //.children._childrenObjects[1] - scroll
                //.children._childrenObjects[0] - mainContainer
                //.children._childrenObjects[2] - __clipper
                //.children._childrenObjects[0] - __content
                //.children._childrenObjects 0,1,2,3
                //
                //
                //ProbeScannerPalette
                if (pyWindow.Attribute("name").IsValid && pyWindow.Attribute("name").ToUnicodeString().ToLower() == "logger".ToLower())
                {
                    //.children._childrenObjects[2] - content
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
                            //.children._childrenObjects[1] - scroll
                            var pyscroll221 = pyMain22.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                            if (!pyscroll221.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: scroll not found");
                                return;
                            }

                            if (pyscroll221.Attribute("name").IsValid && pyscroll221.Attribute("name").ToUnicodeString().ToLower() != "scroll".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != scroll");
                                return;
                            }
                            else
                            {
                                //.children._childrenObjects[0] - mainContainer
                                var pymainContainer2210 = pyscroll221.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                if (!pymainContainer2210.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: maincontainer not found");
                                    return;
                                }

                                if (pymainContainer2210.Attribute("name").IsValid && pymainContainer2210.Attribute("name").ToUnicodeString().ToLower() != "maincontainer".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != maincontainer");
                                    return;
                                }
                                else
                                {
                                    //.children._childrenObjects[2] - __clipper
                                    var py__clipper22102 = pymainContainer2210.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                                    if (!py__clipper22102.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: __clipper not found");
                                        return;
                                    }

                                    if (py__clipper22102.Attribute("name").IsValid && py__clipper22102.Attribute("name").ToUnicodeString().ToLower() != "__clipper".ToLower())
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != __clipper");
                                        return;
                                    }
                                    else
                                    {
                                        //.children._childrenObjects[0] - __content
                                        var py__content221020 = py__clipper22102.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                        if (!py__content221020.IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: __content not found");
                                            return;
                                        }

                                        if (py__content221020.Attribute("name").IsValid && py__content221020.Attribute("name").ToUnicodeString().ToLower() != "__content".ToLower())
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != __content");
                                            return;
                                        }
                                        else
                                        {
                                            //.children._childrenObjects 0,1,2,3, etc...
                                            var pyListOfMessageEntries = py__content221020.Attribute("children").Attribute("_childrenObjects").ToList();
                                            int iCount = 0;
                                            if (DirectEve.Interval(10000) && DebugConfig.DebugLogAndMessagesWindow) Log.WriteLine("LogAndMessagesWindow: Found [" + pyListOfMessageEntries.Count + "] entries");
                                            foreach (var pyMessageEntry in pyListOfMessageEntries)
                                            {
                                                iCount++;
                                                if (pyMessageEntry.Attribute("name").IsValid)
                                                {
                                                    if (pyMessageEntry.Attribute("name").ToUnicodeString().ToLower().Contains("entry_".ToLower()))
                                                    {
                                                        //.children._childrenObjects[0] - __content
                                                        var pyMessage = pyMessageEntry.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                                        if (!pyMessage.IsValid)
                                                        {
                                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: !pyMessage.IsValid");
                                                            return;
                                                        }

                                                        if (pyMessage.Attribute("name").IsValid && pyMessage.Attribute("name").ToUnicodeString().ToLower() == "entryLabel".ToLower())
                                                        {
                                                            if (pyMessageEntry.Attribute("text").IsValid)
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, pyMessageEntry.Attribute("name").ToUnicodeString() + iCount) && DebugConfig.DebugLogAndMessagesWindow) Logging.Log.WriteLine("LogAndMessagesWindow: [" + iCount + "][" + pyMessage.Attribute("text").ToUnicodeString() + "]");
                                                            }
                                                            else
                                                            {
                                                                if (DirectEve.Interval(10000, 10000, pyMessageEntry.Attribute("name").ToUnicodeString() + iCount) && DebugConfig.DebugLogAndMessagesWindow) Logging.Log.WriteLine("LogAndMessagesWindow: [" + iCount + "][text not found]");
                                                            }
                                                        }
                                                    }
                                                    else if (DirectEve.Interval(10000, 10000, pyMessageEntry.Attribute("name").ToUnicodeString() + iCount) && DebugConfig.DebugLogAndMessagesWindow) Logging.Log.WriteLine("LogAndMessagesWindow: [" + iCount + "][" + pyMessageEntry.Attribute("name").ToUnicodeString() + "]!!!");
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

        #endregion Constructors

        #region Properties


        #endregion Properties

        #region Methods


        #endregion Methods
    }
}