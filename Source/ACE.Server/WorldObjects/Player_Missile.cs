using System;
using System.Numerics;

using ACE.Entity.Enum;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics.Animation;
using ACE.Server.Physics.Extensions;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        private float _accuracyLevel;

        public float AccuracyLevel
        {
            get => IsExhausted ? 0.0f : _accuracyLevel;
            set => _accuracyLevel = value;
        }

        public Creature MissileTarget;

        public PowerAccuracy GetAccuracyRange()
        {
            if (AccuracyLevel < 0.33f)
                return PowerAccuracy.Low;
            else if (AccuracyLevel < 0.66f)
                return PowerAccuracy.Medium;
            else
                return PowerAccuracy.High;
        }

        /// <summary>
        /// Called by network packet handler 0xA - GameActionTargetedMissileAttack
        /// </summary>
        /// <param name="targetGuid">The target guid</param>
        /// <param name="attackHeight">The attack height 1-3</param>
        /// <param name="accuracyLevel">The 0-1 accuracy bar level</param>
        public void HandleActionTargetedMissileAttack(uint targetGuid, uint attackHeight, float accuracyLevel)
        {
            //log.Info($"-");

            if (CombatMode != CombatMode.Missile)
            {
                log.Error($"{Name}.HandleActionTargetedMissileAttack({targetGuid:X8}, {attackHeight}, {accuracyLevel}) - CombatMode mismatch {CombatMode}, LastCombatMode: {LastCombatMode}");

                if (LastCombatMode == CombatMode.Missile)
                    CombatMode = CombatMode.Missile;
                else
                {
                    OnAttackDone();
                    return;
                }
            }

            if (IsBusy || Teleporting || suicideInProgress)
            {
                SendWeenieError(WeenieError.YoureTooBusy);
                OnAttackDone();
                return;
            }

            if (IsJumping)
            {
                SendWeenieError(WeenieError.YouCantDoThatWhileInTheAir);
                OnAttackDone();
                return;
            }

            if (PKLogout)
            {
                SendWeenieError(WeenieError.YouHaveBeenInPKBattleTooRecently);
                OnAttackDone();
                return;
            }

            var weapon = GetEquippedMissileWeapon();
            var ammo = GetEquippedAmmo();

            // sanity check
            accuracyLevel = Math.Clamp(accuracyLevel, 0.0f, 1.0f);

            if (weapon == null || weapon.IsAmmoLauncher && ammo == null)
            {
                OnAttackDone();
                return;
            }

            AttackHeight = (AttackHeight)attackHeight;
            AttackQueue.Add(accuracyLevel);

            if (MissileTarget == null)
                AccuracyLevel = accuracyLevel;  // verify

            // get world object of target guid
            var targetWo = CurrentLandblock?.GetObject(targetGuid);
            var target = targetWo as Creature;
            if (target == null)
            {
                //log.Warn($"{Name}.HandleActionTargetedMissileAttack({targetGuid:X8}, {AttackHeight}, {accuracyLevel}) - couldn't find creature target guid");
                OnAttackDone();
                return;
            }

            if (Attacking || MissileTarget != null && MissileTarget.IsAlive)
                return;

            if (!CanDamageNoTeleport(target))
            {
                SendTransientError($"You cannot attack {target.Name}");
                OnAttackDone();
                return;
            }

            //log.Info($"{Name}.HandleActionTargetedMissileAttack({targetGuid:X8}, {attackHeight}, {accuracyLevel})");

            AttackTarget = target;
            MissileTarget = target;
            LastAttackTarget = target;

            var attackSequence = ++AttackSequence;

            // record stance here and pass it along
            // accounts for odd client behavior with swapping bows during repeat attacks
            var stance = CurrentMotionState.Stance;

            // turn if required
            var rotateTime = Rotate(target);
            var actionChain = new ActionChain();

            var delayTime = rotateTime;
            if (NextRefillTime > DateTime.UtcNow.AddSeconds(delayTime))
                delayTime = (float)(NextRefillTime - DateTime.UtcNow).TotalSeconds;

            actionChain.AddDelaySeconds(delayTime);

            // do missile attack
            actionChain.AddAction(this, () => LaunchMissile(target, attackSequence, stance));
            actionChain.EnqueueChain();
        }

        /// <summary>
        /// Launches a missile attack from player to target
        /// </summary>
        public void LaunchMissile(WorldObject target, int attackSequence, MotionStance stance, bool subsequent = false)
        {
            if (AttackSequence != attackSequence)
                return;

            var weapon = GetEquippedMissileWeapon();
            if (weapon == null || CombatMode == CombatMode.NonCombat)
            {
                OnAttackDone();
                return;
            }

            var ammo = weapon.IsAmmoLauncher ? GetEquippedAmmo() : weapon;
            if (ammo == null)
            {
                OnAttackDone();
                return;
            }

            var launcher = GetEquippedMissileLauncher();

            var creature = target as Creature;
            if (!IsAlive || IsBusy || Teleporting || MissileTarget == null || creature == null || !creature.IsAlive || suicideInProgress)
            {
                OnAttackDone();
                return;
            }

            if (!TargetInRange(target))
            {
                // this must also be sent to actually display the transient message
                SendWeenieError(WeenieError.MissileOutOfRange);

                // this prevents the accuracy bar from refilling when 'repeat attacks' is enabled
                OnAttackDone();

                return;
            }

            var actionChain = new ActionChain();

            if (subsequent && !IsFacing(target))
            {
                var rotateTime = Rotate(target);
                actionChain.AddDelaySeconds(rotateTime);
            }

            // launch animation
            // point of no return beyond this point -- cannot be cancelled
            actionChain.AddAction(this, () => Attacking = true);

            EndSneaking();

            if (subsequent && Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                // client shows hourglass, until attack done is received
                // retail only did this for subsequent attacks w/ repeat attacks on
                Session.Network.EnqueueSend(new GameEventCombatCommenceAttack(Session));
            }

            var projectileSpeed = GetProjectileSpeed();

            // get z-angle for aim motion
            var aimVelocity = GetAimVelocity(target, projectileSpeed);

            var aimLevel = GetAimLevel(aimVelocity);

            // calculate projectile spawn pos and velocity
            var localOrigin = GetProjectileSpawnOrigin(ammo.WeenieClassId, aimLevel);

            var velocity = CalculateProjectileVelocity(localOrigin, target, projectileSpeed, out Vector3 origin, out Quaternion orientation);

            //Console.WriteLine($"Velocity: {velocity}");

            if (velocity == Vector3.Zero)
            {
                // pre-check succeeded, but actual velocity calculation failed
                SendWeenieError(WeenieError.MissileOutOfRange);

                // this prevents the accuracy bar from refilling when 'repeat attacks' is enabled
                Attacking = false;
                OnAttackDone();
                return;
            }

            var launchTime = EnqueueMotionPersist(actionChain, aimLevel);

            // launch projectile
            actionChain.AddAction(this, () =>
            {
                // handle self-procs
                TryProcEquippedItems(this, this, true, weapon);

                var sound = GetLaunchMissileSound(weapon);
                EnqueueBroadcast(new GameMessageSound(Guid, sound, 1.0f));

                // stamina usage
                // TODO: ensure enough stamina for attack
                // TODO: verify formulas - double/triple cost for bow/xbow?
                var staminaCost = GetAttackStamina(GetAccuracyRange());
                UpdateVitalDelta(Stamina, -staminaCost);

                var launchOrigin = origin;
                var launchOrientation = orientation;
                var launchVelocity = velocity;

                // MISSILE FIX 1: the firing solution above was calculated before the turn and before the
                // aim animation played, but the projectile only spawns now. Measured from client_portal.dat
                // (motion table 0900020D), that gap is 0.033s for a level bow shot, 0.167-0.567s for
                // elevated bow shots, and a flat 0.378s for every thrown weapon attack -- plus the rotate
                // time on repeat attacks against a circling target. Both the intercept prediction and the
                // spawn origin are stale by that much, so the arrow also leaves from where the shooter was
                // rather than where they are.
                //
                // Re-solve here, at the instant the projectile actually spawns. aimLevel (and therefore
                // localOrigin) is deliberately reused from the earlier pass: the aim animation has already
                // played, so the spawn offset must stay consistent with what the client rendered.
                if (ACE.Server.Managers.PropertyManager.GetBool("missile_fresh_solution").Item)
                {
                    var freshVelocity = CalculateProjectileVelocity(localOrigin, target, projectileSpeed, out var freshOrigin, out var freshOrientation);

                    // if the re-solve fails (target moved out of range mid-animation), keep the original
                    // solution rather than misfiring -- the attack was already committed
                    if (freshVelocity != Vector3.Zero && freshVelocity.IsValid())
                    {
                        launchOrigin = freshOrigin;
                        launchOrientation = freshOrientation;
                        launchVelocity = freshVelocity;
                    }
                }

                var projectile = LaunchProjectile(launcher, ammo, target, launchOrigin, launchOrientation, launchVelocity);
                UpdateAmmoAfterLaunch(ammo);
            });

            // ammo remaining?
            if (!ammo.UnlimitedUse && (ammo.StackSize == null || ammo.StackSize <= 1))
            {
                actionChain.AddAction(this, () =>
                {
                    Session.Network.EnqueueSend(new GameEventCommunicationTransientString(Session, "You are out of ammunition!"));
                    SetCombatMode(CombatMode.NonCombat);
                    Attacking = false;
                    OnAttackDone();
                });

                actionChain.EnqueueChain();
                return;
            }

            // reload animation
            var animSpeed = GetAnimSpeed();
            var reloadTime = EnqueueMotionPersist(actionChain, stance, MotionCommand.Reload, animSpeed);

            // reset for next projectile
            EnqueueMotionPersist(actionChain, stance, MotionCommand.Ready);
            var linkTime = MotionTable.GetAnimationLength(MotionTableId, stance, MotionCommand.Reload, MotionCommand.Ready);
            //var cycleTime = MotionTable.GetCycleLength(MotionTableId, CurrentMotionState.Stance, MotionCommand.Ready);

            actionChain.AddAction(this, () =>
            {
                if (CombatMode == CombatMode.Missile)
                    EnqueueBroadcast(new GameMessageParentEvent(this, ammo, ACE.Entity.Enum.ParentLocation.RightHand, ACE.Entity.Enum.Placement.RightHandCombat));
            }); 

            actionChain.AddDelaySeconds(linkTime);

            if (ammo.MaterialType != null && !ammo.UnlimitedUse && ammo.IsThrownWeapon && ammo.StackSize <= 2)
            {
                actionChain.AddAction(this, () =>
                {
                    Session.Network.EnqueueSend(new GameEventCommunicationTransientString(Session, $"You refrain from throwing your last {ammo.NameWithMaterial}!"));
                    SetCombatMode(CombatMode.NonCombat);
                    Attacking = false;
                    OnAttackDone();
                });

                actionChain.EnqueueChain();
                return;
            }

            actionChain.AddAction(this, () =>
            {
                Attacking = false;

                if (creature.IsAlive && GetCharacterOption(CharacterOption.AutoRepeatAttacks) && !IsBusy && !AttackCancelled)
                {
                    // client starts refilling accuracy bar
                    Session.Network.EnqueueSend(new GameEventAttackDone(Session));

                    AccuracyLevel = AttackQueue.Fetch();

                    // can be cancelled, but cannot be pre-empted with another attack
                    var nextAttack = new ActionChain();
                    var nextRefillTime = AccuracyLevel;

                    NextRefillTime = DateTime.UtcNow.AddSeconds(nextRefillTime);
                    nextAttack.AddDelaySeconds(nextRefillTime);

                    // perform next attack
                    nextAttack.AddAction(this, () => { LaunchMissile(target, attackSequence, stance, true); });
                    nextAttack.EnqueueChain();
                }
                else
                    OnAttackDone();
            });

            actionChain.EnqueueChain();

            if (UnderLifestoneProtection)
                LifestoneProtectionDispel();
        }

        // TODO: the damage pipeline currently uses the creature ammo instead of the projectile
        // for calculating damage. when the last arrow is launched, the player ammo will be null
        // give projectiles an owner, and have the damage pipeline take the actual damage source object
        // (ie. the arrow-in-flight, or a melee weapon)

        public override float GetAimHeight(WorldObject target)
        {
            switch (AttackHeight.Value)
            {
                case ACE.Entity.Enum.AttackHeight.High: return 1.0f;
                case ACE.Entity.Enum.AttackHeight.Medium: return 2.0f;
                //case AttackHeight.Low: return target.Height;
                case ACE.Entity.Enum.AttackHeight.Low: return 3.0f;
            }
            return 2.0f;
        }

        public override void UpdateAmmoAfterLaunch(WorldObject ammo)
        {
            //if (ammo.UnlimitedUse)
            //    return;

            // hide previously held ammo
            EnqueueBroadcast(new GameMessagePickupEvent(ammo));

            if (ammo.UnlimitedUse)
                return;

            if (ammo.StackSize == null || ammo.StackSize <= 1)
                TryDequipObjectWithNetworking(ammo.Guid, out _, DequipObjectAction.ConsumeItem);
            else
                TryConsumeFromInventoryWithNetworking(ammo, 1);
        }

        public bool TargetInRange(WorldObject target)
        {
            // 2d or 3d distance?
            var dist = Location.DistanceTo(target.Location);

            var maxRange = GetMaxMissileRange();

            return dist <= maxRange;
        }
    }
}
