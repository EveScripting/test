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
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectHackingTile : DirectObject
    {
        #region Fields

        private readonly PyObject _pyHackingTile;

        #endregion Fields

        #region Constructors

        internal DirectHackingTile(DirectEve directEve, PyObject pyHackingTile)
            : base(directEve)
        {
            try
            {
                _pyHackingTile = pyHackingTile;
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private DirectHackingTileData _tileData = null;

        public DirectHackingTileData tileData
        {
            get
            {
                if (_tileData != null)
                    return _tileData;

                _tileData = new DirectHackingTileData(DirectEve, _pyHackingTile.Attribute("tileData"));
                return _tileData;
            }
        }

        #endregion Constructors

        #region Properties

        //TileType
        //
        /**
         * hackingUIConst.py
         *
        HINTS_BY_TILE_TYPE = {hackingConst.TYPE_NONE: 'UI/Hacking/UnknownNode',
         hackingConst.TYPE_SEGMENT: 'UI/Hacking/SegmentNode',
         hackingConst.TYPE_VIRUS: 'UI/Hacking/Virus',
         hackingConst.TYPE_CORE: 'UI/Hacking/CoreNode',                                         //2
         hackingConst.TYPE_DEFENSESOFTWARE: 'UI/Hacking/DefenseNode',
         hackingConst.TYPE_UTILITYELEMENTTILE: 'UI/Hacking/UtilityNode',
         hackingConst.TYPE_DATACACHE: 'UI/Hacking/DataCacheNode'}                               //6
            DS_HINTS_BY_SUBTYPE = {hackingConst.SUBTYPE_DS_FIREWALL: 'UI/Hacking/FirewallNode',
         hackingConst.SUBTYPE_DS_ANTIVIRUS: 'UI/Hacking/AntiVirusNode',
         hackingConst.SUBTYPE_DS_HONEYPOT_STRENGTH: 'UI/Hacking/HoneyPotStrength',
         hackingConst.SUBTYPE_DS_HONEYPOT_HEALING: 'UI/Hacking/HoneyPotHealing',
         hackingConst.SUBTYPE_DS_DISRUPTOR: '/UI/Hacking/DisruptorNode'}
        UE_HINTS_BY_SUBTYPE = {
            hackingConst.SUBTYPE_UE_KERNALROT: 'UI/Hacking/UtilityKernalRot',
         hackingConst.SUBTYPE_UE_SELFREPAIR: 'UI/Hacking/UtilitySelfRepair',
         hackingConst.SUBTYPE_UE_SECONDARYVECTOR: 'UI/Hacking/UtilitySecondaryVector',
         hackingConst.SUBTYPE_UE_POLYMORPHICSHIELD: 'UI/Hacking/UtilityPolymorphicShield'}
        ICONPATH_BY_SUBTYPE = {
            hackingConst.SUBTYPE_UE_SELFREPAIR: 'res:/UI/Texture/classes/hacking/utilSelfRepair.png',
         hackingConst.SUBTYPE_UE_KERNALROT: 'res:/UI/Texture/classes/hacking/utilKernalRot.png',
         hackingConst.SUBTYPE_UE_SECONDARYVECTOR: 'res:/UI/Texture/classes/hacking/utilSecondVector.png',
         hackingConst.SUBTYPE_UE_POLYMORPHICSHIELD: 'res:/UI/Texture/classes/hacking/utilPolymorphShield.png',
         hackingConst.SUBTYPE_DS_FIREWALL: 'res:/UI/Texture/classes/hacking/defSoftFirewall.png',
         hackingConst.SUBTYPE_DS_ANTIVIRUS: 'res:/UI/Texture/classes/hacking/defSoftAntiVirus.png',
         hackingConst.SUBTYPE_DS_HONEYPOT_STRENGTH: 'res:/UI/Texture/classes/hacking/defSoftHoneyPotStrength.png',
         hackingConst.SUBTYPE_DS_HONEYPOT_HEALING: 'res:/UI/Texture/classes/hacking/defSoftHoneyPotHealing.png',
         hackingConst.SUBTYPE_DS_DISRUPTOR: 'res:/UI/Texture/classes/hacking/defSoftIds.png',
         hackingConst.SUBTYPE_CORE_LOW: 'res:/UI/Texture/classes/hacking/coreLow.png',
         hackingConst.SUBTYPE_CORE_MEDIUM: 'res:/UI/Texture/classes/hacking/coreMedium.png',
         hackingConst.SUBTYPE_CORE_HIGH: 'res:/UI/Texture/classes/hacking/coreHigh.png'}
        **/

        //
        //
        //
        //GetNeighbors
        //IsFlippable
        //  hackingTileData.py
        //  def IsFlippable(self):
        //      for neighbour in self.neighbourTiles:
        //           if not neighbour.hidden and not neighbour.blocked:
        //                return True
        //GetXY
        //GetHexXY
        //
        //
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


        public long distanceIndicator
        {
            get
            {
                return 0; // _pyHackingTile.Attribute("distanceIndicator").ToLong();
            }
        }

        public bool Unflipped
        {
            get
            {
                if (tileData.type == -1) //hackingConst.TYPE_NONE
                {
                    if (tileData.blocked)
                        return false;

                    if (tileData.IsFlippable)
                        return true;

                    return false;
                }

                return false;
            }
        }

        /**
         *
         * def GetHexXY(self):
                hexX, hexY = self.coord
                if hexY % 2:
                    hexX += 0.5
                return (hexX, hexY)

            def GetXY(self):
                hexX, hexY = self.GetHexXY()
                x = hexX * hackingUIConst.GRID_X
                y = hexY * hackingUIConst.GRID_Y
                return (x, y)
         *
         * **/

        //def GetNeighbours(self):
        //  return self.neighbourTiles

        public string Text { get; internal set; }

        #endregion Properties

        #region Methods

        public bool Click()
        {
            if (DateTime.UtcNow < Time.Instance.LastWindowInteraction.AddSeconds(3))
                return false;

            if (DirectEve.ThreadedCall(_pyHackingTile.Attribute("OnClick")))
            {
                Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        #endregion Methods
    }
}