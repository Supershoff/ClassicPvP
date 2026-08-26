using System;

using log4net;

using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.AllegianceHometown;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects
{
    /// <summary>
    /// Temporary Creature spawned at the bindstone position during Phase 2 of
    /// Allegiance Hometown Capture.  Because the AC client only sends
    /// TargetedMeleeAttack packets for Creature-type objects, we cannot make the
    /// static Bindstone WorldObject directly attackable.  This proxy looks like
    /// the bindstone (same Setup model) and receives all standard melee damage.
    /// When destroyed it triggers the attacker-victory path instead of the normal
    /// creature-death path (no corpse, no XP, no loot).
    /// </summary>
    public class BindstoneCreatureProxy : Creature
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public byte TownId { get; set; }

        /// <summary>
        /// Anti-"peacing" flag: while any non-attacker player is inside the meeting hall, attacker damage
        /// is heavily reduced (see DamageEvent / SpellProjectile). Recomputed each Phase 2 hall tick.
        /// Written and read only on the hall landblock thread, so no synchronization is needed.
        /// </summary>
        public bool SuppressDamage { get; set; }

        private DateTime _lastPhase2Broadcast = DateTime.MinValue;

        public BindstoneCreatureProxy(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            NoCorpse = true;
        }

        // -----------------------------------------------------------------------
        // Non-PK rejection — the stone strikes back
        // -----------------------------------------------------------------------

        /// <summary>
        /// Only PK players may harm the bind stone. Anyone else (NPK, pets, wandering monsters)
        /// has their attack turned back on them: the stone takes nothing and the attacker eats
        /// the full amount. Every damage path must call this — melee/missile via DamageEvent and
        /// war magic via SpellProjectile, which applies damage without going through TakeDamage.
        /// Returns true if the attack was rejected, in which case the caller must deal 0 damage.
        /// </summary>
        public bool TryReflectNonPkAttack(WorldObject source, DamageType damageType, float amount)
        {
            var attacker = source as Creature ?? source?.ProjectileSource as Creature;

            if (attacker is Player playerAttacker && playerAttacker.IsPK)
                return false;

            if (attacker == null || !attacker.IsAlive || amount <= 0 || IsDead)
                return true;

            try
            {
                if (attacker is Player player)
                {
                    player.TakeDamage(this, damageType, amount, BodyPart.Chest);
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat(
                        $"The Bind Stone's wards turn your attack back upon you for {(uint)Math.Round(amount):N0} points of damage!",
                        ChatMessageType.Combat));
                }
                else
                    attacker.TakeDamage(this, damageType, amount);

                attacker.EnqueueBroadcast(new GameMessageScript(attacker.Guid, PlayScript.EnchantUpBlue));
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception reflecting attack from {attacker.Name} ({attacker.Guid}) on town {TownId} bindstone proxy. Ex: {ex}");
            }

            return true;
        }

        // -----------------------------------------------------------------------
        // Defender mending — defenders repair the stone instead of damaging it
        // -----------------------------------------------------------------------

        /// <summary>
        /// If the attacking player belongs to the defending (owner) allegiance, their attack mends the
        /// Bind Stone instead of harming it: it heals by 10% of the damage they would have dealt and takes
        /// none. Returns true when the source is a defender, in which case the caller must apply 0 damage.
        /// Non-defenders (attackers, third parties) return false and are handled normally.
        /// </summary>
        public bool TryApplyDefenderHeal(WorldObject source, float wouldBeDamage)
        {
            var attacker = source as Creature ?? source?.ProjectileSource as Creature;
            if (attacker is not Player player)
                return false;

            // Only PK defenders mend the stone. Non-PK attacks are already reflected upstream, but guard here
            // too so a non-PK defender can never heal it regardless of how this is reached.
            if (!player.IsPK)
                return false;

            if (!AllegianceHometownManager.IsTownDefender(TownId, player))
                return false;

            // It's a defender — their hit never damages the stone, alive or not.
            if (IsDead)
                return true;

            try
            {
                var healPct = (float)PropertyManager.GetDouble("ah_bindstone_defender_heal_mod", 0.10).Item;
                var heal    = (uint)Math.Max(0, Math.Round(wouldBeDamage * healPct));
                if (heal > 0)
                {
                    var newHp = (uint)Math.Min(Health.MaxValue, Health.Current + heal);
                    UpdateVital(Health, newHp);
                    BroadcastStatus();
                }

                player.Session?.Network.EnqueueSend(new GameMessageSystemChat(
                    $"Your attack mends the Bind Stone for {heal:N0} points.",
                    ChatMessageType.Combat));
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception applying defender heal from {player.Name} ({player.Guid}) on town {TownId} bindstone proxy. Ex: {ex}");
            }

            return true;
        }

        // -----------------------------------------------------------------------
        // Death — trigger attacker victory instead of normal creature death
        // -----------------------------------------------------------------------

        public override DeathMessage OnDeath(DamageHistoryInfo lastDamager, DamageType damageType, bool criticalHit = false)
        {
            // Skip XP grants and kill quests; just log and proceed
            OnMovementStopped();
            return GetDeathMessage(lastDamager, damageType, criticalHit);
        }

        private bool _dieEntered = false;
        private bool _resolved   = false;

        protected override void Die(DamageHistoryInfo lastDamager, DamageHistoryInfo topDamager)
        {
            if (_dieEntered) return;
            _dieEntered = true;

            UpdateVital(Health, 0);

            var deathAnimLength = 0.0;

            // Never let a presentation failure abort the sequence — reaching the resolve below is
            // what ends Phase 2, and once _dieEntered is set nothing else will call Die() again.
            try
            {
                // No corpse, no loot — just despawn after death animation
                CurrentMotionState = new Motion(MotionStance.NonCombat, MotionCommand.Ready);
                PhysicsObj?.StopCompletely(true);

                var motionDeath = new Motion(MotionStance.NonCombat, MotionCommand.Dead);
                deathAnimLength = ExecuteMotion(motionDeath);
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception in death animation for town {TownId} bindstone proxy. Ex: {ex}");
            }

            var chain = new ACE.Server.Entity.Actions.ActionChain();
            chain.AddDelaySeconds(Math.Max(deathAnimLength, 0.5));
            chain.AddAction(this, () => ResolvePhase2(attackerVictory: true));
            chain.EnqueueChain();
        }

        /// <summary>
        /// Despawns the proxy without resolving Phase 2 for either side. Used by the admin reset,
        /// which clears the conflict itself — latching the guards here keeps a queued death chain
        /// or heartbeat from resolving a conflict that no longer exists.
        /// </summary>
        public void AdminDespawn()
        {
            _resolved   = true;
            _dieEntered = true;
            Destroy();
        }

        /// <summary>
        /// Ends Phase 2 and removes the proxy. The despawn runs in a finally so that a throw in the
        /// outcome handler can never strand the proxy and the real bindstone in the world together
        /// with the conflict stuck open.
        /// </summary>
        private void ResolvePhase2(bool attackerVictory)
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                if (attackerVictory)
                    AllegianceHometownManager.HandleAttackerVictory(TownId);
                else
                    AllegianceHometownManager.HandleDefenderVictory(TownId);
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception resolving Phase 2 for town {TownId} (attackerVictory={attackerVictory}); forcing the conflict closed. Ex: {ex}");
                AllegianceHometownManager.ForceEndConflict(TownId);
            }
            finally
            {
                AllegianceHometownManager.UnregisterPhase2Proxy(TownId);
                Destroy();
            }
        }

        /// <summary>
        /// Ends Phase 2 early as a repelled attack (defenders held the area). Driven by the Phase 2 landblock
        /// tick, which shares the proxy's landblock thread. Mirrors <see cref="ResolvePhase2"/>'s cleanup so
        /// the proxy and cloaked bindstone can never be stranded, and shares the same <c>_resolved</c> guard
        /// so it can't race the Phase 2 timeout or death handoff.
        /// </summary>
        public void ResolveRepel()
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                AllegianceHometownManager.HandleDefenderRepel(TownId);
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception resolving repel for town {TownId}; forcing the conflict closed. Ex: {ex}");
                AllegianceHometownManager.ForceEndConflict(TownId);
            }
            finally
            {
                AllegianceHometownManager.UnregisterPhase2Proxy(TownId);
                Destroy();
            }
        }

        // -----------------------------------------------------------------------
        // Kill effects called from Player_Death
        // -----------------------------------------------------------------------

        /// <summary>A defender (owner allegiance member) was killed — bindstone loses 5% max HP.</summary>
        public void OnDefenderKilled()
        {
            if (!IsAlive) return;
            var loss = AllegianceHometownManager.GetKillEffect((int)Health.MaxValue);
            var newHp = (uint)Math.Max(1, (int)Health.Current - loss);
            UpdateVital(Health, newHp);
            BroadcastStatus();

            if (Health.Current <= 1)
                TakeDamage(this, DamageType.Bludgeon, 1);
        }

        /// <summary>An attacker (attacking allegiance member) was killed — bindstone heals 5% max HP.</summary>
        public void OnAttackerKilled()
        {
            if (!IsAlive) return;
            var heal = AllegianceHometownManager.GetKillEffect((int)Health.MaxValue);
            var newHp = (uint)Math.Min(Health.MaxValue, Health.Current + heal);
            UpdateVital(Health, newHp);
            BroadcastStatus();
        }

        // -----------------------------------------------------------------------
        // Broadcast HP status after each hit
        // -----------------------------------------------------------------------

        public void BroadcastStatus()
        {
            if (Health.MaxValue == 0) return;
            var pct = (float)Health.Current / Health.MaxValue * 100f;
            var msg = new GameMessageSystemChat(
                $"[Bind Stone] HP: {Health.Current:N0} / {Health.MaxValue:N0} ({pct:0.0}%)",
                ChatMessageType.WorldBroadcast);
            CurrentLandblock?.EnqueueBroadcast(null, false, Location, null, msg);
        }

        // -----------------------------------------------------------------------
        // Heartbeat — 30-min Phase 2 timeout + 60-s global broadcast
        // -----------------------------------------------------------------------

        public override void Heartbeat(double currentUnixTime)
        {
            // Must run whether or not the proxy is alive: a proxy sitting at 0 HP with an
            // unfinished death sequence is precisely the case that needs rescuing, and it is
            // the only thing left that can end Phase 2.
            try
            {
                CheckPhase2();
            }
            catch (Exception ex)
            {
                log.Error($"[AllegianceHometown] Exception in Phase 2 heartbeat for town {TownId} bindstone proxy. Ex: {ex}");
            }

            base.Heartbeat(currentUnixTime);
        }

        /// <summary>How long a proxy may sit at 0 HP before we assume its death sequence died with it.</summary>
        private static readonly TimeSpan DeathWatchdog = TimeSpan.FromSeconds(30);

        private DateTime? _deadSince;

        private void CheckPhase2()
        {
            if (_resolved) return;

            var now = DateTime.UtcNow;

            // Watchdog: HP is gone but the death sequence never finished the handoff. Resolve it
            // here rather than leave the town locked with both stones spawned.
            if (!IsAlive)
            {
                if (_deadSince == null)
                {
                    _deadSince = now;
                    return;
                }

                if (now - _deadSince.Value >= DeathWatchdog)
                {
                    log.Error($"[AllegianceHometown] Town {TownId} bindstone proxy has been at 0 HP for " +
                              $"{(now - _deadSince.Value).TotalSeconds:0}s without completing its death sequence " +
                              $"(dieEntered={_dieEntered}); forcing attacker victory.");
                    ResolvePhase2(attackerVictory: true);
                }
                return;
            }

            _deadSince = null;

            // Read the Phase 2 state atomically — conflicts resolve on other landblock threads.
            if (!AllegianceHometownManager.TryGetPhase2Status(TownId, out var phase2Start, out var attackerName, out var townName))
            {
                // The conflict ended somewhere else and left us behind — despawn rather than linger
                // as an unkillable stone nobody can resolve.
                log.Warn($"[AllegianceHometown] Town {TownId} bindstone proxy is orphaned (town is no longer in Phase 2); despawning.");
                _resolved = true;
                AllegianceHometownManager.UnregisterPhase2Proxy(TownId);
                AllegianceHometownManager.UncloakPhase2Bindstone(TownId);
                Destroy();
                return;
            }

            var elapsed        = now - phase2Start;
            var phase2Duration = AllegianceHometownManager.Phase2Duration;

            if (elapsed >= phase2Duration)
            {
                ResolvePhase2(attackerVictory: false);
                return;
            }

            if ((now - _lastPhase2Broadcast).TotalSeconds >= 60)
            {
                _lastPhase2Broadcast = now;
                var remaining = phase2Duration - elapsed;
                var hpPct     = Health.MaxValue > 0
                    ? (float)Health.Current / Health.MaxValue * 100f
                    : 0f;
                var timeStr = AllegianceHometownManager.FormatTimeSpan(remaining);
                PlayerManager.BroadcastToAll(
                    new GameMessageSystemChat(
                        $"[{townName}] The Bind Stone is under attack by {attackerName}! " +
                        $"Time remaining: {timeStr} — Bind Stone HP: {hpPct:0.0}%",
                        ChatMessageType.WorldBroadcast));
            }
        }
    }
}
