using System.Collections.Generic;

using ACE.Entity;

namespace ACE.Server.Entity.AllegianceHometown
{
    /// <summary>
    /// Static registry of all capturable towns and their bindstone positions.
    /// Keyed by TownId (matches allegiance_hometown_town.town_id).
    /// </summary>
    public static class AllegianceHometownRegistry
    {
        /// <summary>
        /// Meeting halls occupy their own dedicated dungeon landblocks, numbered in the same order as
        /// the town table below: Al-Arqas (TownId 1) is 0x011D through Zaikhal (TownId 25) is 0x0135.
        /// Verified against the entry portals (wcids 6088-6112), each of which is placed on its town's
        /// landblock and lands in the matching hall.
        /// </summary>
        private const uint HallLandblockBase = 0x011Cu;

        // All 25 hall interiors are structurally identical - same cells, same object origins, only the
        // exit portal wcid differs - so one cell offset and one position serve every town.
        //
        // Centre of the refreshing pool on the hall's main floor, taken from Holtburg (TownId 9):
        //   0x01250104 [9.895210 -30.003195 0.005000] 0.709234 0.000000 0.000000 -0.704974
        // Note @loc prints the quaternion as W X Y Z while the Position ctor takes X Y Z W.
        private const uint  Phase2CellOffset = 0x0104u;
        private const float Phase2X = 9.895210f;
        private const float Phase2Y = -30.003195f;
        private const float Phase2Z = 0.005000f;
        private const float Phase2RotationX = 0f;
        private const float Phase2RotationY = 0f;
        private const float Phase2RotationZ = -0.704974f;
        private const float Phase2RotationW = 0.709234f;

        public class TownEntry
        {
            public byte   TownId        { get; }
            public string TownName      { get; }
            /// <summary>Outdoor town landblock. Phase 0 (idle) and Phase 1 run here.</summary>
            public uint   LandblockId   { get; }   // upper 16 bits of obj_cell_id
            /// <summary>Indoor meeting hall landblock. Phase 2 runs here.</summary>
            public uint   HallLandblockId { get; }
            public uint   BindstoneWcid { get; }
            /// <summary>The real outdoor bind stone. Phase 1 proximity is measured from this.</summary>
            public Position BindstonePosition { get; }
            /// <summary>Where the Phase 2 bind stone proxy spawns inside the meeting hall.</summary>
            public Position Phase2Position    { get; }

            public TownEntry(byte townId, string townName, uint objCellId, float x, float y, float z)
            {
                TownId           = townId;
                TownName         = townName;
                LandblockId      = objCellId >> 16;
                HallLandblockId  = HallLandblockBase + townId;
                BindstoneWcid    = 27547;
                BindstonePosition = new Position(objCellId, x, y, z, 0f, 0f, 0f, 1f);
                Phase2Position    = new Position((HallLandblockId << 16) | Phase2CellOffset,
                                                 Phase2X, Phase2Y, Phase2Z,
                                                 Phase2RotationX, Phase2RotationY, Phase2RotationZ, Phase2RotationW);
            }
        }

        private static readonly Dictionary<byte, TownEntry> _byId   = new Dictionary<byte, TownEntry>();
        private static readonly Dictionary<uint, TownEntry> _byLandblock = new Dictionary<uint, TownEntry>();
        private static readonly Dictionary<uint, TownEntry> _byHallLandblock = new Dictionary<uint, TownEntry>();

        static AllegianceHometownRegistry()
        {
            var towns = new[]
            {
                new TownEntry( 1, "Al-Arqas",     0x90580012, 142.744f,  89.052f,   0.005f),
                new TownEntry( 2, "Al-Jalima",    0x85890012, 121.536f,  16.192f,  86.005f),
                new TownEntry( 3, "Arwic",        0xC5A90012,  38.592f,  88.652f,  52.005f),
                new TownEntry( 4, "Baishi",       0xCD410012,  59.472f, 116.820f,  54.005f),
                new TownEntry( 5, "Cragstone",    0xBC9F0012, 160.467f, 169.838f,  32.005f),
                new TownEntry( 6, "Eastham",      0xCE950012, 158.128f,  95.061f,  20.005f),
                new TownEntry( 7, "Glenden Wood", 0xA1A40012, 141.670f, 163.534f,  50.005f),
                new TownEntry( 8, "Hebian-to",    0xE74E0012, 119.401f,  54.415f,  32.005f),
                new TownEntry( 9, "Holtburg",     0xA9B40012, 144.989f,  41.548f,  94.005f),
                new TownEntry(10, "Khayyaban",    0x9D430012, 187.625f,  44.155f,  40.005f),
                new TownEntry(11, "Lin",          0xDA3A0012, 179.175f, 190.571f,   0.005f),
                new TownEntry(12, "Lytelthorpe",  0xBF800012, 191.860f,  89.490f,  34.005f),
                new TownEntry(13, "Mayoi",        0xE5320012, 123.212f, 146.872f,  28.005f),
                new TownEntry(14, "Nanto",        0xE63D0012, 106.468f, 143.584f,  86.872f),
                new TownEntry(15, "Qalaba'r",     0x97220012,  36.857f, 144.922f, 102.005f),
                new TownEntry(16, "Rithwic",      0xC88D0012,  46.284f,  10.585f,  22.005f),
                new TownEntry(17, "Samsur",       0x987B0012,  17.036f, 167.747f,   0.005f),
                new TownEntry(18, "Sawato",       0xC95B0012, 116.226f, 154.406f,  12.005f),
                new TownEntry(19, "Shoushi",      0xDA550012,  93.573f, 173.178f,  20.005f),
                new TownEntry(20, "Tou-Tou",      0xF65C0012, 113.874f,  23.120f,  20.005f),
                new TownEntry(21, "Tufa",         0x856D0012, 128.821f, 189.221f,  14.005f),
                new TownEntry(22, "Uziz",         0xA25F0012, 154.634f, 177.720f,  20.005f),
                new TownEntry(23, "Yanshi",       0xB9720012, 134.025f, 159.987f,  40.005f),
                new TownEntry(24, "Yaraq",        0x7D640012,  54.162f,  33.492f,  12.005f),
                new TownEntry(25, "Zaikhal",      0x8090001A,  75.524f,  32.495f, 124.005f),
            };

            foreach (var t in towns)
            {
                _byId[t.TownId]         = t;
                _byLandblock[t.LandblockId] = t;
                _byHallLandblock[t.HallLandblockId] = t;
            }
        }

        public static IReadOnlyDictionary<byte, TownEntry> All => _byId;

        public static TownEntry GetById(byte townId)
        {
            _byId.TryGetValue(townId, out var t);
            return t;
        }

        public static TownEntry GetByLandblock(uint landblockId)
        {
            _byLandblock.TryGetValue(landblockId, out var t);
            return t;
        }

        /// <summary>Resolves a meeting hall landblock back to its town. Phase 2 lookups use this.</summary>
        public static TownEntry GetByHallLandblock(uint landblockId)
        {
            _byHallLandblock.TryGetValue(landblockId, out var t);
            return t;
        }

        public static bool IsTownLandblock(uint landblockId) => _byLandblock.ContainsKey(landblockId);

        public static bool IsHallLandblock(uint landblockId) => _byHallLandblock.ContainsKey(landblockId);
    }
}
