extern alias SC;

using EVESharpCore.Lookup;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Xml;

namespace EVESharpCore.Framework
{
    public class DirectHackingWindow : DirectWindow
    {
        //https://www.youtube.com/watch?v=rFXLEDn9CmU
        //

        // Relic / data guide - Eve online - Before 8 it's bait
        // https://www.youtube.com/watch?v=SeEW0_3ksX0
        //
        // Nodes (All, even non active ones?)
        // SpawnLocation
        // ActiveNodes
        // EmptySpace?
        //
        // game board is 10 wide and 9 high (always?!)
        //
        // Virus Health
        //          Skills
        //              Archeology / Hacking: +30 at level 3
        //              Archeology / Hacking: +50 at level 5
        //          Modules:
        //              T1 Relic / data analyzer + 40
        //              T2 Relic / data analyzer + 60
        //          Rigs:
        //              Small Emission Scope Sharpener I + 10
        //              Small Memetic Algorithm Bank I + 10
        //
        // Virus Strength -
        //          Calculated
        //          Toon (skills): Maxed Alpha
        //          Module:
        //              T1 relic / data analyzer: +20
        //              T2 relic / data analyzer: +30
        //
        //          Ship:
        //              Magnate / Heron / Imicus / Probe: +5
        //          Total: 25? virus strength - this seems backwards might need to look this up separately
        //
        //
        // Utility Slot1
        // Utility Slot2
        // Utility Slot3
        //
        // Nodes when activated can be:
        // Defense System
        // Disabled Node
        // White Node (surprise packages) - when selected defense system or utility?
        // Number indicating nearest utility / white node / core
        //
        // There are 4 levels/flavors of this mini game: 1 easiest to 4 hardest
        // Core - Attributes 50/10(blue?), 70/10(blue?), 70/10 (yellow), 90/10 (red)
        // Firewall - Attributes 40/20, 60/20, 80/20, 90/20
        // Antivirus - Attributes 30/30, 30/40, 50/40, 60/40
        // Healer - Attributes - n/a, n/a, 80/10, 80/10
        // Supressor - Attributes - n/a, n/a, n/a, 60/15
        //
        // Utilities: These help the virus: you are the virus!
        // Repair - Heals your virus for 5-10 + full map bonus, for 3 turns: good against Core, Healer
        // 50/50 - Deals 50% of a defense systems health: good against Firewall
        // Shield - Prevent the next 2 instances of damage to your virus: good against AntiVirus
        // Hawkeye - Deal 60 Damage over the next 3 turns: good against Antivirus and Suppressor
        //
        // Defense: You are fighting against these: you are the virus!
        // Firewall - Attributes 90/20 - High health, moderate attack
        // Antivirus - Attributes 60/40 - Low Health, high attack
        // Healer - Attributes 80/10 - Gives another defense system +20 health at the end of your turn
        // Suppressor - Attributes 60/15 - Lowers your virus strength by -15. Your virus cannot go below 10 strength
        //
        // At the hardest level - level 4:
        // The core is at least 8 nodes away from the spawn location
        //
        // Where the span location is located is important
        // If the spawn location is in the middle the core can be anywhere
        // If the spawn location is on any of the edges or corners it makes calculating where the core can be and cannot be easier
        // If its possible to put the core 8 or more from the spawn location it WILL BE 8 or more from the spawn location.
        // If the spawn location is such that 8 or more from there is not possible then the core will be randomly placed
        //
        //
        //
        //
        // The 1st node you click will never be a defense system! nearest the spawn location...
        //
        // Rule of 6:
        // A node connected to 6 other nodes cannot be a:
        //      defense system unless it is adjacent to the core!
        //      also cannot be a utility or white node
        //
        // White nodes have a maximum of 3 connections: if a mode has more than 3 connections it cannot be a white node
        // therefore: a node with 3 or less connections has a higher chance to contain a white node or a utility
        //
        // Before 8 its bait:
        //      Core is at least 8 nodes from spawn (by air)
        //      Use the rule of 6 to spot the safest path
        //      Try to fight as little as possible
        //      Dont get distracted by utilities too much
        //      Use numbers to find the core
        //
        // UtliltyElement
        // Tile
        //
        //
        // ----------------------------------------------------
        // ----------------------------------------------------
        // hackingUISvc.py
        //
        // class HackingSvc(service.Service):
        // __guid__ = 'svc.hackingUI'
        // __servicename__ = 'hackingUI'
        //
        //
        // def QuitHackingAttempt(self):
        // self.hackingMgr.QuitHackingAttempt()
        //
        // def OnTileClicked(self, tileCoord): - used to click on tiles!
        //
        // ----------------------------------------------------
        // -----------------------------

