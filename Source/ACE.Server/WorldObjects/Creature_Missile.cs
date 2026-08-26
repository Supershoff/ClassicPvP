using System;
using System.Linq;
using System.Numerics;

using ACE.Database;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics;
using ACE.Server.Physics.Extensions;

namespace ACE.Server.WorldObjects
{
    partial class Creature
    {
        public float ReloadMissileAmmo(ActionChain actionChain = null)
        {
            var weapon = GetEquippedMissileWeapon();
            var ammo = GetEquippedAmmo();

            if (weapon == null || ammo == null) return 0.0f;

            var newChain = actionChain == null;
            if (newChain)
                actionChain = new ActionChain();

            var animLength = 0.0f;
            if (weapon.IsAmmoLauncher)
            {
                var animSpeed = GetAnimSpeed();
                //Console.WriteLine($"AnimSpeed: {animSpeed}");

                animLength = EnqueueMotionPersist(actionChain, MotionCommand.Reload, animSpeed);   // start pulling out next arrow
                EnqueueMotionPersist(actionChain, MotionCommand.Ready);    // finish reloading
            }

            // ensure ammo visibility for players
            actionChain.AddAction(this, () =>
            {
                if (CombatMode != CombatMode.Missile)
                    return;

                EnqueueActionBroadcast(p => p.TrackEquippedObject(this, ammo));

                var delayChain = new ActionChain();
                delayChain.AddDelaySeconds(0.001f);     // ensuring this message gets sent after player broadcasts above...
                delayChain.AddAction(this, () =>
                {
                    EnqueueBroadcast(new GameMessageParentEvent(this, ammo, ACE.Entity.Enum.ParentLocation.RightHand, ACE.Entity.Enum.Placement.RightHandCombat));
                });
                delayChain.EnqueueChain();
            });

            if (newChain)
                actionChain.EnqueueChain();

            var animLength2 = Physics.Animation.MotionTable.GetAnimationLength(MotionTableId, CurrentMotionState.Stance, MotionCommand.Reload, MotionCommand.Ready);
            //Console.WriteLine($"AnimLength: {animLength} + {animLength2}");

            return animLength + animLength2;
        }

        public Vector3 GetDir2D(Vector3 source, Vector3 dest)
        {
            var diff = dest - source;
            diff.Z = 0;
            return Vector3.Normalize(diff);
        }

        /// <summary>
        /// Launches a projectile from player to target
        /// </summary>
        public WorldObject LaunchProjectile(WorldObject weapon, WorldObject ammo, WorldObject target, Vector3 origin, Quaternion orientation, Vector3 velocity)
        {
            var player = this as Player;

            if (!velocity.IsValid())
            {
                if (player != null)
                    player.SendWeenieError(WeenieError.YourAttackMisfired);

                return null;
            }

            var hasSplitArrows = ammo?.GetProperty(PropertyBool.SplitArrows) ?? false;

            var proj = WorldObjectFactory.CreateNewWorldObject(ammo.WeenieClassId);

            if (ammo.WeenieType == WeenieType.Missile && ammo.MaterialType != null)
            {
                // Copy some values here so our thrown weapon "ammo" is a representative copy of our mutated weapon.
                proj.Damage = ammo.Damage;
                proj.DamageVariance = ammo.DamageVariance;
                proj.WeaponTime = ammo.WeaponTime;
                proj.WeaponOffense = ammo.WeaponOffense;
                proj.WeaponDefense = ammo.WeaponDefense;
                proj.WeaponMissileDefense = ammo.WeaponMissileDefense;
                proj.WeaponMagicDefense = ammo.WeaponMagicDefense;

                proj.MaterialType = ammo.MaterialType;
                proj.Workmanship = ammo.Workmanship;
                proj.GemType = ammo.GemType;
                proj.GemCount = ammo.GemCount;

                proj.EncumbranceVal = ammo.StackUnitEncumbrance;
                proj.StackUnitEncumbrance = ammo.StackUnitEncumbrance;
                proj.Value = ammo.StackUnitValue;
                proj.StackUnitValue = ammo.StackUnitValue;

                proj.CriticalMultiplier = ammo.CriticalMultiplier;
                proj.CriticalFrequency = ammo.CriticalFrequency;
                proj.SlayerCreatureType = ammo.SlayerCreatureType;
                proj.SlayerDamageBonus = ammo.SlayerDamageBonus;
                proj.IgnoreMagicResist = ammo.IgnoreMagicResist;
                proj.IgnoreMagicArmor = ammo.IgnoreMagicArmor;
                proj.Translucency = ammo.Translucency;
                proj.IgnoreArmor = ammo.IgnoreArmor;
                proj.IgnoreShield = ammo.IgnoreShield;
                proj.AbsorbMagicDamage = ammo.AbsorbMagicDamage;
                proj.ResistanceModifierType = ammo.ResistanceModifierType;
                proj.ResistanceModifier = ammo.ResistanceModifier;
                proj.ImbuedEffect = ammo.ImbuedEffect;
            }

            proj.ProjectileSource = this;
            proj.ProjectileTarget = target;

            proj.ProjectileLauncher = weapon;
            proj.ProjectileAmmo = ammo;

            proj.Location = new Position(Location);
            proj.Location.Pos = origin;
            proj.Location.Rotation = orientation;

            if (hasSplitArrows)
                proj.SetProperty(PropertyBool.IsSplitArrow, true);

            SetProjectilePhysicsState(proj, target, velocity);

            var success = LandblockManager.AddObject(proj);

            if (!success || proj.PhysicsObj == null)
            {
                if (!proj.HitMsg)
                {
                    if (player != null)
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("Your missile attack hit the environment.", ChatMessageType.Broadcast));
                    else
                        MonsterProjectile_OnCollideEnvironment();
                }

                proj.Destroy();
                return null;
            }

