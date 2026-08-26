using ACE.Common;
using ACE.Common.Extensions;
using ACE.Common.Performance;
using ACE.Database;
using ACE.Database.Models.World;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages;
using ACE.Server.Pathfinding;
using ACE.Server.Physics.Common;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Position = ACE.Entity.Position;

namespace ACE.Server.Entity
{
    /// <summary>
    /// the gist of a landblock is that, generally, everything on it publishes
    /// to and subscribes to everything else in the landblock.  x/y in an outdoor
    /// landblock goes from 0 to 192.  "indoor" (dungeon) landblocks have no
    /// functional limit as players can't freely roam in/out of them
    /// </summary>
    public class Landblock : IActor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static float AdjacencyLoadRange { get; } = 96f;
        public static float OutdoorChatRange { get; } = 75f;
        public static float IndoorChatRange { get; } = 25f;
        public static float MaxXY { get; } = 192f;
        public static float MaxObjectRange { get; } = 192f;
        public static float MaxObjectGhostRange { get; } = 250f;


        public LandblockId Id { get; }

        /// <summary>
        /// Flag indicates if this landblock is permanently loaded (for example, towns on high-traffic servers)
        /// </summary>
        public bool Permaload { get; private set; } = false;

        /// <summary>
        /// UTC time at which a timed permaload expires, or null when the permaload is permanent
        /// (config preloads). Set this instead of holding a landblock open forever: if whatever
        /// asked for the permaload fails to clean up, the deadline still releases the landblock.
        /// </summary>
        public DateTime? PermaloadEndDate { get; private set; } = null;

        private readonly object permaloadLock = new object();

        /// <summary>
        /// Marks this landblock permaloaded so it neither goes dormant nor unloads while empty.
        /// A null endDate means permanent. A permanent permaload always wins over a timed one, and
        /// a timed request never shortens a deadline that is already further out — so overlapping
        /// callers can't cut each other short.
        /// </summary>
        public void SetPermaload(DateTime? endDate = null)
        {
            lock (permaloadLock)
            {
                if (endDate == null)
                {
                    PermaloadEndDate = null;
                    Permaload = true;
                    return;
                }

                // Already held open permanently — a deadline would only weaken it.
                if (Permaload && PermaloadEndDate == null)
                    return;

                if (PermaloadEndDate == null || endDate.Value > PermaloadEndDate.Value)
                    PermaloadEndDate = endDate;

                Permaload = true;
            }
        }

        /// <summary>
        /// Releases a timed permaload early. Permanent permaloads are left alone, so a caller
        /// tidying up its own temporary permaload can never unpin a landblock that the server
        /// config wants held open.
        /// </summary>
        public void ClearTimedPermaload()
        {
            lock (permaloadLock)
            {
                if (!Permaload || PermaloadEndDate == null)
                    return;

                Permaload = false;
                PermaloadEndDate = null;
            }
        }

        private void CheckPermaloadExpiry(DateTime thisHeartBeat)
        {
            lock (permaloadLock)
            {
                if (!Permaload || PermaloadEndDate == null || thisHeartBeat < PermaloadEndDate.Value)
                    return;

                log.Debug($"Landblock {Id.Landblock:X4}: timed permaload expired, releasing.");
                Permaload = false;
                PermaloadEndDate = null;
            }
        }

        /// <summary>
        /// Flag indicates if this landblock has no keep alive objects
        /// </summary>
        public bool HasNoKeepAliveObjects = true;

        /// <summary>
        /// This must be true before a player enters a landblock.
        /// This prevents a player from possibly pasing through a door that hasn't spawned in yet, and other scenarios.
        /// </summary>
        public bool CreateWorldObjectsCompleted { get; private set; }

        private DateTime lastActiveTime;

        /// <summary>
        /// Dormant landblocks suppress Monster AI ticking and physics processing
        /// </summary>
        public bool IsDormant;

        private readonly Dictionary<ObjectGuid, WorldObject> worldObjects = new Dictionary<ObjectGuid, WorldObject>();
        private readonly Dictionary<ObjectGuid, WorldObject> pendingAdditions = new Dictionary<ObjectGuid, WorldObject>();
        private readonly List<ObjectGuid> pendingRemovals = new List<ObjectGuid>();

        // Cache used for Tick efficiency
        private readonly List<Player> players = new List<Player>();
        private readonly LinkedList<Creature> sortedCreaturesByNextTick = new LinkedList<Creature>();
        private readonly LinkedList<WorldObject> sortedWorldObjectsByNextHeartbeat = new LinkedList<WorldObject>();
        private readonly LinkedList<WorldObject> sortedGeneratorsByNextGeneratorUpdate = new LinkedList<WorldObject>();
        private readonly LinkedList<WorldObject> sortedGeneratorsByNextRegeneration = new LinkedList<WorldObject>();

        /// <summary>
        /// This is used to detect and manage cross-landblock group (which is potentially cross-thread) operations.
        /// </summary>
        public LandblockGroup CurrentLandblockGroup { get; internal set; }

        public List<Landblock> Adjacents = new List<Landblock>();

        private readonly ActionQueue actionQueue = new ActionQueue();

        /// <summary>
        /// Landblocks heartbeat every 5 seconds
        /// </summary>
        private static readonly TimeSpan heartbeatInterval = TimeSpan.FromSeconds(5);

        private DateTime lastHeartBeat = DateTime.MinValue;

        /// <summary>
        /// When a per-object tick (Monster_Tick, GeneratorUpdate, GeneratorRegeneration, Heartbeat) throws, we push the
        /// object's next scheduled tick out by this many seconds. This keeps the object in the world while guaranteeing
        /// forward progress through the sorted tick lists, so a repeatably-throwing object can't spin the tick in-frame.
        /// </summary>
        private const double tickExceptionRetryInterval = 5.0;

        /// <summary>
        /// Landblock items will be saved to the database every 5 minutes
        /// </summary>
        private static readonly TimeSpan databaseSaveInterval = TimeSpan.FromMinutes(5);

        private DateTime lastDatabaseSave = DateTime.MinValue;

        // ZergControl enforcement tick (see HandleZergControl) fires every 5 seconds
        private DateTime lastZergControlTickDateTime = DateTime.MinValue;

        // Allegiance Hometown Phase 1 tick fires every 5 seconds on capturable-town landblocks
        private static readonly TimeSpan ahPhase1TickInterval = TimeSpan.FromSeconds(5);
        private DateTime lastAhPhase1Tick = DateTime.MinValue;
        private double _ahPhase1AccumulatedSeconds;
        private DateTime _lastAhPhase1Broadcast = DateTime.MinValue;
        private bool _phase1Interrupted = false;
        private double _phase1EnemyPresenceSeconds = 0; // accumulates while an enemy is within Phase1EnemyRadius
        private const double Phase1EnemyGraceSeconds = 30;

        // Phase 1 radii. Both the auto-start check (TryAutoStartPhase1) and the ongoing hold check
        // (HandleAllegianceHometownTick) read these, so start and hold can never drift apart.
        private const float Phase1AttackerRadius = 5f;    // attackers counted toward progress; 2+ required
        private const float Phase1EnemyRadius    = 10f;   // enemy presence that interrupts, and the
                                                          // auto-start "only one attacking allegiance" check

        // Phase 2 repel accumulator: seconds defenders have held the hall (2+ defenders, 0 non-defenders present).
        // Phase 2 is fought inside the town's meeting hall, so this state lives on the hall landblock and
        // presence in the hall replaces the old outdoor distance rings.
        private double _phase2RepelSeconds = 0;
        private DateTime _lastPhase2RepelBroadcast = DateTime.MinValue;

        private bool? _isAllegianceHometownTownLandblock;
        /// <summary>Outdoor town landblock — Phase 0 auto-start detection and Phase 1 run here.</summary>
        public bool IsAllegianceHometownTownLandblock
        {
            get
            {
                if (_isAllegianceHometownTownLandblock == null)
                    _isAllegianceHometownTownLandblock = ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.IsTownLandblock(Id.Landblock);
                return _isAllegianceHometownTownLandblock.Value;
            }
        }

        private bool? _isAllegianceHometownHallLandblock;
        /// <summary>Indoor meeting hall landblock — Phase 2 runs here.</summary>
        public bool IsAllegianceHometownHallLandblock
        {
            get
            {
                if (_isAllegianceHometownHallLandblock == null)
                    _isAllegianceHometownHallLandblock = ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.IsHallLandblock(Id.Landblock);
                return _isAllegianceHometownHallLandblock.Value;
            }
        }

        /// <summary>True for either half of a capturable town: the outdoor town or its meeting hall.</summary>
        public bool IsAllegianceHometownLandblock => IsAllegianceHometownTownLandblock || IsAllegianceHometownHallLandblock;

        /// <summary>
        /// Landblocks which have been inactive for this many seconds will be dormant
        /// </summary>
        private static readonly TimeSpan dormantInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Landblocks which have been inactive for this many seconds will be unloaded
        /// </summary>
        public static readonly TimeSpan UnloadInterval = TimeSpan.FromMinutes(30);


        /// <summary>
        /// The clientlib backing store landblock
        /// Eventually these classes could be merged, but for now they are separate...
        /// </summary>
        public Physics.Common.Landblock PhysicsLandblock { get; }

        public CellLandblock CellLandblock { get; }
        public LandblockInfo LandblockInfo { get; }

        /// <summary>
        /// The landblock static meshes for
        /// collision detection and physics simulation
        /// </summary>
        public LandblockMesh LandblockMesh { get; private set; }
        public List<ModelMesh> LandObjects { get; private set; }
        public List<ModelMesh> Buildings { get; private set; }
        public List<ModelMesh> WeenieMeshes { get; private set; }
        public List<ModelMesh> Scenery { get; private set; }


        public readonly RateMonitor Monitor5m = new RateMonitor();
        private readonly TimeSpan last5mClearInteval = TimeSpan.FromMinutes(5);
        private DateTime last5mClear;
        public readonly RateMonitor Monitor1h = new RateMonitor();
        private readonly TimeSpan last1hClearInteval = TimeSpan.FromHours(1);
        private DateTime last1hClear;
        private bool monitorsRequireEventStart = true;

        // Used for cumulative ServerPerformanceMonitor event recording
        private readonly Stopwatch stopwatch = new Stopwatch();


        private EnvironChangeType fogColor;

        public EnvironChangeType FogColor
        {
            get
            {
                if (LandblockManager.GlobalFogColor.HasValue)
                    return LandblockManager.GlobalFogColor.Value;

                return fogColor;
            }
            set => fogColor = value;
        }


        public Landblock(LandblockId id)
        {
            //log.DebugFormat("Landblock({0:X8})", (id.Raw | 0xFFFF));

            Id = id;

            CellLandblock = DatManager.CellDat.ReadFromDat<CellLandblock>(Id.Raw | 0xFFFF);
            LandblockInfo = DatManager.CellDat.ReadFromDat<LandblockInfo>((uint)Id.Landblock << 16 | 0xFFFE);

            lastActiveTime = DateTime.UtcNow;

            var cellLandblock = DBObj.GetCellLandblock(Id.Raw | 0xFFFF);
            PhysicsLandblock = new Physics.Common.Landblock(cellLandblock);
        }

        public void Init(bool reload = false)
        {
            if (!reload)
                PhysicsLandblock.PostInit();
            else
                Houses.Clear();

            Task.Run(() =>
            {
                CreateWorldObjects();

                SpawnDynamicShardObjects();

                SpawnEncounters();

                InitializeExplorationMarkers();
            });

            //LoadMeshes(objects);
        }


        public double NextExplorationMarkerRefresh;
        private double ExplorationMarkerRefreshInterval = 300;
        private List<Position> PositionsForExplorationMarkers;
        private int ExplorationMarkerCount;
        private int ExplorationMarkerCurrentIndex;
        public void InitializeExplorationMarkers()
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return;

            var explorationSites = DatabaseManager.World.GetExplorationSitesByLandblock(Id.Landblock);
            if (explorationSites.Count == 0)
                return;

            var instances = GetLandblockInstances(false);
            if (instances.Count > 0)
                PositionsForExplorationMarkers = new List<Position>();
            else
                return;

            foreach (var instance in instances)
            {
                var instanceWeenie = DatabaseManager.World.GetCachedWeenie(instance.WeenieClassId);
                if (instanceWeenie.WeenieType == WeenieType.Portal)
                    continue;

                Position instancePos = new Position(instance.ObjCellId, instance.OriginX, instance.OriginY, instance.OriginZ, instance.AnglesX, instance.AnglesY, instance.AnglesZ, instance.AnglesW);
                PositionsForExplorationMarkers.Add(instancePos);
            }

            if (PositionsForExplorationMarkers.Count == 0)
                return;