        //
        // Rule of 6 - a node connected to 6 other nodes cannot be a defense system unless it is adjacent to the core!
        //
        //
        // Dimensions of the game board: 10 wide, 9 high
        //
        // Core - Attributes 50/10(blue?), 70/10(blue?), 70/10 (yellow), 90/10 (red)

        //carbonui.uicore.uicore.registry.windows[13]
        //.children._childrenObjects 0,1,2,3
        //.children._childrenObjects[2]
        //content
        //.children._childrenObjects[2]
        //main
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
        //bottomCont
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
        //boardTransform
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
        //boardContainer
        // width //self.boardContainer.width = x * hackingUIConst.GRID_X + hackingUIConst.TILE_SIZE
        // height //self.boardContainer.height = y * hackingUIConst.GRID_Y + hackingUIConst.TILE_SIZE
        // top //self.boardContainer.top = 16 + offsetY * hackingUIConst.GRID_Y
        // left //self.boardContainer.left = 15 + offsetX * hackingUIConst.GRID_X
        //text @ Endgame //text = localization.GetByLabel('UI/Hacking/HackSuccess') if won else localization.GetByLabel('UI/Hacking/HackFailed')
        //
        //

        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0 - 150?
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
        //Tile or Vectorline
        //tileType = self.tileData.type
        //tileSubType = self.tileData.subtype
        //
        //bgColor = self.bgColor = self.GetTileBackgroundColor()
        //
        //def GetTileBackgroundColor(self):
        //tileType = self.tileData.type
        //if tileType == hackingConst.TYPE_CORE:
        //    return hackingUIConst.COLOR_BY_SUBTYPE[self.tileData.subtype]
        //elif self.tileData.blocked and tileType == hackingConst.TYPE_NONE:
        //    return hackingUIConst.COLOR_DEFENSE
        //elif not self.tileData.IsFlippable():
        //    return hackingUIConst.COlOR_UNREACHABLE
        //else:
        //    return hackingUIConst.COLOR_TILE_BG_BY_TYPE.get(tileType, hackingUIConst.COLOR_UNFLIPPED)
        //
        //
        //
        //
        //
        //
        //
        //display (bool)
        //conerenaceCont
        //distanceIndicatorCont
        //healingGivenSprite
        //healingrecievedSprite
        //HideCoherance()
        //HideStrength()
        //OnClick()
        //opacity = 1?
        //pickState = 1
        //state = 0
        //top  = 106
        //utilElementMarketSprite
        //

        //tiledata
        //  blocked
        //  coherence
        //  coord - 0 = 6, 1 = 2
        //  distanceIndicator
        //  hidden (bool)
        //  id (-1?)
        //  strength (int)
        //  subtype (int) -1?
        //  type (int) -1?
        //  neighborTiles 0,1,2,?
        // 0 = tiledata for that tile
        // 1 = tiledata for that tile
        // 2 = tiledata for that tile
        //
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[10].children._childrenObjects[0]
        //mouseHoverSprite
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[10].children._childrenObjects[1]
        //iconSprite
        //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[10].children._childrenObjects[2]
        //tileBgTransform