            if (!IsProjectileVisible(proj))
            {
                proj.OnCollideEnvironment();

                proj.Destroy();
                return null;
            }

            var pkStatus = player?.PlayerKillerStatus ?? PlayerKillerStatus.Creature;

            proj.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(proj, PropertyInt.PlayerKillerStatus, (int)pkStatus));
            proj.EnqueueBroadcast(new GameMessageScript(proj.Guid, PlayScript.Launch, 0f));

            //Custom Missile Volleys
            if (hasSplitArrows)
            {
                var splitCount = ammo?.GetProperty(PropertyInt.SplitArrowCount) ?? DEFAULT_SPLIT_ARROW_COUNT;
                if (splitCount > 0)
                {
                    CreateSplitArrows(weapon, ammo, target, origin, orientation, velocity);
                }
            }

            // detonate point-blank projectiles immediately
            /*var radsum = target.PhysicsObj.GetRadius() + proj.PhysicsObj.GetRadius();
            var dist = Vector3.Distance(origin, dest);
            if (dist < radsum)
            {
                Console.WriteLine($"Point blank");
                proj.OnCollideObject(target);
            }*/

            return proj;
        }

        public const float ProjSpawnHeight = 0.8454f;

        /// <summary>
        /// Returns the origin to spawn the projectile in the attacker local space
        /// </summary>
        public Vector3 GetProjectileSpawnOrigin(uint projectileWcid, MotionCommand motion)
        {
            var attackerRadius = PhysicsObj.GetPhysicsRadius();
            var projectileRadius = GetProjectileRadius(projectileWcid);

            //Console.WriteLine($"{Name} radius: {attackerRadius}");
            //Console.WriteLine($"Projectile {projectileWcid} radius: {projectileRadius}");

            var radsum = attackerRadius * 2.0f + projectileRadius * 2.0f + PhysicsGlobals.EPSILON;

            var origin = new Vector3(0, radsum, 0);

            // rotate by aim angle
            var angle = motion.GetAimAngle().ToRadians();
            var zRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);

            origin = Vector3.Transform(origin, zRotation);

            origin.Z += Height * ProjSpawnHeight;

            return origin;
        }

        /// <summary>
        /// Returns the cached physics radius for a projectile wcid
        /// </summary>
        private static float GetProjectileRadius(uint projectileWcid)
        {
            if (ProjectileRadiusCache.TryGetValue(projectileWcid, out var radius))
                return radius;

            var weenie = DatabaseManager.World.GetCachedWeenie(projectileWcid);

            if (weenie == null)
            {
                log.Error($"Creature_Missile.GetProjectileRadius(): couldn't find projectile weenie {projectileWcid}");
                return 0.0f;
            }

            if (!weenie.PropertiesDID.TryGetValue(PropertyDataId.Setup, out var setupId))
            {
                log.Error($"Creature_Missile.GetProjectileRadius(): couldn't find SetupId for {weenie.WeenieClassId} - {weenie.ClassName}");
                return 0.0f;
            }

            var setup = DatManager.PortalDat.ReadFromDat<SetupModel>(setupId);

            if (!weenie.PropertiesFloat.TryGetValue(PropertyFloat.DefaultScale, out var scale))
                scale = 1.0f;

            var result = (float)(setup.Spheres[0].Radius * scale);

            ProjectileRadiusCache.TryAdd(projectileWcid, result);

            return result;
        }

        // lowest value found in data / for starter bows
        public const float DefaultProjectileSpeed = 20.0f;

        public float GetProjectileSpeed()
        {
            var missileLauncher = GetEquippedMissileWeapon();

            double maxVelocity;
            if (missileLauncher?.WeenieType == WeenieType.Missile)
                maxVelocity = GetThrownWeaponMaxVelocity(missileLauncher);
            else
                maxVelocity = missileLauncher?.MaximumVelocity ?? DefaultProjectileSpeed;

            if (maxVelocity == 0.0f)
            {
                log.Warn($"{Name}.GetMissileSpeed() - {missileLauncher.Name} ({missileLauncher.Guid}) has speed 0");

                maxVelocity = DefaultProjectileSpeed;
            }

            if (this is Player player && player.GetCharacterOption(CharacterOption.UseFastMissiles))
            {
                maxVelocity *= PropertyManager.GetDouble("fast_missile_modifier").Item;
            }

            // hard cap in physics engine
            maxVelocity = Math.Min(maxVelocity, PhysicsGlobals.MaxVelocity);

            //Console.WriteLine($"MaxVelocity: {maxVelocity}");

            return (float)maxVelocity;
        }

        /// <summary>
        /// Returns the height above the target's base position to aim a projectile at.
        ///
        /// Default (retail) behavior divides the target height by GetAimHeight(), which for a player
        /// puts the High aim point at 1.0 * Height -- the exact top of the upper collision sphere --
        /// and the Medium aim point in the gap between the two spheres. Both are low-tolerance spots.
        ///
        /// With 'missile_aim_center_mass' enabled, the aim point is instead a fraction of the target
        /// height chosen to land on a collision sphere center.
        ///
        /// Human collision volume (setup 0200004E): 2 spheres, r 0.48, at z 0.475 and z 1.35, Height 1.835.
        /// Combined with an arrow radius of 0.10 that gives a 0.58m hit envelope, and lateral tolerances of:
        ///     High   1.000 * Height = 1.835m -> 0.318m      (center-mass 0.75 -> 0.580m)
        ///     Medium 0.500 * Height = 0.918m -> 0.386m      (center-mass 0.62 -> 0.540m)
        ///     Low    0.333 * Height = 0.612m -> 0.564m      (center-mass 0.27 -> 0.580m)
        ///
        /// These fractions are derived from the player collision model specifically, so they are only
        /// applied against player targets. Monsters use a wide variety of setups -- many are a single
        /// sphere, where the existing Height/2 aim point is already center of mass -- and reusing the
        /// player-tuned fractions there would be a regression rather than a fix.
        /// </summary>
        public float GetAimPointOffset(WorldObject target)
        {
            if (!PropertyManager.GetBool("missile_aim_center_mass").Item || !(target is Player))
                return target.Height / GetAimHeight(target);

            var fraction = (AttackHeight ?? ACE.Entity.Enum.AttackHeight.Medium) switch
            {
                ACE.Entity.Enum.AttackHeight.High => PropertyManager.GetDouble("missile_aim_center_mass_high").Item,
                ACE.Entity.Enum.AttackHeight.Low => PropertyManager.GetDouble("missile_aim_center_mass_low").Item,
                _ => PropertyManager.GetDouble("missile_aim_center_mass_medium").Item,
            };

            return target.Height * (float)fraction;
        }

        public Vector3 GetAimVelocity(WorldObject target, float projectileSpeed)
        {
            var crossLandblock = Location.Landblock != target.Location.Landblock;

            // eye level -> target point
            var origin = crossLandblock ? Location.ToGlobal(false) : Location.Pos;
            origin.Z += Height * ProjSpawnHeight;

            var dest = crossLandblock ? target.Location.ToGlobal(false) : target.Location.Pos;
            dest.Z += GetAimPointOffset(target);

            var dir = Vector3.Normalize(dest - origin);

            var velocity = GetProjectileVelocity(target, origin, dir, dest, projectileSpeed, out float time);

            return velocity;
        }

        public Vector3 CalculateProjectileVelocity(Vector3 localOrigin, WorldObject target, float projectileSpeed, out Vector3 origin, out Quaternion rotation)
        {
            var sourceLoc = PhysicsObj.Position.ACEPosition();
            var targetLoc = target.PhysicsObj.Position.ACEPosition();

            var crossLandblock = sourceLoc.Landblock != targetLoc.Landblock;

            var startPos = crossLandblock ? sourceLoc.ToGlobal(false) : sourceLoc.Pos;
            var endPos = crossLandblock ? targetLoc.ToGlobal(false) : targetLoc.Pos;

            var dir = Vector3.Normalize(endPos - startPos);

            var angle = Math.Atan2(-dir.X, dir.Y);

            rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)angle);

            origin = sourceLoc.Pos + Vector3.Transform(localOrigin, rotation);

            startPos += Vector3.Transform(localOrigin, rotation);
            endPos.Z += GetAimPointOffset(target);

            var velocity = GetProjectileVelocity(target, startPos, dir, endPos, projectileSpeed, out float time);

            return velocity;
        }

        /// <summary>
        /// Updates the ammo count or destroys the ammo after launching the projectile.
        /// </summary>
        /// <param name="ammo">The equipped missile ammo object</param>
        public virtual void UpdateAmmoAfterLaunch(WorldObject ammo)
        {
            // hide previously held ammo
            EnqueueBroadcast(new GameMessagePickupEvent(ammo));

            // monsters have infinite ammo?

            /*if (ammo.StackSize == null || ammo.StackSize <= 1)
            {
                TryUnwieldObjectWithBroadcasting(ammo.Guid, out _, out _);
                ammo.Destroy();
            }
            else
            {
                ammo.SetStackSize(ammo.StackSize - 1);
                EnqueueBroadcast(new GameMessageSetStackSize(ammo));
            }*/
        }

        /// <summary>
        /// Calculates the velocity to launch the projectile from origin to dest
        /// </summary>
        public Vector3 GetProjectileVelocity(WorldObject target, Vector3 origin, Vector3 dir, Vector3 dest, float speed, out float time, bool useGravity = true)
        {
            time = 0.0f;
            Vector3 s0;
            float t0;

            var gravity = useGravity ? -PhysicsGlobals.Gravity : 0.00001f;

            var targetVelocity = target.PhysicsObj.CachedVelocity;

            if (!targetVelocity.Equals(Vector3.Zero))
            {
                if (this is Player player && !player.GetCharacterOption(CharacterOption.LeadMissileTargets))
                {
                    // fall through
                }
                else
                {
                    // use movement quartic solver
                    if (!PropertyManager.GetBool("trajectory_alt_solver").Item)
                    {
                        var numSolutions = Trajectory.solve_ballistic_arc(origin, speed, dest, targetVelocity, gravity, out s0, out _, out time);

                        if (numSolutions > 0)
                            return s0;

                        // MISSILE FIX 2: the quartic has no intercept solution across a band that is still
                        // well inside the weapon's max range -- thrown weapons past ~21-30m and bows past
                        // ~51-61m against a fleeing target. Retail ACE silently drops through to the
                        // stationary solver below, which aims at where the target is standing right now:
                        // a guaranteed miss after a 1-2s flight, with no diagnostic.
                        //
                        // Fall back to the lateral solver instead (the same one spell projectiles use). It
                        // holds the horizontal speed fixed and solves for the vertical component, so it
                        // finds a solution whenever the horizontal intercept quadratic does.
                        //
                        // Caveat: because no solution exists at exactly 'speed' (that is why the quartic
                        // failed), the resulting velocity is necessarily faster than the weapon's max
                        // velocity. Measured within each weapon's actual max range vs a target fleeing at
                        // 6 m/s: thrown +10% at 22m rising to +18% at its ~35m range limit, bows +10% at
                        // 55m rising to +16% at their ~76m limit. Well under the PhysicsGlobals.MaxVelocity
                        // cap of 50, but it does mean these long shots land slightly sooner than a
                        // strict reading of the weapon's MaximumVelocity would imply.
                        var leadFallback = PropertyManager.GetBool("missile_lead_fallback").Item;
                        var leadFallbackLog = PropertyManager.GetBool("missile_lead_fallback_log").Item;

                        if (leadFallback || leadFallbackLog)
                        {
                            // NOTE: solve_ballistic_arc_lateral takes gravity as a signed acceleration
                            // (negative == down), while solve_ballistic_arc above takes it positive-down.
                            // The two solvers in Trajectory.cs use opposite conventions.
                            var lateralGravity = useGravity ? PhysicsGlobals.Gravity : 0.0f;

                            var solved = Trajectory.solve_ballistic_arc_lateral(origin, speed, dest, targetVelocity, lateralGravity, out var lateralVelocity, out var lateralTime, out _);

                            if (leadFallbackLog)
                            {
                                var dist = Vector3.Distance(origin, dest);

                                if (solved && lateralVelocity.IsValid())
                                    log.Info($"[MISSILE_LEAD] {Name} - quartic found no intercept at {dist:F1}m vs targetVelocity {targetVelocity} (speed {speed:F1}). Lateral solver -> {lateralVelocity} (|v| {lateralVelocity.Length():F1}, t {lateralTime:F2}s). Applied: {leadFallback}");
                                else
                                    log.Info($"[MISSILE_LEAD] {Name} - quartic found no intercept at {dist:F1}m vs targetVelocity {targetVelocity} (speed {speed:F1}). Lateral solver also failed - falling through to zero-lead stationary aim");
                            }

                            if (leadFallback && solved && lateralVelocity.IsValid())
                            {
                                time = lateralTime;
                                return lateralVelocity;
                            }
                        }
                    }
                    else
                        return Trajectory2.CalculateTrajectory(origin, dest, targetVelocity, speed, useGravity);
                }
            }

            // use stationary solver
            if (!PropertyManager.GetBool("trajectory_alt_solver").Item)
            {
                Trajectory.solve_ballistic_arc(origin, speed, dest, gravity, out s0, out _, out t0, out _);

                time = t0;
                return s0;
            }
            else
                return Trajectory2.CalculateTrajectory(origin, dest, Vector3.Zero, speed, useGravity);
        }

        /// <summary>
        /// Sets the physics state for a launched projectile
        /// </summary>
        public void SetProjectilePhysicsState(WorldObject obj, WorldObject target, Vector3 velocity)
        {
            obj.InitPhysicsObj();

            obj.ReportCollisions = true;
            obj.Missile = true;
            obj.AlignPath = true;
            obj.PathClipped = true;
            obj.Ethereal = false;
            obj.IgnoreCollisions = false;

            var pos = obj.Location.Pos;
            var rotation = obj.Location.Rotation;
            obj.PhysicsObj.Position.Frame.Origin = pos;
            obj.PhysicsObj.Position.Frame.Orientation = rotation;

            if (obj.HasMissileFlightPlacement)
                obj.Placement = ACE.Entity.Enum.Placement.MissileFlight;
            else
                obj.Placement = null;

            obj.CurrentMotionState = null;

            obj.PhysicsObj.Velocity = velocity;
            obj.PhysicsObj.ProjectileTarget = target.PhysicsObj;

            // Projectiles with RotationSpeed get omega values and "align path" turned off which
            // creates the nice swirling animation
            if ((obj.RotationSpeed ?? 0) != 0)
            {
                obj.AlignPath = false;
                obj.PhysicsObj.Omega = new Vector3((float)(Math.PI * 2 * obj.RotationSpeed), 0, 0);
            }

            obj.PhysicsObj.set_active(true);
        }

        public Sound GetLaunchMissileSound(WorldObject weapon)
        {
            switch (weapon.DefaultCombatStyle)
            {
                case CombatStyle.Bow:
                    return Sound.BowRelease;
                case CombatStyle.Crossbow:
                    return Sound.CrossbowRelease;
                default:
                    return Sound.ThrownWeaponRelease1;
            }
        }

        public const float MetersToYards = 1.094f;    // 1.09361
        public const float MissileRangeCap = 85.0f / MetersToYards;   // 85 yards = ~77.697 meters w/ ac formula
        public const float DefaultMaxVelocity = 20.0f;    // ?

        public float GetMaxMissileRange()
        {
            var weapon = GetEquippedMissileWeapon();
            double maxVelocity = weapon?.MaximumVelocity ?? DefaultMaxVelocity;

            //if (WeenieType == WeenieType.Missile && (weapon?.MaximumVelocity ?? 0) == 0)
            if (weapon?.WeenieType == WeenieType.Missile)
                maxVelocity = GetThrownWeaponMaxVelocity(weapon);
            else
                maxVelocity = weapon?.MaximumVelocity ?? DefaultMaxVelocity;

            var missileRange = (float)Math.Pow(maxVelocity, 2.0f) * 0.1020408163265306f;
            //var missileRange = (float)Math.Pow(maxVelocity, 2.0f) * 0.0682547266398198f;

            //var strengthMod = SkillFormula.GetAttributeMod((int)Strength.Current);
            //var maxRange = Math.Min(missileRange * strengthMod, MissileRangeCap);
            var maxRange = Math.Min(missileRange, MissileRangeCap);

            // any kind of other caps for monsters specifically?
            // throwing lugian rocks @ 85 yards seems a bit far...

            //Console.WriteLine($"{Name}.GetMaxMissileRange(): maxVelocity={maxVelocity}, strengthMod={strengthMod}, maxRange={maxRange}");

            // for client display
            /*var maxRangeYards = maxRange * MetersToYards;
            if (maxRangeYards >= 10.0f)
                maxRangeYards -= maxRangeYards % 5.0f;
            else
                maxRangeYards = (float)Math.Ceiling(maxRangeYards);

            Console.WriteLine($"Max range: {maxRange} ({maxRangeYards} yds.)");*/

            return maxRange;
        }

        public static MotionCommand GetAimLevel(Vector3 velocity)
        {
            // get z-angle?
            var zAngle = Vector3.Normalize(velocity).Z * 90.0f;

            var aimLevel = MotionCommand.AimLevel;

            if (zAngle >= 82.5f)
                aimLevel = MotionCommand.AimHigh90;
            else if (zAngle >= 67.5f)
                aimLevel = MotionCommand.AimHigh75;
            else if (zAngle >= 52.5f)
                aimLevel = MotionCommand.AimHigh60;
            else if (zAngle >= 37.5f)
                aimLevel = MotionCommand.AimHigh45;
            else if (zAngle >= 22.5f)
                aimLevel = MotionCommand.AimHigh30;
            else if (zAngle >= 7.5f)
                aimLevel = MotionCommand.AimHigh15;
            else if (zAngle > -7.5f)
                aimLevel = MotionCommand.AimLevel;
            else if (zAngle > -22.5f)
                aimLevel = MotionCommand.AimLow15;
            else if (zAngle > -37.5f)
                aimLevel = MotionCommand.AimLow30;
            else if (zAngle > -52.5f)
                aimLevel = MotionCommand.AimLow45;
            else if (zAngle > -67.5f)
                aimLevel = MotionCommand.AimLow60;
            else if (zAngle > -82.5f)
                aimLevel = MotionCommand.AimLow75;
            else
                aimLevel = MotionCommand.AimLow90;

            //Console.WriteLine($"Z Angle: {aimLevel.GetAimAngle()}");

            return aimLevel;
        }

        // Split arrow constants
        private const int DEFAULT_SPLIT_ARROW_COUNT = 3;
        private const float DEFAULT_SPLIT_ARROW_DAMAGE_MULTIPLIER = 0.5f;

        // Split arrow validation constants
        private const int SPLIT_ARROW_COUNT_MIN = 3;
        private const int SPLIT_ARROW_COUNT_MAX = 9;
        private const float SPLIT_ARROW_DAMAGE_MULTIPLIER_MIN = 0f;
        private const float SPLIT_ARROW_DAMAGE_MULTIPLIER_MAX = 1f;

        /// <summary>
        /// Creates additional projectiles for split arrow effect
        /// </summary>
        /// <param name="weapon">The weapon that has split arrows capability</param>
        /// <param name="ammo">The ammunition to use for split arrows</param>
        /// <param name="target">The primary target</param>
        /// <param name="mainArrowOrigin">Origin position for split arrows</param>
        /// <param name="mainArrowOrientation">Orientation for split arrows</param>
        private void CreateSplitArrows(WorldObject weapon, WorldObject ammo, WorldObject target, Vector3 mainArrowOrigin, Quaternion mainArrowOrientation, Vector3 mainArrowVelocity)
        {
            try
            {
                // Validate inputs
                if (weapon == null || ammo == null || target == null)
                {
                    log.Warn("CreateSplitArrows called with null parameters");
                    return;
                }

                // Additional safety checks
                if (!mainArrowOrigin.IsValid())
                {
                    log.Warn($"CreateSplitArrows called with invalid origin: {mainArrowOrigin}");
                    return;
                }

                if (target is not Creature targetCreature)
                {
                    log.Warn($"CreateSplitArrows called with non-creature target: {target?.Name}");
                    return;
                }

                // Ensure target is fully initialized before creating split arrows
                if (targetCreature.PhysicsObj == null)
                {
                    log.Warn($"CreateSplitArrows called with uninitialized target: {targetCreature.Name} (PhysicsObj is null)");
                    return;
                }

                // Cache weapon properties to avoid repeated property lookups
                var splitCount = ammo.SplitArrowCount ?? DEFAULT_SPLIT_ARROW_COUNT;
                var damageMultiplier = (float?)(ammo.SplitArrowDamageMultiplier) ?? DEFAULT_SPLIT_ARROW_DAMAGE_MULTIPLIER;

                // Apply safety clamps to prevent invalid values
                splitCount = Math.Clamp(splitCount, SPLIT_ARROW_COUNT_MIN, SPLIT_ARROW_COUNT_MAX);
                damageMultiplier = Math.Clamp(damageMultiplier, SPLIT_ARROW_DAMAGE_MULTIPLIER_MIN, SPLIT_ARROW_DAMAGE_MULTIPLIER_MAX);

                var additionalArrowCount = splitCount - 1; // SplitArrowCount directly represents number of split arrows to create

                // Cache projectile speed before the loop to avoid repeated calls
                var cachedSpeed = GetProjectileSpeed();
                if (cachedSpeed <= 0)
                {
                    log.Warn($"Invalid projectile speed: {cachedSpeed}, skipping split arrows");
                    return;
                }
                var arrowsCreated = 0;
                var currSpread = 5.0f;
                var currOrigin = mainArrowOrigin;

                // Create new projectile with error handling
                for (int i = 0; i < additionalArrowCount; i++)
                {
                    WorldObject splitProj;
                    try
                    {
                        splitProj = WorldObjectFactory.CreateNewWorldObject(ammo.WeenieClassId);
                        if (splitProj == null)
                        {
                            log.Error($"Failed to create split projectile for ammo {ammo.WeenieClassId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Exception creating split projectile for ammo {ammo.WeenieClassId}: {ex.Message}", ex);
                        continue;
                    }

                    // Set normal projectile properties
                    splitProj.ProjectileSource = this;
                    splitProj.ProjectileTarget = target;
                    splitProj.ProjectileLauncher = weapon;
                    splitProj.ProjectileAmmo = ammo;
                    splitProj.Damage = (int)Math.Round((decimal)(ammo.Damage * damageMultiplier));
                    splitProj.SlayerCreatureType = ammo.SlayerCreatureType;
                    splitProj.SlayerDamageBonus = ammo.SlayerDamageBonus;

                    // Mark as split arrow for special handling
                    splitProj.SetProperty(PropertyBool.IsSplitArrow, true);

                    //If this is an odd iterator, spawn to the right and increment the spread by 5 degrees
                    bool isLeft = true;
                    if ((i & 1) == 1)
                    {
                        currSpread += 5f;
                        isLeft = false;
                    }

                    splitProj.Location = new Position(Location);
                    splitProj.Location.Pos = mainArrowOrigin;
                    splitProj.Location.Rotation = mainArrowOrientation;

                    // For left arrow - rotate velocity left by X degrees
                    var spreadAngle = currSpread * (float)(Math.PI / 180.0f); // currSpread degrees in radians

                    // Rotate around Z axis for horizontal spread
                    var splitRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, spreadAngle * (isLeft ? 1 : -1));
                    var splitVelocity = Vector3.Transform(mainArrowVelocity, splitRotation);

                    // Add small random offset to prevent arrow collision during simultaneous spawning
                    var spawnOffset = new Vector3(
                        (float)(Random.Shared.NextDouble() - 0.5) * 0.3f, // ±0.15 units
                        (float)(Random.Shared.NextDouble() - 0.5) * 0.3f, // ±0.15 units
                        0.0f  // Keep Z the same
                    );
                    currOrigin += spawnOffset;

                    // Position the split arrow at the calculated origin with proper rotation
                    // Combine mainRotation with spread rotation for correct projectile facing
                    var finalRotation = Quaternion.Multiply(mainArrowOrientation, splitRotation);

                    // Match the standard projectile spawn pattern
                    splitProj.Location = new Position(Location);
                    splitProj.Location.Pos = currOrigin;
                    splitProj.Location.Rotation = finalRotation;

                    log.Debug($"[SPLIT ARROW] Before AddObject - Cell: 0x{splitProj.Location.Cell:X8}, Pos: {currOrigin}, PhysicsObj null: {splitProj.PhysicsObj == null}");

                    // Validate velocity and position before adding to world
                    if (!splitVelocity.IsValid())
                    {
                        log.Error($"Invalid velocity for split arrow {arrowsCreated + 1}: {splitVelocity}");
                        splitProj.Destroy();
                        continue;
                    }

                    if (!currOrigin.IsValid())
                    {
                        log.Error($"Invalid position for split arrow {arrowsCreated + 1}: {currOrigin}");
                        splitProj.Destroy();
                        continue;
                    }

                    // Set physics state (ensure target has physics)
                    if (target?.PhysicsObj == null)
                    {
                        splitProj.Destroy();
                        continue;
                    }
                    SetProjectilePhysicsState(splitProj, target, splitVelocity);

                    // Add to world
                    var success = LandblockManager.AddObject(splitProj);
                    if (!success)
                    {
                        log.Debug($"[SPLIT ARROW] Skipped close target - Target: {target?.Name}, Distance: {Vector3.Distance(this.Location.Pos, target.Location.Pos):F2} units");
                        splitProj.Destroy();
                        continue;
                    }

                    // Check if projectile is visible after adding to world
                    if (!IsProjectileVisible(splitProj))
                    {
                        log.Error($"[SPLIT ARROW VISIBILITY FAILURE] Split arrow not visible after AddObject - Target: {target?.Name}");
                        splitProj.Destroy();
                        continue;
                    }

                    // Projectile successfully added and visible - activate and broadcast
                    if (splitProj.PhysicsObj != null)
                    {
                        splitProj.PhysicsObj.set_active(true);
                        splitProj.ReportCollisions = true;

                        // Send launch broadcasts like the main projectile
                        var pkStatus = (this as Player)?.PlayerKillerStatus ?? PlayerKillerStatus.Creature;
                        splitProj.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(splitProj, PropertyInt.PlayerKillerStatus, (int)pkStatus));
                        splitProj.EnqueueBroadcast(new GameMessageScript(splitProj.Guid, PlayScript.Launch, 0f));

                        arrowsCreated++;
                    }
                    else
                    {
                        log.Error($"Split arrow has null PhysicsObj after AddObject - Target: {target?.Name}");
                        splitProj.Destroy();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in CreateSplitArrows: {ex.Message}", ex);
            }
        }
    }
}
