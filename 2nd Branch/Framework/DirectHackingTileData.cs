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

    public class DirectHackingTileData : DirectObject
    {
        #region Fields

        private PyObject _pyHackingTileData = null;

        #endregion Fields

        #region Constructors

        internal DirectHackingTileData(DirectEve directEve, PyObject pyHackingTileData)
            : base(directEve)
        {
            _pyHackingTileData = pyHackingTileData;
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

        public long id
        {
            get
            {
                if (_pyHackingTileData == null)
                    return -999;

                if (!_pyHackingTileData.HasAttrString("id"))
                    return -999;

                //example: -1
                return (long)_pyHackingTileData.Attribute("id").ToLong();
            }
        }

        public List<PyObject> pyCoord
        {
            get
            {
                if (_pyHackingTileData == null)
                    return new List<PyObject>();

                if (!_pyHackingTileData.HasAttrString("coord"))
                    return new List<PyObject>();

                //0 = (int) ex: 6,
                //1 = (int) ex: 2
                return _pyHackingTileData.Attribute("coord").ToList();
            }
        }

        public List<int> _coord = new List<int>();

        public List<int> Coord
        {
            get
            {
                if (pyCoord == null || pyCoord.Count != 2)
                    return new List<int>();

                _coord = new List<int>();
                //0 = (int) ex: 6,
                //1 = (int) ex: 2
                foreach (var coord in pyCoord)
                {
                    if (coord == null)
                        return new List<int>();

                    _coord.Add(coord.ToInt());
                }

                return _coord;
            }
        }

        public string CoordAsString
        {
            get
            {
                if (Coord == null || Coord.Count != 2)
                    return string.Empty;

                return string.Format("{0}, {1}", Coord[0], Coord[1]);
            }
        }

        public bool blocked
        {
            get
            {
                if (_pyHackingTileData == null)
                    return false;

                if (!_pyHackingTileData.HasAttrString("blocked"))
                    return false;

                return _pyHackingTileData.Attribute("blocked").ToBool();
            }
        }

        public int coherence
        {
            get
            {
                if (_pyHackingTileData == null)
                    return -999;

                if (!_pyHackingTileData.HasAttrString("coherence"))
                    return -999;

                return _pyHackingTileData.Attribute("coherence").ToInt();
            }
        }

        public int strength
        {
            get
            {
                if (_pyHackingTileData == null)
                    return -999;

                if (!_pyHackingTileData.HasAttrString("strength"))
                    return -999;

                return _pyHackingTileData.Attribute("strength").ToInt();
            }
        }

        public int subtype
        {
            get
            {
                if (_pyHackingTileData == null)
                    return -999;

                if (!_pyHackingTileData.HasAttrString("subtype"))
                    return -999;

                return _pyHackingTileData.Attribute("subtype").ToInt();
            }
        }

        public int type
        {
            get
            {
                if (_pyHackingTileData == null)
                    return -999;

                if (!_pyHackingTileData.HasAttrString("type"))
                    return -999;

                return _pyHackingTileData.Attribute("type").ToInt();
            }
        }

        public List<PyObject> pyNeighborTiles
        {
            get
            {
                if (_pyHackingTileData == null)
                    return new List<PyObject>();

                if (!_pyHackingTileData.Attribute("neighborTiles").IsValid)
                    return new List<PyObject>();

                return _pyHackingTileData.Attribute("neighborTiles").ToList();
            }
        }

        public List<DirectHackingTileData> _neighborTiles = new List<DirectHackingTileData>();

        public List<DirectHackingTileData> NeighborTiles
        {
            get
            {
                if (!pyNeighborTiles.Any())
                    return new List<DirectHackingTileData>();


                _neighborTiles = new List<DirectHackingTileData>();
                foreach (var pyNeighborTileData in pyNeighborTiles)
                {
                    if (pyNeighborTileData == null)
                        return new List<DirectHackingTileData>();

                    _neighborTiles.Add(new DirectHackingTileData(DirectEve, pyNeighborTileData));
                }

                return _neighborTiles;
            }
        }

        public int NeighborsCount
        {
            get
            {
                if (NeighborTiles == null)
                    return 0;

                if (!NeighborTiles.Any())
                    return 0;

                return NeighborTiles.Count;
            }
        }

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

        public bool hidden
        {
            get
            {
                if (_pyHackingTileData == null)
                    return false;

                if (!_pyHackingTileData.HasAttrString("hidden"))
                    return false;

                return _pyHackingTileData.Attribute("hidden").ToBool();
            }
        }

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
                if (type == -1) //hackingConst.TYPE_NONE
                {
                    if (blocked)
                        return false;

                    if (IsFlippable)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsFlippable
        {
            get
            {
                foreach (var NeighborTile in NeighborTiles)
                {
                    if (!NeighborTile.hidden)
                    {
                        if (!NeighborTile.blocked)
                        {
                            return true;
                        }
                    }
                }

                return false; //DirectEve.ThreadedCall(_pyHackingTile.Attribute("IsFlippable"));
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

        #endregion Methods
    }
}