        #region Constructors



        internal DirectHackingWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            try
            {
                //Full path
                //carbonui.uicore.uicore.registry.windows[13].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0 - 150?
                //broken down path
                //carbonui.uicore.uicore.registry.windows[13]
                //.children._childrenObjects[2] - content
                //.children._childrenObjects[2] - main
                //.children._childrenObjects[1] - boardTransform
                //.children._childrenObjects[0] - boardContainer
                //.children._childrenObjects 0 - 150? - Tile or Vectorline
                //
                if (pyWindow.Attribute("name").IsValid && pyWindow.Attribute("name").ToUnicodeString().ToLower() == "HackingWindow".ToLower())
                {
                    var pyContent2 = pyWindow.Attribute("children").Attribute("_childrenObjects").GetItemAt(2);
                    if (!pyContent2.IsValid)
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Content not found");
                        return;
                    }

                    //carbonui.uicore.uicore.registry.windows[11]
                    //.children._childrenObjects[2] - content
                    if (pyContent2.Attribute("name").IsValid && pyContent2.Attribute("name").ToUnicodeString().ToLower() != "Content".ToLower())
                    {
                        if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != Content");
                        return;
                    }
                    else
                    {
                        if (DebugConfig.DebugHackingWindow && DirectEve.Interval(6000)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Found content");
                        //carbonui.uicore.uicore.registry.windows[11]
                        //.children._childrenObjects[2] - content
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
                            if (DebugConfig.DebugHackingWindow && DirectEve.Interval(6000)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Found main");
                            //carbonui.uicore.uicore.registry.windows[13]
                            //.children._childrenObjects[2] - content
                            //.children._childrenObjects[2] - main
                            //.children._childrenObjects[1] - boardTransform
                            var pyboardTransform221 = pyMain22.Attribute("children").Attribute("_childrenObjects").GetItemAt(1);
                            if (!pyboardTransform221.IsValid)
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: boardTransform not found");
                                return;
                            }

                            if (pyboardTransform221.Attribute("name").IsValid && pyboardTransform221.Attribute("name").ToUnicodeString().ToLower() != "boardTransform".ToLower())
                            {
                                if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != boardTransform");
                                return;
                            }
                            else
                            {
                                if (DebugConfig.DebugHackingWindow && DirectEve.Interval(6000)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Found boardTransform");
                                //carbonui.uicore.uicore.registry.windows[13]
                                //.children._childrenObjects[2] - content
                                //.children._childrenObjects[2] - main
                                //.children._childrenObjects[1] - boardTransform
                                //.children._childrenObjects[0] - boardContainer
                                var pyboardContainer2210 = pyboardTransform221.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                                if (!pyboardContainer2210.IsValid)
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: boardContainer not found");
                                    return;
                                }

                                if (pyboardContainer2210.Attribute("name").IsValid && pyboardContainer2210.Attribute("name").ToUnicodeString().ToLower() != "boardContainer".ToLower())
                                {
                                    if (DirectEve.Interval(10000, 10000, WindowId)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Name != boardContainer");
                                    return;
                                }
                                else
                                {

                                    if (DebugConfig.DebugHackingWindow && DirectEve.Interval(6000)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Found boardContainer");
                                    //.children._childrenObjects 0 - 150? - Tile or Vectorline
                                    //
                                    //
                                    List<PyObject> ListOfTilesAndOrLines = pyboardContainer2210.Attribute("children").Attribute("_childrenObjects").ToList();
                                    if (ListOfTilesAndOrLines.Any())
                                    {
                                        if (DebugConfig.DebugHackingWindow && DirectEve.Interval(6000)) Logging.Log.WriteLine("WindowID [" + this.WindowId + "] GUID [" + this.Guid + "]: Found [" + ListOfTilesAndOrLines.Count() + "] tiles");
                                        foreach (var TileAndOrLine in ListOfTilesAndOrLines)
                                        {
                                            if (TileAndOrLine.Attribute("name").ToUnicodeString().ToLower() == "Vectorline".ToLower())
                                            {
                                                try
                                                {
                                                    var thisDirectHackingLine = new DirectHackingLine(DirectEve, TileAndOrLine);
                                                    if (thisDirectHackingLine != null)
                                                    {
                                                        ListOfDirectHackingLines.Add(thisDirectHackingLine);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logging.Log.WriteLine("Exception [" + ex + "]");
                                                }
                                            }
                                            else if (TileAndOrLine.Attribute("name").ToUnicodeString().ToLower() == "Tile".ToLower())
                                            {
                                                try
                                                {
                                                    var thisDirectHackingTile = new DirectHackingTile(DirectEve, TileAndOrLine);
                                                    if (thisDirectHackingTile != null)
                                                    {
                                                        ListOfDirectHackingTiles.Add(thisDirectHackingTile);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logging.Log.WriteLine("Exception [" + ex + "]");
                                                }
                                            }
                                            else Logging.Log.WriteLine("Unknown Tile or Line [" + TileAndOrLine.Attribute("name").ToUnicodeString() + "]");
                                        }


                                    }
                                }
                            }
                        }
                    }
                }

                if (DirectEve.Interval(6000) && DebugConfig.DebugHackingWindow)
                {
                    Logging.Log.WriteLine("ListOfDirectHackingTiles [" + ListOfDirectHackingTiles.Count() + "]");
                    Logging.Log.WriteLine("ListOfDirectHackingTiles");
                    int iCount = 0;
                    foreach (var IndividualHackingTile in ListOfDirectHackingTiles)
                    {
                        iCount++;
                        Logging.Log.WriteLine("[" + iCount + "][" + IndividualHackingTile.tileData.id + "] NeighborsCount [" + IndividualHackingTile.tileData.NeighborsCount + "] hidden [" + IndividualHackingTile.tileData.hidden + "] blocked [" + IndividualHackingTile.tileData.blocked + "] type [" + IndividualHackingTile.tileData.type + "] subtype [" + IndividualHackingTile.tileData.subtype + "] strength [" + IndividualHackingTile.tileData.strength + "] coherence [" + IndividualHackingTile.tileData.coherence + "] coord [" + IndividualHackingTile.tileData.CoordAsString + "]");
                        foreach (var NeighborTile in IndividualHackingTile.tileData.NeighborTiles)
                        {
                            Logging.Log.WriteLine("--- [" + iCount + "][" + NeighborTile.id + "] NeighborsCount [" + NeighborTile.NeighborsCount + "] hidden [" + NeighborTile.hidden + "] blocked [" + NeighborTile.blocked + "] type [" + NeighborTile.type + "] subtype [" + NeighborTile.subtype + "] strength [" + NeighborTile.strength + "] coherence [" + NeighborTile.coherence + "] coord [" + NeighborTile.CoordAsString + "]");
                        }
                    }

                    //Logging.Log.WriteLine("ListOfDirectHackingLines [" + ListOfDirectHackingLines.Count() + "]");
                    //Logging.Log.WriteLine("ListOfDirectHackingLines");
                    //iCount = 0;
                    //foreach (var IndividualHackingLine in ListOfDirectHackingLines)
                    //{
                    //    iCount++;
                    //    Logging.Log.WriteLine("[" + iCount + "] p0 [" + IndividualHackingLine.p0 + "] p1 [" + IndividualHackingLine.p1 + "]");
                    //}
                }
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
            }
        }

        List<DirectHackingLine> ListOfDirectHackingLines = new List<DirectHackingLine>();
        List<DirectHackingTile> ListOfDirectHackingTiles = new List<DirectHackingTile>();

        #endregion Constructors

        #region Methods






        #endregion Methods
    }
}