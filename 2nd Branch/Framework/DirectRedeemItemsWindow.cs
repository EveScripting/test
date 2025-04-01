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

using EVESharpCore.Lookup;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum RedeemItemsButtonType
    {
        RedeemToCurrentStation,
        RedeemToHomeStation,
        SelectAll,
        UNKNOWN,
        None
    }

    public class DirectRedeemItemsWindow : DirectWindow
    {

        public List<DirectRedeemItemsButton> Buttons { get; internal set; }


        internal DirectRedeemItemsWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            if (!DebugConfig.RedeemItems)
                return;

            RedeemItemsButtonType GetButtonType(string s)
            {
                try
                {
                    if (string.IsNullOrEmpty(s))
                        return RedeemItemsButtonType.UNKNOWN;

                    //if (s.Contains("Sorry, I have no jobs available for you."))
                    //    return ButtonType.NO_JOBS_AVAILABLE;

                    if (s.ToLower().Contains("Redeem to Current Station".ToLower()))
                        return RedeemItemsButtonType.RedeemToCurrentStation;

                    if (s.ToLower().Contains("Redeem to Home Station".ToLower()))
                        return RedeemItemsButtonType.RedeemToHomeStation;

                    return RedeemItemsButtonType.UNKNOWN;
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine("Exception [" + ex + "]");
                    return RedeemItemsButtonType.UNKNOWN;
                }
            }

            Buttons = new List<DirectRedeemItemsButton>();

            //
            // Where is the select all button?
            //

            //carbonui.uicore.uicore.registry.windows[7]
            //redeem
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects 0, 1 ,2, 3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[0]
            //window_controls_cont
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[1]
            //resizer
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2]
            //content
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects 0, 1, 2
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[0]
            //__loadingParent
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //RedeemItemContainer
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //dragArea
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //dragContainer
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //FrameThemeColored
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //FillThemeColored
            //
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //Header
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //ControlContainer
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //footer
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4]
            //HomeStationSection
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //fill
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //ItemIcon
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //Button
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5]
            //CurrentStationSection
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0]
            //fill
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[1]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //ItemIcon
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[2]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //Button
            //
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[3]
            //ContainerAutoSize
            //
            //
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[6]
            //AutoInjectNotice
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7]
            //redeemItemsContainer
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0]
            //redeemItems
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0].children._childrenObjects[0]
            //scroll
            //carbonui.uicore.uicore.registry.windows[7].children._childrenObjects[2]
            //underlay

            //redeem To Current Station Button is:
            //carbonui.uicore.uicore.registry.windows[7] - redeem
            //.children._childrenObjects[2] - content
            //.children._childrenObjects[2] - main
            //.children._childrenObjects[0] - RedeemItemContainer
            //.children._childrenObjects[5] - CurrentStationSection
            //.children._childrenObjects[0] - ContainerAutoSize
            //.children._childrenObjects[2] - ContainerAutoSize
            //.children._childrenObjects[0] - Button

            //redeem To Home Station Button is:
            //carbonui.uicore.uicore.registry.windows[7]
            //.children._childrenObjects[2] - content
            //.children._childrenObjects[2] - main
            //.children._childrenObjects[0] - RedeemItemContainer
            //.children._childrenObjects[4] - HomeStationSection
            //.children._childrenObjects[0] - ContainerAutoSize
            //.children._childrenObjects[2] - ContainerAutoSize
            //.children._childrenObjects[0] - Button

            var main = pyWindow.Attribute("children").Attribute("_childrenObjects").GetItemAt(2).Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
            if (!main.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: main not found");
                return;
            }

            if (main.Attribute("name").IsValid && main.Attribute("name").ToUnicodeString().ToLower() != "main".ToLower())
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != main");
                return;
            }

            //RedeemItemContainer
            var RedeemItemContainer = main.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
            if (!RedeemItemContainer.IsValid)
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: RedeemItemContainer not found");
                return;
            }

            if (!RedeemItemContainer.Attribute("name").IsValid && RedeemItemContainer.Attribute("text").ToUnicodeString().ToLower() != "RedeemItemContainer".ToLower())
            {
                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != RedeemItemContainer");
                return;
            }
            else
            {
                //HomeStationSection
                var HomeStationSection = RedeemItemContainer.Attribute("children").Attribute("_childrenObjects").GetItemAt(4);
                if (!HomeStationSection.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: HomeStationSection not found");
                }

                if (!HomeStationSection.Attribute("name").IsValid && HomeStationSection.Attribute("name").ToUnicodeString().ToLower() != "CurrentStationSection".ToLower())
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != CurrentStationSection");
                }
                else
                {
                    //ContainerAutoSize #1 for HomeStationSection
                    var ContainerAutoSize1HomeStation = HomeStationSection.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                    if (!ContainerAutoSize1HomeStation.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize not found");
                    }

                    if (!ContainerAutoSize1HomeStation.Attribute("name").IsValid && ContainerAutoSize1HomeStation.Attribute("name").ToUnicodeString().ToLower() != "ContainerAutoSize".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ContainerAutoSize");
                    }
                    else
                    {
                        //ContainerAutoSize #2 for HomeStationSection
                        var ContainerAutoSize2HomeStation = ContainerAutoSize1HomeStation.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                        if (!ContainerAutoSize2HomeStation.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize not found");
                        }

                        if (!ContainerAutoSize2HomeStation.Attribute("name").IsValid && ContainerAutoSize2HomeStation.Attribute("name").ToUnicodeString().ToLower() != "ContainerAutoSize".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ContainerAutoSize");
                        }
                        else
                        {
                            //redeemToHomeStationButton
                            var redeemToHomeStationButton = ContainerAutoSize2HomeStation.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                            if (redeemToHomeStationButton == null)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: redeemToHomeStationButton == null");
                            }

                            if (redeemToHomeStationButton != null && !redeemToHomeStationButton.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: redeemToHomeStationButton not found");
                            }
                            else
                            {
                                if (redeemToHomeStationButton != null)
                                {
                                    if (!redeemToHomeStationButton.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("redeemToHomeStationButton !IsValid");
                                    }

                                    if (redeemToHomeStationButton.IsValid)
                                    {
                                        if (!redeemToHomeStationButton.Attribute("text").IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: if (!redeemToHomeStationButton.Attribute(\"text\").IsValid");
                                        }

                                        if (redeemToHomeStationButton.Attribute("text").IsValid)
                                        {
                                            if (!redeemToHomeStationButton.Attribute("text").ToUnicodeString().ToLower().Contains("Home".ToLower()))
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: redeemToHomeStationButton.Attribute(text).ToUnicodeString() [" + redeemToHomeStationButton.Attribute("text").ToUnicodeString() + "]");
                                            }
                                            else
                                            {
                                                if (redeemToHomeStationButton.Attribute("enabled").ToBool() && redeemToHomeStationButton.Attribute("display").ToBool())
                                                {
                                                    DirectRedeemItemsButton redeemToHomeStationDirectRedeemItemsButton = new DirectRedeemItemsButton(directEve, redeemToHomeStationButton);
                                                    redeemToHomeStationDirectRedeemItemsButton.Text = (string)redeemToHomeStationButton.Attribute("text");
                                                    redeemToHomeStationDirectRedeemItemsButton.Type = GetButtonType(redeemToHomeStationDirectRedeemItemsButton.Text);
                                                    redeemToHomeStationDirectRedeemItemsButton.ButtonName = (string)redeemToHomeStationButton.Attribute("RedeemToCurrentStation");
                                                    if (DebugConfig.DebugWindows)
                                                    {
                                                        if (DirectEve.Interval(15000, 15000, WindowId)) Logging.Log.WriteLine("button Text [" + redeemToHomeStationDirectRedeemItemsButton.Text + "] Type [" + redeemToHomeStationDirectRedeemItemsButton.Type + "] ButtonName [" + redeemToHomeStationDirectRedeemItemsButton.ButtonName + "]");
                                                    }

                                                    if (redeemToHomeStationDirectRedeemItemsButton.Type != RedeemItemsButtonType.UNKNOWN)
                                                        Buttons.Add(redeemToHomeStationDirectRedeemItemsButton);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //CurrentStationSection
                var CurrentStationSection = RedeemItemContainer.Attribute("children").Attribute("_childrenObjects").GetItemAt(5);
                if (!CurrentStationSection.IsValid)
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: CurrentStationSection not found");
                }

                if (!CurrentStationSection.Attribute("name").IsValid && CurrentStationSection.Attribute("name").ToUnicodeString().ToLower() != "HomeStationSection".ToLower())
                {
                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != HomeStationSection");
                }
                else
                {
                    //ContainerAutoSize #1 for CurrentStationSection
                    var ContainerAutoSize1CurrentStation = CurrentStationSection.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                    if (!ContainerAutoSize1CurrentStation.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize not found");
                    }

                    if (!ContainerAutoSize1CurrentStation.Attribute("name").IsValid && ContainerAutoSize1CurrentStation.Attribute("name").ToUnicodeString().ToLower() != "ContainerAutoSize".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ContainerAutoSize");
                    }
                    else
                    {
                        //ContainerAutoSize #2 for CurrentStationSection
                        var ContainerAutoSize2CurrentStation = ContainerAutoSize1CurrentStation.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                        if (!ContainerAutoSize2CurrentStation.IsValid)
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: ContainerAutoSize not found");
                        }

                        if (!ContainerAutoSize2CurrentStation.Attribute("name").IsValid && ContainerAutoSize2CurrentStation.Attribute("name").ToUnicodeString().ToLower() != "ContainerAutoSize".ToLower())
                        {
                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: name != ContainerAutoSize");
                        }
                        else
                        {
                            //redeemToCurrentStationButton
                            var redeemToCurrentStationButton = ContainerAutoSize2CurrentStation.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);

                            if (redeemToCurrentStationButton == null)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: redeemToCurrentStationButton == null");
                            }

                            if (redeemToCurrentStationButton != null && !redeemToCurrentStationButton.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: redeemToCurrentStationButton not found");
                            }
                            else
                            {
                                if (redeemToCurrentStationButton != null)
                                {
                                    if (!redeemToCurrentStationButton.IsValid)
                                    {
                                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("redeemToCurrentStationButton !IsValid");
                                    }

                                    if (redeemToCurrentStationButton.IsValid)
                                    {
                                        if (!redeemToCurrentStationButton.Attribute("text").IsValid)
                                        {
                                            if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: if (!redeemToCurrentStationButton.Attribute(\"text\").IsValid");
                                        }

                                        if (redeemToCurrentStationButton.Attribute("text").IsValid)
                                        {
                                            if (!redeemToCurrentStationButton.Attribute("text").ToUnicodeString().ToLower().Contains("Current".ToLower()))
                                            {
                                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: redeemToCurrentStationButton.Attribute(text).ToUnicodeString() [" + redeemToCurrentStationButton.Attribute("text").ToUnicodeString() + "]");
                                            }
                                            else
                                            {
                                                if (redeemToCurrentStationButton.Attribute("enabled").ToBool() && redeemToCurrentStationButton.Attribute("display").ToBool())
                                                {
                                                    DirectRedeemItemsButton redeemToCurrentStationDirectredeemItemsButton = new DirectRedeemItemsButton(directEve, redeemToCurrentStationButton);
                                                    redeemToCurrentStationDirectredeemItemsButton.Text = (string)redeemToCurrentStationButton.Attribute("text");
                                                    redeemToCurrentStationDirectredeemItemsButton.Type = GetButtonType(redeemToCurrentStationDirectredeemItemsButton.Text);
                                                    redeemToCurrentStationDirectredeemItemsButton.ButtonName = (string)redeemToCurrentStationButton.Attribute("RedeemToCurrentStation");
                                                    if (DebugConfig.DebugWindows)
                                                    {
                                                        if (DirectEve.Interval(15000, 15000, WindowId)) Logging.Log.WriteLine("button Text [" + redeemToCurrentStationDirectredeemItemsButton.Text + "] Type [" + redeemToCurrentStationDirectredeemItemsButton.Type + "] ButtonName [" + redeemToCurrentStationDirectredeemItemsButton.ButtonName + "]");
                                                    }

                                                    if (redeemToCurrentStationDirectredeemItemsButton.Type != RedeemItemsButtonType.UNKNOWN)
                                                        Buttons.Add(redeemToCurrentStationDirectredeemItemsButton);
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