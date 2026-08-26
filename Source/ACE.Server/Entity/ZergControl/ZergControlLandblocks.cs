using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ACE.Server.Entity
{
    /// <summary>
    /// Defines "zerg control" areas.  A ZergControlArea is a group of one or more landblocks
    /// that share a single per-allegiance player cap.  Enforcement happens in two places:
    ///   - On a landblock tick (Landblock.HandleZergControl): every player across all landblocks
    ///     in the area is bucketed by allegiance, and any allegiance over its cap has its excess
    ///     players (the most recently teleported ones) booted to their lifestone.
    ///   - When a player teleports into / logs into one of these landblocks
    ///     (Player.IsInZergRestrictedEntry / Player.EvaluateZergEntry): the player is blocked if
    ///     their own allegiance is already at the cap.
    ///
    /// The map contains permanent areas (e.g. the Abandoned Mine / Subway) plus dynamic areas
    /// added at runtime — currently the active Hot Dungeons, which HotDungeonManager adds when a
    /// dungeon becomes hot and removes when it is no longer hot. Reads happen on landblock ticks
    /// (possibly parallel) while writes happen on the world tick, so the map is a
    /// ConcurrentDictionary.
    ///
    /// NOTE: every landblock listed in a ZergControlArea's AreaLandblockIds must ALSO be added as
    /// a key in the map (pointing at the same area) so a tick on any of them enforces the whole area.
    /// </summary>
    public static class ZergControlLandblocks
    {
        /// <summary>
        /// Per-allegiance cap inside an Allegiance Hometown meeting hall. The halls are permanently
        /// zerg-controlled, not only while a Phase 2 conflict is live — a hall is a small dungeon with
        /// one entrance, so an uncapped allegiance could simply wall it off at any time.
        /// </summary>
        private const uint MeetingHallMaxPerAllegiance = 7;

        /// <summary>Landblocks that are permanently zerg-controlled and can never be removed at runtime.</summary>
        private static readonly HashSet<uint> _permanent = BuildPermanentSet();

        private static readonly ConcurrentDictionary<uint, ZergControlArea> _zergControlLandblocksMap = BuildInitialMap();

        private static HashSet<uint> BuildPermanentSet()
        {
            // Abandoned Mine (Subway)
            var set = new HashSet<uint> { 0x01C9 };

            // Every Allegiance Hometown meeting hall. Marking them permanent stops AddDynamicLandblock /
            // RemoveDynamicLandblock (Hot Dungeons) from overriding the cap or dropping the area entirely.
            foreach (var town in AllegianceHometown.AllegianceHometownRegistry.All.Values)
                set.Add(town.HallLandblockId);

            return set;
        }

        private static ConcurrentDictionary<uint, ZergControlArea> BuildInitialMap()
        {
            var map = new ConcurrentDictionary<uint, ZergControlArea>();

            // Abandoned Mine (Subway) — permanent.
            map[0x01C9] = new ZergControlArea
            {
                MaxPlayersPerAllegiance = 5,
                AreaLandblockIds = new uint[] { 0x01C9 },
            };

            // Allegiance Hometown meeting halls (0x011D..0x0135) — permanent, one area each so the caps
            // are independent: an allegiance holding Holtburg's hall does not consume Arwic's allowance.
            // Derived from the town registry rather than hardcoded so the two can never drift apart.
            foreach (var town in AllegianceHometown.AllegianceHometownRegistry.All.Values)
            {
                map[town.HallLandblockId] = new ZergControlArea
                {
                    MaxPlayersPerAllegiance = MeetingHallMaxPerAllegiance,
                    AreaLandblockIds = new uint[] { town.HallLandblockId },
                };
            }

            return map;
        }

        public static IReadOnlyDictionary<uint, ZergControlArea> ZergControlLandblocksMap => _zergControlLandblocksMap;

        public static bool IsZergControlLandblock(uint landblockId)
        {
            return _zergControlLandblocksMap.ContainsKey(landblockId);
        }

        public static ZergControlArea GetLandblockZergControlArea(uint landblockId)
        {
            return _zergControlLandblocksMap.TryGetValue(landblockId, out var area) ? area : null;
        }

        /// <summary>
        /// Adds (or updates) a single-landblock dynamic zerg control area — e.g. a Hot Dungeon while
        /// it is active. No-op for permanent landblocks so their configured cap is never overridden.
        /// </summary>
        public static void AddDynamicLandblock(uint landblockId, uint maxPlayersPerAllegiance)
        {
            if (_permanent.Contains(landblockId))
                return;

            _zergControlLandblocksMap[landblockId] = new ZergControlArea
            {
                MaxPlayersPerAllegiance = maxPlayersPerAllegiance,
                AreaLandblockIds = new uint[] { landblockId },
            };
        }

        /// <summary>
        /// Removes a dynamic zerg control area (e.g. a Hot Dungeon that is no longer hot).
        /// Permanent landblocks (e.g. the Subway) are never removed.
        /// </summary>
        public static void RemoveDynamicLandblock(uint landblockId)
        {
            if (_permanent.Contains(landblockId))
                return;

            _zergControlLandblocksMap.TryRemove(landblockId, out _);
        }
    }

    public class ZergControlArea
    {
        /// <summary>
        /// All landblocks that make up this area.  Players across every one of these landblocks
        /// are counted together against MaxPlayersPerAllegiance.
        /// </summary>
        public uint[] AreaLandblockIds;

        /// <summary>
        /// The maximum number of players a single allegiance may have inside this area.
        /// </summary>
        public uint MaxPlayersPerAllegiance;
    }
}
