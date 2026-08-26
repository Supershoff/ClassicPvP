using System;
using System.Collections.Generic;
using System.Linq;

using log4net;

using ACE.Common;
using ACE.Database;
using ACE.Database.Models.Log;
using ACE.Entity.Enum;
using ACE.Server.Entity.AllegianceHometown;
using ACE.Server.Factories;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Managers
{
    /// <summary>
    /// Manages in-memory state for Allegiance Hometown capture.
    /// All hot-path reads go through in-memory dictionaries; DB is only hit during
    /// Initialize() and on explicit saves.
    /// </summary>
    public static class AllegianceHometownManager
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool IsInitialized { get; private set; }

        // town_id → current DB row (mutable, kept in sync with DB)
        /// <summary>
        /// Guards every collection below, and the AllegianceHometownTown objects inside _towns.
        /// Each town's landblock ticks on its own landblock group, and LandblockManager runs those
        /// groups through Parallel.ForEach — so TickPhase1, the Phase 2 proxy heartbeats, the proxy
        /// death chains, and the player/admin commands all reach this state concurrently.
        ///
        /// Hold this only across in-memory work. Database writes block on a round-trip and the
        /// broadcast/reward paths call into PlayerManager, LandblockManager and world objects, so
        /// they must happen outside the lock: doing otherwise stalls landblock threads on the
        /// database and risks a lock-order deadlock. The pattern throughout is to decide and mutate
        /// under the lock, capture what's needed into locals, then do the callouts after releasing.
        /// (Monitor is reentrant, so the locked private helpers are safe to call from locked code.)
        /// </summary>
        private static readonly object _lock = new object();

        private static readonly Dictionary<byte, AllegianceHometownTown> _towns = new();

        // monarch_id → set of town_ids that allegiance currently owns
        private static readonly Dictionary<uint, HashSet<byte>> _ownedByMonarch = new();

        // monarch_id → active conflict town_ids (phase 1 or 2 in progress)
        private static readonly Dictionary<uint, HashSet<byte>> _activeConflictsByMonarch = new();

        // cooldowns: (monarchId, townId) → DateTime when cooldown expires
        // covers both phase-1-timeout cooldowns and phase-2-failure cooldowns
        private static readonly Dictionary<(uint, byte), DateTime> _attackerCooldowns = new();

        // per-town: monarch_id → cooldown expiry after a successful capture (8h default)
        private static readonly Dictionary<byte, DateTime> _captureProtection = new();

        // latest event per town (for audit log cache)
        private static readonly Dictionary<byte, AllegianceHometownEvent> _latestEventByTown = new();

        // town_id → active Phase 2 creature proxy world object
        private static readonly Dictionary<byte, BindstoneCreatureProxy> _phase2Proxies = new();

        // player_guid → next UTC time that player is eligible for a periodic Phase 2 participation trophy.
        // Guarded by _lock. Stale entries for players no longer in a conflict are harmless (they just
        // mean the next award is already due) and are overwritten the next time that player participates.
        private static readonly Dictionary<uint, DateTime> _phase2NextTrophy = new();

        // town_id → real Bindstone WO cloaked during Phase 2
        private static readonly Dictionary<byte, WorldObjects.Bindstone> _phase2CloakedBindstones = new();

        // monarch IDs permanently banned from attacking towns
        private static readonly System.Collections.Generic.HashSet<uint> _blacklist = new();
        private static readonly System.Collections.Generic.Dictionary<uint, ACE.Database.Models.Log.AllegianceHometownBlacklist> _blacklistEntries = new();

        public const int MaxSimultaneousAttacks = 2;

        /// <summary>
        /// Seconds an attacking allegiance must hold the bind stone to complete Phase 1.
        /// Configurable via the "ah_phase1_seconds" server property (default 240 = 4 minutes).
        /// </summary>
        public static double Phase1DurationSeconds =>
            PropertyManager.GetLong("ah_phase1_seconds", 240).Item;

        /// <summary>
        /// How long the attackers have to destroy the bind stone before the defenders win Phase 2.
        /// Configurable via the "ah_phase2_minutes" server property (default 30).
        /// </summary>
        public static TimeSpan Phase2Duration =>
            TimeSpan.FromMinutes(PropertyManager.GetLong("ah_phase2_minutes", 30).Item);

        /// <summary>
        /// How long defenders must hold the meeting hall (2+ defenders, 0 non-defenders present) before
        /// Phase 2 auto-resolves as a repelled attack. Configurable via "ah_phase2_repel_minutes" (default 10).
        /// </summary>
        public static double Phase2RepelSeconds =>
            PropertyManager.GetLong("ah_phase2_repel_minutes", 10).Item * 60.0;

        /// <summary>
        /// How long the meeting hall landblock is held loaded once Phase 2 starts. Derived from
        /// Phase2Duration so raising the phase length can't leave the landblock unloading
        /// underneath a live conflict; the margin covers the death sequence and payout.
        /// </summary>
        public static TimeSpan Phase2PermaloadDuration =>
            Phase2Duration + TimeSpan.FromMinutes(5);

        /// <summary>
        /// A human-readable identity for a player's allegiance: the custom allegiance name if
        /// one is set, otherwise "{Monarch}'s Allegiance". Falls back to the player's own name
        /// when no allegiance or monarch name is available.
        /// </summary>
        public static string GetAllegianceIdentity(WorldObjects.Player player)
        {
            var allegiance = player?.Allegiance;
            if (allegiance == null)
                return player?.Name ?? "Unknown";

            if (!string.IsNullOrWhiteSpace(allegiance.AllegianceName))
                return allegiance.AllegianceName;

            var monarchName = allegiance.Monarch?.Player?.Name;
            return !string.IsNullOrWhiteSpace(monarchName) ? $"{monarchName}'s Allegiance" : player.Name;
        }

        // -----------------------------------------------------------------------
        // Initialization
        // -----------------------------------------------------------------------

        public static void Initialize()
        {
            try
            {
                // Read from the database before taking the lock.
                var blacklistRows = DatabaseManager.Log.GetAllAllegianceHometownBlacklist();
                var rows          = DatabaseManager.Log.GetAllAllegianceHometownTowns();

                // Towns left mid-conflict by a shutdown; persisted after the lock is released.
                var conflictsToClear = new List<AllegianceHometownTown>();
                int townCount;

                lock (_lock)
                {
                    _towns.Clear();
                    _ownedByMonarch.Clear();
                    _activeConflictsByMonarch.Clear();
                    _attackerCooldowns.Clear();
                    _captureProtection.Clear();
                    _latestEventByTown.Clear();
                    _phase2Proxies.Clear();
                    _blacklist.Clear();
                    _blacklistEntries.Clear();

                    foreach (var bl in blacklistRows)
                    {
                        _blacklist.Add(bl.MonarchId);
                        _blacklistEntries[bl.MonarchId] = bl;
                    }

                    // Index by town_id; fill in any missing rows from registry
                    foreach (var entry in AllegianceHometownRegistry.All.Values)
                    {
                        var row = rows.FirstOrDefault(r => r.TownId == entry.TownId);
                        if (row == null)
                        {
                            row = new AllegianceHometownTown { TownId = entry.TownId, TownName = entry.TownName };
                        }
                        _towns[entry.TownId] = row;

                        // Rebuild ownership index
                        if (row.OwnerMonarchId.HasValue)
                            GetOrCreateSet(_ownedByMonarch, row.OwnerMonarchId.Value).Add(row.TownId);

                        // Any town that was in-conflict when server shut down: clear conflict state
                        // (Phase 1/2 cannot survive a server restart gracefully)
                        if (row.ConflictPhase != 0)
                        {
                            row.ConflictPhase             = 0;
                            row.ConflictAttackerMonarchId = null;
                            row.ConflictAttackerName      = null;
                            row.ConflictStartTime         = null;
                            row.Phase2StartTime           = null;
                            conflictsToClear.Add(row);
                        }

                        // Restore capture protection: if captured_at is within the protection window, honour it
                        if (row.CapturedAt.HasValue)
                        {
                            var protectionWindowHours = PropertyManager.GetDouble("ah_capture_protection_hours", 8.0).Item;
                            var expiry = row.CapturedAt.Value.AddHours(protectionWindowHours);
                            if (expiry > DateTime.UtcNow)
                                _captureProtection[row.TownId] = expiry;
                        }
                    }

                    townCount = _towns.Count;
                }

                foreach (var row in conflictsToClear)
                    DatabaseManager.Log.UpdateAllegianceHometownTown(row);

                IsInitialized = true;
                log.Info($"[AllegianceHometown] Initialized with {townCount} town(s).");
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Initialize failed. Ex: {ex}");
            }
        }

        // -----------------------------------------------------------------------
        // Town reads
        // -----------------------------------------------------------------------

        public static AllegianceHometownTown GetTown(byte townId)
        {
            lock (_lock)
            {
                _towns.TryGetValue(townId, out var t);
                return t;
            }
        }

        /// <summary>
        /// Snapshot of every town. Returns a copy — handing out the live collection would let a
        /// caller enumerate it while another landblock thread writes to it.
        /// </summary>
        public static IReadOnlyCollection<AllegianceHometownTown> GetAllTowns()
        {
            lock (_lock)
                return _towns.Values.ToList();
        }

        /// <summary>
        /// Phase 2 state for a town, read atomically. The Phase 2 proxy heartbeat runs on a
        /// landblock thread while conflicts resolve on others, so it reads through this rather than
        /// pulling fields off a shared town object mid-update.
        /// </summary>
        public static bool TryGetPhase2Status(byte townId, out DateTime phase2StartTime, out string attackerName, out string townName)
        {
            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out var t) || t.ConflictPhase != 2 || !t.Phase2StartTime.HasValue)
                {
                    phase2StartTime = default;
                    attackerName    = null;
                    townName        = null;
                    return false;
                }

                phase2StartTime = t.Phase2StartTime.Value;
                attackerName    = t.ConflictAttackerName;
                townName        = t.TownName;
                return true;
            }
        }

        public static IReadOnlyCollection<byte> GetOwnedTownIds(uint monarchId)
        {
            lock (_lock)
            {
                _ownedByMonarch.TryGetValue(monarchId, out var s);
                return s != null ? s.ToList() : (IReadOnlyCollection<byte>)Array.Empty<byte>();
            }
        }

        public static int GetOwnedTownCount(uint monarchId)
        {
            lock (_lock)
            {
                _ownedByMonarch.TryGetValue(monarchId, out var s);
                return s?.Count ?? 0;
            }
        }

        public static bool IsInConflict(byte townId)
        {
            lock (_lock)
            {
                _towns.TryGetValue(townId, out var t);
                return t != null && t.ConflictPhase != 0;
            }
        }

        public static bool IsTownProtected(byte townId)
        {
            lock (_lock)
                return _captureProtection.TryGetValue(townId, out var exp) && exp > DateTime.UtcNow;
        }

        public static bool ClearTownProtection(byte townId)
        {
            lock (_lock)
                return _captureProtection.Remove(townId);
        }

        /// <summary>
        /// Returns a human-readable block reason if this monarch cannot attack the town due to
        /// blacklist, protection, or cooldown. Returns null if no persistent block exists.
        /// Does NOT modify any state.
        /// </summary>
        public static string GetAttackBlockReason(byte townId, uint attackerMonarchId)
        {
            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out var town)) return null;

                if (_blacklist.Contains(attackerMonarchId))
                    return "Your allegiance has been suspended from hometown warfare.";

                if (town.OwnerMonarchId == attackerMonarchId)
                    return null; // owner — handled separately by caller

                if (_captureProtection.TryGetValue(townId, out var exp) && exp > DateTime.UtcNow)
                    return $"This town cannot be attacked for another {FormatTimeSpan(exp - DateTime.UtcNow)}.";

                if (HasAttackerCooldown(attackerMonarchId, townId, out var cd))
                    return $"Your allegiance cannot attack {town.TownName} for another {FormatTimeSpan(cd)}.";

                return null;
            }
        }

        // -----------------------------------------------------------------------
        // Cooldown checks
        // -----------------------------------------------------------------------

        public static bool HasAttackerCooldown(uint monarchId, byte townId, out TimeSpan remaining)
        {
            lock (_lock)
            {
                if (_attackerCooldowns.TryGetValue((monarchId, townId), out var exp) && exp > DateTime.UtcNow)
                {
                    remaining = exp - DateTime.UtcNow;
                    return true;
                }
                remaining = TimeSpan.Zero;
                return false;
            }
        }

        public static int GetActiveConflictCount(uint monarchId)
        {
            lock (_lock)
            {
                _activeConflictsByMonarch.TryGetValue(monarchId, out var s);
                return s?.Count ?? 0;
            }
        }

        // -----------------------------------------------------------------------
        // Phase 1 — start / tick / timeout
        // -----------------------------------------------------------------------

        /// <summary>
        /// Attempts to start Phase 1 for the given attacker allegiance on the given town.
        /// Returns false (with reason message) if blocked by cooldowns, protection, or conflict limits.
        /// </summary>
        public static bool TryStartPhase1(byte townId, uint attackerMonarchId, string attackerName,
            out string failReason)
        {
            failReason = null;

            AllegianceHometownTown town;
            string defenderName;
            uint?  defenderMonarchId;
            string townName;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town))
                {
                    failReason = "Unknown town.";
                    return false;
                }

                // Blacklisted allegiances cannot attack
                if (_blacklist.Contains(attackerMonarchId))
                {
                    failReason = "Your allegiance has been suspended from hometown warfare.";
                    return false;
                }

                // Can't attack your own town
                if (town.OwnerMonarchId == attackerMonarchId)
                {
                    failReason = "Your allegiance already owns that town.";
                    return false;
                }

                // Town already in conflict
                if (town.ConflictPhase != 0)
                {
                    failReason = "That town is already under attack.";
                    return false;
                }

                // Town is under capture protection
                if (_captureProtection.TryGetValue(townId, out var exp) && exp > DateTime.UtcNow)
                {
                    failReason = $"That town cannot be attacked for another {FormatTimeSpan(exp - DateTime.UtcNow)}.";
                    return false;
                }

                // Attacker cooldown (phase-1 timeout or phase-2 failure)
                if (HasAttackerCooldown(attackerMonarchId, townId, out var cd))
                {
                    failReason = $"Your allegiance cannot attack {town.TownName} for another {FormatTimeSpan(cd)}.";
                    return false;
                }

                // Simultaneous attack limit
                if (GetActiveConflictCount(attackerMonarchId) >= MaxSimultaneousAttacks)
                {
                    failReason = $"Your allegiance is already attacking {MaxSimultaneousAttacks} towns.";
                    return false;
                }

                defenderName      = town.OwnerAllegianceName;
                defenderMonarchId = town.OwnerMonarchId;
                townName          = town.TownName;

                town.ConflictPhase             = 1;
                town.ConflictAttackerMonarchId = attackerMonarchId;
                town.ConflictAttackerName      = attackerName;
                town.ConflictStartTime         = DateTime.UtcNow;
                town.Phase2StartTime           = null;

                GetOrCreateSet(_activeConflictsByMonarch, attackerMonarchId).Add(townId);
            }

            SaveTownDb(town);

            // Log event
            var evt = DatabaseManager.Log.StartAllegianceHometownEvent(
                townId, attackerMonarchId, attackerName,
                defenderMonarchId, defenderName);
            if (evt != null)
            {
                lock (_lock)
                    _latestEventByTown[townId] = evt;
            }

            // Global announcement
            var announcement = defenderMonarchId.HasValue
                ? $"{attackerName} is attempting to wrest control of {townName} from {defenderName}! Phase 1 has begun."
                : $"{attackerName} is assaulting {townName} (unclaimed)! Phase 1 has begun.";
            GlobalBroadcast(announcement);

            return true;
        }

        /// <summary>
        /// Called every 5 seconds by the landblock tick for capturable-town landblocks.
        /// Handles Phase 1 accumulation, interruption, and timeout.
        /// Returns true if Phase 2 should now start.
        /// </summary>
        public static Phase1TickResult TickPhase1(byte townId,
            int attackersWithinRange,   // attacking allegiance members within 5m
            bool enemyOnLandblock,      // any non-attacker PK on the landblock
            ref double accumulatedSeconds)
        {
            bool timedOut;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out var town) || town.ConflictPhase != 1 || !town.ConflictStartTime.HasValue)
                    return Phase1TickResult.NotActive;

                timedOut = DateTime.UtcNow - town.ConflictStartTime.Value > TimeSpan.FromHours(1);
            }

            // Timeout check. HandlePhase1Timeout broadcasts and writes to the database, so it runs
            // outside the lock and re-checks the town state under it.
            if (timedOut)
            {
                HandlePhase1Timeout(townId);
                accumulatedSeconds = 0;
                return Phase1TickResult.TimedOut;
            }

            if (enemyOnLandblock)
            {
                // Interrupt: reset progress, keep the attempt alive
                accumulatedSeconds = 0;
                return Phase1TickResult.Interrupted;
            }

            if (attackersWithinRange >= 2)
            {
                accumulatedSeconds += 5;
                if (accumulatedSeconds >= Phase1DurationSeconds)
                    return Phase1TickResult.PhaseComplete;
            }

            return Phase1TickResult.Progressing;
        }

        private static void HandlePhase1Timeout(byte townId)
        {
            AllegianceHometownTown town;
            string attackerName, defenderName, townName;

            lock (_lock)
            {
                // Re-check under the lock: another thread may have resolved this conflict between
                // the tick's timeout test and this call.
                if (!_towns.TryGetValue(townId, out town) || town.ConflictPhase != 1 || !town.ConflictAttackerMonarchId.HasValue)
                    return;

                var attackerMonarchId = town.ConflictAttackerMonarchId.Value;
                attackerName = town.ConflictAttackerName;
                defenderName = town.OwnerAllegianceName;
                townName     = town.TownName;

                // Set 3-hour cooldown
                _attackerCooldowns[(attackerMonarchId, town.TownId)] = DateTime.UtcNow.AddHours(3);

                ClearConflictState(town);
            }

            CloseLatestEvent(townId, outcome: 2);
            SaveTownDb(town);

            GlobalBroadcast($"{attackerName}'s assault on {townName} has been repelled by {defenderName}!");
        }

        // -----------------------------------------------------------------------
        // Phase 2 — start / outcome
        // -----------------------------------------------------------------------

        public static void StartPhase2(byte townId)
        {
            AllegianceHometownTown town;
            AllegianceHometownEvent evt;
            string attackerName, defenderName, townName;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town) || town.ConflictPhase != 1) return;

                town.ConflictPhase   = 2;
                town.Phase2StartTime = DateTime.UtcNow;

                attackerName = town.ConflictAttackerName;
                defenderName = town.OwnerAllegianceName;
                townName     = town.TownName;

                if (_latestEventByTown.TryGetValue(townId, out evt))
                    evt.Phase2StartTime = town.Phase2StartTime;
            }

            SaveTownDb(town);

            if (evt != null)
                DatabaseManager.Log.UpdateAllegianceHometownEvent(evt);

            GlobalBroadcast($"{attackerName}'s assault on {townName} has breached Phase 2 — the Bind Stone has withdrawn into the {townName} Meeting Hall. Will they claim victory over {defenderName}?");
        }

        /// <summary>Called when bindstone HP reaches 0 — attacker wins.</summary>
        public static void HandleAttackerVictory(byte townId)
        {
            AllegianceHometownTown town;
            uint   attackerMonarchId;
            string attackerName, prevOwnerName, townName;
            uint?  prevOwnerId;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town)) return;

                // Already resolved, or resolved without an attacker on record — nothing to award, and
                // dereferencing the attacker below would throw and strand the proxy mid-cleanup.
                if (town.ConflictPhase != 2 || !town.ConflictAttackerMonarchId.HasValue)
                {
                    log.Warn($"[AllegianceHometown] HandleAttackerVictory({townId}) with ConflictPhase={town.ConflictPhase} and attacker={town.ConflictAttackerMonarchId?.ToString() ?? "none"}; ignoring.");
                    return;
                }

                attackerMonarchId = town.ConflictAttackerMonarchId.Value;
                attackerName      = town.ConflictAttackerName;
                prevOwnerName     = town.OwnerAllegianceName;
                prevOwnerId       = town.OwnerMonarchId;
                townName          = town.TownName;

                // Transfer ownership
                if (prevOwnerId.HasValue)
                    GetOrCreateSet(_ownedByMonarch, prevOwnerId.Value).Remove(town.TownId);
                GetOrCreateSet(_ownedByMonarch, attackerMonarchId).Add(town.TownId);

                town.OwnerMonarchId       = attackerMonarchId;
                town.OwnerAllegianceName  = attackerName;
                town.CapturedAt           = DateTime.UtcNow;

                // Apply capture protection
                var protectionHours = PropertyManager.GetDouble("ah_capture_protection_hours", 8.0).Item;
                _captureProtection[town.TownId] = DateTime.UtcNow.AddHours(protectionHours);

                // Ownership is transferred and the conflict closed atomically — a reader must never
                // see the town owned by the attacker while it is still flagged in conflict.
                ClearConflictState(town);
            }

            SaveTownDb(town);
            CloseLatestEvent(townId, outcome: 0);

            UncloakPhase2Bindstone(townId);

            // Capture rewards: attackers win, defenders smited
            DistributeRewards(townId, attackerMonarchId, prevOwnerId, isDefense: false);

            var ownerPart = prevOwnerId.HasValue ? $" from {prevOwnerName}" : "";
            GlobalBroadcast($"{attackerName} has captured {townName}{ownerPart}!");
        }

        /// <summary>Called when Phase 2 timer expires with bindstone still alive — defender wins.</summary>
        public static void HandleDefenderVictory(byte townId)
        {
            AllegianceHometownTown town;
            uint   attackerMonarchId;
            uint?  defenderMonarchId;
            string attackerName, defenderName, townName;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town)) return;

                if (town.ConflictPhase != 2 || !town.ConflictAttackerMonarchId.HasValue)
                {
                    log.Warn($"[AllegianceHometown] HandleDefenderVictory({townId}) with ConflictPhase={town.ConflictPhase} and attacker={town.ConflictAttackerMonarchId?.ToString() ?? "none"}; ignoring.");
                    return;
                }

                attackerMonarchId = town.ConflictAttackerMonarchId.Value;
                attackerName      = town.ConflictAttackerName;
                defenderName      = town.OwnerAllegianceName;
                defenderMonarchId = town.OwnerMonarchId;
                townName          = town.TownName;

                // 6-hour cooldown on the attacking allegiance for this town
                _attackerCooldowns[(attackerMonarchId, town.TownId)] = DateTime.UtcNow.AddHours(6);

                ClearConflictState(town);
            }

            SaveTownDb(town);
            CloseLatestEvent(townId, outcome: 1);

            UncloakPhase2Bindstone(townId);

            // Defense rewards: defenders win, attackers smited
            DistributeRewards(townId, defenderMonarchId ?? 0, attackerMonarchId, isDefense: true);

            GlobalBroadcast($"{defenderName} has defended {townName}! {attackerName} failed to capture the town.");
        }

        /// <summary>
        /// Called when defenders have cleared and held the Bind Stone area long enough to end Phase 2 early —
        /// the attack is repelled and the defenders win. Mirrors <see cref="HandleDefenderVictory"/> (attacker
        /// cooldown, defense rewards, smite) but with a "repelled" announcement, and is driven by the landblock
        /// Phase 2 tick rather than the Phase 2 timeout.
        /// </summary>
        public static void HandleDefenderRepel(byte townId)
        {
            AllegianceHometownTown town;
            uint   attackerMonarchId;
            uint?  defenderMonarchId;
            string attackerName, defenderName, townName;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town)) return;

                if (town.ConflictPhase != 2 || !town.ConflictAttackerMonarchId.HasValue)
                {
                    log.Warn($"[AllegianceHometown] HandleDefenderRepel({townId}) with ConflictPhase={town.ConflictPhase} and attacker={town.ConflictAttackerMonarchId?.ToString() ?? "none"}; ignoring.");
                    return;
                }

                attackerMonarchId = town.ConflictAttackerMonarchId.Value;
                attackerName      = town.ConflictAttackerName;
                defenderName      = town.OwnerAllegianceName;
                defenderMonarchId = town.OwnerMonarchId;
                townName          = town.TownName;

                // Same 6-hour attacker cooldown as a timed-out Phase 2 defense.
                _attackerCooldowns[(attackerMonarchId, town.TownId)] = DateTime.UtcNow.AddHours(6);

                ClearConflictState(town);
            }

            SaveTownDb(town);
            CloseLatestEvent(townId, outcome: 1);

            UncloakPhase2Bindstone(townId);

            // Defense rewards: defenders win, attackers smited
            DistributeRewards(townId, defenderMonarchId ?? 0, attackerMonarchId, isDefense: true);

            GlobalBroadcast($"{defenderName} has repelled {attackerName}'s attack on {townName}! The attackers were driven off.");
        }

        // -----------------------------------------------------------------------
        // Free claim (unowned town)
        // -----------------------------------------------------------------------

        public static void ClaimTown(byte townId, uint monarchId, string allegianceName)
        {
            AllegianceHometownTown town;
            string townName;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town)) return;

                GetOrCreateSet(_ownedByMonarch, monarchId).Add(town.TownId);

                town.OwnerMonarchId      = monarchId;
                town.OwnerAllegianceName = allegianceName;
                town.CapturedAt          = DateTime.UtcNow;
                townName                 = town.TownName;
            }

            SaveTownDb(town);

            GlobalBroadcast($"{allegianceName} has claimed {townName}!");
        }

        // -----------------------------------------------------------------------
        // Reward distribution
        // -----------------------------------------------------------------------

        /// <summary>
        /// Distributes capture/defense rewards to all eligible PK players currently on
        /// the town landblock (and adjacent landblocks for smite).
        /// <para>
        /// <paramref name="winnerMonarchId"/> identifies whose allegiance wins and receives rewards.
        /// <paramref name="loserMonarchId"/> identifies whose allegiance loses (smited and receives no reward).
        /// </para>
        /// <para>
        /// <paramref name="isDefense"/> selects the reward tier: defenders (holding the town) receive a
        /// larger payout than attackers (who also gain the town itself on capture).
        /// </para>
        /// </summary>
        public static void DistributeRewards(byte townId, uint winnerMonarchId, uint? loserMonarchId, bool isDefense)
        {
            // Existence check only — everything below hands out rewards through the landblock and
            // player subsystems, so no manager state is held while it runs.
            lock (_lock)
            {
                if (!_towns.ContainsKey(townId)) return;
            }

            var entry = AllegianceHometownRegistry.GetById(townId);
            if (entry == null) return;

            // Phase 2 resolves inside the meeting hall, so eligibility is simply "present in the hall".
            // Build the LandblockId from the Phase 2 Position so the landblock resolves correctly
            // (a raw 16-bit value would be interpreted as landblock 0x0000).
            var hallLb = LandblockManager.GetLandblock(entry.Phase2Position.LandblockId, false);
            if (hallLb == null) return;

            // Collect winner players present in the meeting hall
            var winnerPlayers = new System.Collections.Generic.List<Player>();

            foreach (var p in hallLb.GetPlayers())
            {
                if (!p.IsPK) continue;
                var monarchId = AllegianceManager.GetVerifiedMonarchId(p) ?? p.Guid.Full;
                if (monarchId == winnerMonarchId)
                    winnerPlayers.Add(p);
            }

            // Smite losing allegiance members present in the meeting hall.
            // Each smite runs the victim's full death handling (broadcasts, corpse, PK bookkeeping),
            // so it is isolated: a throw on one player must not skip the remaining smites or abort the
            // winners' payout below, and must never bubble up to ResolvePhase2 — that catch force-ends
            // the conflict, turning a won siege into an inconclusive one.
            if (loserMonarchId.HasValue)
            {
                foreach (var p in hallLb.GetPlayers())
                {
                    if (!p.IsPK) continue;
                    var monarchId = AllegianceManager.GetVerifiedMonarchId(p) ?? p.Guid.Full;
                    if (monarchId != loserMonarchId.Value) continue;

                    try
                    {
                        p.Smite(p);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"[AllegianceHometown] Exception smiting {p.Name} ({p.Guid}) on town {townId} resolution; continuing. Ex: {ex}");
                    }
                }
            }

            if (winnerPlayers.Count == 0) return;

            // Reward tiers. Attackers also gain the town itself on capture, so the loot pool is
            // weighted toward defenders to give allegiances a reason to show up and hold.
            //                     Attackers  Defenders
            int totalTrophies = isDefense ? 80  : 40;   // PK Trophy pool (split by player count)
            int totalMmds     = isDefense ? 40  : 20;   // MMD pool (split by player count)
            double xpPct      = isDefense ? 0.15 : 0.05; // fraction of XP-to-next-level (flat per player)
            int phials        = isDefense ? 1   : 0;    // Phial of Bloody Tears (flat per player)
            int darkbeatKeys  = isDefense ? 2   : 0;    // Darkbeat Keys (flat per player)

            int perTrophies   = Math.Max(1, totalTrophies / winnerPlayers.Count);
            int perMmds       = Math.Max(1, totalMmds     / winnerPlayers.Count);

            foreach (var winner in winnerPlayers)
            {
                // Isolated per winner for the same reason as the smite loop above: one player failing to
                // be paid (a full inventory, a session that dropped mid-resolution) must not cost every
                // other winner their reward, nor bubble up and force-end an already-decided conflict.
                try
                {
                    // PK Trophies (stackable)
                    GiveStacked(winner, CustomWeenieId.PkTrophy, perTrophies);

                    // MMDs (stackable)
                    GiveStacked(winner, 20630u, perMmds);

                    // Phials of Bloody Tears
                    for (int k = 0; k < phials; k++)
                        GiveSingle(winner, CustomWeenieId.PhialOfBloodyTears);

                    // Darkbeat Keys
                    for (int k = 0; k < darkbeatKeys; k++)
                        GiveSingle(winner, CustomWeenieId.DarkbeatKey);

                    // Bonus XP toward next level (fixed reward; GrantXP already bypasses the season xp_modifier)
                    var level = winner.Level ?? 1;
                    var xpBand = (long)winner.GetXPBetweenLevels(level, level + 1);
                    var bonusXp = (long)Math.Round(xpBand * xpPct);
                    if (bonusXp > 0)
                        winner.GrantXP(bonusXp, XpType.PvP, ACE.Entity.Enum.ShareType.None, "hometown capture reward");

                    var extras = new System.Collections.Generic.List<string>
                    {
                        $"{perTrophies} PK Trophy/Trophies",
                        $"{perMmds} MMD(s)"
                    };
                    if (phials > 0)       extras.Add($"{phials} Phial(s) of Bloody Tears");
                    if (darkbeatKeys > 0) extras.Add($"{darkbeatKeys} Darkbeat Key(s)");

                    winner.Session?.Network.EnqueueSend(new GameMessageSystemChat(
                        $"[Hometown] You received {string.Join(", ", extras)} for your service!",
                        ChatMessageType.Magic));
                }
                catch (Exception ex)
                {
                    log.Error($"[AllegianceHometown] Exception rewarding {winner.Name} ({winner.Guid}) on town {townId} resolution; continuing. Ex: {ex}");
                }
            }
        }

        private static void GiveStacked(Player player, uint wcid, int amount)
        {
            var wo = WorldObjectFactory.CreateNewWorldObject(wcid);
            if (wo == null) return;
            wo.SetStackSize(amount);
            if (!player.TryCreateInInventoryWithNetworking(wo))
            {
                // Drop at player's feet if inventory full
                wo.Location = new ACE.Entity.Position(player.Location);
                player.CurrentLandblock?.AddWorldObject(wo);
            }
        }

        private static void GiveSingle(Player player, uint wcid)
        {
            var wo = WorldObjectFactory.CreateNewWorldObject(wcid);
            if (wo == null) return;
            if (!player.TryCreateInInventoryWithNetworking(wo))
            {
                wo.Location = new ACE.Entity.Position(player.Location);
                player.CurrentLandblock?.AddWorldObject(wo);
            }
        }

        // -----------------------------------------------------------------------
        // Phase 2 participation rewards
        // -----------------------------------------------------------------------

        /// <summary>
        /// One-time award of PK Trophies to attacking-allegiance members near the Bind Stone at the moment
        /// Phase 2 begins. Default 5 each, configurable via "ah_phase2_start_trophies". Scans the town
        /// landblock and its neighbours (bind stones can sit on a boundary) within 100m of the stone.
        ///
        /// This one stays outdoors and distance-based on purpose: it fires the instant Phase 1 completes,
        /// when the attackers are still stacked on the outdoor bind stone and nobody has entered the
        /// meeting hall yet. Switching it to hall presence would award nothing.
        /// </summary>
        public static void AwardPhase2StartTrophies(byte townId)
        {
            uint attackerMonarchId;
            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out var town) || !town.ConflictAttackerMonarchId.HasValue) return;
                attackerMonarchId = town.ConflictAttackerMonarchId.Value;
            }

            var entry = AllegianceHometownRegistry.GetById(townId);
            if (entry == null) return;

            var mainLb = LandblockManager.GetLandblock(entry.BindstonePosition.LandblockId, false);
            if (mainLb == null) return;

            var allLbs = new List<Entity.Landblock> { mainLb };
            foreach (var adjId in LandblockManager.GetAdjacentIDs(mainLb))
            {
                var adjLb = LandblockManager.GetLandblock(adjId, false);
                if (adjLb != null) allLbs.Add(adjLb);
            }

            var count        = (int)PropertyManager.GetLong("ah_phase2_start_trophies", 5).Item;
            if (count <= 0) return;
            var bindstonePos = entry.BindstonePosition;

            foreach (var lb in allLbs)
            {
                foreach (var p in lb.GetPlayers())
                {
                    if (!p.IsPK) continue;
                    var monarchId = AllegianceManager.GetVerifiedMonarchId(p) ?? p.Guid.Full;
                    if (monarchId == attackerMonarchId && p.Location.DistanceTo(bindstonePos) <= 100f)
                    {
                        GiveStacked(p, CustomWeenieId.PkTrophy, count);
                        p.Session?.Network.EnqueueSend(new GameMessageSystemChat(
                            $"[Hometown] You received {count} PK Trophies for breaching Phase 2!",
                            ChatMessageType.Magic));
                    }
                }
            }
        }

        /// <summary>
        /// Periodic participation trophies for attackers and defenders holding the Bind Stone area during
        /// Phase 2. Each player earns one PK Trophy at most once per "ah_phase2_trophy_interval_seconds"
        /// (default 60). Called from the Phase 2 landblock tick with the players currently in range.
        /// </summary>
        public static void AwardPhase2PeriodicTrophies(byte townId, List<Player> participants)
        {
            if (participants == null || participants.Count == 0) return;

            var interval = TimeSpan.FromSeconds(Math.Max(1, PropertyManager.GetLong("ah_phase2_trophy_interval_seconds", 60).Item));
            var now      = DateTime.UtcNow;

            var toAward = new List<Player>();
            lock (_lock)
            {
                foreach (var p in participants)
                {
                    if (!_phase2NextTrophy.TryGetValue(p.Guid.Full, out var next) || now >= next)
                    {
                        _phase2NextTrophy[p.Guid.Full] = now + interval;
                        toAward.Add(p);
                    }
                }
            }

            foreach (var p in toAward)
            {
                GiveStacked(p, CustomWeenieId.PkTrophy, 1);
                p.Session?.Network.EnqueueSend(new GameMessageSystemChat(
                    "[Hometown] You received a PK Trophy for holding the line.",
                    ChatMessageType.Broadcast));
            }
        }

        /// <summary>
        /// True if the given player belongs to the allegiance that currently owns the town (a defender).
        /// Ownership only transfers on attacker victory, so during Phase 2 the owner is still the defender.
        /// </summary>
        public static bool IsTownDefender(byte townId, Player player)
        {
            if (player == null) return false;

            uint ownerId;
            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out var t) || !t.OwnerMonarchId.HasValue) return false;
                ownerId = t.OwnerMonarchId.Value;
            }

            var monarchId = AllegianceManager.GetVerifiedMonarchId(player) ?? player.Guid.Full;
            return monarchId == ownerId;
        }

        // -----------------------------------------------------------------------
        // Phase 2 creature proxy registry
        // -----------------------------------------------------------------------

        /// <summary>Audit outcome for a conflict closed without a winner (handler failure or admin reset).</summary>
        private const byte OutcomeForced = 3;

        /// <summary>
        /// Last-resort close for a conflict whose normal outcome handler failed. Awards nothing —
        /// it only puts the town back in a state where it can be attacked again, so a failure can
        /// never leave a town locked in Phase 2 forever. Does not touch the proxy; callers that own
        /// one are responsible for despawning it.
        /// </summary>
        public static void ForceEndConflict(byte townId)
        {
            AllegianceHometownTown town;
            string townName;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town)) return;
                townName = town.TownName;
                ClearConflictState(town);
            }

            // Best-effort from here: the conflict is already closed in memory, so a failure below
            // can't leave the town locked.
            try
            {
                SaveTownDb(town);
                UncloakPhase2Bindstone(townId);
                CloseLatestEvent(townId, OutcomeForced);
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception force-ending conflict for town {townId}. Ex: {ex}");
            }

            GlobalBroadcast($"The assault on {townName} has ended inconclusively.");
        }

        /// <summary>
        /// Operator recovery: tears a town's conflict all the way back to peace — despawns the
        /// Phase 2 proxy, restores the real bindstone, releases the landblock, and clears the
        /// conflict. Awards nothing to either side and leaves ownership untouched. Safe to call on
        /// a town with no conflict running. Returns true if there was a conflict to clear.
        /// </summary>
        public static bool ResetTown(byte townId, string resetBy)
        {
            AllegianceHometownTown town;
            BindstoneCreatureProxy proxy;
            bool   hadConflict;
            string townName;

            lock (_lock)
            {
                if (!_towns.TryGetValue(townId, out town)) return false;

                hadConflict = town.ConflictPhase != 0;
                townName    = town.TownName;

                // Claim the proxy and clear the conflict in one step, so a concurrent resolve on a
                // landblock thread finds nothing left to act on.
                _phase2Proxies.Remove(townId, out proxy);

                if (hadConflict)
                    ClearConflictState(town);
            }

            if (proxy != null)
            {
                try
                {
                    proxy.AdminDespawn();
                }
                catch (Exception ex)
                {
                    log.Error($"[AllegianceHometown] Exception despawning proxy during reset of town {townId}. Ex: {ex}");
                }
            }

            try
            {
                UncloakPhase2Bindstone(townId);
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception uncloaking bindstone during reset of town {townId}. Ex: {ex}");
            }

            if (hadConflict)
            {
                SaveTownDb(town);
                CloseLatestEvent(townId, OutcomeForced);
                GlobalBroadcast($"The conflict at {townName} has been reset by an administrator.");
            }

            log.Warn($"[AllegianceHometown] Town {townId} ({townName}) reset by {resetBy}. hadConflict={hadConflict}");
            return hadConflict;
        }

        public static void RegisterPhase2Proxy(byte townId, BindstoneCreatureProxy proxy)
        {
            lock (_lock)
                _phase2Proxies[townId] = proxy;
        }

        public static void UnregisterPhase2Proxy(byte townId)
        {
            lock (_lock)
                _phase2Proxies.Remove(townId);
        }

        public static BindstoneCreatureProxy GetPhase2Proxy(byte townId)
        {
            lock (_lock)
            {
                _phase2Proxies.TryGetValue(townId, out var proxy);
                return proxy;
            }
        }

        // -----------------------------------------------------------------------
        // Blacklist management
        // -----------------------------------------------------------------------

        public static bool IsBlacklisted(uint monarchId)
        {
            lock (_lock)
                return _blacklist.Contains(monarchId);
        }

        public static bool AddBlacklist(uint monarchId, string allegianceName, string reason, string addedBy)
        {
            var entry = new ACE.Database.Models.Log.AllegianceHometownBlacklist
            {
                MonarchId      = monarchId,
                AllegianceName = allegianceName,
                Reason         = reason,
                AddedBy        = addedBy,
                AddedAt        = DateTime.UtcNow
            };

            lock (_lock)
            {
                if (!_blacklist.Add(monarchId)) return false;
                _blacklistEntries[monarchId] = entry;
            }

            DatabaseManager.Log.AddAllegianceHometownBlacklist(entry);
            return true;
        }

        public static bool RemoveBlacklist(uint monarchId)
        {
            lock (_lock)
            {
                if (!_blacklist.Remove(monarchId)) return false;
                _blacklistEntries.Remove(monarchId);
            }

            DatabaseManager.Log.RemoveAllegianceHometownBlacklist(monarchId);
            return true;
        }

        /// <summary>Snapshot of the blacklist — a copy, so callers can enumerate it safely.</summary>
        public static System.Collections.Generic.IReadOnlyDictionary<uint, ACE.Database.Models.Log.AllegianceHometownBlacklist> GetBlacklist()
        {
            lock (_lock)
                return new Dictionary<uint, ACE.Database.Models.Log.AllegianceHometownBlacklist>(_blacklistEntries);
        }

        /// <summary>
        /// True when the given landblock is a town meeting hall whose Phase 2 is currently running.
        /// Portal.CheckUseRequirements uses this to let PK-tagged players through the meeting hall
        /// portals during a siege, so nobody can be locked out of Phase 2 by being repeatedly tagged.
        /// Outside Phase 2 the normal PK timer applies, so the halls are not a general PvP escape hatch.
        /// </summary>
        public static bool IsPhase2HallOpen(uint landblockId)
        {
            var entry = AllegianceHometownRegistry.GetByHallLandblock(landblockId);
            if (entry == null) return false;

            lock (_lock)
                return _towns.TryGetValue(entry.TownId, out var t) && t.ConflictPhase == 2;
        }

        public static void RegisterPhase2CloakedBindstone(byte townId, WorldObjects.Bindstone bindstone)
        {
            lock (_lock)
                _phase2CloakedBindstones[townId] = bindstone;
        }

        public static void UncloakPhase2Bindstone(byte townId)
        {
            WorldObjects.Bindstone bs;

            // Claim the bindstone under the lock so two threads resolving the same conflict can't
            // both run the uncloak; everything below touches world objects and must stay outside it.
            lock (_lock)
            {
                if (!_phase2CloakedBindstones.Remove(townId, out bs))
                    return;
            }

            bs.Visibility  = false;
            bs.Cloaked     = (bool?)false;
            bs.Ethereal    = (bool?)false;
            bs.NoDraw      = (bool?)false;
            bs.Attackable  = true;
            bs.EnqueueBroadcastPhysicsState();
            // Send CreateObject so all nearby clients who lack it in their tracking get it back
            bs.EnqueueBroadcast(false, new GameMessageCreateObject(bs));

            // Lift the Phase 2 permaload so the meeting hall can unload normally when empty again.
            // The permaload is taken on the hall landblock (where the proxy lives), not on the town
            // landblock the real bind stone sits in. Only the timed permaload is dropped — a landblock
            // the config preloads permanently stays pinned.
            var entry = AllegianceHometownRegistry.GetById(townId);
            if (entry != null)
                LandblockManager.GetLandblock(entry.Phase2Position.LandblockId, false)?.ClearTimedPermaload();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        public static void SaveTown(AllegianceHometownTown town)
        {
            lock (_lock)
                _towns[town.TownId] = town;

            SaveTownDb(town);
        }

        /// <summary>Persists a town. Blocks on the database — never call this holding _lock.</summary>
        private static void SaveTownDb(AllegianceHometownTown town)
        {
            DatabaseManager.Log.UpdateAllegianceHometownTown(town);
        }

        /// <summary>
        /// Returns a town to peace in memory only. Callers must hold _lock, and are responsible for
        /// persisting the town afterwards via SaveTownDb.
        /// </summary>
        private static void ClearConflictState(AllegianceHometownTown town)
        {
            if (town.ConflictAttackerMonarchId.HasValue)
                GetOrCreateSet(_activeConflictsByMonarch, town.ConflictAttackerMonarchId.Value).Remove(town.TownId);

            town.ConflictPhase             = 0;
            town.ConflictAttackerMonarchId = null;
            town.ConflictAttackerName      = null;
            town.ConflictStartTime         = null;
            town.Phase2StartTime           = null;
        }

        private static void CloseLatestEvent(byte townId, byte outcome)
        {
            AllegianceHometownEvent evt;

            lock (_lock)
            {
                if (!_latestEventByTown.TryGetValue(townId, out evt)) return;
                evt.EventEndTime = DateTime.UtcNow;
                evt.Outcome      = outcome;
            }

            DatabaseManager.Log.UpdateAllegianceHometownEvent(evt);
        }

        private static void GlobalBroadcast(string message)
        {
            PlayerManager.BroadcastToAll(
                new GameMessageSystemChat(message, ChatMessageType.WorldBroadcast));
            DiscordWebhookManager.SendHometownBroadcast(message);
        }

        private static HashSet<T> GetOrCreateSet<TKey, T>(Dictionary<TKey, HashSet<T>> dict, TKey key)
        {
            if (!dict.TryGetValue(key, out var set))
            {
                set = new HashSet<T>();
                dict[key] = set;
            }
            return set;
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m {ts.Seconds}s";
        }

        // -----------------------------------------------------------------------
        // HP formula
        // -----------------------------------------------------------------------

        /// <summary>
        /// Computes the bindstone's starting HP based on the current level cap.
        /// Designed so 3 players attacking unopposed can destroy it in ~18 minutes
        /// (a 20% reduction from the original ~22-minute tuning).
        /// </summary>
        public static int ComputeBindstoneHp()
        {
            var xpCap    = RollingLevelCapManager.GetCurrentXpCap();
            int levelCap = RollingLevelCapManager.GetCurrentLevelCap(xpCap);
            float dpsPerPlayer = Math.Clamp(25f + 75f * (levelCap - 15f) / 115f, 25f, 100f);
            return (int)(dpsPerPlayer * 3f * 1350f * 0.8f);
        }

        /// <summary>
        /// Damage/heal amount applied to the bindstone per player kill during Phase 2 (5% of max HP).
        /// </summary>
        public static int GetKillEffect(int bindstoneMaxHp) => (int)(bindstoneMaxHp * 0.05f);

        // Bind stone damage falloff: full damage inside this radius (meters)...
        public const float BindstoneFullDamageRadius = 15f;
        // ...linear falloff to zero at this radius; no damage beyond it.
        public const float BindstoneZeroDamageRadius = 20f;

        /// <summary>
        /// Distance-based damage multiplier for attacks on the Phase 2 bind stone.
        /// Full damage within <see cref="BindstoneFullDamageRadius"/>, linear falloff to zero at
        /// <see cref="BindstoneZeroDamageRadius"/>, and no damage beyond — forcing attackers to hold
        /// the stone at close range rather than snipe it. Applies to weapon and war-magic damage alike.
        /// </summary>
        public static float GetDistanceMultiplier(float distanceMeters)
        {
            if (distanceMeters <= BindstoneFullDamageRadius) return 1f;
            if (distanceMeters >= BindstoneZeroDamageRadius) return 0f;
            return (BindstoneZeroDamageRadius - distanceMeters) / (BindstoneZeroDamageRadius - BindstoneFullDamageRadius);
        }
    }

    public enum Phase1TickResult
    {
        NotActive,
        Progressing,
        Interrupted,
        TimedOut,
        PhaseComplete,
    }
}