            PositionsForExplorationMarkers.Shuffle();
            if (LandblockInfo != null && LandblockInfo.NumCells >= 30)
                ExplorationMarkerCount = 1 + ((int)LandblockInfo.NumCells - 30) / 80;
            else
                ExplorationMarkerCount = 1 + PositionsForExplorationMarkers.Count / 50;
            ExplorationMarkerCount = Math.Min(ExplorationMarkerCount, PositionsForExplorationMarkers.Count);

            actionQueue.EnqueueAction(new ActionEventDelegate(() =>
            {
                for (int i = 0; i < ExplorationMarkerCount; i++)
                {
                    SpawnExplorationMarker();
                }

                NextExplorationMarkerRefresh = Time.GetFutureUnixTime(ExplorationMarkerRefreshInterval);
            }));
        }

        private void HandleAllegianceHometownTick()
        {
            // Meeting hall landblock: Phase 2 only. The hall ticks on its own landblock group, so the
            // repel accumulator and suppression flag are maintained here rather than on the town.
            var hallRegistry = ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.GetByHallLandblock(Id.Landblock);
            if (hallRegistry != null)
            {
                var hallTown = Managers.AllegianceHometownManager.GetTown(hallRegistry.TownId);
                if (hallTown != null && hallTown.ConflictPhase == 2)
                    HandleAllegianceHometownPhase2Tick(hallRegistry, hallTown);
                else
                {
                    // Not in Phase 2 — clear the repel accumulator so the next Phase 2 starts fresh.
                    _phase2RepelSeconds = 0;
                    _lastPhase2RepelBroadcast = DateTime.MinValue;
                }
                return;
            }

            var registry = ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.GetByLandblock(Id.Landblock);
            if (registry == null) return;

            var town = Managers.AllegianceHometownManager.GetTown(registry.TownId);
            if (town == null) return;

            // Auto-start Phase 1 when an eligible allegiance gathers near the bindstone
            if (town.ConflictPhase == 0 && town.OwnerMonarchId.HasValue)
                TryAutoStartPhase1(registry, town);

            // Phase 2 is fought in the meeting hall and ticks on that landblock, not this one.
            if (town.ConflictPhase == 2) return;

            if (town.ConflictPhase != 1) return;

            var attackerMonarchId = town.ConflictAttackerMonarchId!.Value;

            // Count attacking allegiance members within Phase1AttackerRadius of the bindstone;
            // detect enemies within Phase1EnemyRadius (main landblock + adjacents, since bindstones
            // can sit on a landblock boundary).
            var bindstonePos = registry.BindstonePosition;
            int attackersNear = 0;
            bool rawEnemyPresent = false;

            foreach (var player in GetPlayersNearBindstone(bindstonePos, Phase1EnemyRadius))
            {
                if (!player.IsPK) continue;

                var allegianceMonarchId = Managers.AllegianceManager.GetVerifiedMonarchId(player) ?? player.Guid.Full;
                if (allegianceMonarchId == attackerMonarchId)
                {
                    if (player.Location.DistanceTo(bindstonePos) <= Phase1AttackerRadius)
                        attackersNear++;
                }
                else
                {
                    rawEnemyPresent = true;
                }
            }

            // Accumulate enemy presence time; only interrupt once the grace period expires.
            // An enemy must remain within Phase1EnemyRadius for the full grace period to reset progress.
            bool wasEnemyPresent = _phase1EnemyPresenceSeconds > 0;
            if (rawEnemyPresent)
            {
                _phase1EnemyPresenceSeconds += 5; // tick interval

                // Warn on first detection (before the grace period expires)
                if (!wasEnemyPresent)
                {
                    var warnMsg = $"[{registry.TownName}] An enemy has entered the assault area! " +
                                  $"They must leave within {Phase1EnemyGraceSeconds:0}s or Phase 1 progress will be reset.";
                    EnqueueBroadcast(null, false, null, null,
                        new Network.GameMessages.Messages.GameMessageSystemChat(warnMsg, ACE.Entity.Enum.ChatMessageType.WorldBroadcast));
                }
            }
            else
            {
                _phase1EnemyPresenceSeconds = 0;
            }

            bool enemyInterrupt = _phase1EnemyPresenceSeconds >= Phase1EnemyGraceSeconds;

            var result = Managers.AllegianceHometownManager.TickPhase1(
                registry.TownId, attackersNear, enemyInterrupt, ref _ahPhase1AccumulatedSeconds);

            switch (result)
            {
                case Managers.Phase1TickResult.PhaseComplete:
                    _ahPhase1AccumulatedSeconds = 0;
                    _phase1Interrupted = false;
                    _phase1EnemyPresenceSeconds = 0;
                    // The repel accumulator lives on the meeting hall landblock and is cleared by its
                    // own tick whenever the town isn't in Phase 2, so there is nothing to reset here.
                    Managers.AllegianceHometownManager.StartPhase2(registry.TownId);
                    SpawnPhase2Proxy(registry);
                    // Reward the attacking allegiance for breaching Phase 2 (5 PK Trophies each, default).
                    Managers.AllegianceHometownManager.AwardPhase2StartTrophies(registry.TownId);
                    break;

                case Managers.Phase1TickResult.TimedOut:
                    _ahPhase1AccumulatedSeconds = 0;
                    _phase1Interrupted = false;
                    _phase1EnemyPresenceSeconds = 0;
                    break;

                case Managers.Phase1TickResult.Progressing:
                    _phase1Interrupted = false;
                    break;

                case Managers.Phase1TickResult.Interrupted:
                    if (!_phase1Interrupted)
                    {
                        _phase1Interrupted = true;
                        _phase1EnemyPresenceSeconds = 0; // reset so the grace period starts fresh next time
                        _lastAhPhase1Broadcast = DateTime.UtcNow;
                        var interruptMsg = $"[{registry.TownName}] Phase 1 assault INTERRUPTED — an enemy remained in the area for {Phase1EnemyGraceSeconds:0}s. Progress has been reset.";
                        EnqueueBroadcast(null, false, null, null,
                            new Network.GameMessages.Messages.GameMessageSystemChat(interruptMsg, ACE.Entity.Enum.ChatMessageType.WorldBroadcast));
                    }
                    break;
            }

            // Broadcast progress to the local landblock once per minute while Phase 1 is active
            if (result == Managers.Phase1TickResult.Progressing || result == Managers.Phase1TickResult.Interrupted)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastAhPhase1Broadcast).TotalSeconds >= 60)
                {
                    _lastAhPhase1Broadcast = now;

                    var phase1Duration = Managers.AllegianceHometownManager.Phase1DurationSeconds;
                    var secsRemaining = Math.Max(0, phase1Duration - _ahPhase1AccumulatedSeconds);
                    string progressMsg;
                    if (rawEnemyPresent)
                    {
                        var graceRemaining = Math.Max(0, Phase1EnemyGraceSeconds - _phase1EnemyPresenceSeconds);
                        progressMsg = $"[{registry.TownName}] Phase 1 assault — enemy in area! {graceRemaining:0}s until progress resets. " +
                                      $"Progress: {_ahPhase1AccumulatedSeconds:0}/{phase1Duration:0}s";
                    }
                    else if (attackersNear >= 2)
                    {
                        progressMsg = $"[{registry.TownName}] Phase 1 assault in progress — {secsRemaining:0}s until Phase 2.";
                    }
                    else
                    {
                        progressMsg = $"[{registry.TownName}] Phase 1 assault paused — need at least 2 attackers within {Phase1AttackerRadius:0.#}m of the Bind Stone. " +
                                      $"Progress: {_ahPhase1AccumulatedSeconds:0}/{phase1Duration:0}s";
                    }

                    EnqueueBroadcast(null, false, null, null,
                        new Network.GameMessages.Messages.GameMessageSystemChat(progressMsg, ACE.Entity.Enum.ChatMessageType.WorldBroadcast));
                }
            }
            else
            {
                _lastAhPhase1Broadcast = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Phase 2 meeting hall tick (every 5 s): awards participation trophies, updates the anti-"peacing"
        /// damage-suppression flag on the proxy, and auto-resolves the conflict as a repelled attack once
        /// the defenders hold the hall (2+ defenders, 0 non-defenders present) long enough.
        ///
        /// Phase 2 is fought indoors, so every check here is presence-in-the-hall rather than a distance
        /// ring: the hall is a small dedicated dungeon landblock with a single entrance, and being inside
        /// it *is* being in the fight. Only the bind stone damage falloff remains distance-based
        /// (see AllegianceHometownManager.GetDistanceMultiplier), so attackers still have to close on the
        /// stone rather than plink it from the entry corridor.
        /// </summary>
        private void HandleAllegianceHometownPhase2Tick(
            ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.TownEntry registry,
            ACE.Database.Models.Log.AllegianceHometownTown town)
        {
            var attackerMonarchId = town.ConflictAttackerMonarchId;
            var ownerMonarchId    = town.OwnerMonarchId;

            int defendersNear    = 0;           // owner-allegiance PKs in the hall
            int nonDefendersNear = 0;           // any non-defender PK in the hall (attackers + neutrals)
            bool nonAttackerNearStone = false;  // any non-attacker PK in the hall
            var participants = new List<Player>(); // attackers + defenders in the hall, for periodic trophies

            foreach (var player in GetPlayers())
            {
                if (!player.IsPK) continue;

                var monarchId  = Managers.AllegianceManager.GetVerifiedMonarchId(player) ?? player.Guid.Full;
                bool isAttacker = attackerMonarchId.HasValue && monarchId == attackerMonarchId.Value;
                bool isDefender = ownerMonarchId.HasValue    && monarchId == ownerMonarchId.Value;

                if (!isAttacker)
                    nonAttackerNearStone = true;

                if (isDefender)
                {
                    defendersNear++;
                    participants.Add(player);
                }
                else
                {
                    // Attackers and neutral third parties alike block the repel.
                    nonDefendersNear++;
                    if (isAttacker) participants.Add(player); // only attackers earn participation trophies
                }
            }

            // Anti-"peacing": suppress bindstone damage while any non-attacker is in the hall.
            var proxy = Managers.AllegianceHometownManager.GetPhase2Proxy(registry.TownId);
            if (proxy != null)
                proxy.SuppressDamage = nonAttackerNearStone;

            // Periodic participation trophies for attackers and defenders holding the area.
            Managers.AllegianceHometownManager.AwardPhase2PeriodicTrophies(registry.TownId, participants);

            // Repel: 2+ defenders and no non-defenders (attackers or neutrals) within the repel radius,
            // sustained for the repel window.
            var repelTarget = Managers.AllegianceHometownManager.Phase2RepelSeconds;
            var now = DateTime.UtcNow;

            if (defendersNear >= 2 && nonDefendersNear == 0)
            {
                bool justStarted = _phase2RepelSeconds <= 0;
                _phase2RepelSeconds += 5;

                if (justStarted)
                {
                    _lastPhase2RepelBroadcast = now;
                    EnqueueBroadcast(null, false, null, null,
                        new Network.GameMessages.Messages.GameMessageSystemChat(
                            $"[{registry.TownName}] The defenders have cleared the Meeting Hall! Hold it for {repelTarget / 60.0:0.#} minute(s) to repel the attack.",
                            ACE.Entity.Enum.ChatMessageType.WorldBroadcast));
                }

                if (_phase2RepelSeconds >= repelTarget)
                {
                    _phase2RepelSeconds = 0;
                    _lastPhase2RepelBroadcast = DateTime.MinValue;
                    if (proxy != null)
                        proxy.ResolveRepel();
                    else
                        Managers.AllegianceHometownManager.HandleDefenderRepel(registry.TownId);
                    return;
                }

                if ((now - _lastPhase2RepelBroadcast).TotalSeconds >= 60)
                {
                    _lastPhase2RepelBroadcast = now;
                    var remaining = Math.Max(0, repelTarget - _phase2RepelSeconds);
                    EnqueueBroadcast(null, false, null, null,
                        new Network.GameMessages.Messages.GameMessageSystemChat(
                            $"[{registry.TownName}] Repelling the attack — {remaining:0}s until {town.ConflictAttackerName} is driven off. Any enemy entering the Meeting Hall will interrupt.",
                            ACE.Entity.Enum.ChatMessageType.WorldBroadcast));
                }
            }
            else
            {
                if (_phase2RepelSeconds > 0)
                    EnqueueBroadcast(null, false, null, null,
                        new Network.GameMessages.Messages.GameMessageSystemChat(
                            $"[{registry.TownName}] Repel interrupted — an enemy has entered the Meeting Hall.",
                            ACE.Entity.Enum.ChatMessageType.WorldBroadcast));

                _phase2RepelSeconds = 0;
                _lastPhase2RepelBroadcast = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Returns all players on this landblock and its adjacent landblocks
        /// that are within <paramref name="maxDistance"/> meters of <paramref name="pos"/>.
        /// Handles bindstones that sit on a landblock boundary.
        /// </summary>
        private IEnumerable<Player> GetPlayersNearBindstone(ACE.Entity.Position pos, float maxDistance)
        {
            foreach (var player in GetPlayers())
            {
                if (player.Location.DistanceTo(pos) <= maxDistance)
                    yield return player;
            }

            foreach (var adjId in Managers.LandblockManager.GetAdjacentIDs(this))
            {
                var adjLb = Managers.LandblockManager.GetLandblock(adjId, false);
                if (adjLb == null) continue;
                foreach (var player in adjLb.GetPlayers())
                {
                    if (player.Location.DistanceTo(pos) <= maxDistance)
                        yield return player;
                }
            }
        }

        private void TryAutoStartPhase1(
            ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.TownEntry registry,
            ACE.Database.Models.Log.AllegianceHometownTown town)
        {
            var bindstonePos   = registry.BindstonePosition;
            var ownerMonarchId = town.OwnerMonarchId!.Value;

            // Pass 1: count non-owner PKs near the bindstone per allegiance,
            //         and track all non-owner allegiances inside Phase1EnemyRadius.
            var nearCount          = new Dictionary<uint, int>();    // monarchId → players within Phase1AttackerRadius
            var allegianceIdentity = new Dictionary<uint, string>(); // monarchId → allegiance identity
            var allMonarchIds      = new System.Collections.Generic.HashSet<uint>(); // all non-owner allegiances in range

            foreach (var player in GetPlayersNearBindstone(bindstonePos, Phase1EnemyRadius))
            {
                if (!player.IsPK) continue;
                var monarchId = Managers.AllegianceManager.GetVerifiedMonarchId(player) ?? player.Guid.Full;
                if (monarchId == ownerMonarchId) continue;

                // Cannot attack a town owned by an allegiance that another character on your
                // account belongs to — such players don't count toward starting an assault.
                if (player.AccountHasAllegianceMember(ownerMonarchId)) continue;

                allMonarchIds.Add(monarchId);

                if (!allegianceIdentity.ContainsKey(monarchId))
                    allegianceIdentity[monarchId] = Managers.AllegianceHometownManager.GetAllegianceIdentity(player);

                if (player.Location.DistanceTo(bindstonePos) <= Phase1AttackerRadius)
                {
                    nearCount.TryGetValue(monarchId, out var c);
                    nearCount[monarchId] = c + 1;
                }
            }

            // Must be exactly one non-owner allegiance within Phase1EnemyRadius of the bindstone
            if (allMonarchIds.Count != 1) return;

            var attackerMonarchId = allMonarchIds.First();

            // Must have 2+ members within Phase1AttackerRadius of the bindstone
            nearCount.TryGetValue(attackerMonarchId, out var nearPlayers);
            if (nearPlayers < 2) return;

            var attackerAllegiance = allegianceIdentity[attackerMonarchId];

            if (Managers.AllegianceHometownManager.TryStartPhase1(registry.TownId, attackerMonarchId, attackerAllegiance, out _))
            {
                var holdMinutes = Managers.AllegianceHometownManager.Phase1DurationSeconds / 60.0;
                EnqueueBroadcast(null, false, null, null,
                    new Network.GameMessages.Messages.GameMessageSystemChat(
                        $"[{registry.TownName}] {attackerAllegiance} has initiated a Phase 1 assault! Hold the bind stone for {holdMinutes:0.#} minute(s).",
                        ACE.Entity.Enum.ChatMessageType.WorldBroadcast));
            }
        }

        /// <summary>
        /// Called on the town landblock when Phase 1 completes. Cloaks the real outdoor bind stone here,
        /// then spawns the attackable proxy inside the town's meeting hall.
        ///
        /// The hall is a separate landblock on its own landblock group, so the proxy is created and added
        /// on the hall's own action queue rather than from this thread. The permaload also moves to the
        /// hall — that is the landblock that must stay loaded for the duration of Phase 2.
        /// </summary>
        private void SpawnPhase2Proxy(Entity.AllegianceHometown.AllegianceHometownRegistry.TownEntry entry)
        {
            // Cloak the real (outdoor) bind stone so only the hall proxy is a valid target.
            // Must send DeleteObject after setting physics state so already-tracked clients remove it.
            var realBindstone = worldObjects.Values
                .OfType<WorldObjects.Bindstone>()
                .FirstOrDefault(b => b.WeenieType == ACE.Entity.Enum.WeenieType.AllegianceBindstone);
            if (realBindstone != null)
            {
                realBindstone.Attackable = false;
                realBindstone.Cloaked    = (bool?)true;
                realBindstone.Ethereal   = (bool?)true;
                realBindstone.NoDraw     = (bool?)true;
                realBindstone.EnqueueBroadcastPhysicsState();
                realBindstone.EnqueueBroadcast(false, new Network.GameMessages.Messages.GameMessageDeleteObject(realBindstone));
                realBindstone.Visibility = true;
                Managers.AllegianceHometownManager.RegisterPhase2CloakedBindstone(entry.TownId, realBindstone);
            }

            var hallLb = Managers.LandblockManager.GetLandblock(entry.Phase2Position.LandblockId, false);
            if (hallLb == null)
            {
                log.Error($"[AllegianceHometown] Could not load meeting hall landblock 0x{entry.HallLandblockId:X4} for {entry.TownName}; Phase 2 has no bind stone. Forcing the conflict closed.");
                Managers.AllegianceHometownManager.ForceEndConflict(entry.TownId);
                return;
            }

            // Hold the hall loaded so it never goes dormant or unloads while the proxy is alive. The
            // deadline is the backstop: if Phase 2 ends badly and nothing lifts the permaload, the
            // landblock still releases itself rather than staying pinned forever.
            hallLb.SetPermaload(DateTime.UtcNow.Add(Managers.AllegianceHometownManager.Phase2PermaloadDuration));

            hallLb.EnqueueAction(new ActionEventDelegate(() =>
            {
                var proxy = WorldObjectFactory.CreateNewWorldObject(ACE.Database.CustomWeenieId.BindstoneCreatureProxy)
                            as WorldObjects.BindstoneCreatureProxy;
                if (proxy == null)
                {
                    log.Error($"[AllegianceHometown] Failed to create BindstoneCreatureProxy (wcid {ACE.Database.CustomWeenieId.BindstoneCreatureProxy}) for {entry.TownName}.");
                    hallLb.ClearTimedPermaload();
                    Managers.AllegianceHometownManager.ForceEndConflict(entry.TownId);
                    return;
                }

                proxy.TownId    = entry.TownId;
                proxy.Location  = new Position(entry.Phase2Position);
                proxy.TimeToRot = -1; // Never decay — proxy is destroyed explicitly when Phase 2 ends

                var maxHp = (uint)Managers.AllegianceHometownManager.ComputeBindstoneHp();
                proxy.Health.StartingValue = maxHp;
                proxy.UpdateVital(proxy.Health, maxHp);

                // The hall may have been sitting dormant (empty since someone last visited). Permaload only
                // stops it going dormant from here on; it does not wake one that already is. Without this the
                // proxy's heartbeat — the only thing that can time Phase 2 out — would never tick.
                hallLb.SetActive();

                hallLb.AddWorldObject(proxy);
                Managers.AllegianceHometownManager.RegisterPhase2Proxy(entry.TownId, proxy);

                log.Info($"[AllegianceHometown] Phase 2 proxy spawned for {entry.TownName} in meeting hall 0x{entry.HallLandblockId:X4} with {maxHp:N0} HP.");
            }));
        }

        public void RefreshExplorationMarkers(bool forceRefresh = false)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM || ExplorationMarkerCount == 0)
                return;

            var allMarkers = worldObjects.Where(i => i.Value.WeenieClassId == (uint)Factories.Enum.WeenieClassName.explorationMarker).ToList();
            allMarkers.AddRange(pendingAdditions.Where(i => i.Value.WeenieClassId == (uint)Factories.Enum.WeenieClassName.explorationMarker).ToList());
            allMarkers = allMarkers.Where(i => !pendingRemovals.Contains(i.Key)).ToList();

            var currentMarkerCount = 0;
            foreach (var obj in allMarkers)
            {
                var marker = obj.Value;
                bool isInRange = false;
                if (!forceRefresh)
                {
                    foreach (var player in players)
                    {
                        if (player.IsOvertlyPlussed)
                            continue;

                        float distance;
                        if (Pathfinder.PathfindingEnabled && player.Location.Indoors)
                        {
                            if(!Pathfinder.GetRouteDistance(player.Location, marker.Location, AgentWidth.Narrow, out distance))
                                distance = player.Location.DistanceTo(marker.Location);
                        }
                        else
                            distance = player.Location.DistanceTo(marker.Location);

                        if (distance < 50)
                        {
                            isInRange = true;
                            break;
                        }
                    }
                }

                if (!isInRange) // Only refresh exploration markers that are not in range of any players at the moment.
                    marker.Destroy();
                else
                    currentMarkerCount++;
            }

            var spawnedCount = 0;
            if (currentMarkerCount < ExplorationMarkerCount)
            {
                for (int i = currentMarkerCount; i < ExplorationMarkerCount; i++)
                {
                    spawnedCount++;
                    SpawnExplorationMarker();
                }
            }

            NextExplorationMarkerRefresh = Time.GetFutureUnixTime(ExplorationMarkerRefreshInterval);
        }

        public void SpawnExplorationMarker(int attempts = 0)
        {
            if (PositionsForExplorationMarkers != null && PositionsForExplorationMarkers.Count > 0)
            {
                var entryPos = new Position(PositionsForExplorationMarkers[ExplorationMarkerCurrentIndex]);

                ExplorationMarkerCurrentIndex++;
                if (ExplorationMarkerCurrentIndex >= PositionsForExplorationMarkers.Count)
                    ExplorationMarkerCurrentIndex = 0;

                if (Pathfinder.PathfindingEnabled && entryPos.Indoors)
                {
                    var randomPos = Pathfinder.GetRandomPointWithinCircle(entryPos, 15, AgentWidth.Wide);
                    if (randomPos != null)
                        entryPos = randomPos;
                }

                var allMarkersAndPortals = worldObjects.Where(i => i.Value.WeenieClassId == (uint)Factories.Enum.WeenieClassName.explorationMarker || i.Value.WeenieType == WeenieType.Portal).ToList();
                allMarkersAndPortals.AddRange(pendingAdditions.Where(i => i.Value.WeenieClassId == (uint)Factories.Enum.WeenieClassName.explorationMarker || i.Value.WeenieType == WeenieType.Portal).ToList());
                allMarkersAndPortals = allMarkersAndPortals.Where(i => !pendingRemovals.Contains(i.Key)).ToList();

                foreach (var obj in allMarkersAndPortals)
                {
                    var markerOrPortal = obj.Value;
                    var minDistance = markerOrPortal.WeenieType == WeenieType.Portal ? 10 : 50;

                    float distance;
                    if (Pathfinder.PathfindingEnabled && entryPos.Indoors)
                    {
                        if(!Pathfinder.GetRouteDistance(entryPos, markerOrPortal.Location, AgentWidth.Narrow, out distance))
                            distance = entryPos.DistanceTo(markerOrPortal.Location);
                    }
                    else
                        distance = entryPos.DistanceTo(markerOrPortal.Location);

                    if (distance < minDistance)
                    {
                        attempts++;
                        if (attempts < 10)
                            SpawnExplorationMarker(attempts);
                        else
                        {
                            var msg = $"Landblock 0x{Id} failed to find position to spawn exploration marker that was not too close to another exploration marker.";
                            if (ExplorationMarkerCount > 1)
                            {
                                ExplorationMarkerCount--;
                                msg += $" Reducing ExplorationMarkerCount to {ExplorationMarkerCount}";
                            }
                            log.Warn(msg);
                        }
                        return;
                    }
                }

                var explorationMarker = WorldObjectFactory.CreateNewWorldObject((uint)Factories.Enum.WeenieClassName.explorationMarker);
                explorationMarker.Location = entryPos;
                explorationMarker.Location.LandblockId = new LandblockId(explorationMarker.Location.GetCell());
                if (!explorationMarker.EnterWorld() && explorationMarker != null)
                {
                    attempts++;
                    explorationMarker.Destroy();
                    if(attempts < 10)
                        SpawnExplorationMarker(attempts);
                    else
                        log.Warn($"Landblock 0x{Id} failed to spawn exploration marker.");
                    return;
                }
            }
        }

        /// <summary>
        /// Monster Locations, Generators<para />
        /// This will be called from a separate task from our constructor. Use thread safety when interacting with this landblock.
        /// </summary>
        private void CreateWorldObjects()
        {
            var objects = DatabaseManager.World.GetCachedInstancesByLandblock(Id.Landblock);
            var shardObjects = DatabaseManager.Shard.BaseDatabase.GetStaticObjectsByLandblock(Id.Landblock);
            var factoryObjects = WorldObjectFactory.CreateNewWorldObjects(objects, shardObjects);

            actionQueue.EnqueueAction(new ActionEventDelegate(() =>
            {
                // for mansion linking
                var houses = new List<House>();

                foreach (var fo in factoryObjects)
                {
                    WorldObject parent = null;
                    if (fo.WeenieType == WeenieType.House)
                    {
                        var house = fo as House;
                        Houses.Add(house);

                        if (fo.HouseType == HouseType.Mansion)
                        {
                            houses.Add(house);
                            house.LinkedHouses.Add(houses[0]);

                            if (houses.Count > 1)
                            {
                                houses[0].LinkedHouses.Add(house);
                                parent = houses[0];
                            }
                        }

                        if (Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.CustomDM)
                            house.SetHooksVisible(false);
                    }

                    AddWorldObject(fo);
                    fo.ActivateLinks(objects, shardObjects, parent);

                    if (fo.PhysicsObj != null)
                        fo.PhysicsObj.Order = 0;
                }

                CreateWorldObjectsCompleted = true;

                PhysicsLandblock.SortObjects();
            }));
        }

        /// <summary>
        /// Corpses<para />
        /// This will be called from a separate task from our constructor. Use thread safety when interacting with this landblock.
        /// </summary>
        private void SpawnDynamicShardObjects()
        {
            var dynamics = DatabaseManager.Shard.BaseDatabase.GetDynamicObjectsByLandblock(Id.Landblock);
            var factoryShardObjects = WorldObjectFactory.CreateWorldObjects(dynamics);

            actionQueue.EnqueueAction(new ActionEventDelegate(() =>
            {
                foreach (var fso in factoryShardObjects)
                    AddWorldObject(fso);
            }));
        }

        private class AvailableCell
        {
            public int CellX;
            public int CellY;
            public ushort TerrainType;

            public AvailableCell(int cellX, int cellY, ushort terrainType)
            {
                CellX = cellX;
                CellY = cellY;
                TerrainType = terrainType;
            }
        }

        private class EncounterInfo
        {
            public int Coords;
            public Encounter Encounter;
            public ushort TerrainType;

            public EncounterInfo(int coords, Encounter encounter, ushort terrainType)
            {
                Coords = coords;
                Encounter = encounter;
                TerrainType = terrainType;
            }
        }

        private ushort getTerrainType(int cellX, int cellY)
        {
            var terrain = PhysicsLandblock.get_terrain(cellX, cellY);
            var terrainType = (ushort)(terrain >> 2 & 0x1F);
            return terrainType;
        }

        /// <summary>
        /// Spawns the semi-randomized monsters scattered around the outdoors<para />
        /// This will be called from a separate task from our constructor. Use thread safety when interacting with this landblock.
        /// </summary>
        private void SpawnEncounters()
        {
            // get the encounter spawns for this landblock
            var encounters = DatabaseManager.World.GetCachedEncountersByLandblock(Id.Landblock, out var wasCached);

            if (PropertyManager.GetBool("increase_minimum_encounter_spawn_density").Item)
            {
                if(!wasCached)
                {
                    if (encounters.Count > 0)
                    {
                        // Landscape spawn density multiplier
                        // The maximum amount of encounters that will fit in a landblock is 64.
                        int newCount;
                        if (encounters.Count < 8)
                            newCount = 8;
                        else
                            newCount = encounters.Count;

                        if (newCount != encounters.Count)
                        {
                            Dictionary<int, EncounterInfo> encountersToDuplicate = new Dictionary<int, EncounterInfo>();
                            Dictionary<ushort, List<AvailableCell>> terrainTypeMap = new Dictionary<ushort, List<AvailableCell>>();

                            foreach (var encounter in encounters)
                            {
                                int coords = encounter.CellX << 16 | encounter.CellY;
                                encountersToDuplicate.Add(coords, new EncounterInfo(coords, encounter, getTerrainType(encounter.CellX, encounter.CellY)));
                            }

                            for (int cellX = 0; cellX < LandDefs.BlockSide; cellX++)
                            {
                                for (int cellY = 0; cellY < LandDefs.BlockSide; cellY++)
                                {
                                    int coords = cellX << 16 | cellY;

                                    if (!encountersToDuplicate.ContainsKey(coords)) // Only add cells that do not yet contain encounters.
                                    {
                                        ushort terrainType = getTerrainType(cellX, cellY);

                                        if (terrainTypeMap.TryGetValue(terrainType, out var entry))
                                            entry.Add(new AvailableCell(cellX, cellY, terrainType));
                                        else
                                            terrainTypeMap.Add(terrainType, new List<AvailableCell>() { new AvailableCell(cellX, cellY, terrainType) });
                                    }
                                }
                            }

                            while (encounters.Count < newCount && encountersToDuplicate.Count > 0)
                            {
                                var sourceEncounter = encountersToDuplicate.ElementAt(ThreadSafeRandom.Next(0, encountersToDuplicate.Count - 1)).Value;
                                if (terrainTypeMap.TryGetValue(sourceEncounter.TerrainType, out var availableCells))
                                {
                                    var newEncounterCell = availableCells[ThreadSafeRandom.Next(0, availableCells.Count - 1)];

                                    Encounter newEncounter = new Encounter();
                                    newEncounter.WeenieClassId = sourceEncounter.Encounter.WeenieClassId;
                                    newEncounter.Landblock = sourceEncounter.Encounter.Landblock;
                                    newEncounter.LastModified = sourceEncounter.Encounter.LastModified;
                                    newEncounter.CellX = newEncounterCell.CellX;
                                    newEncounter.CellY = newEncounterCell.CellY;

                                    encounters.Add(newEncounter);
                                    availableCells.Remove(newEncounterCell);
                                    if (availableCells.Count == 0)
                                    {
                                        terrainTypeMap.Remove(sourceEncounter.TerrainType);
                                        encountersToDuplicate = encountersToDuplicate.Where(i => i.Value.TerrainType != sourceEncounter.TerrainType).ToDictionary(i => i.Key, i => i.Value);
                                    }
                                }
                                else
                                {
                                    // This should never happen.
                                    terrainTypeMap.Remove(sourceEncounter.TerrainType);
                                    encountersToDuplicate = encountersToDuplicate.Where(i => i.Value.TerrainType != sourceEncounter.TerrainType).ToDictionary(i => i.Key, i => i.Value);
                                }
                            }
                        }
                    }
                }
            }

            List<LandblockInstance> landblockInstances = null;
            List<(uint Wcid, Position Location)> portalDrops = null;
            if (Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.CustomDM || PropertyManager.GetBool("increase_minimum_encounter_spawn_density").Item)
            {
                landblockInstances = GetLandblockInstances(true);
                portalDrops = DatabaseManager.World.GetPortalDestinationsByLandblock(GetAdjacents());

                foreach(var destination in portalDrops)
                {
                    if (!destination.Location.Indoors)
                        destination.Location.AdjustMapCoords();
                }
            }

            foreach (var encounter in encounters)
            {
                var wo = WorldObjectFactory.CreateNewWorldObject(encounter.WeenieClassId);

                if (wo == null) continue;

                wo.SetProperty(PropertyBool.IsEncounterGenerator, true);

                if (encounter.Id == 0)
                {
                    wo.SetProperty(PropertyFloat.DefaultScale, 0.5f);
                    wo.SetProperty(PropertyString.LongDesc, "Not a permanent encounter.\nAutomatically generated by the increase_minimum_encounter_spawn_density setting.\nDisabling the setting will remove this.");
                }
                else
                    wo.SetProperty(PropertyFloat.DefaultScale, 1.5f);

                actionQueue.EnqueueAction(new ActionEventDelegate(() =>
                {
                    var xPos = Math.Clamp((encounter.CellX * LandDefs.CellLength) + LandDefs.CellLength / 2, 0.5f, LandDefs.BlockLength - 0.5f);
                    var yPos = Math.Clamp((encounter.CellY * LandDefs.CellLength) + LandDefs.CellLength / 2, 0.5f, LandDefs.BlockLength - 0.5f);

                    var pos = new Physics.Common.Position();
                    pos.ObjCellID = (uint)(Id.Landblock << 16) | 1;
                    pos.Frame = new Physics.Animation.AFrame(new Vector3(xPos, yPos, 0), Quaternion.Identity);
                    pos.adjust_to_outside();

                    pos.Frame.Origin.Z = PhysicsLandblock.GetZ(pos.Frame.Origin);

                    wo.Location = new Position(pos.ObjCellID, pos.Frame.Origin, pos.Frame.Orientation);

                    var sortCell = LScape.get_landcell(pos.ObjCellID) as SortCell;
                    if (sortCell != null && sortCell.has_building())
                    {
                        wo.Destroy();
                        return;
					}

                    if (PropertyManager.GetBool("increase_minimum_encounter_spawn_density").Item)
                    {
                        // Avoid some less than ideal locations
                        if (!wo.Location.IsWalkable() || PhysicsLandblock.IsRoad(null, xPos, yPos))
                        {
                            wo.Destroy();
                            return;
                        }
                    }

                    if (PropertyManager.GetBool("override_encounter_spawn_rates").Item)
                    {
                        wo.RegenerationInterval = PropertyManager.GetDouble("encounter_regen_interval").Item;

                        wo.ReinitializeHeartbeats();

                        if (wo.Biota.PropertiesGenerator != null)
                        {
                            // While this may be ugly, it's done for performance reasons.
                            // Common weenie properties are not cloned into the bota on creation. Instead, the biota references simply point to the weenie collections.
                            // The problem here is that we want to update one of those common collection properties. If the biota is referencing the weenie collection,
                            // then we'll end up updating the global weenie (from the cache), instead of just this specific biota.
                            if (wo.Biota.PropertiesGenerator == wo.Weenie.PropertiesGenerator)
                            {
                                wo.Biota.PropertiesGenerator = new List<PropertiesGenerator>(wo.Weenie.PropertiesGenerator.Count);

                                foreach (var record in wo.Weenie.PropertiesGenerator)
                                    wo.Biota.PropertiesGenerator.Add(record.Clone());
                            }

                            foreach (var profile in wo.Biota.PropertiesGenerator)
                                profile.Delay = (float)PropertyManager.GetDouble("encounter_delay").Item;
                        }
                    }

                    if (!AddWorldObject(wo))
                    {
                        wo.Destroy();
                        return;
                    }

                    if (!wo.GeneratorDisabled)
                    {
                        if (Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.CustomDM || PropertyManager.GetBool("increase_minimum_encounter_spawn_density").Item)
                        {
                            // Disable the landscape encounter generators that are too close to lifestones or portals
                            var awarenessMod = PropertyManager.GetDouble("mob_awareness_range").Item;

                            if (!wo.Name.ToLower().Contains("harmless")) // Harmless generators can stay enabled
                            {
                                foreach (var landblockInstance in landblockInstances)
                                {
                                    var weenie = DatabaseManager.World.GetCachedWeenie(landblockInstance.WeenieClassId);
                                    if (weenie == null)
                                        continue;

                                    var itemType = weenie.GetItemType();
                                    var pkStatus = (PlayerKillerStatus)(weenie.GetProperty(PropertyInt.PlayerKillerStatus) ?? 0);
                                    var location = new Position(landblockInstance.ObjCellId, landblockInstance.OriginX, landblockInstance.OriginY, landblockInstance.OriginZ, landblockInstance.AnglesX, landblockInstance.AnglesY, landblockInstance.AnglesZ, landblockInstance.AnglesW);

                                    if ((itemType == ItemType.LifeStone || itemType == ItemType.Portal || pkStatus == PlayerKillerStatus.Vendor || pkStatus == PlayerKillerStatus.RubberGlue) && wo.Location.DistanceTo(location) < 60 * awarenessMod)
                                    {
                                        wo.GeneratorDisabled = true;
                                        wo.LongDesc = $"{(wo.LongDesc != null && wo.LongDesc.Length > 0 ? $"{wo.LongDesc}\n\n" : "")}Disabled due to proximity to {weenie.GetName()}";
                                        break;
                                    }
                                }

                                foreach (var portalDest in portalDrops)
                                {
                                    if (wo.Location.DistanceTo(portalDest.Location) < 60 * awarenessMod)
                                    {
                                        wo.GeneratorDisabled = true;

                                        var weenie = DatabaseManager.World.GetCachedWeenie(portalDest.Wcid);
                                        if (weenie == null)
                                            continue;

                                        wo.LongDesc = $"{(wo.LongDesc != null && wo.LongDesc.Length > 0 ? $"{wo.LongDesc}\n\n" : "")}Disabled due to proximity to portal drop: {weenie.GetName()}";
                                        break;
                                    }
                                }

                                if (GetDistanceToNearestRoad(wo.Location, out _) < 60 * awarenessMod)
                                {
                                    wo.GeneratorDisabled = true;

                                    wo.LongDesc = $"{(wo.LongDesc != null && wo.LongDesc.Length > 0 ? $"{wo.LongDesc}\n\n" : "")}Disabled due to proximity to a road.";
                                }
                            }
                        }
                    }
                }));
            }
        }

        /// <summary>
        /// Loads the meshes for the landblock<para />
        /// This isn't used by ACE, but we still retain it for the following reason:<para />
        /// its useful, concise, high level overview code for everything needed to load landblocks, all their objects, scenery, polygons
        /// without getting into all of the low level methods that acclient uses to do it
        /// </summary>
        private void LoadMeshes(List<LandblockInstance> objects)
        {
            LandblockMesh = new LandblockMesh(Id);
            LoadLandObjects();
            LoadBuildings();
            LoadWeenies(objects);
            LoadScenery();
        }

        /// <summary>
        /// Loads the meshes for the static landblock objects,
        /// also known as obstacles
        /// </summary>
        private void LoadLandObjects()
        {
            LandObjects = new List<ModelMesh>();

            foreach (var obj in LandblockInfo.Objects)
                LandObjects.Add(new ModelMesh(obj.Id, obj.Frame));
        }

        /// <summary>
        /// Loads the meshes for the buildings on the landblock
        /// </summary>
        private void LoadBuildings()
        {
            Buildings = new List<ModelMesh>();

            foreach (var obj in LandblockInfo.Buildings)
                Buildings.Add(new ModelMesh(obj.ModelId, obj.Frame));
        }

        /// <summary>
        /// Loads the meshes for the weenies on the landblock
        /// </summary>
        private void LoadWeenies(List<LandblockInstance> objects)
        {
            WeenieMeshes = new List<ModelMesh>();

            foreach (var obj in objects)
            {
                var weenie = DatabaseManager.World.GetCachedWeenie(obj.WeenieClassId);
                WeenieMeshes.Add(
                    new ModelMesh(weenie.GetProperty(PropertyDataId.Setup) ?? 0,
                    new DatLoader.Entity.Frame(new Position(obj.ObjCellId, obj.OriginX, obj.OriginY, obj.OriginZ, obj.AnglesX, obj.AnglesY, obj.AnglesZ, obj.AnglesW))));
            }
        }

        /// <summary>
        /// Loads the meshes for the scenery on the landblock
        /// </summary>
        private void LoadScenery()
        {
            Scenery = Entity.Scenery.Load(this);
        }

        /// <summary>
        /// This should be called before TickLandblockGroupThreadSafeWork() and before Tick()
        /// </summary>
        public void TickPhysics(double portalYearTicks, ConcurrentBag<WorldObject> movedObjects)
        {
            if (IsDormant)
                return;

            Monitor5m.Restart();
            Monitor1h.Restart();
            monitorsRequireEventStart = false;

            ProcessPendingWorldObjectAdditionsAndRemovals();

            foreach (WorldObject wo in worldObjects.Values)
            {
                try
                {
                    // set to TRUE if object changes landblock
                    var landblockUpdate = wo.UpdateObjectPhysics();

                    if (landblockUpdate)
                        movedObjects.Add(wo);
                }
                catch (Exception ex)
                {
                    // One misbehaving object must not crash the physics tick for the entire landblock (group/thread).
                    // Skip this object's physics update for this frame; leave the object in the world.
                    log.Error($"[TICK_EXCEPTION] UpdateObjectPhysics aborted for {wo.Guid}:{wo.Name} in landblock {Id}. ex: {ex}");
                }
            }

            Monitor5m.Pause();
            Monitor1h.Pause();
        }

        /// <summary>
        /// This will tick anything that can be multi-threaded safely using LandblockGroups as thread boundaries
        /// This should be called after TickPhysics() and before Tick()
        /// </summary>
        public void TickMultiThreadedWork(double currentUnixTime)
        {
            if (monitorsRequireEventStart)
            {
                Monitor5m.Restart();
                Monitor1h.Restart();
            }
            else
            {
                Monitor5m.Resume();
                Monitor1h.Resume();
            }

            stopwatch.Restart();
            // This will consist of the following work:
            // - this.CreateWorldObjects
            // - this.SpawnDynamicShardObjects
            // - this.SpawnEncounters
            // - Adding items back onto the landblock from failed player movements: Player_Inventory.cs DoHandleActionPutItemInContainer()
            // - Executing trade between two players: Player_Trade.cs FinalizeTrade()
            actionQueue.RunActions();
            ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_RunActions, stopwatch.Elapsed.TotalSeconds);

            ProcessPendingWorldObjectAdditionsAndRemovals();

            // When a WorldObject Ticks, it can end up adding additional WorldObjects to this landblock
            if (!IsDormant)
            {
                stopwatch.Restart();
                while (sortedCreaturesByNextTick.Count > 0) // Monster_Tick()
                {
                    var first = sortedCreaturesByNextTick.First.Value;

                    // If they wanted to run before or at now
                    if (first.NextMonsterTickTime <= currentUnixTime)
                    {
                        sortedCreaturesByNextTick.RemoveFirst();
                        try
                        {
                            first.Monster_Tick(currentUnixTime);
                        }
                        catch (Exception ex)
                        {
                            // One misbehaving creature must not crash the world tick. Skip its AI update this frame.
                            log.Error($"[TICK_EXCEPTION] Monster_Tick aborted for {first.Guid}:{first.Name} in landblock {Id}. ex: {ex}");

                            // Ensure its next tick is in the future so it isn't re-processed this same frame (which would infinite-loop).
                            if (first.NextMonsterTickTime <= currentUnixTime)
                                first.NextMonsterTickTime = currentUnixTime + tickExceptionRetryInterval;
                        }
                        sortedCreaturesByNextTick.AddLast(first); // All creatures tick at a fixed interval
                    }
                    else
                    {
                        break;
                    }
                }
                ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_Monster_Tick, stopwatch.Elapsed.TotalSeconds);
            }

            stopwatch.Restart();
            while (sortedGeneratorsByNextGeneratorUpdate.Count > 0)
            {
                var first = sortedGeneratorsByNextGeneratorUpdate.First.Value;

                // If they wanted to run before or at now
                if (first.NextGeneratorUpdateTime <= currentUnixTime)
                {
                    sortedGeneratorsByNextGeneratorUpdate.RemoveFirst();
                    try
                    {
                        first.GeneratorUpdate(currentUnixTime);
                    }
                    catch (Exception ex)
                    {
                        // One misbehaving generator must not crash the world tick. Skip its update this frame.
                        log.Error($"[TICK_EXCEPTION] GeneratorUpdate aborted for {first.Guid}:{first.Name} in landblock {Id}. ex: {ex}");

                        // Ensure its next update is in the future so it isn't re-processed this same frame (which would infinite-loop).
                        if (first.NextGeneratorUpdateTime <= currentUnixTime)
                            first.NextGeneratorUpdateTime = currentUnixTime + tickExceptionRetryInterval;
                    }
                    //InsertWorldObjectIntoSortedGeneratorUpdateList(first);
                    sortedGeneratorsByNextGeneratorUpdate.AddLast(first);
                }
                else
                {
                    break;
                }
            }
            ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_GeneratorUpdate, stopwatch.Elapsed.TotalSeconds);

            stopwatch.Restart();
            while (sortedGeneratorsByNextRegeneration.Count > 0) // GeneratorRegeneration()
            {
                var first = sortedGeneratorsByNextRegeneration.First.Value;

                //Console.WriteLine($"{first.Name}.Landblock_Tick_GeneratorRegeneration({currentUnixTime})");

                // If they wanted to run before or at now
                if (first.NextGeneratorRegenerationTime <= currentUnixTime)
                {
                    sortedGeneratorsByNextRegeneration.RemoveFirst();
                    try
                    {
                        first.GeneratorRegeneration(currentUnixTime);
                    }
                    catch (Exception ex)
                    {
                        // One misbehaving generator must not crash the world tick. Skip its regeneration this frame.
                        log.Error($"[TICK_EXCEPTION] GeneratorRegeneration aborted for {first.Guid}:{first.Name} in landblock {Id}. ex: {ex}");

                        // Push the next regeneration into the future before re-inserting, otherwise it re-sorts to the
                        // front of the list and gets re-processed this same frame (which would infinite-loop).
                        if (first.NextGeneratorRegenerationTime <= currentUnixTime)
                            first.NextGeneratorRegenerationTime = currentUnixTime + tickExceptionRetryInterval;
                    }
                    InsertWorldObjectIntoSortedGeneratorRegenerationList(first); // Generators can have regnerations at different intervals
                }
                else
                {
                    break;
                }
            }
            ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_GeneratorRegeneration, stopwatch.Elapsed.TotalSeconds);

            // Heartbeat
            stopwatch.Restart();
            if (lastHeartBeat + heartbeatInterval <= DateTime.UtcNow)
            {
                var thisHeartBeat = DateTime.UtcNow;

                ProcessPendingWorldObjectAdditionsAndRemovals();

                // Decay world objects
                if (lastHeartBeat != DateTime.MinValue)
                {
                    foreach (var wo in worldObjects.Values)
                    {
                        try
                        {
                            if (wo.IsDecayable())
                                wo.Decay(thisHeartBeat - lastHeartBeat);
                        }
                        catch (Exception ex)
                        {
                            // One misbehaving object must not crash the landblock heartbeat. Skip its decay this frame.
                            log.Error($"[TICK_EXCEPTION] Decay aborted for {wo.Guid}:{wo.Name} in landblock {Id}. ex: {ex}");
                        }
                    }
                }

                CheckPermaloadExpiry(thisHeartBeat);

                if (!Permaload && HasNoKeepAliveObjects)
                {
                    if (lastActiveTime + dormantInterval < thisHeartBeat)
                    {
                        if (!IsDormant)
                        {
                            var spellProjectiles = worldObjects.Values.Where(i => i is SpellProjectile).ToList();
                            foreach (var spellProjectile in spellProjectiles)
                            {
                                spellProjectile.PhysicsObj.set_active(false);
                                spellProjectile.Destroy();
                            }
                        }

                        IsDormant = true;
                    }
                    if (lastActiveTime + UnloadInterval < thisHeartBeat)
                        LandblockManager.AddToDestructionQueue(this);
                }

                //log.Info($"Landblock {Id.ToString()}.Tick({currentUnixTime}).Landblock_Tick_Heartbeat: thisHeartBeat: {thisHeartBeat.ToString()} | lastHeartBeat: {lastHeartBeat.ToString()} | worldObjects.Count: {worldObjects.Count()}");
                lastHeartBeat = thisHeartBeat;
            }
            ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_Heartbeat, stopwatch.Elapsed.TotalSeconds);

            // Allegiance Hometown tick — runs every 5 s on capturable-town landblocks (Phase 1 and Phase 2)
            if (IsAllegianceHometownLandblock && lastAhPhase1Tick + ahPhase1TickInterval <= DateTime.UtcNow)
            {
                lastAhPhase1Tick = DateTime.UtcNow;
                try
                {
                    HandleAllegianceHometownTick();
                }
                catch (Exception ex)
                {
                    // Custom ClassicPvP capture logic runs on landblock-group worker threads; a bug here must not crash the tick.
                    log.Error($"[TICK_EXCEPTION] HandleAllegianceHometownTick aborted for landblock {Id}. ex: {ex}");
                }
            }

            // Database Save
            stopwatch.Restart();
            if (lastDatabaseSave + databaseSaveInterval <= DateTime.UtcNow)
            {
                ProcessPendingWorldObjectAdditionsAndRemovals();

                SaveDB();
                lastDatabaseSave = DateTime.UtcNow;
            }
            ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_Database_Save, stopwatch.Elapsed.TotalSeconds);

            Monitor5m.Pause();
            Monitor1h.Pause();
        }

        /// <summary>
        /// This will tick everything that should be done single threaded on the main ACE World thread
        /// This should be called after TickPhysics() and after Tick()
        /// </summary>
        public void TickSingleThreadedWork(double currentUnixTime)
        {
            if (monitorsRequireEventStart)
            {
                Monitor5m.Restart();
                Monitor1h.Restart();
            }
            else
            {
                Monitor5m.Resume();
                Monitor1h.Resume();
            }

            ProcessPendingWorldObjectAdditionsAndRemovals();

            if (!IsDormant && NextExplorationMarkerRefresh <= currentUnixTime)
                RefreshExplorationMarkers();

            stopwatch.Restart();
            foreach (var player in players)
            {
                try
                {
                    player.Player_Tick(currentUnixTime);
                }
                catch (Exception ex)
                {
                    // One player's tick throwing must not crash the world thread or skip every other player on this landblock.
                    // Short-circuit this player's tick for this frame and leave them in the world.
                    log.Error($"[TICK_EXCEPTION] Player_Tick aborted for {player.Guid}:{player.Name} in landblock {Id}. ex: {ex}");
                }
            }
            ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_Player_Tick, stopwatch.Elapsed.TotalSeconds);

            stopwatch.Restart();
            while (sortedWorldObjectsByNextHeartbeat.Count > 0) // Heartbeat()
            {
                var first = sortedWorldObjectsByNextHeartbeat.First.Value;

                // If they wanted to run before or at now
                if (first.NextHeartbeatTime <= currentUnixTime)
                {
                    sortedWorldObjectsByNextHeartbeat.RemoveFirst();
                    try
                    {
                        first.Heartbeat(currentUnixTime);
                    }
                    catch (Exception ex)
                    {
                        // One misbehaving object must not crash the world tick. Skip its heartbeat this frame.
                        log.Error($"[TICK_EXCEPTION] Heartbeat aborted for {first.Guid}:{first.Name} in landblock {Id}. ex: {ex}");

                        // Push the next heartbeat into the future before re-inserting, otherwise it re-sorts to the
                        // front of the list and gets re-processed this same frame (which would infinite-loop).
                        if (first.NextHeartbeatTime <= currentUnixTime)
                            first.NextHeartbeatTime = currentUnixTime + tickExceptionRetryInterval;
                    }
                    InsertWorldObjectIntoSortedHeartbeatList(first); // WorldObjects can have heartbeats at different intervals
                }
                else
                {
                    break;
                }
            }
            ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Landblock_Tick_WorldObject_Heartbeat, stopwatch.Elapsed.TotalSeconds);

            if (lastZergControlTickDateTime < DateTime.Now.AddSeconds(-5))
            {
                HandleZergControl();
                lastZergControlTickDateTime = DateTime.Now;
            }

            Monitor5m.RegisterEventEnd();
            Monitor1h.RegisterEventEnd();
            monitorsRequireEventStart = true;

            if (DateTime.UtcNow - last5mClear >= last5mClearInteval)
            {
                Monitor5m.ClearEventHistory();
                last5mClear = DateTime.UtcNow;
            }

            if (DateTime.UtcNow - last1hClear >= last1hClearInteval)
            {
                Monitor1h.ClearEventHistory();
                last1hClear = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// If this landblock belongs to a ZergControlArea, buckets every player across all landblocks
        /// in the area by allegiance and boots any allegiance's excess players (over the area's
        /// MaxPlayersPerAllegiance) to their lifestone, kicking the most recently teleported players first.
        /// </summary>
        public void HandleZergControl()
        {
            try
            {
                if (ZergControlLandblocks.IsZergControlLandblock(this.Id.Landblock))
                {
                    var area = ZergControlLandblocks.GetLandblockZergControlArea(this.Id.Landblock);

                    Dictionary<uint, List<Player>> clansInZergControlArea = new Dictionary<uint, List<Player>>();
                    List<Player> playersInZergControlArea = new List<Player>();

                    foreach (var block in area.AreaLandblockIds)
                    {
                        var landblock = LandblockManager.GetLandblock(new LandblockId(block << 16), false);
                        var playersInLandblock = landblock.GetCurrentLandblockPlayers();
                        foreach (var landblockPlayer in playersInLandblock)
                        {
                            var lbPlayerAlleg = AllegianceManager.GetAllegiance(landblockPlayer);
                            if (lbPlayerAlleg != null && lbPlayerAlleg.MonarchId.HasValue && !playersInZergControlArea.Contains(landblockPlayer))
                            {
                                if (clansInZergControlArea.ContainsKey(lbPlayerAlleg.MonarchId.Value))
                                {
                                    clansInZergControlArea[lbPlayerAlleg.MonarchId.Value].Add(landblockPlayer);
                                }
                                else
                                {
                                    var playerList = new List<Player>();
                                    playerList.Add(landblockPlayer);
                                    clansInZergControlArea.Add(lbPlayerAlleg.MonarchId.Value, playerList);
                                }

                                playersInZergControlArea.Add(landblockPlayer);
                            }
                        }
                    }

                    //Boot any excess players from clans with too many players in the area
                    foreach (var clanPlayers in clansInZergControlArea.Values)
                    {
                        if (clanPlayers.Count > area.MaxPlayersPerAllegiance)
                        {
                            var overageCount = clanPlayers.Count - (int)area.MaxPlayersPerAllegiance;
                            var playersToKick = clanPlayers.OrderByDescending(x => x.LastTeleportTime).Take(overageCount);

                            foreach (var playerToKick in playersToKick)
                            {
                                try
                                {
                                    //Teleport to LS.  ThreadSafeTeleport is used because this player may be
                                    //ticking on a different thread than the landblock running this check.
                                    playerToKick.Session.Network.EnqueueSend(new Network.GameMessages.Messages.GameMessageSystemChat("You have exceeded the maximum number of allegiance members allowed inside a zerg restricted area.  You have been returned to your lifestone.", ChatMessageType.Broadcast));
                                    WorldManager.ThreadSafeTeleport(playerToKick, playerToKick.Sanctuary);
                                }
                                catch (Exception ex)
                                {
                                    log.Error($"Failed kicking player {playerToKick.Name} to lifestone after allegiance violated zerg control landblock restrictions.  Ex: {ex}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in HandleZergControl. Ex: {ex}");
            }
        }

        private void ProcessPendingWorldObjectAdditionsAndRemovals()
        {
            if (pendingAdditions.Count > 0)
            {
                foreach (var kvp in pendingAdditions)
                {
                    worldObjects[kvp.Key] = kvp.Value;

                    if (kvp.Value is Player player)
                        players.Add(player);
                    else if (kvp.Value is Creature creature)
                        sortedCreaturesByNextTick.AddLast(creature);

                    InsertWorldObjectIntoSortedHeartbeatList(kvp.Value);
                    InsertWorldObjectIntoSortedGeneratorUpdateList(kvp.Value);
                    InsertWorldObjectIntoSortedGeneratorRegenerationList(kvp.Value);

                    if (kvp.Value.WeenieClassId == 80007) // Landblock KeepAlive weenie (ACE custom)
                        HasNoKeepAliveObjects = false;
                }

                pendingAdditions.Clear();
            }

            if (pendingRemovals.Count > 0)
            {
                foreach (var objectGuid in pendingRemovals)
                {
                    if (worldObjects.Remove(objectGuid, out var wo))
                    {
                        if (wo is Player player)
                            players.Remove(player);
                        else if (wo is Creature creature)
                            sortedCreaturesByNextTick.Remove(creature);

                        sortedWorldObjectsByNextHeartbeat.Remove(wo);
                        sortedGeneratorsByNextGeneratorUpdate.Remove(wo);
                        sortedGeneratorsByNextRegeneration.Remove(wo);

                        if (wo.WeenieClassId == 80007) // Landblock KeepAlive weenie (ACE custom)
                        {
                            var keepAliveObject = worldObjects.Values.FirstOrDefault(w => w.WeenieClassId == 80007);

                            if (keepAliveObject == null)
                                HasNoKeepAliveObjects = true;
                        }
                    }
                }

                pendingRemovals.Clear();
            }
        }

        private void InsertWorldObjectIntoSortedHeartbeatList(WorldObject worldObject)
        {
            // If you want to add checks to exclude certain object types from heartbeating, you would do it here
            if (worldObject.NextHeartbeatTime == double.MaxValue)
                return;

            if (sortedWorldObjectsByNextHeartbeat.Count == 0)
            {
                sortedWorldObjectsByNextHeartbeat.AddFirst(worldObject);
                return;
            }

            if (sortedWorldObjectsByNextHeartbeat.Last.Value.NextHeartbeatTime <= worldObject.NextHeartbeatTime)
            {
                sortedWorldObjectsByNextHeartbeat.AddLast(worldObject);
                return;
            }

            var currentNode = sortedWorldObjectsByNextHeartbeat.First;

            while (currentNode != null)
            {
                if (worldObject.NextHeartbeatTime <= currentNode.Value.NextHeartbeatTime)
                {
                    sortedWorldObjectsByNextHeartbeat.AddBefore(currentNode, worldObject);
                    return;
                }

                currentNode = currentNode.Next;
            }

            sortedWorldObjectsByNextHeartbeat.AddLast(worldObject); // This line really shouldn't be hit
        }

        private void InsertWorldObjectIntoSortedGeneratorUpdateList(WorldObject worldObject)
        {
            // If you want to add checks to exclude certain object types from heartbeating, you would do it here
            if (worldObject.NextGeneratorUpdateTime == double.MaxValue)
                return;

            if (sortedGeneratorsByNextGeneratorUpdate.Count == 0)
            {
                sortedGeneratorsByNextGeneratorUpdate.AddFirst(worldObject);
                return;
            }

            if (sortedGeneratorsByNextGeneratorUpdate.Last.Value.NextGeneratorUpdateTime <= worldObject.NextGeneratorUpdateTime)
            {
                sortedGeneratorsByNextGeneratorUpdate.AddLast(worldObject);
                return;
            }

            var currentNode = sortedGeneratorsByNextGeneratorUpdate.First;

            while (currentNode != null)
            {
                if (worldObject.NextGeneratorUpdateTime <= currentNode.Value.NextGeneratorUpdateTime)
                {
                    sortedGeneratorsByNextGeneratorUpdate.AddBefore(currentNode, worldObject);
                    return;
                }

                currentNode = currentNode.Next;
            }

            sortedGeneratorsByNextGeneratorUpdate.AddLast(worldObject); // This line really shouldn't be hit
        }

        private void InsertWorldObjectIntoSortedGeneratorRegenerationList(WorldObject worldObject)
        {
            // If you want to add checks to exclude certain object types from heartbeating, you would do it here
            if (worldObject.NextGeneratorRegenerationTime == double.MaxValue)
                return;

            if (sortedGeneratorsByNextRegeneration.Count == 0)
            {
                sortedGeneratorsByNextRegeneration.AddFirst(worldObject);
                return;
            }

            if (sortedGeneratorsByNextRegeneration.Last.Value.NextGeneratorRegenerationTime <= worldObject.NextGeneratorRegenerationTime)
            {
                sortedGeneratorsByNextRegeneration.AddLast(worldObject);
                return;
            }

            var currentNode = sortedGeneratorsByNextRegeneration.First;

            while (currentNode != null)
            {
                if (worldObject.NextGeneratorRegenerationTime <= currentNode.Value.NextGeneratorRegenerationTime)
                {
                    sortedGeneratorsByNextRegeneration.AddBefore(currentNode, worldObject);
                    return;
                }

                currentNode = currentNode.Next;
            }

            sortedGeneratorsByNextRegeneration.AddLast(worldObject); // This line really shouldn't be hit
        }

        public void ResortWorldObjectIntoSortedGeneratorRegenerationList(WorldObject worldObject)
        {
            if (sortedGeneratorsByNextRegeneration.Contains(worldObject))
            {
                sortedGeneratorsByNextRegeneration.Remove(worldObject);
                InsertWorldObjectIntoSortedGeneratorRegenerationList(worldObject);
            }
        }

        public void EnqueueAction(IAction action)
        {
            actionQueue.EnqueueAction(action);
        }

        /// <summary>
        /// This will fail if the wo doesn't have a valid location.
        /// </summary>
        public bool AddWorldObject(WorldObject wo)
        {
            if (wo.Location == null)
            {
                log.DebugFormat("Landblock 0x{0} failed to add 0x{1:X8} {2}. Invalid Location", Id, wo.Biota.Id, wo.Name);
                return false;
            }

            wo.BeforeEnterWorld();

            return AddWorldObjectInternal(wo);
        }

        public void AddWorldObjectForPhysics(WorldObject wo)
        {
            AddWorldObjectInternal(wo);
        }

        private bool AddWorldObjectInternal(WorldObject wo)
        {
            if (LandblockManager.CurrentlyTickingLandblockGroupsMultiThreaded)
            {
                if (CurrentLandblockGroup != null && CurrentLandblockGroup != LandblockManager.CurrentMultiThreadedTickingLandblockGroup.Value)
                {
                    // Prevent possible multi-threaded crash
                    // The following scenario can happen rarely in ACE, all in the same call stack with no ActionQueue usage:
                    // Moster successfully lands an attack on a player that procs a cloak spell
                    // The code goes through and does the LaunchSpellProjectiles() which adds the spell projectiles to (presumably) the players landblock
                    // For some unknown reason, the LandblockGroup/Thread where the monster exists seems to be a different LandblockGroup/Thread where the spells are added to.
                    // Maybe there's a player death race condition? Maybe it's a teleport race condition? I dunno.
                    // Because this happens so rarely, and, it only seems to affect cloak projectiles, and, cloak projectiles are pretty benign, we simply don't add the object, and only log it as a warning.
                    if (wo.WeenieType == WeenieType.ProjectileSpell)
                    {
                        log.Warn($"Landblock 0x{Id} entered AddWorldObjectInternal in a cross-thread operation for a ProjectileSpell. This is normally not an issue unless it's happening more than once an hour.");
                        return false;
                    }

                    log.Error($"Landblock 0x{Id} entered AddWorldObjectInternal in a cross-thread operation.");
                    log.Error($"Landblock 0x{Id} CurrentLandblockGroup: {CurrentLandblockGroup}");
                    log.Error($"LandblockManager.CurrentMultiThreadedTickingLandblockGroup.Value: {LandblockManager.CurrentMultiThreadedTickingLandblockGroup.Value}");

                    log.Error($"wo: 0x{wo.Guid}:{wo.Name} [{wo.WeenieClassId} - {wo.WeenieType}], previous landblock 0x{wo.CurrentLandblock?.Id}");

                    if (wo.WeenieType == WeenieType.ProjectileSpell)
                    {
                        if (wo.ProjectileSource != null)
                            log.Error($"wo.ProjectileSource: 0x{wo.ProjectileSource?.Guid}:{wo.ProjectileSource?.Name}, position: {wo.ProjectileSource?.Location}");

                        if (wo.ProjectileTarget != null)
                            log.Error($"wo.ProjectileTarget: 0x{wo.ProjectileTarget?.Guid}:{wo.ProjectileTarget?.Name}, position: {wo.ProjectileTarget?.Location}");
                    }

                    log.Error(System.Environment.StackTrace);

                    log.Error("PLEASE REPORT THIS TO THE ACE DEV TEAM !!!");

                    // This may still crash...
                }
            }

            wo.CurrentLandblock = this;

            if (wo.PhysicsObj == null)
                wo.InitPhysicsObj();
            else
                wo.PhysicsObj.set_object_guid(wo.Guid);  // re-add to ServerObjectManager

            if (wo.PhysicsObj.CurCell == null)
            {
                var success = wo.AddPhysicsObj();
                if (!success)
                {
                    wo.CurrentLandblock = null;

                    if (wo.Generator != null)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug($"AddWorldObjectInternal: couldn't spawn 0x{wo.Guid}:{wo.Name} [{wo.WeenieClassId} - {wo.WeenieType}] at {wo.Location.ToLOCString()} from generator {wo.Generator.WeenieClassId} - 0x{wo.Generator.Guid}:{wo.Generator.Name}");
                        wo.NotifyOfEvent(RegenerationType.PickUp); // Notify generator the generated object is effectively destroyed, use Pickup to catch both cases.
                    }
                    else if (wo.IsGenerator) // Some generators will fail random spawns if they're circumference spans over water or cliff edges
                    {
                        if (log.IsDebugEnabled)
                            log.Debug($"AddWorldObjectInternal: couldn't spawn generator 0x{wo.Guid}:{wo.Name} [{wo.WeenieClassId} - {wo.WeenieType}] at {wo.Location.ToLOCString()}");
                    }
                    else if (wo.ProjectileTarget == null && !(wo is SpellProjectile))
                        log.Warn($"AddWorldObjectInternal: couldn't spawn 0x{wo.Guid}:{wo.Name} [{wo.WeenieClassId} - {wo.WeenieType}] at {wo.Location.ToLOCString()}");

                    return false;
                }
            }

            if (!worldObjects.ContainsKey(wo.Guid))
                pendingAdditions[wo.Guid] = wo;
            else
                pendingRemovals.Remove(wo.Guid);

            // broadcast to nearby players
            wo.NotifyPlayers();

            if (wo is Player player)
                player.SetFogColor(FogColor);

            if (wo is Corpse && wo.Level.HasValue)
            {
                //Set corpse rot time in arena landblocks based on config
                if (this.IsArenaLandblock)
                {
                    wo.TimeToRot = PropertyManager.GetDouble("arena_corpse_rot_seconds", 900).Item;
                }

                var corpseLimit = PropertyManager.GetLong("corpse_spam_limit").Item;
                var corpseList = worldObjects.Values.Union(pendingAdditions.Values).Where(w => w is Corpse && w.Level.HasValue && w.VictimId == wo.VictimId).OrderBy(w => w.CreationTimestamp);

                if (corpseList.Count() > corpseLimit)
                {
                    var corpse = GetObject(corpseList.First(w => w.TimeToRot > Corpse.EmptyDecayTime).Guid);

                    if (corpse != null)
                    {
                        log.Warn($"[CORPSE] Landblock.AddWorldObjectInternal(): {wo.Name} (0x{wo.Guid}) exceeds the per player limit of {corpseLimit} corpses for 0x{Id.Landblock:X4}. Adjusting TimeToRot for oldest {corpse.Name} (0x{corpse.Guid}), CreationTimestamp: {corpse.CreationTimestamp} ({Common.Time.GetDateTimeFromTimestamp(corpse.CreationTimestamp ?? 0).ToLocalTime():yyyy-MM-dd HH:mm:ss}), to Corpse.EmptyDecayTime({Corpse.EmptyDecayTime}).");
                        corpse.TimeToRot = Corpse.EmptyDecayTime;
                    }
                }
            }

            return true;
        }

        public void RemoveWorldObject(ObjectGuid objectId, bool adjacencyMove = false, bool fromPickup = false, bool showError = true)
        {
            RemoveWorldObjectInternal(objectId, adjacencyMove, fromPickup, showError);
        }

        /// <summary>
        /// Should only be called by physics/relocation engines -- not from player
        /// </summary>
        /// <param name="objectId">The object ID to be removed from the current landblock</param>
        /// <param name="adjacencyMove">Flag indicates if object is moving to an adjacent landblock</param>
        public void RemoveWorldObjectForPhysics(ObjectGuid objectId, bool adjacencyMove = false)
        {
            RemoveWorldObjectInternal(objectId, adjacencyMove);
        }

        private void RemoveWorldObjectInternal(ObjectGuid objectId, bool adjacencyMove = false, bool fromPickup = false, bool showError = true)
        {
            if (LandblockManager.CurrentlyTickingLandblockGroupsMultiThreaded)
            {
                if (CurrentLandblockGroup != null && CurrentLandblockGroup != LandblockManager.CurrentMultiThreadedTickingLandblockGroup.Value)
                {
                    log.Error($"Landblock 0x{Id} entered RemoveWorldObjectInternal in a cross-thread operation.");
                    log.Error($"Landblock 0x{Id} CurrentLandblockGroup: {CurrentLandblockGroup}");
                    log.Error($"LandblockManager.CurrentMultiThreadedTickingLandblockGroup.Value: {LandblockManager.CurrentMultiThreadedTickingLandblockGroup.Value}");

                    log.Error($"objectId: 0x{objectId}");

                    log.Error(System.Environment.StackTrace);

                    log.Error("PLEASE REPORT THIS TO THE ACE DEV TEAM !!!");

                    // This may still crash...
                }
            }

            if (worldObjects.TryGetValue(objectId, out var wo))
                pendingRemovals.Add(objectId);
            else if (!pendingAdditions.Remove(objectId, out wo))
            {
                if (showError)
                    log.Warn($"RemoveWorldObjectInternal: Couldn't find {objectId.Full:X8}");
                return;
            }

            wo.CurrentLandblock = null;

            // Weenies can come with a default of 0 (Instant Rot) or -1 (Never Rot). If they still have that value, we want to retain it.
            // We also want to make sure fromPickup is true so that we're not clearing out TimeToRot on server shutdown (unloads all landblocks and removed all objects).
            if (fromPickup && wo.TimeToRot.HasValue && wo.TimeToRot != 0 && wo.TimeToRot != -1)
                wo.TimeToRot = null;

            if (!adjacencyMove)
            {
                // really remove it - send message to client to remove object
                wo.EnqueueActionBroadcast(p => p.RemoveTrackedObject(wo, fromPickup));

                wo.PhysicsObj.DestroyObject();
            }
        }

        public void EmitSignal(WorldObject emitter, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            foreach (var wo in worldObjects.Values.Where(w => w.HearLocalSignals).ToList())
            {
                if (emitter == wo) continue;

                if (emitter.IsWithinUseRadiusOf(wo, wo.HearLocalSignalsRadius))
                {
                    //Console.WriteLine($"{wo.Name}.EmoteManager.OnLocalSignal({emitter.Name}, {message})");
                    wo.EmoteManager.OnLocalSignal(emitter, message);
                }
            }
        }

        /// <summary>
        /// Check to see if we are close enough to interact.   Adds a fudge factor of 1.5f
        /// </summary>
        public bool WithinUseRadius(Player player, ObjectGuid targetGuid, out bool validTargetGuid, float? useRadius = null)
        {
            var target = GetObject(targetGuid);

            validTargetGuid = target != null;

            if (target != null)
                return player.IsWithinUseRadiusOf(target, useRadius);

            return false;
        }

        /// <summary>
        /// Returns landblock objects with physics initialized
        /// </summary>
        public ICollection<WorldObject> GetWorldObjectsForPhysicsHandling()
        {
            // If a missile is destroyed when it runs it's UpdateObjectPhysics(), it will remove itself from the landblock, thus, modifying the worldObjects collection.

            ProcessPendingWorldObjectAdditionsAndRemovals();

            return worldObjects.Values;
        }

        public List<WorldObject> GetAllWorldObjectsForDiagnostics()
        {
            // We do not ProcessPending here, and we return ToList() to avoid cross-thread issues.
            // This can happen if we "loadalllandblocks" and do a "serverstatus".
            return worldObjects.Values.ToList();
        }

        public List<ushort> GetAdjacents(bool includeSelf = true)
        {
            List<ushort> list = new List<ushort>();

            if (includeSelf)
                list.Add(Id.Landblock);

            var adjacentLandblocks = LandblockManager.GetAdjacentIDs(this);
            foreach (var adjacent in adjacentLandblocks)
            {
                list.Add(adjacent.Landblock);
            }

            return list;
        }

        public List<LandblockInstance> GetLandblockInstances(bool includeAdjacents)
        {
            var list = DatabaseManager.World.GetCachedInstancesByLandblock(Id.Landblock).ToList();

            if (includeAdjacents)
            {
                var adjacentLandblocks = LandblockManager.GetAdjacentIDs(this);
                foreach (var adjacent in adjacentLandblocks)
                {
                    list.AddRange(DatabaseManager.World.GetCachedInstancesByLandblock(adjacent.Landblock));
                }
            }

            return list;
        }

        private List<Position> RoadList;

        private static List<Position> BuildRoadList(LandblockId landblockId)
        {
            var roadList = new List<Position>();

            var cellLandblock = DatManager.CellDat.ReadFromDat<CellLandblock>(landblockId.Raw | 0xFFFF);
            var landblockInfo = DatManager.CellDat.ReadFromDat<LandblockInfo>((uint)landblockId.Landblock << 16 | 0xFFFE);

            if (cellLandblock != null)
            {
                for (int cellX = 0; cellX < LandDefs.BlockSide; cellX++)
                {
                    for (int cellY = 0; cellY < LandDefs.BlockSide; cellY++)
                    {
                        var terrain = cellLandblock.Terrain[cellX * (LandDefs.BlockSide + 1) + cellY];
                        if ((terrain & 0x3) != 0) // road cell
                        {
                            var xPos = Math.Clamp(cellX * LandDefs.CellLength, 0, LandDefs.BlockLength);
                            var yPos = Math.Clamp(cellY * LandDefs.CellLength, 0, LandDefs.BlockLength);

                            var pos = new Position((uint)(landblockId.Landblock << 16) | 1, xPos, yPos, 0, 0, 0, 0, 1);
                            pos.AdjustMapCoords();

                            roadList.Add(pos);
                        }
                    }
                }
            }

            if (landblockInfo != null)
            {
                foreach (var obj in landblockInfo.Objects)
                {
                    if (obj.Id == 0x02000451) // Desert path marker
                    {
                        var pos = new Position((uint)(landblockId.Landblock << 16) | 1, obj.Frame.Origin.X, obj.Frame.Origin.Y, obj.Frame.Origin.Z, obj.Frame.Orientation.X, obj.Frame.Orientation.Y, obj.Frame.Orientation.Z, obj.Frame.Orientation.W);
                        pos.AdjustMapCoords();
                        roadList.Add(pos);
                    }
                }
            }

            return roadList;
        }
        public float GetDistanceToNearestRoad(Position position, out Position roadPosition, Position avoidPosition = null)
        {
            roadPosition = null;

            if (IsDungeon)
                return float.MaxValue;

            if (RoadList == null)
            {
                RoadList = BuildRoadList(Id);

                var adjacentLandblocks = LandblockManager.GetAdjacentIDs(this);
                foreach (var adjacent in adjacentLandblocks)
                {
                    RoadList.AddRange(BuildRoadList(adjacent));
                }
            }

            var closest = float.MaxValue;
            foreach(var entry in RoadList)
            {
                if (avoidPosition != null && entry.DistanceTo(avoidPosition) < 2)
                    continue;

                var distance = entry.DistanceTo(position);
                if (distance < closest)
                {
                    closest = distance;
                    roadPosition = new Position(entry);
                }
            }

            return closest;
        }
        public WorldObject GetObject(uint objectId)
        {
            return GetObject(new ObjectGuid(objectId));
        }

        /// <summary>
        /// This will return null if the object was not found in the current or adjacent landblocks.
        /// </summary>
        public WorldObject GetObject(ObjectGuid guid, bool searchAdjacents = true)
        {
            if (pendingRemovals.Contains(guid))
                return null;

            if (worldObjects.TryGetValue(guid, out var worldObject) || pendingAdditions.TryGetValue(guid, out worldObject))
                return worldObject;

            if (searchAdjacents)
            {
                foreach (Landblock lb in Adjacents)
                {
                    if (lb != null)
                    {
                        var wo = lb.GetObject(guid, false);

                        if (wo != null)
                            return wo;
                    }
                }
            }

            return null;
        }

        public WorldObject GetWieldedObject(uint objectGuid, bool searchAdjacents = true)
        {
            return GetWieldedObject(new ObjectGuid(objectGuid), searchAdjacents); // todo fix
        }

        /// <summary>
        /// Searches this landblock (and possibly adjacents) for an ObjectGuid wielded by a creature
        /// </summary>
        public WorldObject GetWieldedObject(ObjectGuid guid, bool searchAdjacents = true)
        {
            // search creature wielded items in current landblock
            var creatures = worldObjects.Values.OfType<Creature>();
            foreach (var creature in creatures)
            {
                var wieldedItem = creature.GetEquippedItem(guid);
                if (wieldedItem != null)
                {
                    if ((wieldedItem.CurrentWieldedLocation & EquipMask.Selectable) != 0)
                        return wieldedItem;

                    return null;
                }
            }

            // try searching adjacent landblocks if not found
            if (searchAdjacents)
            {
                foreach (var adjacent in Adjacents)
                {
                    if (adjacent == null) continue;

                    var wieldedItem = adjacent.GetWieldedObject(guid, false);
                    if (wieldedItem != null)
                        return wieldedItem;
                }
            }
            return null;
        }

        /// <summary>
        /// Sets a landblock to active state, with the current time as the LastActiveTime
        /// </summary>
        /// <param name="isAdjacent">Public calls to this function should always set isAdjacent to false</param>
        public void SetActive(bool isAdjacent = false)
        {
            lastActiveTime = DateTime.UtcNow;
            IsDormant = false;

            if (isAdjacent || PhysicsLandblock == null || PhysicsLandblock.IsDungeon) return;

            // for outdoor landblocks, recursively call 1 iteration to set adjacents to active
            foreach (var landblock in Adjacents)
            {
                if (landblock != null)
                    landblock.SetActive(true);
            }
        }

        /// <summary>
        /// Handles the cleanup process for a landblock
        /// This method is called by LandblockManager
        /// </summary>
        public void Unload()
        {
            var landblockID = Id.Raw | 0xFFFF;

            //log.DebugFormat("Landblock.Unload({0:X8})", landblockID);

            ProcessPendingWorldObjectAdditionsAndRemovals();

            SaveDB();

            // remove all objects
            foreach (var wo in worldObjects.ToList())
            {
                if (!wo.Value.BiotaOriginatedFromOrHasBeenSavedToDatabase())
                    wo.Value.Destroy(false, true);
                else
                    RemoveWorldObjectInternal(wo.Key);
            }

            ProcessPendingWorldObjectAdditionsAndRemovals();

            actionQueue.Clear();

            // remove physics landblock
            LScape.unload_landblock(landblockID);

            PhysicsLandblock.release_shadow_objs();

            Pathfinder.TryUnloadMesh(this);
        }

        public void DestroyAllNonPlayerObjects()
        {
            ProcessPendingWorldObjectAdditionsAndRemovals();

            SaveDB();

            // remove all objects
            foreach (var wo in worldObjects.Where(i => !(i.Value is Player)).ToList())
            {
                if (!wo.Value.BiotaOriginatedFromOrHasBeenSavedToDatabase())
                    wo.Value.Destroy(false, true);
                else
                    RemoveWorldObjectInternal(wo.Key);
            }

            ProcessPendingWorldObjectAdditionsAndRemovals();

            actionQueue.Clear();
        }

        private void SaveDB()
        {
            var biotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();

            foreach (var wo in worldObjects.Values)
            {
                if (wo.IsStaticThatShouldPersistToShard() || wo.IsDynamicThatShouldPersistToShard())
                    AddWorldObjectToBiotasSaveCollection(wo, biotas);
            }

            DatabaseManager.Shard.SaveBiotasInParallel(biotas, null);
        }

        private void AddWorldObjectToBiotasSaveCollection(WorldObject wo, Collection<(Biota biota, ReaderWriterLockSlim rwLock)> biotas)
        {
            if (wo.ChangesDetected)
            {
                wo.SaveBiotaToDatabase(false);
                biotas.Add((wo.Biota, wo.BiotaDatabaseLock));
            }

            if (wo is Container container)
            {
                foreach (var item in container.Inventory.Values)
                    AddWorldObjectToBiotasSaveCollection(item, biotas);
            }
        }

        /// <summary>
        /// This is only used for very specific instances, such as broadcasting player deaths to the destination lifestone block
        /// This is a rarely used method to broadcast network messages to all of the players within a landblock,
        /// and possibly the adjacent landblocks.
        /// </summary>
        public void EnqueueBroadcast(ICollection<Player> excludeList, bool adjacents, Position pos = null, float? maxRangeSq = null, params GameMessage[] msgs)
        {
            var players = worldObjects.Values.OfType<Player>();

            // for landblock death broadcasts:
            // exclude players that have already been broadcast to within range of the death
            if (excludeList != null)
                players = players.Except(excludeList);

            // broadcast messages to player in this landblock
            foreach (var player in players)
            {
                if (pos != null && maxRangeSq != null)
                {
                    var distSq = player.Location.SquaredDistanceTo(pos);
                    if (distSq > maxRangeSq)
                        continue;
                }
                player.Session.Network.EnqueueSend(msgs);
            }

            // if applicable, iterate into adjacent landblocks
            if (adjacents)
            {
                foreach (var adjacent in this.Adjacents.Where(adj => adj != null))
                    adjacent.EnqueueBroadcast(excludeList, false, pos, maxRangeSq, msgs);
            }
        }

        /// <summary>
        /// Returns a snapshot list of all players currently on this landblock.
        /// Used by Town Control reward distribution and other cross-system queries.
        /// </summary>
        public List<Player> GetPlayers() => new List<Player>(players);

        private bool? isDungeon;

        /// <summary>
        /// Returns TRUE if this landblock is a dungeon,
        /// with no traversable overworld
        /// </summary>
        public bool IsDungeon
        {
            get
            {
                // return cached value
                if (isDungeon != null)
                    return isDungeon.Value;

                // hack for NW island
                // did a worldwide analysis for adding watercells into the formula,
                // but they are inconsistently defined for some of the edges of map unfortunately
                if (Id.LandblockX < 0x08 && Id.LandblockY > 0xF8)
                {
                    isDungeon = false;
                    return isDungeon.Value;
                }

                // a dungeon landblock is determined by:
                // - all heights being 0
                // - having at least 1 EnvCell (0x100+)
                // - contains no buildings
                foreach (var height in CellLandblock.Height)
                {
                    if (height != 0)
                    {
                        isDungeon = false;
                        return isDungeon.Value;
                    }
                }
                isDungeon = LandblockInfo != null && LandblockInfo.NumCells > 0 && LandblockInfo.Buildings != null && LandblockInfo.Buildings.Count == 0;
                return isDungeon.Value;
            }
        }

        private bool? hasDungeon;

        /// <summary>
        /// Returns TRUE if this landblock contains a dungeon
        //
        /// If a landblock contains both a dungeon + traversable overworld,
        /// this field will return TRUE, whereas IsDungeon will return FALSE
        /// 
        /// This property should only be used in very specific scenarios,
        /// such as determining if a landblock contains a mansion basement
        /// </summary>
        public bool HasDungeon
        {
            get
            {
                // return cached value
                if (hasDungeon != null)
                    return hasDungeon.Value;

                hasDungeon = LandblockInfo != null && LandblockInfo.NumCells > 0 && LandblockInfo.Buildings != null && LandblockInfo.Buildings.Count == 0;
                return hasDungeon.Value;
            }
        }


        private bool? _isArenaLandblock;
        public bool IsArenaLandblock
        {
            get
            {
                if (_isArenaLandblock == null)
                    _isArenaLandblock = ACE.Server.Entity.ArenaLocation.IsArenaLandblock(this.Id.Landblock);
                return _isArenaLandblock.Value;
            }
        }

        /// <summary>
        /// Returns true when this landblock is a valid location for bounty hunting.
        /// ClassicPvP has no special bounty-only zones, so this always returns false and
        /// the system relies on bounty_allow_all_locations = true (default).
        /// Add specific landblock IDs here if dedicated bounty zones are ever added.
        /// </summary>
        public bool IsBountyLocation => false;

        public List<Player> GetCurrentLandblockPlayers()
        {
            var playerList = new List<Player>();
            playerList.AddRange(players);
            return playerList;
        }

        public List<House> Houses = new List<House>();

        public void SetFogColor(EnvironChangeType environChangeType)
        {
            if (environChangeType.IsFog())
            {
                FogColor = environChangeType;

                foreach (var lb in Adjacents)
                    lb.FogColor = environChangeType;

                foreach(var player in players)
                {
                    player.SetFogColor(FogColor);
                }
            }
        }

        public void SendEnvironSound(EnvironChangeType environChangeType)
        {
            if (environChangeType.IsSound())
            {
                SendEnvironChange(environChangeType);

                foreach (var lb in Adjacents)
                    lb.SendEnvironChange(environChangeType);
            }
        }

        public void SendEnvironChange(EnvironChangeType environChangeType)
        {
            foreach (var player in players)
            {
                player.SendEnvironChange(environChangeType);
            }
        }

        public void SendCurrentEnviron()
        {
            foreach (var player in players)
            {
                if (FogColor.IsFog())
                {
                    player.SetFogColor(FogColor);
                }
                else
                {
                    player.SendEnvironChange(FogColor);
                }
            }
        }

        public void DoEnvironChange(EnvironChangeType environChangeType)
        {
            if (environChangeType.IsFog())
                SetFogColor(environChangeType);
            else
                SendEnvironSound(environChangeType);
        }

        public static string GetLocationString(ushort landblockId)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return "";

            var landblock = DatabaseManager.World.GetLandblockDescriptionsByLandblock(landblockId).FirstOrDefault();
            string locationString = "";
            if (landblock != null)
            {
                if (landblock.Name != "")
                {
                    if (landblock.Reference != $"in {landblock.Name}")
                        locationString = $" in {landblock.Name} {landblock.Reference}";
                    else
                        locationString = $" {landblock.Reference}";
                }
                else
                {
                    if (landblock.MicroRegion != "")
                        locationString = $" {landblock.Reference} in {landblock.MicroRegion}";
                    else if (landblock.MacroRegion != "" && landblock.MacroRegion != "Dereth")
                        locationString = $" {landblock.Reference} in {landblock.MacroRegion}";
                    else
                        locationString = $" {landblock.Reference}";
                }
            }

            return locationString;
        }

        public static string GetLocString(Position pos)
        {
            if (pos == null)
                return "Unknown Location";

            if (!pos.Indoors)
                return pos.GetMapCoordStr();

            var dungeon = DatabaseManager.World.GetDungeonInformationByLandblock(pos.GetLandblockHex());
            if (dungeon != null)
                return dungeon.Name;

            // Not a named dungeon. The position is flagged Indoors because the target is
            // standing inside an environment cell (cell >= 0x100), but that cell may just be
            // a building, hut, or tunnel stamped onto an outdoor landblock rather than a true
            // underground dungeon. In that case the landblock is still on the surface map, so
            // we can hand back its map coordinates (accurate to within the landblock) instead
            // of a bare id that the player can't act on.
            try
            {
                var physicsLandblock = LScape.get_landblock(pos.LandblockId.Raw);
                if (physicsLandblock != null && !physicsLandblock.IsDungeon)
                {
                    // resolve to the landblock center as an outdoor cell so GetMapCoordStr
                    // can compute coords (the env-cell-relative X/Y can't be trusted here)
                    var surfacePos = new Position(pos);
                    surfacePos.LandblockId = new LandblockId((pos.LandblockId.Raw & 0xFFFF0000) | 0x0001);
                    surfacePos.PositionX = Position.BlockLength / 2;
                    surfacePos.PositionY = Position.BlockLength / 2;

                    var coords = surfacePos.GetMapCoordStr();
                    if (coords != null)
                        return $"{coords} (inside a structure)";
                }
            }
            catch (Exception ex)
            {
                log.Error($"GetLocString: failed resolving surface coords for 0x{pos.Cell:X8}: {ex}");
            }

            return $"Dungeon (0x{pos.Landblock:X4} / Cell 0x{pos.Cell:X8})";
        }
    }
}
