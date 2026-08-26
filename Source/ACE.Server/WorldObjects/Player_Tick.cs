using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ACE.Common;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.Enum;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Sequence;
using ACE.Server.Network.Structure;
using ACE.Server.Physics;
using ACE.Server.Physics.Common;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        private readonly ActionQueue actionQueue = new ActionQueue();

        private int initialAge;
        private DateTime initialAgeTime;

        private const double ageUpdateInterval = 7;
        private double nextAgeUpdateTime;

        private double houseRentWarnTimestamp;
        private const double houseRentWarnInterval = 3600;

        private double leyLineAmuletsTickTimestamp;
        private const double leyLineAmuletsTickInterval = 1800;

        private double vitaeTickTimestamp;
        private const double vitaeTickInterval = 300;

        private double enchantmentTickTimestamp;
        private const double enchantmentTickInterval = 0.5;

        private double PvPInciteTickTimestamp;

        private double RoadCheckTimestamp;
        private const double RoadCheckInterval = 2.5;
        private int OnRoadStatus;

        public void Player_Tick(double currentUnixTime)
        {
            if (CharacterSaveFailed)
            {
                // Boot the player as their Character object is not saving properly
                if (!IsLoggingOut)
                {
                    log.Error($"{Session.Player.Name} | 0x{Guid} | Account: {Account.AccountName} - disconnected for CharacterSaveFailed");
                    //Session.SendCharacterError(CharacterError.AccountLogin); // forces client to error screen
                    Session.Terminate(SessionTerminationReason.CharacterSaveFailed, new GameMessageCharacterError(CharacterError.AccountLogin));
                    //Session.LogOffPlayer(true);
                    CharacterSaveFailed = false;
                }
                return;
            }

            if (BiotaSaveFailed)
            {
                // Boot the player as their Biota object is not saving properly
                if (!IsLoggingOut)
                {
                    log.Error($"{Session.Player.Name} | 0x{Guid} | Account: {Account.AccountName} - disconnected for BiotaSaveFailed");
                    //Session.SendCharacterError(CharacterError.AccountLogin); // forces client to error screen
                    Session.Terminate(SessionTerminationReason.BiotaSaveFailed, new GameMessageCharacterError(CharacterError.AccountLogin));
                    //Session.LogOffPlayer(true);
                    BiotaSaveFailed = false;
                }
                return;
            }

            actionQueue.RunActions();

            if (nextAgeUpdateTime <= currentUnixTime)
            {
                nextAgeUpdateTime = currentUnixTime + ageUpdateInterval;

                if (initialAgeTime == DateTime.MinValue)
                {
                    initialAge = Age ?? 1;
                    initialAgeTime = DateTime.UtcNow;
                }

                Age = initialAge + (int)(DateTime.UtcNow - initialAgeTime).TotalSeconds;

                // In retail, this is sent every 7 seconds. If you adjust ageUpdateInterval from 7, you'll need to re-add logic to send this every 7s (if you want to match retail)
                Session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt(this, PropertyInt.Age, Age ?? 1));
            }

            if (FellowVitalUpdate && Fellowship != null)
            {
                Fellowship.OnVitalUpdate(this);
                FellowVitalUpdate = false;
            }

            if (House != null && PropertyManager.GetBool("house_rent_enabled").Item)
            {
                if (houseRentWarnTimestamp > 0 && currentUnixTime > houseRentWarnTimestamp)
                {
                    HouseManager.GetHouse(House.Guid.Full, (house) =>
                    {
                        if (house != null && house.HouseStatus == HouseStatus.Active && !house.SlumLord.IsRentPaid())
                            Session.Network.EnqueueSend(new GameMessageSystemChat($"Warning!  You have not paid your maintenance costs for the last {(house.IsApartment ? "90" : "30")} day maintenance period.  Please pay these costs by this deadline or you will lose your house, and all your items within it.", ChatMessageType.Broadcast));
                    });

                    houseRentWarnTimestamp = Time.GetFutureUnixTime(houseRentWarnInterval);
                }
                else if (houseRentWarnTimestamp == 0)
                    houseRentWarnTimestamp = Time.GetFutureUnixTime(houseRentWarnInterval);
            }

            // Vitae decays over real time only under the CustomDM ruleset. Other rulesets
            // (e.g. Infiltration) burn vitae solely through earned XP, matching retail/Doctide.
            if (Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.CustomDM && currentUnixTime > vitaeTickTimestamp)
            {
                var vitae = EnchantmentManager.GetVitae();

                if (vitae != null)
                {
                    ReduceVitae(1);
                    VitaeDecayTimestamp = currentUnixTime;
                }

                vitaeTickTimestamp = Time.GetFutureUnixTime(vitaeTickInterval);
            }

            if (enchantmentTickTimestamp == 0 || currentUnixTime > enchantmentTickTimestamp)
            {
                if (EnchantmentManager.HasEnchantments)
                    EnchantmentManager.HeartBeat(enchantmentTickInterval, false);
                enchantmentTickTimestamp = Time.GetFutureUnixTime(enchantmentTickInterval);
            }

            // Bounty and Arena Observer are not CustomDM-specific; they must run under all rule sets.
            BountyTick();

            if (IsArenaObserver && !ArenaLocation.IsArenaLandblock(Location?.Landblock ?? 0))
                ArenaManager.ExitArenaObserverMode(this);

            if (Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.CustomDM)
            {
                if (leyLineAmuletsTickTimestamp == 0 || currentUnixTime > leyLineAmuletsTickTimestamp)
                {
                    LeyLineAmuletsTick(currentUnixTime);

                    leyLineAmuletsTickTimestamp = Time.GetFutureUnixTime(leyLineAmuletsTickInterval);
                }

                DoTHotTick(currentUnixTime);

                if (PvPInciteTickTimestamp == 0)
                    PvPInciteTickTimestamp = Time.GetFutureUnixTime(PropertyManager.GetLong("bz_whispers_login_delay").Item);
                else if (currentUnixTime > PvPInciteTickTimestamp)
                {
                    PvPInciteTick(currentUnixTime);
                    PvPInciteTickTimestamp = Time.GetFutureUnixTime(PropertyManager.GetLong("bz_whispers_interval").Item);
                }

                if (RoadCheckTimestamp == 0 || currentUnixTime > RoadCheckTimestamp)
                {
                    if (!IsLoggingOut && !Indoors && CurrentLandblock != null && CurrentLandblock.PhysicsLandblock.OnRoad(Location.Pos))
                    {
                        // We require 2 ticks before activating the buff as a way to minimize activations while just crossing the road
                        // as that will make the player momentarily pause their movement which can be annoying if you're not following the road.
                        if (OnRoadStatus == 1)
                        {
                            Session.Network.EnqueueSend(new GameMessageSystemChat("Your run speed increases due to being on a road.", ChatMessageType.Broadcast));
                            GrantRoadSpeedBuff();
                        }
                        else if (OnRoadStatus == 0)
                            OnRoadStatus = 1;
                    }
                    else if (OnRoadStatus != 0)
                    {
                        if (OnRoadStatus == 2)
                        {
                            Session.Network.EnqueueSend(new GameMessageSystemChat("Your run speed returns to normal as you are no longer on a road.", ChatMessageType.Broadcast));
                            RemoveRoadSpeedBuff();
                        }
                        OnRoadStatus = 0;
                    }
                    RoadCheckTimestamp = Time.GetFutureUnixTime(RoadCheckInterval);
                }
            }
        }

        private static readonly TimeSpan MaximumTeleportTime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Called every ~5 seconds for Players
        /// </summary>
        public override void Heartbeat(double currentUnixTime)
        {
            NotifyLandblocks();

            ManaConsumersTick();

            HandleTargetVitals();

            LifestoneProtectionTick();

            PK_DeathTick();

            GagsTick();

            PhysicsObj.ObjMaint.DestroyObjects();

            // Check if we're due for our periodic SavePlayer
            if (LastRequestedDatabaseSave == DateTime.MinValue)
                LastRequestedDatabaseSave = DateTime.UtcNow;

            if (LastRequestedDatabaseSave.AddSeconds(PlayerSaveIntervalSecs) <= DateTime.UtcNow)
                SavePlayerToDatabase();

            if (Teleporting && DateTime.UtcNow > Time.GetDateTimeFromTimestamp(LastTeleportStartTimestamp ?? 0).Add(MaximumTeleportTime))
            {
                if (Session != null)
                    Session.LogOffPlayer(true);
                else
                    LogOut();
            }

            bool wasSpeedEnforcing   = EnforceMovementSpeed; // capture BEFORE config reload
            bool wasAlreadyEnforcing = EnforceMovement || EnforceMovementSpeed;
            EnforceMovement = PropertyManager.GetBool("enforce_player_movement").Item;
            EnforceMovementSpeed = PropertyManager.GetBool("enforce_player_movement_speed").Item;
            if ((EnforceMovement || EnforceMovementSpeed) && !Teleporting)
            {
                // Re-init when:
                //   (a) enforcement is enabled for the first time, OR
                //   (b) speed checking is newly toggled on even if enforce_player_movement
                //       was already active.  SnapPos, MovementWindowBuffer, and
                //       LastPlayerMovementCheckTime are maintained ONLY inside the
                //       EnforceMovementSpeed block; if speed was off they are stale.
                bool speedJustEnabled = EnforceMovementSpeed && !wasSpeedEnforcing;
                if (!wasAlreadyEnforcing || speedJustEnabled)
                {
                    MovementEnforcementCounter = 0;
                    MovementSuspicionScore = 0.0f;
                    _suspicionScoreAtLastHeartbeat = 0.0f;
                    _cleanHeartbeatStreak = 0;
                    _lastAvgViolationTime = 0.0;
                    RubberBandRecoveryUntil = 0.0;
                    MovementWindowBuffer.Clear();
                    ReversalEventTimes.Clear();
                    WasJumping = false;
                    JumpStartZ = 0f;
                    JumpPeakZ = 0f;
                    Location = PhysicsObj.Position.ACEPosition();
                    SnapPos = Location;
                    PrevMovementUpdateMaxSpeed = 0.0f;
                    LastPlayerMovementCheckTime = currentUnixTime;
                    HasPerformedActionsSinceLastMovementUpdate = false;
                }

                // Decay the suspicion score during clean movement so occasional false positives
                // (glitch-running speed spikes, transient packet-rate bursts) fade instead of
                // ratcheting permanently toward a kick.  This runs regardless of
                // MovementEnforcementCounter: the heuristic script checks (packet_rate, timing,
                // reversal, avg) add to the score but never touch the counter, so the old
                // counter-gated decay never ran for those at all — a player who only tripped
                // heuristic checks accumulated a score that never came back down.
                //
                // "No violation this interval" is detected by comparing the live score against the
                // snapshot from the previous heartbeat: a genuine cheater keeps the score rising and
                // never benefits, while a one-off false positive clears within a heartbeat or two.
                // Average-speed violations fire only once per window (3 s / 15 s), far less often than
                // the 5 s heartbeat, so plain decay between fires cancels a moderate quickness hack's
                // gains and lets it run for minutes.  Suppress decay for a grace period after any
                // average-speed violation: a SUSTAINED cheat (fires every window) never gets a clean
                // grace gap and climbs monotonically, while a one-off false positive resumes decaying
                // once the grace lapses.
                bool _avgViolationActive = _lastAvgViolationTime > 0.0
                    && currentUnixTime - _lastAvgViolationTime < 20.0;

                if (MovementSuspicionScore > 0.0f && !_avgViolationActive)
                {
                    if (MovementSuspicionScore <= _suspicionScoreAtLastHeartbeat)
                    {
                        _cleanHeartbeatStreak++;
                        // Bleed faster the longer the player stays clean: -3/heartbeat, rising to
                        // -6 after ~3 clean heartbeats (~15 s).
                        float decay = _cleanHeartbeatStreak >= 3 ? 6.0f : 3.0f;
                        MovementSuspicionScore = Math.Max(0.0f, MovementSuspicionScore - decay);
                    }
                    else
                    {
                        _cleanHeartbeatStreak = 0; // score rose → a violation occurred this interval
                    }
                }
                _suspicionScoreAtLastHeartbeat = MovementSuspicionScore;

                if (MovementEnforcementCounter > 0)
                    MovementEnforcementCounter--;

                if (!HasAnyMovement() && currentUnixTime > LastPlayerMovementCheckTime + 5)
                {
                    LastPlayerMovementCheckTime = currentUnixTime;
                    PrevMovementUpdateMaxSpeed = 0.0f;
                }
            }

            base.Heartbeat(currentUnixTime);
        }

        public static float MaxSpeed = 50;
        public static float MaxSpeedSq = MaxSpeed * MaxSpeed;

        public static bool DebugPlayerMoveToStatePhysics { get; set; } = false;

        /// <summary>
        /// Flag indicates if player is doing full physics simulation
        /// </summary>
        //public bool FastTick => IsPKType;
        public bool FastTick => true;

        public bool EnforceMovement { get; set; } = false;
        public bool EnforceMovementSpeed { get; set; } = false;

        /// <summary>
        /// For advanced spellcasting / players glitching around during powersliding,
        /// the reason for this retail bug is from 2 different functions for player movement
        /// 
        /// The client's self-player uses DoMotion/StopMotion
        /// The server and other players on the client use apply_raw_movement
        ///
        /// When a 3+ button powerslide is performed, this bugs out apply_raw_movement,
        /// and causes the player to spin in place. With DoMotion/StopMotion, it performs a powerslide.
        ///
        /// With this option enabled (retail defaults to false), the player's position on the server
        /// will match up closely with the player's client during powerslides.
        ///
        /// Since the client uses apply_raw_movement to simulate the movement of nearby players,
        /// the other players will still glitch around on screen, even with this option enabled.
        ///
        /// If you wish for the positions of other players to be less glitchy, the 'MoveToState_UpdatePosition_Threshold'
        /// can be lowered to achieve that
        /// </summary>

        public void OnMoveToState(MoveToState moveToState)
        {
            HasPerformedActionsSinceLastMovementUpdate = true;

            LastMoveToStateWasRun = CheckIsRunning();
            IsFirstAutoPosPacketSinceMoveToState = true;
            //Session.Network.EnqueueSend(new GameMessageSystemChat($"moveToState - Running: {LastMoveToStateWasRun}", ChatMessageType.Broadcast));

            if (!FastTick)
                return;

            if (DebugPlayerMoveToStatePhysics)
                Console.WriteLine(moveToState.RawMotionState);

            if (RecordCast.Enabled)
                RecordCast.OnMoveToState(moveToState);

            if (!PhysicsObj.IsMovingOrAnimating)
                PhysicsObj.UpdateTime = PhysicsTimer.CurrentTime;

            if (!PropertyManager.GetBool("client_movement_formula").Item || moveToState.StandingLongJump)
                OnMoveToState_ServerMethod(moveToState);
            else
                OnMoveToState_ClientMethod(moveToState);

            if (MagicState.IsCasting && MagicState.PendingTurnRelease && moveToState.RawMotionState.TurnCommand == 0)
                OnTurnRelease();
        }

        public void OnMoveToState_ClientMethod(MoveToState moveToState)
        {
            var rawState = moveToState.RawMotionState;
            var prevState = LastMoveToState?.RawMotionState ?? RawMotionState.None;

            var mvp = new Physics.Animation.MovementParameters();
            mvp.HoldKeyToApply = rawState.CurrentHoldKey;

            if (!PhysicsObj.IsMovingOrAnimating)
                PhysicsObj.UpdateTime = PhysicsTimer.CurrentTime;

            // ForwardCommand
            if (rawState.ForwardCommand != MotionCommand.Invalid)
            {
                // press new key
                if (prevState.ForwardCommand == MotionCommand.Invalid)
                {
                    PhysicsObj.DoMotion((uint)MotionCommand.Ready, mvp);
                    PhysicsObj.DoMotion((uint)rawState.ForwardCommand, mvp);
                }
                // press alternate key
                else if (prevState.ForwardCommand != rawState.ForwardCommand)
                {
                    PhysicsObj.DoMotion((uint)rawState.ForwardCommand, mvp);
                }
            }
            else if (prevState.ForwardCommand != MotionCommand.Invalid)
            {
                // release key
                PhysicsObj.StopMotion((uint)prevState.ForwardCommand, mvp, true);
            }

            // StrafeCommand
            if (rawState.SidestepCommand != MotionCommand.Invalid)
            {
                // press new key
                if (prevState.SidestepCommand == MotionCommand.Invalid)
                {
                    PhysicsObj.DoMotion((uint)rawState.SidestepCommand, mvp);
                }
                // press alternate key
                else if (prevState.SidestepCommand != rawState.SidestepCommand)
                {
                    PhysicsObj.DoMotion((uint)rawState.SidestepCommand, mvp);
                }
            }
            else if (prevState.SidestepCommand != MotionCommand.Invalid)
            {
                // release key
                PhysicsObj.StopMotion((uint)prevState.SidestepCommand, mvp, true);
            }

            // TurnCommand
            if (rawState.TurnCommand != MotionCommand.Invalid)
            {
                // press new key
                if (prevState.TurnCommand == MotionCommand.Invalid)
                {
                    PhysicsObj.DoMotion((uint)rawState.TurnCommand, mvp);
                }
                // press alternate key
                else if (prevState.TurnCommand != rawState.TurnCommand)
                {
                    PhysicsObj.DoMotion((uint)rawState.TurnCommand, mvp);
                }
            }
            else if (prevState.TurnCommand != MotionCommand.Invalid)
            {
                // release key
                PhysicsObj.StopMotion((uint)prevState.TurnCommand, mvp, true);
            }
        }

        public void OnMoveToState_ServerMethod(MoveToState moveToState)
        {
            var minterp = PhysicsObj.get_minterp();
            minterp.RawState.SetState(moveToState.RawMotionState);

            if (moveToState.StandingLongJump)
            {
                minterp.RawState.ForwardCommand = (uint)MotionCommand.Ready;
                minterp.RawState.SideStepCommand = 0;
            }

            var allowJump = minterp.motion_allows_jump(minterp.InterpretedState.ForwardCommand) == WeenieError.None;

            //PhysicsObj.cancel_moveto();

            minterp.apply_raw_movement(true, allowJump);
        }

        public bool InUpdate;

        public override bool UpdateObjectPhysics()
        {
            try
            {
                stopwatch.Restart();

                bool landblockUpdate = false;

                InUpdate = true;

                // update position through physics engine
                if (RequestedLocation != null)
                {
                    landblockUpdate = UpdatePlayerPosition(RequestedLocation);
                    RequestedLocation = null;
                }

                if (FastTick && PhysicsObj.IsMovingOrAnimating || PhysicsObj.Velocity != Vector3.Zero)
                {
                    UpdatePlayerPhysics();

                    if (MoveToParams?.Callback != null && !PhysicsObj.IsMovingOrAnimating)
                        HandleMoveToCallback();
                }

                InUpdate = false;

                return landblockUpdate;
            }
            finally
            {
                var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Player_Tick_UpdateObjectPhysics, elapsedSeconds);
                if (elapsedSeconds >= 1) // Yea, that ain't good....
                    log.Warn($"[PERFORMANCE][PHYSICS] {Guid}:{Name} took {(elapsedSeconds * 1000):N1} ms to process UpdateObjectPhysics() at loc: {Location}");
                else if (elapsedSeconds >= 0.010)
                    log.DebugFormat("[PERFORMANCE][PHYSICS] {0}:{1} took {2:N1} ms to process UpdateObjectPhysics() at loc: {3}", Guid, Name, (elapsedSeconds * 1000), Location);
            }
        }

        public void UpdatePlayerPhysics()
        {
            if (DebugPlayerMoveToStatePhysics)
                Console.WriteLine($"{Name}.UpdatePlayerPhysics({PhysicsObj.PartArray.Sequence.CurrAnim.Value.Anim.ID:X8})");

            //Console.WriteLine($"{PhysicsObj.Position.Frame.Origin}");
            //Console.WriteLine($"{PhysicsObj.Position.Frame.get_heading()}");

            PhysicsObj.update_object();

            // sync ace position?
            Location.Rotation = PhysicsObj.Position.Frame.Orientation;

            if (!FastTick) return;

            // ensure PKLogout position is synced up for other players
            if (PKLogout)
            {
                EnqueueBroadcast(new GameMessageUpdateMotion(this, new Motion(MotionStance.NonCombat, MotionCommand.Ready)));
                PhysicsObj.StopCompletely(true);

                if (!PhysicsObj.IsMovingOrAnimating)
                {
                    SyncLocation();
                    EnqueueBroadcast(new GameMessageUpdatePosition(this));
                }
            }

            // this fixes some differences between client movement (DoMotion/StopMotion) and server movement (apply_raw_movement)
            //
            // scenario: start casting a self-spell, and then immediately start holding the run forward key during the windup
            // on client: player will start running forward after the cast has completed
            // on server: player will stand still

            // this block of code can improve the sync between these 2 methods,
            // however there are some bugs that originate in acclient that cannot be resolved on the server
            // for example, equip a wand, and then start running forward in non-combat mode. switch to magic combat mode, and then release forward during the stance swap
            // the client will never send a 'client released forward' MoveToState in this scenario unfortunately.
            // because of this, it's better to have the 'client blip forward' bug without it, than to have the client invisibly running forward on the server.
            // commenting out this block because of this...

            /*if (!PhysicsObj.IsMovingOrAnimating && LastMoveToState != null)
            {
                // apply latest MoveToState, if applicable
                //if ((LastMoveToState.RawMotionState.Flags & (RawMotionFlags.ForwardCommand | RawMotionFlags.SideStepCommand | RawMotionFlags.TurnCommand)) != 0)
                if ((LastMoveToState.RawMotionState.Flags & RawMotionFlags.ForwardCommand) != 0 && LastMoveToState.RawMotionState.ForwardHoldKey == HoldKey.Invalid)
                {
                    if (DebugPlayerMoveToStatePhysics)
                        Console.WriteLine("Re-applying movement: " + LastMoveToState.RawMotionState.Flags);

                    OnMoveToState(LastMoveToState);

                    // re-broadcast MoveToState to other clients only
                    EnqueueBroadcast(false, new GameMessageUpdateMotion(this, CurrentMovementData));
                }
                LastMoveToState = null;
            }*/

            if (MagicState.IsCasting && MagicState.PendingTurnRelease)
                CheckTurn();
        }

        /// <summary>
        /// The maximum rate UpdatePosition packets from MoveToState will be broadcast for each player
        /// AutonomousPosition still always broadcasts UpdatePosition
        ///  
        /// The default value (1 second) was estimated from this retail video:
        /// https://youtu.be/o5lp7hWhtWQ?t=112
        /// 
        /// If you wish for players to glitch around less during powerslides, lower this value
        ///
        /// MISSILE FIX 5: this also governs how far the server's authoritative position for a moving
        /// player can drift from what other clients are showing. Between broadcasts, each client
        /// dead-reckons other players from their MoveToState, so a missile aimed at the server position
        /// can visibly fly at empty space on the shooter's screen even when the intercept math was
        /// self-consistent. Now driven by the 'player_update_position_threshold' server property so it
        /// can be tuned live (0.2 - 0.33 tightens pvp sync, at a bandwidth cost). Default stays 1.0.
        /// </summary>
        public static TimeSpan MoveToState_UpdatePosition_Threshold => TimeSpan.FromSeconds(PropertyManager.GetDouble("player_update_position_threshold").Item);

        bool LastMoveToStateWasRun = false;
        bool IsFirstAutoPosPacketSinceMoveToState = false;

        public bool CheckIsRunning()
        {
            var minterp = PhysicsObj.get_minterp();
            var isRunning = minterp.RawState.CurrentHoldKey == HoldKey.Run;
            var isSideStepping = minterp.RawState.SideStepCommand != (uint)MotionCommand.Invalid;
            if (isSideStepping)
            {
                // We're dealing with a lot of inconsistencies here.
                var interpretedMotionState = CurrentMotionState.MotionState;
                isRunning = (interpretedMotionState.ForwardCommand == MotionCommand.RunForward && minterp.RawState.ForwardCommand != (uint)MotionCommand.WalkBackwards)
                    || (interpretedMotionState.ForwardCommand == MotionCommand.Invalid && minterp.RawState.SideStepCommand != (uint)MotionCommand.Invalid && minterp.RawState.CurrentHoldKey == HoldKey.Run)
                    || (interpretedMotionState.ForwardCommand == MotionCommand.WalkForward && interpretedMotionState.SidestepCommand == MotionCommand.Invalid && !(minterp.RawState.CurrentHoldKey == HoldKey.Invalid && (minterp.RawState.ForwardCommand == (uint)MotionCommand.WalkForward || minterp.RawState.ForwardCommand == (uint)MotionCommand.WalkBackwards)))
                    || (interpretedMotionState.ForwardCommand == MotionCommand.WalkForward && (interpretedMotionState.SidestepCommand == MotionCommand.SideStepRight || interpretedMotionState.SidestepCommand == MotionCommand.SideStepLeft) && minterp.RawState.CurrentHoldKey == HoldKey.Run);
            }

            return isRunning;
        }

        /// <summary>
        /// Used by physics engine to actually update a player position
        /// Automatically notifies clients of updated position
        /// </summary>
        /// <param name="newPosition">The new position being requested, before verification through physics engine</param>
        /// <returns>TRUE if object moves to a different landblock</returns>
        public bool UpdatePlayerPosition(ACE.Entity.Position newPosition, bool forceUpdate = false)
        {
            var currentTime = Time.GetUnixTime();
            float deltaTime = (float)(currentTime - LastPlayerAutoposTime);
            LastPlayerAutoposTime = currentTime;

            //Console.WriteLine($"{Name}.UpdatePlayerPhysics({newPosition}, {forceUpdate}, {Teleporting})");
            bool verifyContact = false;

            // possible bug: while teleporting, client can still send AutoPos packets from old landblock
            if (Teleporting && !forceUpdate) return false;

            // pre-validate movement
            if (!ValidateMovement(newPosition))
            {
                log.Error($"{Name}.UpdatePlayerPosition() - movement pre-validation failed from {Location} to {newPosition}");
                return false;
            }

            try
            {
                if (!forceUpdate) // This is needed beacuse this function might be called recursively
                    stopwatch.Restart();

                var success = true;

                if (PhysicsObj != null)
                {
                    var distSq = Location.SquaredDistanceTo(newPosition);

                    if (distSq > PhysicsGlobals.EpsilonSq)
                    {
                        /*var p = new Physics.Common.Position(newPosition);
                        var dist = PhysicsObj.Position.Distance(p);
                        Console.WriteLine($"Dist: {dist}");*/

                        if (newPosition.Landblock == 0x18A && Location.Landblock != 0x18A)
                            log.Info($"{Name} is getting swanky");

                        if (!Teleporting)
                        {
                            var blockDist = PhysicsObj.GetBlockDist(Location.Cell, newPosition.Cell);

                            // verify movement
                            if (distSq > MaxSpeedSq && blockDist > 1)
                            {
                                //Session.Network.EnqueueSend(new GameMessageSystemChat("Movement error", ChatMessageType.Broadcast));
                                log.Warn($"MOVEMENT SPEED: {Name} trying to move from {Location} to {newPosition}, speed: {Math.Sqrt(distSq)}");
                                return false;
                            }

                            // verify z-pos
                            if (blockDist == 0 && LastGroundPos != null && newPosition.PositionZ - LastGroundPos.PositionZ > 10 && DateTime.UtcNow - LastJumpTime > TimeSpan.FromSeconds(1) && GetCreatureSkill(Skill.Jump).Current < 1000)
                                verifyContact = true;
                        }

                        var curCell = LScape.get_landcell(newPosition.Cell);
                        if (curCell != null)
                        {
                            //if (PhysicsObj.CurCell == null || curCell.ID != PhysicsObj.CurCell.ID)
                            //PhysicsObj.change_cell_server(curCell);

                            PhysicsObj.set_request_pos(newPosition.Pos, newPosition.Rotation, curCell, Location.LandblockId.Raw);

                            if (!Teleporting)
                            {
                                // The client does not seem to send any packets when walk/run is toggled by hitting shift, so unless the player does something else(like turning) we won't find about it.
                                // To reduce the delay we check the distance the player requested on this packet and toggle walk/run ourselves, this still has a delay compared to the client.
                                var minterp = PhysicsObj.get_minterp();
                                if (!IsJumping && !PhysicsObj.TransientState.HasFlag(TransientStateFlags.Sliding) && !IsFirstAutoPosPacketSinceMoveToState && (minterp.RawState.ForwardCommand != (uint)MotionCommand.Ready || minterp.RawState.SideStepCommand != (uint)MotionCommand.Invalid))
                                {
                                    var isRunning = CheckIsRunning();
                                    var isForward = minterp.RawState.ForwardCommand != (uint)MotionCommand.WalkBackwards;
                                    var hasForwardOrBackwardsMovement = minterp.RawState.ForwardCommand != (uint)MotionCommand.Ready;
                                    var isSideStepping = minterp.RawState.SideStepCommand != (uint)MotionCommand.Invalid;
                                    var myRunRate = GetRunRate();

                                    var curPos = Location.PhysPosition();
                                    var reqPos = RequestedLocation.PhysPosition();
                                    var realDist = curPos.Distance(reqPos);

                                    float runRate;
                                    float walkRate;
                                    if (isForward)
                                    {
                                        runRate = myRunRate;
                                        walkRate = 1.0f;
                                    }
                                    else
                                    {
                                        runRate = -0.65f * myRunRate * 0.65f;
                                        walkRate = -0.65f * 0.65f;
                                    }

                                    if (isSideStepping)
                                    {
                                        if (hasForwardOrBackwardsMovement)
                                        {
                                            runRate *= 3.12f / 1.25f * 0.5f;
                                            walkRate *= 3.12f / 1.25f * 0.5f;
                                        }
                                        else
                                        {
                                            var multiplier = 1.0f;
                                            if (minterp.RawState.SideStepCommand == (uint)MotionCommand.SideStepLeft)
                                                multiplier = -1.0f;

                                            runRate = multiplier * 0.65f * myRunRate * 0.65f;
                                            walkRate = multiplier * 0.65f * 0.65f;
                                        }
                                    }

                                    var heading = curPos.Frame.get_vector_heading();
                                    var testRunPoint = curPos.Frame.Origin + (heading * (runRate * 4.0f * deltaTime));
                                    var testRunDist = (curPos.Frame.Origin - testRunPoint).Length();

                                    var testWalkPoint = curPos.Frame.Origin + (heading * (walkRate * 4.0f * deltaTime));
                                    var testWalkDist = (curPos.Frame.Origin - testWalkPoint).Length();

                                    var shouldBeWalking = false;
                                    var runDistDelta = Math.Abs(testRunDist - realDist);
                                    var walkDistDelta = Math.Abs(testWalkDist - realDist);
                                    if (LastMoveToStateWasRun)
                                        runDistDelta -= 0.25f * deltaTime;
                                    else
                                        walkDistDelta -= 0.25f * deltaTime;

                                    if (runDistDelta > walkDistDelta)
                                        shouldBeWalking = true;

                                    //Session.Network.EnqueueSend(new GameMessageSystemChat($"isRunning: {isRunning} wasRunning: {LastMoveToStateWasRun}", ChatMessageType.Broadcast));
                                    //Session.Network.EnqueueSend(new GameMessageSystemChat($"{realDist.ToString("0.00")} {testRunDist.ToString("0.00")} {testWalkDist.ToString("0.00")}", ChatMessageType.Broadcast));

                                    var toggledRunWalkState = false;
                                    if (isRunning && shouldBeWalking)
                                    {
                                        toggledRunWalkState = true;

                                        minterp.RawState.CurrentHoldKey = HoldKey.Invalid;
                                        CurrentMoveToState.RawMotionState.CurrentHoldKey = HoldKey.Invalid;
                                        CurrentMoveToState.RawMotionState.Flags &= ~RawMotionFlags.CurrentHoldKey;

                                        if (isSideStepping && (hasForwardOrBackwardsMovement && !isForward))
                                            CurrentMoveToState.RawMotionState.Flags &= ~RawMotionFlags.SideStepCommand;

                                        //Session.Network.EnqueueSend(new GameMessageSystemChat($"{realDist.ToString("0.00")} {testRunDist.ToString("0.00")} {testWalkDist.ToString("0.00")}", ChatMessageType.Broadcast));
                                        //Session.Network.EnqueueSend(new GameMessageSystemChat("Switch to walk", ChatMessageType.Broadcast));
                                    }
                                    else if (!isRunning && !shouldBeWalking)
                                    {
                                        toggledRunWalkState = true;

                                        minterp.RawState.CurrentHoldKey = HoldKey.Run;
                                        CurrentMoveToState.RawMotionState.CurrentHoldKey = HoldKey.Run;
                                        CurrentMoveToState.RawMotionState.Flags |= RawMotionFlags.CurrentHoldKey;

                                        if (isSideStepping && (hasForwardOrBackwardsMovement && !isForward))
                                            CurrentMoveToState.RawMotionState.Flags |= RawMotionFlags.SideStepCommand;

                                        //Session.Network.EnqueueSend(new GameMessageSystemChat($"{realDist.ToString("0.00")} {testRunDist.ToString("0.00")} {testWalkDist.ToString("0.00")}", ChatMessageType.Broadcast));
                                        //Session.Network.EnqueueSend(new GameMessageSystemChat("Switch to run", ChatMessageType.Broadcast));
                                    }

                                    if (toggledRunWalkState)
                                    {
                                        var allowJump = minterp.motion_allows_jump(minterp.InterpretedState.ForwardCommand) == WeenieError.None;
                                        minterp.apply_raw_movement(true, allowJump);

                                        BroadcastMovement(CurrentMoveToState);
                                    }
                                }
                                else
                                {
                                    IsFirstAutoPosPacketSinceMoveToState = false;
                                }
                            }

                            if (FastTick)
                                success = PhysicsObj.update_object_server_new(!EnforceMovement) ;
                            else
                                success = PhysicsObj.update_object_server();

                            if (PhysicsObj.CurCell == null && curCell.ID >> 16 != 0x18A)
                            {
                                PhysicsObj.CurCell = curCell;
                            }

                            if (verifyContact && IsJumping)
                            {
                                var blockDist = PhysicsObj.GetBlockDist(newPosition.Cell, LastGroundPos.Cell);

                                if (blockDist <= 1)
                                {
                                    log.Warn($"z-pos hacking detected for {Name}, lastGroundPos: {LastGroundPos.ToLOCString()} - requestPos: {newPosition.ToLOCString()}");
                                    Location = new ACE.Entity.Position(LastGroundPos);
                                    Sequences.GetNextSequence(SequenceType.ObjectForcePosition);
                                    SendUpdatePosition();
                                    return false;
                                }
                            }

                            CheckMonsters();
                        }
                    }
                    else
                        PhysicsObj.Position.Frame.Orientation = newPosition.Rotation;

                    if (EnforceMovementSpeed && success && !Teleporting && GodState == null)
                    {
                        // --- Local helper: correct rubber-band that also keeps physics in sync ---
                        // Every rubber-band inside this block must use this instead of the bare
                        // Location=/Sequences/SendUpdatePosition triple.
                        //
                        // WHY: physics accepted the move and called set_current_pos(newPosition)
                        // before we ever entered this block (success == true).  If we then set
                        // Location = SnapPos without reverting PhysicsObj.Position, the two are
                        // out of sync.  The next packet arrives; physics validates from newPosition
                        // toward the client's near-SnapPos position; that backwards step fails the
                        // physics 0.1-unit threshold; the engine fires its OWN rubber-band sending
                        // the player forward again.  The client bounces, each bounce can generate
                        // another violation, and suspicion accumulates until kick.
                        void RollbackToSnap(string violationInfo = null)
                        {
                            // Revert the physics engine's accepted position back to SnapPos.
                            var _rb = new Position(); // ACE.Server.Physics.Common.Position
                            _rb.ObjCellID             = SnapPos.Cell;
                            _rb.Frame.Origin          = SnapPos.Pos;
                            _rb.Frame.Orientation     = SnapPos.Rotation;
                            PhysicsObj.set_current_pos(_rb);

                            Location = new ACE.Entity.Position(SnapPos);
                            Sequences.GetNextSequence(SequenceType.ObjectForcePosition);
                            SendUpdatePosition();

                            if (violationInfo != null)
                                Session?.Network.EnqueueSend(new GameMessageSystemChat(
                                    $"[AntiCheat] {violationInfo}", ChatMessageType.Help));
                        }

                        float enforcementDeltaTime = (float)(currentTime - LastPlayerMovementCheckTime);
                        LastPlayerMovementCheckTime = currentTime;

                        // Check for illegal player movements.
                        var loggingHasPerformedActionsSinceLastMovementUpdate = HasPerformedActionsSinceLastMovementUpdate;
                        var loggingPrevMaxMovementSpeed = PrevMovementUpdateMaxSpeed;
                        var loggingInertia = false;

                        // Measure HORIZONTAL displacement only.  GetRunRate() governs horizontal
                        // ground speed; the full 3-D DistanceTo inflates the number on slopes by the
                        // vertical climb/descent, which previously forced large fudge factors that also
                        // loosened flat ground (where a client-side quickness hack is most visible).
                        // Checking the horizontal component lets flat ground stay tight while hills
                        // self-attenuate: the horizontal projection of run speed is runRate*cos(slope),
                        // always <= the flat budget, so climbing a hill can never exceed it.  Downhill
                        // and airborne momentum are still covered by the CachedVelocity term below,
                        // which is computed server-side by the physics engine and cannot be spoofed.
                        var dist = Location.Distance2D(newPosition);
                        float velocity = PhysicsObj.CachedVelocity.Length();
                        float currentMaxSpeed;

                        if (dist > PhysicsGlobals.EPSILON)
                        {
                            if (FastTick)
                            {
                                var runRate = GetRunRate();
                                // Clamp deltaTime to 200 ms so a lag spike that delivers a large position
                                // jump as a single packet doesn't get an artificially tiny budget.
                                var clampedDelta = Math.Min(enforcementDeltaTime, 0.2f);
                                // Scale the flat fudge bonus with clamped time so it stays proportional
                                // rather than dominating at normal (high-frequency) packet rates.
                                // Now that vertical terrain inflation is removed from dist, the floor is
                                // brought down to tighten flat-ground detection — but kept generous
                                // enough to absorb packet-timing jitter and brief legitimate catch-up.
                                // A legit runner covers <1 unit in a typical sub-100 ms packet, so 3.5
                                // is still several times the honest per-packet distance: wiggle room,
                                // not a tripwire.
                                var flatBonus = Math.Max(3.5f, clampedDelta * 30.0f);
                                // Physics-velocity consistency allowance:
                                // The AC client stops sending AutoPos packets for ~1 second after it
                                // receives a ForcePosition (rubber-band) correction.  When it resumes,
                                // the first packet covers the full elapsed distance at the player's
                                // normal running speed (typically 10-14 units/s × ~1 s = 10-14 units).
                                // The 200 ms clampedDelta cap only budgets ~8 units for that packet,
                                // causing a false violation that triggers another rubber-band, which
                                // causes another pause, and the cycle repeats indefinitely.
                                //
                                // Fix: when enforcementDeltaTime > 200 ms, raise the flat bonus so
                                // that movement whose distance is consistent with the player's current
                                // physics velocity over the full elapsed time is always allowed.
                                // CachedVelocity is computed server-side by the physics engine and
                                // cannot be injected by the client, so this cannot be spoofed.
                                if (velocity > 0.5f && enforcementDeltaTime > 0.2f)
                                    flatBonus = Math.Max(flatBonus, velocity * enforcementDeltaTime * 1.2f);
                                // Cap velocity at a physically plausible ceiling so injected velocity
                                // values can't inflate the allowed movement budget.
                                var clampedVelocity = Math.Min(velocity, runRate * 12.0f);
                                currentMaxSpeed = (1.8f * runRate * clampedDelta * (1.0f + clampedVelocity / 8.0f)) + flatBonus;
                                if (runRate < 1.9f && PhysicsObj.CachedVelocity.Z < -20.0f) // Very slow characters can still fall pretty quickly.
                                    currentMaxSpeed *= 2.5f;
                                // Casting or attacking while moving causes the client to send burst
                                // position updates that overshoot the normal speed budget: the character
                                // pivots toward a target, executes an attack animation, or channels a
                                // spell while still travelling forward.  The non-FastTick path applies a
                                // 1.8× action multiplier; replicate that here so combat doesn't generate
                                // false speed violations.
                                if (HasPerformedActionsSinceLastMovementUpdate)
                                    currentMaxSpeed *= 1.5f;
                            }
                            else
                            {
                                // This is no longer used because FastTick is set to be always on but leaving it here for now.
                                currentMaxSpeed = (5.5f * GetRunRate() * enforcementDeltaTime * (1.0f + velocity / 5.0f)) + 2.0f;

                                if (HasPerformedActionsSinceLastMovementUpdate)
                                    currentMaxSpeed *= 1.8f;
                            }

                            // --- Budget floor: full-rate coverage over the elapsed time ---
                            // enforcementDeltaTime spans any packets that failed physics validation
                            // (common on hills, where the slope solver stops short and zeroes the
                            // physics velocity, killing the velocity-rescue term above).  Guarantee
                            // the budget always covers what a legitimate runner could travel in the
                            // elapsed time (capped at 1.5 s so long pauses can't bank a teleport):
                            // true speed = 4.0 * runRate, 1.248x when strafing (the engine's own
                            // strafe-run multiplier), plus 25% headroom.  A cheater pacing packets
                            // to ride this floor still exceeds the average-speed windows, which
                            // integrate the same per-segment allowance without the headroom.
                            var sideStepping = PhysicsObj.get_minterp()?.RawState.SideStepCommand != (uint)MotionCommand.Invalid;
                            var effectiveRate = GetRunRate() * (sideStepping ? 1.248f : 1.0f);
                            var floorBudget = 4.0f * effectiveRate * Math.Min(enforcementDeltaTime, 1.5f) * 1.25f + 2.0f;
                            currentMaxSpeed = Math.Max(currentMaxSpeed, floorBudget);

                            if (currentMaxSpeed < PrevMovementUpdateMaxSpeed && PrevMovementUpdateMaxSpeed > 25.0f)
                            {
                                // We were going really fast and now we are slowing down but we might still have some inertia.
                                loggingInertia = true;
                                currentMaxSpeed = PrevMovementUpdateMaxSpeed * 0.5f;
                            }
                            PrevMovementUpdateMaxSpeed = currentMaxSpeed;

                            if (dist > currentMaxSpeed && !IsMeleeStickyChasing())
                            {
                                // Recovery guard: if we just rubber-banded, the client hasn't acknowledged
                                // the correction yet and is still sending packets from its old position.
                                // Suppress scoring (but still apply the rubber-band correction) until the
                                // client has had 750 ms to catch up. Without this, 4 recovery packets × +15
                                // score = instant false kick for any legitimate lag spike.
                                if (currentTime < RubberBandRecoveryUntil)
                                {
                                    RollbackToSnap(); // silent — already alerted on the triggering event
                                    return false;
                                }

                                // Suspicion gain is proportional to how far over the limit the player moved.
                                // Capped at 15 per event so a single extreme packet can't instantly reach 50.
                                var overage = currentMaxSpeed > 0 ? (dist / currentMaxSpeed) - 1.0f : 1.0f;
                                var suspicionGain = Math.Min(overage * 10.0f, 15.0f);

                                if (currentMaxSpeed != 0 && dist < currentMaxSpeed * 1.3f)
                                {
                                    // Borderline overage (≤ 30%): typical during casting, charging, or
                                    // brief post-collision recovery.  Accept the position without a
                                    // rubber-band so the player isn't disrupted; score at half-weight so
                                    // a sustained borderline pattern still accumulates suspicion over time.
                                    MovementEnforcementCounter++;
                                    MovementSuspicionScore += suspicionGain * 0.5f;
                                }
                                else
                                {
                                    MovementEnforcementCounter++;
                                    MovementSuspicionScore += suspicionGain;

                                    // Stamp the recovery window so this rubber-band doesn't cascade into
                                    // multiple score events while the client catches up.
                                    RubberBandRecoveryUntil = currentTime + 0.75;

                                    RollbackToSnap($"speed_packet {dist:0.00}/{currentMaxSpeed:0.00}");

                                    log.Warn($"{Name} - INVALID MOVEMENT DETECTED - Speed: {dist.ToString("0.00")}/{currentMaxSpeed.ToString("0.00")} PrevMaxSpeed: {loggingPrevMaxMovementSpeed.ToString("0.00")}({loggingInertia}) FastTick: {FastTick} TimeSpam: {enforcementDeltaTime.ToString("0.00")} Velocity: {velocity.ToString("0.00")} actionsSinceLastMovementUpdate: {loggingHasPerformedActionsSinceLastMovementUpdate} SuspicionScore: {MovementSuspicionScore:0.0}");
                                    if (PropertyManager.GetBool("movement_debug_chat").Item)
                                        Session?.Network.EnqueueSend(new GameMessageSystemChat(
                                            $"[AC DBG] speed_packet {dist:0.00}/{currentMaxSpeed:0.00} | Δt={enforcementDeltaTime*1000:0}ms | v={velocity:0.0} | score={MovementSuspicionScore:0.0}",
                                            ChatMessageType.Help));

                                    // Log to ace_log and Discord webhook for long-term ban evidence (fire-and-forget).
                                    var _violationLocation = Location?.ToString() ?? "unknown";
                                    var _violationAccount  = Session?.Account ?? "unknown";
                                    var _capturedScore     = MovementSuspicionScore;
                                    var _capturedDist      = dist;
                                    var _capturedMax       = currentMaxSpeed;
                                    DiscordWebhookManager.SendMovementViolation("speed_packet", Name, _violationAccount, _capturedDist, _capturedMax, _capturedScore, _violationLocation);
                                    System.Threading.Tasks.Task.Run(() =>
                                        DatabaseManager.Log.LogMovementViolation(
                                            Guid.Full, Name, _violationAccount, "speed_packet",
                                            _capturedDist, _capturedMax, _capturedScore, _violationLocation));

                                    // Kick when suspicion score reaches 50 — sustained cheating pattern.
                                    // This fires regardless of the movement_violation_kick config.
                                    if (MovementSuspicionScore >= 50.0f)
                                    {
                                        log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                        Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                            new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                        return false;
                                    }

                                    // Also kick when counter hits 10 if the config flag is enabled.
                                    if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                                    {
                                        log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                        Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                            new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                        return false;
                                    }

                                    return false;
                                }
                            }
                            else if (dist > currentMaxSpeed)
                            {
                                // Melee sticky-chase grace: the player is over the per-packet speed
                                // budget but is actively chasing a live melee target (the branch above
                                // was skipped by !IsMeleeStickyChasing()).  The client's sticky auto-
                                // face/follow produces short, legitimate speed/position spikes — it
                                // snaps the character to face the target, and on hills the 3-D step
                                // distance inflates past the horizontal run budget.  Accept the
                                // position with NO rubber-band and NO score.  This is bounded (see
                                // IsMeleeStickyChasing): the geometry wall-walk check and the 15-second
                                // average-speed check still run, so a real hack is still caught.
                                if (PropertyManager.GetBool("movement_debug_chat").Item && currentTime - _lastDebugChatTime >= 1.0)
                                {
                                    _lastDebugChatTime = currentTime;
                                    Session?.Network.EnqueueSend(new GameMessageSystemChat(
                                        $"[AC DBG] melee-sticky grace {dist:0.00}/{currentMaxSpeed:0.00} | Δt={enforcementDeltaTime*1000:0}ms | v={velocity:0.0}",
                                        ChatMessageType.Help));
                                }
                            }
                            else if (PropertyManager.GetBool("movement_debug_chat").Item)
                            {
                                // Throttle to 1 message/sec so chat isn't completely unusable.
                                // Still shows which packets are near the limit.
                                if (currentTime - _lastDebugChatTime >= 1.0)
                                {
                                    _lastDebugChatTime = currentTime;
                                    Session?.Network.EnqueueSend(new GameMessageSystemChat(
                                        $"[AC DBG] ok {dist:0.00}/{currentMaxSpeed:0.00} | Δt={enforcementDeltaTime*1000:0}ms | v={velocity:0.0} | score={MovementSuspicionScore:0.0}",
                                        ChatMessageType.Help));
                                }
                            }
                        }
                        else if (PropertyManager.GetBool("movement_debug_chat").Item)
                        {
                            // dist <= EPSILON: no real movement this tick (rotation-only packet).
                            // Don't spam; already throttled in the branch above on moving ticks.
                        }

                        if (HasPerformedActionsSinceLastMovementUpdate && !IsJumping)
                            HasPerformedActionsSinceLastMovementUpdate = false; // Delay disabling this until we're done with the jump.

                        // Never advance SnapPos onto a tick where a hard barrier — a closed door or a
                        // wall — was clipped this transition.  The door and geometry checks run LATER in
                        // this block and roll back TO SnapPos; if SnapPos were allowed to advance onto the
                        // far side of a barrier first (e.g. the grounded landing packet of a jump through
                        // a client-side-deleted door), the rollback target would itself be past the
                        // barrier and the "rubber-band" would leave the player through it.  Freezing
                        // SnapPos here keeps the rollback anchored on the last barrier-free position.
                        bool _barrierClipThisTick = PhysicsObj.LastTransitionHitClosedDoor
                                                 || PhysicsObj.LastTransitionHitGeometry;

                        // Primary SnapPos advance: grounded and not jumping — most precise.
                        // Use newPosition (the packet we just accepted) rather than Location (the previous
                        // packet's position).  Location isn't updated to newPosition until line 1485, so using
                        // Location here would leave SnapPos one step behind, causing rollbacks to overshoot.
                        if (!_barrierClipThisTick && !IsJumping && PhysicsObj.TransientState.HasFlag(TransientStateFlags.OnWalkable))
                        {
                            SnapPos = new ACE.Entity.Position(newPosition);
                            LastSnapPosAdvanceTime = currentTime;
                        }
                        // Fallback SnapPos advance: player has had no violations and hasn't been updated
                        // in over 2 seconds (e.g. sliding, stair-climbing, brief air time on terrain).
                        // Keeps the rollback target fresh so any rubber-band is imperceptible.
                        else if (!_barrierClipThisTick && MovementEnforcementCounter == 0 && currentTime - LastSnapPosAdvanceTime > 2.0)
                        {
                            SnapPos = new ACE.Entity.Position(newPosition);
                            LastSnapPosAdvanceTime = currentTime;
                        }

                        // --- Position ring buffer maintenance ---
                        // Always maintained on clean ticks so that any check below that needs
                        // historical positions (speed windows, timing regularity, packet rate,
                        // direction reversal) works regardless of which individual configs are on.
                        //
                        // Discontinuity guard: Teleport() clears this buffer, but if a position jump
                        // reaches here through any path that bypassed Teleport(), a single cross-map
                        // pair would poison the window sums with hundreds of units of "travelled"
                        // distance and instantly fire both average-speed windows.  100 units (2-D) is
                        // far above any step the per-packet check can accept (even the velocity-
                        // consistent post-lag catch-up), so clearing here can never hide real movement.
                        if (MovementWindowBuffer.Count > 0 &&
                            MovementWindowBuffer[MovementWindowBuffer.Count - 1].Pos.Distance2D(newPosition) > 100.0f)
                            MovementWindowBuffer.Clear();

                        // Record the server-authoritative RAW run rate plus whether a sidestep command
                        // was active.  The average-speed windows apply the strafe allowance per segment
                        // (see EvalSpeedWindow): the 15 s window caps a strafe segment FLAT rather than
                        // stacking the 1.248x strafe multiplier on top of the ceiling, so a client that
                        // fakes the sidestep flag to run forward fast can't buy 1.248 x ceiling of
                        // sustained speed.  Rates are server-side and can't be spoofed.
                        var _bufSideStep = PhysicsObj.get_minterp()?.RawState.SideStepCommand != (uint)MotionCommand.Invalid;
                        MovementWindowBuffer.Add((currentTime, new ACE.Entity.Position(newPosition), GetRunRate(), _bufSideStep));
                        // Prune entries older than the longest window (15 s).
                        while (MovementWindowBuffer.Count > 0 && currentTime - MovementWindowBuffer[0].Timestamp > 15.0)
                            MovementWindowBuffer.RemoveAt(0);

                        // --- Sliding window average speed checks (Change 6) ---
                        // Catches cheaters who pace teleport packets to stay under the per-packet limit,
                        // and sustained client-side speed boosts (e.g. injected Quickness) too small to
                        // trip the per-packet budget.
                        if (PropertyManager.GetBool("enforce_player_movement_avg").Item)
                        {
                            // The window compares travelled horizontal distance against a
                            // TIME-INTEGRATED allowance: for each segment between consecutive
                            // samples, a legitimate player could cover at most
                            //   4.0 * effectiveRate * segmentTime
                            // (4.0 * runRate is the engine's speed conversion — its own walk/run
                            // prediction uses runRate * 4.0f * deltaTime — and effectiveRate folds
                            // in the 1.248x strafe-run multiplier for segments where the player was
                            // actually strafing).  This replaces the earlier flat max-rate ceiling:
                            // rate changes mid-window (buff expiry, road exit, exhaustion, toggling
                            // strafe) are each budgeted at their own rate for their own duration,
                            // so no transient can misprice the whole window.
                            //
                            // Ceiling multipliers over that allowance (server properties):
                            //   movement_avg_ceiling_3s  (default 1.30) — headroom for short bursts
                            //     (downhill, jump momentum, post-lag catch-up)
                            //   movement_avg_ceiling_15s (default 1.15) — bursts wash out over 15 s
                            // A +20% quickness hack lands above the 15 s ceiling and alerts every
                            // window; the violation streak escalator below turns any SUSTAINED
                            // overage into a kick within minutes, while an isolated burst scores
                            // once and decays.

                            // Local function: evaluate one window and return true if a kick was triggered.
                            bool EvalSpeedWindow(double windowSecs, float ceilingMultiplier, float strafeSegMult, float scoreMultiplier, float scoreCap, int sustainedKickWindows, string violationType, ref double lastScoreTime, ref int violationStreak)
                            {
                                // A single burst keeps the trailing average elevated for seconds, and
                                // this runs on every movement packet — without a cooldown one breach
                                // would score dozens of times and instantly hit the kick threshold.
                                // Each window scores at most once per its own length.
                                if (currentTime - lastScoreTime < windowSecs) return false;

                                // Right after a rubber-band the client replays from its pre-correction
                                // position; those round-trips inflate the window sum with distance the
                                // player never really travelled.  Skip until the client has caught up.
                                if (currentTime < RubberBandRecoveryUntil) return false;

                                // Find the first entry within the window.
                                int startIdx = 0;
                                while (startIdx < MovementWindowBuffer.Count - 1 &&
                                       currentTime - MovementWindowBuffer[startIdx].Timestamp > windowSecs)
                                    startIdx++;

                                // Need at least two entries spanning at least 0.5 s for a meaningful measurement.
                                if (MovementWindowBuffer.Count - startIdx < 2) return false;
                                var span = MovementWindowBuffer[MovementWindowBuffer.Count - 1].Timestamp
                                         - MovementWindowBuffer[startIdx].Timestamp;
                                if (span < 0.5) return false;

                                // Cumulative step-wise displacement so round-trip teleport patterns are
                                // caught, horizontal only (vertical terrain noise excluded).  Each
                                // segment's allowance uses the effective rate sampled at its start;
                                // segment time is capped at 1.5 s (matching the per-packet budget
                                // floor) so packet gaps can't bank a large allowance for later.
                                float totalDist = 0f;
                                float totalAllowed = 0f;
                                for (int i = startIdx; i < MovementWindowBuffer.Count - 1; i++)
                                {
                                    totalDist += MovementWindowBuffer[i].Pos.Distance2D(MovementWindowBuffer[i + 1].Pos);
                                    var segTime = (float)(MovementWindowBuffer[i + 1].Timestamp - MovementWindowBuffer[i].Timestamp);
                                    // Per-segment allowance multiplier: forward segments get the window
                                    // ceiling; strafe segments (sidestep flag set) get strafeSegMult
                                    // INSTEAD of stacking the strafe multiplier on top of the ceiling.
                                    // This closes the quickness-hack leak where faking the sidestep flag
                                    // yielded 1.248 x ceiling (1.435x sustained on the 15 s window).
                                    var segMult = MovementWindowBuffer[i].SideStep ? strafeSegMult : ceilingMultiplier;
                                    totalAllowed += 4.0f * MovementWindowBuffer[i].RawRunRate * Math.Min(segTime, 1.5f) * segMult;
                                }

                                // Ceiling is already folded into each segment's allowance above.
                                var allowedDist = totalAllowed;
                                if (allowedDist <= 0f || totalDist <= allowedDist)
                                {
                                    violationStreak = 0; // clean evaluation — sustained pattern broken
                                    return false;
                                }

                                lastScoreTime = currentTime;
                                violationStreak++;
                                // Suppress the general heartbeat score decay while average-speed
                                // violations are actively firing (see Heartbeat): a sustained cheat fires
                                // only once per window, and without this the decay between fires cancels
                                // the gains, letting a moderate quickness hack run for minutes.
                                _lastAvgViolationTime = currentTime;

                                // For logging, express both sides as average speeds over the span.
                                var avgSpeed = (float)(totalDist / span);
                                var avgWindowMaxSpeed = (float)(allowedDist / span);

                                // Proportional gain for large overages; the streak floor (per consecutive
                                // violated window, capped at the score cap) ensures a small-but-SUSTAINED
                                // overage escalates instead of being cancelled by heartbeat score decay.
                                var overage = (totalDist / allowedDist) - 1.0f;
                                var suspicionGain = Math.Min(Math.Max(overage * scoreMultiplier, violationStreak * 5.0f), scoreCap);
                                MovementSuspicionScore += suspicionGain;

                                log.Warn($"{Name} - AVG SPEED VIOLATION ({windowSecs:0}s window) - AvgSpeed: {avgSpeed:0.00}/{avgWindowMaxSpeed:0.00} SuspicionScore: {MovementSuspicionScore:0.0}");

                                // Log to ace_log and Discord webhook (fire-and-forget).
                                var _loc     = Location?.ToString() ?? "unknown";
                                var _account = Session?.Account ?? "unknown";
                                var _score   = MovementSuspicionScore;
                                var _avg     = avgSpeed;
                                var _max     = avgWindowMaxSpeed;
                                var _vtype   = violationType;
                                DiscordWebhookManager.SendMovementViolation(_vtype, Name, _account, _avg, _max, _score, _loc);
                                System.Threading.Tasks.Task.Run(() =>
                                    DatabaseManager.Log.LogMovementViolation(
                                        Guid.Full, Name, _account, _vtype, _avg, _max, _score, _loc));

                                // Decisive sustained-overage kick: several consecutive violated windows is
                                // cheating the fuzzy score can't be talked out of by decay.  N × 15 s of
                                // average speed over the ceiling is not achievable legitimately — glitch-
                                // runners top out at 1.248× and can't hold even that continuously, so they
                                // never trip the ceiling in the first place.  Blatant overages (≥ 50% over
                                // the ceiling, i.e. a ~2× hack) kick one window sooner.  sustainedKickWindows
                                // is int.MaxValue for the 3 s window (burst headroom — only the 15 s window
                                // carries this decisive kick).
                                if (sustainedKickWindows < int.MaxValue)
                                {
                                    var _kickWindows = overage >= 0.5f ? Math.Max(2, sustainedKickWindows - 1) : sustainedKickWindows;
                                    if (violationStreak >= _kickWindows)
                                    {
                                        log.Warn($"{Name} - SUSTAINED AVG SPEED KICK ({windowSecs:0}s window, streak {violationStreak} >= {_kickWindows}, avg {avgSpeed:0.00}/{avgWindowMaxSpeed:0.00}) - KICKING");
                                        Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                            new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                        return true;
                                    }
                                }

                                // Kick when suspicion score reaches 50 (sustained cheating pattern).
                                if (MovementSuspicionScore >= 50.0f)
                                {
                                    log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                    Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                        new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                    return true;
                                }

                                // Also kick when counter hits 10 and the config flag is enabled.
                                if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                                {
                                    log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                    Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                        new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                    return true;
                                }

                                return false;
                            }

                            // Evaluate short window first; if it kicks, skip long window.
                            // A 2x speed hack gains ~ +10/3s + +15/15s and kicks in ~15-20 s; a 1.5x
                            // hack kicks in under a minute; a sustained +20% quickness hack alerts on
                            // every 15 s window and the streak escalator kicks it within a few
                            // minutes.  An isolated burst scores once and decays.
                            const float StrafeRunMultiplier = 1.248f; // engine's own strafe-run speed multiplier
                            var _ceiling3s  = (float)PropertyManager.GetDouble("movement_avg_ceiling_3s").Item;
                            var _ceiling15s = (float)PropertyManager.GetDouble("movement_avg_ceiling_15s").Item;
                            // 3 s window keeps burst headroom: strafe segments get their own (looser)
                            // ceiling to cover downhill / jump / lag-catch-up bursts.  Tunable via
                            // movement_avg_strafe_ceiling_3s (default 1.45) — tighter than the old derived
                            // 1.248 × ceiling (1.62) so moderate cheats trip the fast window too.
                            var _strafe3s   = (float)PropertyManager.GetDouble("movement_avg_strafe_ceiling_3s").Item;
                            // 15 s window is the sustained-hack detector: strafe segments are capped FLAT
                            // at movement_avg_strafe_ceiling_15s (legit strafe-run 1.248x + a small
                            // margin) rather than 1.248 x ceiling, so faking the sidestep flag no longer
                            // buys a large sustained overage.
                            var _strafe15s  = (float)PropertyManager.GetDouble("movement_avg_strafe_ceiling_15s").Item;
                            // Consecutive 15 s windows over the ceiling before a decisive kick (see the
                            // sustained-overage kick in EvalSpeedWindow).  The 3 s window passes MaxValue
                            // so only the high-confidence 15 s window carries the direct kick.
                            var _kickWindows = (int)PropertyManager.GetLong("movement_avg_sustained_kick_windows").Item;
                            if (EvalSpeedWindow(3.0, _ceiling3s, _strafe3s, 25.0f, 10.0f, int.MaxValue, "speed_avg_3s", ref _lastSpeedAvg3sScoreTime, ref _speedAvg3sViolationStreak)) return false;
                            if (EvalSpeedWindow(15.0, _ceiling15s, _strafe15s, 40.0f, 15.0f, _kickWindows, "speed_avg_15s", ref _lastSpeedAvg15sScoreTime, ref _speedAvg15sViolationStreak)) return false;
                        }

                        // --- Script detection: inter-packet timing regularity ---
                        // Human hands always jitter; scripts tick at machine-clock precision.
                        // Measures the coefficient of variation (stddev / mean) of inter-packet
                        // intervals over a 4-second rolling window. CV < 0.04 means less than
                        // 4% variation — impossible to sustain for a real player.
                        if (PropertyManager.GetBool("enforce_player_timing_regularity").Item)
                        {
                            // Find window start (last 4 seconds).
                            int timingStart = 0;
                            while (timingStart < MovementWindowBuffer.Count - 1 &&
                                   currentTime - MovementWindowBuffer[timingStart].Timestamp > 4.0)
                                timingStart++;

                            int timingSamples = MovementWindowBuffer.Count - timingStart;
                            if (timingSamples >= 13) // 13 entries = 12 intervals for a stable CV
                            {
                                double timingSpan = MovementWindowBuffer[MovementWindowBuffer.Count - 1].Timestamp
                                                  - MovementWindowBuffer[timingStart].Timestamp;
                                if (timingSpan >= 3.0)
                                {
                                    double sumI = 0.0, sumISq = 0.0;
                                    int intervalCount = 0;
                                    for (int i = timingStart; i < MovementWindowBuffer.Count - 1; i++)
                                    {
                                        double interval = MovementWindowBuffer[i + 1].Timestamp
                                                        - MovementWindowBuffer[i].Timestamp;
                                        sumI   += interval;
                                        sumISq += interval * interval;
                                        intervalCount++;
                                    }
                                    double meanI    = sumI / intervalCount;
                                    double varianceI = (sumISq / intervalCount) - (meanI * meanI);
                                    double stddevI  = Math.Sqrt(Math.Max(0.0, varianceI));
                                    double cv       = meanI > 0.001 ? stddevI / meanI : 1.0;

                                    if (cv < 0.015)  // 0.04 false-positives on legit AC clients (machine-clock FPS loop); 0.015 only catches true script precision
                                    {
                                        MovementSuspicionScore += 6.0f;

                                        // Heuristic check — score and log only, no rubber-band.
                                        // Timing regularity is a statistical inference, not a provable
                                        // physics violation; rubber-banding would jerk a legitimate
                                        // player (e.g. a rock-steady framerate) around.  The accumulated
                                        // score still kicks a sustained scripter.
                                        log.Warn($"{Name} - SCRIPTED TIMING DETECTED - CV: {cv:0.0000} (mean: {meanI * 1000:0.0} ms, stddev: {stddevI * 1000:0.2} ms) SuspicionScore: {MovementSuspicionScore:0.0}");

                                        var _stLoc     = Location?.ToString() ?? "unknown";
                                        var _stAccount = Session?.Account ?? "unknown";
                                        var _stScore   = MovementSuspicionScore;
                                        DiscordWebhookManager.SendMovementViolation("script_timing", Name, _stAccount, (float)cv, 0.015f, _stScore, _stLoc);
                                        System.Threading.Tasks.Task.Run(() =>
                                            DatabaseManager.Log.LogMovementViolation(
                                                Guid.Full, Name, _stAccount, "script_timing", (float)cv, 0.015f, _stScore, _stLoc));

                                        if (MovementSuspicionScore >= 50.0f)
                                        {
                                            log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                            Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                                new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                            return false;
                                        }

                                        if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                                        {
                                            log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                            Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                                new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                            return false;
                                        }

                                        // fall through: position is accepted normally (no rubber-band)
                                    }
                                }
                            }
                        }

                        // --- Script detection: packet flood rate ---
                        // A legitimate AC client sends up to ~35 movement updates per second (higher on
                        // fast machines and during glitch-running); scripts flood at 100+/s.  Keep
                        // movement_packet_rate_limit well above the legitimate ceiling — see the notes
                        // on this property in the admin guide.
                        if (PropertyManager.GetBool("enforce_player_packet_rate").Item)
                        {
                            // Count entries added in the last 2 seconds.
                            int floodStart = 0;
                            while (floodStart < MovementWindowBuffer.Count &&
                                   currentTime - MovementWindowBuffer[floodStart].Timestamp > 2.0)
                                floodStart++;

                            int packetsIn2s = MovementWindowBuffer.Count - floodStart;
                            float packetRate = packetsIn2s / 2.0f;
                            var rateLimit = (float)PropertyManager.GetLong("movement_packet_rate_limit").Item;

                            // Heuristic check — score and log only, no rubber-band.  Throttled to once
                            // per second: this evaluates on every clean tick, so without the throttle a
                            // brief burst over the limit would score on every packet and spike the score.
                            // A genuine flood stays over the limit for many seconds and still accrues
                            // score steadily toward a kick.
                            if (packetRate > rateLimit && currentTime - _lastPacketRateScoreTime >= 1.0)
                            {
                                _lastPacketRateScoreTime = currentTime;
                                MovementSuspicionScore += 4.0f;

                                log.Warn($"{Name} - PACKET FLOOD DETECTED - Rate: {packetRate:0.0}/s (limit: {rateLimit:0}/s) SuspicionScore: {MovementSuspicionScore:0.0}");

                                var _pfLoc     = Location?.ToString() ?? "unknown";
                                var _pfAccount = Session?.Account ?? "unknown";
                                var _pfScore   = MovementSuspicionScore;
                                DiscordWebhookManager.SendMovementViolation("script_packet_rate", Name, _pfAccount, packetRate, rateLimit, _pfScore, _pfLoc);
                                System.Threading.Tasks.Task.Run(() =>
                                    DatabaseManager.Log.LogMovementViolation(
                                        Guid.Full, Name, _pfAccount, "script_packet_rate", packetRate, rateLimit, _pfScore, _pfLoc));

                                if (MovementSuspicionScore >= 50.0f)
                                {
                                    log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                    Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                        new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                    return false;
                                }

                                if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                                {
                                    log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                    Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                        new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                    return false;
                                }

                                // fall through: position is accepted normally (no rubber-band)
                            }
                        }

                        // --- Script detection: inhuman direction reversal ---
                        // A back-and-forth kiting script performs consecutive ~180° heading reversals
                        // faster and more precisely than a human can.  Each detection requires 4
                        // consecutive buffer entries (3 steps), each step with real displacement
                        // (> 0.2 units), all three intervals under 66 ms, and both adjacent heading
                        // changes within 10° of 180°.  A single detection is NOT scored — it only
                        // counts toward a sustained-pattern threshold below, because brief glitch-
                        // running produces the occasional qualifying flip while only a continuous
                        // script sustains several in a row.
                        if (PropertyManager.GetBool("enforce_player_reversal_detection").Item
                            && MovementWindowBuffer.Count >= 4)
                        {
                            int last = MovementWindowBuffer.Count - 1;
                            var rp0 = MovementWindowBuffer[last - 3];
                            var rp1 = MovementWindowBuffer[last - 2];
                            var rp2 = MovementWindowBuffer[last - 1];
                            var rp3 = MovementWindowBuffer[last];

                            // Interval checks first (cheap).
                            // All three steps must be under 66 ms (≈ 2 game frames at 30 fps).
                            // Sticky-target auto-facing during combat produces reversals at
                            // 83–150 ms intervals (the client takes multiple frames to rotate);
                            // true speed-scripts run at machine-clock precision (16–33 ms).
                            // The 66 ms cutoff cleanly separates the two without false-positiving
                            // on any of the combat-reversal patterns observed in testing.
                            double rdt01 = rp1.Timestamp - rp0.Timestamp;
                            double rdt12 = rp2.Timestamp - rp1.Timestamp;
                            double rdt23 = rp3.Timestamp - rp2.Timestamp;

                            if (rdt01 < 0.066 && rdt12 < 0.066 && rdt23 < 0.066)
                            {
                                // Displacement checks — each step must be meaningful movement.
                                float rdx01 = rp1.Pos.PositionX - rp0.Pos.PositionX;
                                float rdy01 = rp1.Pos.PositionY - rp0.Pos.PositionY;
                                float rdx12 = rp2.Pos.PositionX - rp1.Pos.PositionX;
                                float rdy12 = rp2.Pos.PositionY - rp1.Pos.PositionY;
                                float rdx23 = rp3.Pos.PositionX - rp2.Pos.PositionX;
                                float rdy23 = rp3.Pos.PositionY - rp2.Pos.PositionY;

                                float rdist01 = (float)Math.Sqrt(rdx01 * rdx01 + rdy01 * rdy01);
                                float rdist12 = (float)Math.Sqrt(rdx12 * rdx12 + rdy12 * rdy12);
                                float rdist23 = (float)Math.Sqrt(rdx23 * rdx23 + rdy23 * rdy23);

                                if (rdist01 > 0.2f && rdist12 > 0.2f && rdist23 > 0.2f)
                                {
                                    double rh01 = Math.Atan2(rdy01, rdx01);
                                    double rh12 = Math.Atan2(rdy12, rdx12);
                                    double rh23 = Math.Atan2(rdy23, rdx23);

                                    // Angular difference, clamped to [0, π].
                                    static double HeadingDiff(double a, double b)
                                    {
                                        double d = Math.Abs(a - b) % (Math.PI * 2);
                                        return d > Math.PI ? Math.PI * 2 - d : d;
                                    }

                                    double rAngle1 = HeadingDiff(rh01, rh12);
                                    double rAngle2 = HeadingDiff(rh12, rh23);

                                    // Both must be within 10° (0.175 rad) of a full 180° reversal.
                                    // Tighter than the old 20° window: sticky-target combat facing
                                    // typically produces 160–170° angles; genuine scripted kiting
                                    // produces near-exact 180° flips.
                                    const double reversalMin = Math.PI - 0.175;
                                    if (rAngle1 >= reversalMin && rAngle2 >= reversalMin)
                                    {
                                        // Record this qualifying double-reversal and prune to a 2 s window.
                                        ReversalEventTimes.Add(currentTime);
                                        while (ReversalEventTimes.Count > 0 && currentTime - ReversalEventTimes[0] > 2.0)
                                            ReversalEventTimes.RemoveAt(0);

                                        // Sustained-pattern gate: a single qualifying flip is not enough
                                        // (brief glitch-running produces the occasional one).  Only score
                                        // once several accumulate within the window — a continuous kiting
                                        // script sustains them; human glitch-running does not.  Score-only,
                                        // no rubber-band: this is a heuristic, not a provable violation.
                                        if (ReversalEventTimes.Count >= 4)
                                        {
                                            ReversalEventTimes.Clear(); // reset the window after scoring (throttle)
                                            MovementSuspicionScore += 7.0f;

                                            log.Warn($"{Name} - INHUMAN REVERSAL DETECTED - Angle1: {rAngle1 * 180 / Math.PI:0.0}° Angle2: {rAngle2 * 180 / Math.PI:0.0}° Intervals: {rdt01 * 1000:0.0}/{rdt12 * 1000:0.0}/{rdt23 * 1000:0.0} ms SuspicionScore: {MovementSuspicionScore:0.0}");

                                            var _rvLoc     = Location?.ToString() ?? "unknown";
                                            var _rvAccount = Session?.Account ?? "unknown";
                                            var _rvScore   = MovementSuspicionScore;
                                            DiscordWebhookManager.SendMovementViolation("script_reversal", Name, _rvAccount, (float)rAngle1, (float)reversalMin, _rvScore, _rvLoc);
                                            System.Threading.Tasks.Task.Run(() =>
                                                DatabaseManager.Log.LogMovementViolation(
                                                    Guid.Full, Name, _rvAccount, "script_reversal", (float)rAngle1, (float)reversalMin, _rvScore, _rvLoc));

                                            if (MovementSuspicionScore >= 50.0f)
                                            {
                                                log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                                Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                                    new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                                return false;
                                            }

                                            if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                                            {
                                                log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                                Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                                    new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                                return false;
                                            }
                                        }
                                        // fall through: position is accepted normally (no rubber-band)
                                    }
                                }
                            }
                        }

                        // --- Option N: Door collision detection ---
                        // Blocks players whose physics transition passed through a closed door.
                        // LastTransitionHitClosedDoor is set in update_object_server_new() when the
                        // collision list contains a Door with IsOpen == false.
                        //
                        // This is treated as pure collision enforcement, NOT a punishable violation:
                        // the player is rubber-banded back so they cannot pass through the door, but
                        // there is no suspicion score and no kick.  A closed door is a physical barrier
                        // like a wall — a player pressed against one (often from client/server door-state
                        // desync) should simply be stopped, never booted.  Logging is throttled since the
                        // rubber-band re-triggers this block every tick the player leans on the door.
                        if (PropertyManager.GetBool("enforce_player_door_collision").Item
                            && PhysicsObj.LastTransitionHitClosedDoor
                            && GodState == null)
                        {
                            RubberBandRecoveryUntil = currentTime + 0.75;
                            RollbackToSnap("door_ghost");

                            // Informational only (throttled to once per 5 s per player). No score, no kick.
                            if (currentTime - _lastDoorGhostLogTime >= 5.0)
                            {
                                _lastDoorGhostLogTime = currentTime;
                                log.Warn($"{Name} - DOOR COLLISION (blocked from passing through closed door) - Location: {newPosition}");

                                var _doorLocation = Location?.ToString() ?? "unknown";
                                var _doorAccount  = Session?.Account ?? "unknown";
                                DiscordWebhookManager.SendMovementViolation("door_ghost", Name, _doorAccount, 0f, 0f, MovementSuspicionScore, _doorLocation);
                                System.Threading.Tasks.Task.Run(() =>
                                    DatabaseManager.Log.LogMovementViolation(
                                        Guid.Full, Name, _doorAccount, "door_ghost", 0f, 0f, MovementSuspicionScore, _doorLocation));
                            }

                            return false;
                        }

                        // --- Option K: Spawn grace period creature collision ---
                        // Flags players whose physics transition passed through a creature that spawned
                        // within the last 5 seconds. This catches the brief window where a mob spawns
                        // inside or immediately adjacent to a player who can then walk through it.
                        // LastTransitionHitNewCreature is set in update_object_server_new().
                        if (PropertyManager.GetBool("enforce_player_spawn_collision").Item
                            && PhysicsObj.LastTransitionHitNewCreature
                            && GodState == null)
                        {
                            MovementSuspicionScore += 4.0f; // lower weight — may be a legitimate spawn overlap

                            RubberBandRecoveryUntil = currentTime + 0.75;
                            RollbackToSnap("spawn_ghost");
                            log.Warn($"{Name} - SPAWN GHOST DETECTED (passed through newly-spawned creature) - Location: {newPosition} SuspicionScore: {MovementSuspicionScore:0.0}");

                            // Log to ace_log for long-term ban evidence (fire-and-forget).
                            var _spawnLocation = Location?.ToString() ?? "unknown";
                            var _spawnAccount  = Session?.Account ?? "unknown";
                            var _spawnScore    = MovementSuspicionScore;
                            DiscordWebhookManager.SendMovementViolation("spawn_ghost", Name, _spawnAccount, 0f, 0f, _spawnScore, _spawnLocation);
                            System.Threading.Tasks.Task.Run(() =>
                                DatabaseManager.Log.LogMovementViolation(
                                    Guid.Full, Name, _spawnAccount, "spawn_ghost", 0f, 0f, _spawnScore, _spawnLocation));

                            // Shared kick thresholds.
                            if (MovementSuspicionScore >= 50.0f)
                            {
                                log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                    new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                return false;
                            }

                            if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                            {
                                log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                    new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                return false;
                            }

                            return false;
                        }

                        // --- Geometry collision detection (Change 7) ---
                        // update_object_server_new() already computed a full physics transition.
                        // LastTransitionHitGeometry = true only when ALL of:
                        //   • the transition was blocked (valid == false)
                        //   • dist > 0.5f  (significant blockage, not terrain precision noise)
                        //   • CollideObject is empty (no dynamic object — creature/door/player —
                        //     caused the block; only static geometry / terrain triggers this flag)
                        // Catches wall-walk and out-of-bounds glitches without firing on players
                        // jostled by monsters or minor terrain precision errors.
                        if (PropertyManager.GetBool("enforce_player_movement_raycast").Item
                            && PhysicsObj.LastTransitionHitGeometry
                            && GodState == null)
                        {
                            if (currentTime < GeometryViolationCooldownUntil)
                            {
                                // Within the per-geometry cooldown window: skip silently.
                                // When enforce_player_movement is on, physics already validated the
                                // transition; the secondary transition() check fires on tight dungeon
                                // geometry even for legitimate positions.  Suppressing follow-on hits
                                // prevents the cascade (e.g. 9 × +5 in 1 s → kick) seen for players
                                // walking through normal dungeon corridors.
                                // Do NOT return false here — let the physics-valid position be accepted
                                // so the player can walk away from the problem area.
                            }
                            else
                            {
                                // First geometry hit (or cooldown expired): score once, set a 2-second
                                // suppression window, and rubber-band so a real wall-walk is corrected.
                                GeometryViolationCooldownUntil = currentTime + 2.0;

                                MovementSuspicionScore += 5.0f;

                                RubberBandRecoveryUntil = currentTime + 0.75;
                                RollbackToSnap("geometry");
                                log.Warn($"{Name} - GEOMETRY COLLISION DETECTED (wall-walk/blink) - Location: {newPosition} SuspicionScore: {MovementSuspicionScore:0.0}");

                                // Log to ace_log for long-term ban evidence (fire-and-forget).
                                var _geoLocation = Location?.ToString() ?? "unknown";
                                var _geoAccount  = Session?.Account ?? "unknown";
                                var _geoScore    = MovementSuspicionScore;
                                DiscordWebhookManager.SendMovementViolation("geometry", Name, _geoAccount, 0f, 0f, _geoScore, _geoLocation);
                                System.Threading.Tasks.Task.Run(() =>
                                    DatabaseManager.Log.LogMovementViolation(
                                        Guid.Full, Name, _geoAccount, "geometry", 0f, 0f, _geoScore, _geoLocation));

                                // Kick thresholds shared with Change 5.
                                if (MovementSuspicionScore >= 50.0f)
                                {
                                    log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                    Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                        new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                    return false;
                                }

                                if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                                {
                                    log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                    Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                        new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                    return false;
                                }

                                return false;
                            }
                        }

                        // --- Jump height tracking and cap (Change 8) ---
                        // Detects the start of a jump, tracks the apex Z, and on landing checks
                        // whether the player reached higher than their Strength/Jump skill allows.
                        // Only enforced within the same landblock to avoid cross-cell coordinate issues.
                        var isJumpingNow = IsJumping;
                        if (!WasJumping && isJumpingNow)
                        {
                            // Jump started: record the launch Z, landblock, and heading.
                            JumpStartZ       = newPosition.PositionZ;
                            JumpStartCell    = newPosition.Cell;
                            JumpPeakZ        = JumpStartZ;
                            JumpStartHeading = PhysicsObj.Position.Frame.get_heading();
                            AirborneHeadingDelta = 0f;
                        }
                        else if (isJumpingNow)
                        {
                            // In-flight: keep tracking the apex and heading delta.
                            if (newPosition.PositionZ > JumpPeakZ)
                                JumpPeakZ = newPosition.PositionZ;
                            var curHeading = PhysicsObj.Position.Frame.get_heading();
                            var rawDelta = Math.Abs(curHeading - JumpStartHeading);
                            var headingDelta = rawDelta > 180f ? 360f - rawDelta : rawDelta;
                            if (headingDelta > AirborneHeadingDelta)
                                AirborneHeadingDelta = headingDelta;
                        }
                        else if (WasJumping)
                        {
                            AirborneHeadingDelta = 0f;
                            // Just landed: evaluate the apex if the feature is enabled.
                            if (PropertyManager.GetBool("enforce_player_jump_height").Item
                                && GodState == null
                                && (JumpStartCell >> 16) == (newPosition.Cell >> 16)) // same landblock
                            {
                                var deltaZ = JumpPeakZ - JumpStartZ;
                                if (deltaZ > 0.5f) // ignore trivial height deltas from uneven terrain
                                {
                                    var maxVz = 0f;
                                    if (PhysicsObj.WeenieObj.InqJumpVelocity(1.0f, out maxVz) && maxVz > 0f)
                                    {
                                        // max height from kinematics: h = vz² / (2g), 2g = 19.6
                                        // add 50% fudge factor to absorb timing/lag differences
                                        var maxHeight = (maxVz * maxVz / 19.6f) * 1.5f;
                                        if (deltaZ > maxHeight)
                                        {
                                            var overage = deltaZ / maxHeight - 1.0f;
                                            var suspicionGain = Math.Min(overage * 10.0f, 15.0f);
                                            MovementSuspicionScore += suspicionGain;

                                            RubberBandRecoveryUntil = currentTime + 0.75;
                                            RollbackToSnap($"jump_height {deltaZ:0.00}/{maxHeight:0.00}");
                                            log.Warn($"{Name} - JUMP HEIGHT VIOLATION - DeltaZ: {deltaZ:0.00} MaxAllowed: {maxHeight:0.00} SuspicionScore: {MovementSuspicionScore:0.0}");

                                            // Log to ace_log for long-term ban evidence (fire-and-forget).
                                            var _jumpLocation = Location?.ToString() ?? "unknown";
                                            var _jumpAccount  = Session?.Account ?? "unknown";
                                            var _jumpScore    = MovementSuspicionScore;
                                            var _jumpDelta    = deltaZ;
                                            var _jumpMax      = maxHeight;
                                            DiscordWebhookManager.SendMovementViolation("jump_height", Name, _jumpAccount, _jumpDelta, _jumpMax, _jumpScore, _jumpLocation);
                                            System.Threading.Tasks.Task.Run(() =>
                                                DatabaseManager.Log.LogMovementViolation(
                                                    Guid.Full, Name, _jumpAccount, "jump_height",
                                                    _jumpDelta, _jumpMax, _jumpScore, _jumpLocation));

                                            // Kick thresholds shared with all movement checks.
                                            if (MovementSuspicionScore >= 50.0f)
                                            {
                                                log.Warn($"{Name} - MOVEMENT SUSPICION THRESHOLD REACHED ({MovementSuspicionScore:0.0}) - KICKING");
                                                Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                                    new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                                WasJumping = false;
                                                return false;
                                            }

                                            if (MovementEnforcementCounter >= 10 && PropertyManager.GetBool("movement_violation_kick").Item)
                                            {
                                                log.Warn($"{Name} - MOVEMENT ENFORCEMENT COUNTER THRESHOLD ({MovementEnforcementCounter}) - KICKING");
                                                Session.Terminate(SessionTerminationReason.MovementEnforcementFailure,
                                                    new GameMessageBootAccount(" because there is a divergence between your server and client locations"));
                                                WasJumping = false;
                                                return false;
                                            }

                                            WasJumping = false;
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                        WasJumping = isJumpingNow;
                    }
                }

                // double update path: landblock physics update -> updateplayerphysics() -> update_object_server() -> Teleport() -> updateplayerphysics() -> return to end of original branch
                if (Teleporting && !forceUpdate) return true;

                // When physics reports a failed transition (cell-mismatch, geometry reject, etc.) but
                // DID run UpdateObjectInternal, PhysicsObj.Position holds the physics-validated position
                // the player actually reached.  Without syncing, Location and LastPlayerMovementCheckTime
                // freeze at the pre-failure values.  On the next successful tick the gap can span dozens
                // of physics steps, making dist >> currentMaxSpeed and triggering a false speed violation.
                //
                // Fix: on a physics failure while speed-checking is active, advance Location to wherever
                // physics placed the player.  SnapPos is intentionally NOT advanced here — it stays at
                // the last clean accepted position so any rollback still returns the player to a
                // confirmed-good point.  LastPlayerMovementCheckTime is deliberately NOT reset: the next
                // successful packet's deltaTime then spans the failure period, and the speed check's
                // budget floor (full run-rate coverage over elapsed time) absorbs the distance the
                // client legitimately covered while the server was rejecting transitions — previously
                // a hill fight ended with a huge dist measured against a single-packet budget.
                if (!success && EnforceMovementSpeed && !Teleporting)
                {
                    var _physPos = PhysicsObj?.Position?.ACEPosition();
                    if (_physPos != null && _physPos.Cell != 0)
                        Location = _physPos;
                }

                if (!success) return false;

                var landblockUpdate = Location.Cell >> 16 != newPosition.Cell >> 16;

                Location = newPosition;

                if (RecordCast.Enabled)
                    RecordCast.Log($"CurPos: {Location.ToLOCString()}");

                if (RequestedLocationBroadcast || DateTime.UtcNow - LastUpdatePosition >= MoveToState_UpdatePosition_Threshold)
                    SendUpdatePosition();
                else
                    Session.Network.EnqueueSend(new GameMessageUpdatePosition(this));

                if (!InUpdate)
                    LandblockManager.RelocateObjectForPhysics(this, true);

                return landblockUpdate;
            }
            finally
            {
                if (!forceUpdate) // This is needed beacuse this function might be called recursively
                {
                    var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                    ServerPerformanceMonitor.AddToCumulativeEvent(ServerPerformanceMonitor.CumulativeEventHistoryType.Player_Tick_UpdateObjectPhysics, elapsedSeconds);
                    if (elapsedSeconds >= 0.100) // Yea, that ain't good....
                        log.Warn($"[PERFORMANCE][PHYSICS] {Guid}:{Name} took {(elapsedSeconds * 1000):N1} ms to process UpdatePlayerPosition() at loc: {Location}");
                    else if (elapsedSeconds >= 0.010)
                        log.DebugFormat("[PERFORMANCE][PHYSICS] {0}:{1} took {2:N1} ms to process UpdatePlayerPosition() at loc: {3}", Guid, Name, (elapsedSeconds * 1000), Location);
                }
            }
        }

        public bool HasAnyMovement()
        {
            if (FastTick)
            {
                if (PhysicsObj.IsMovingOrAnimating || IsMoving || IsPlayerMovingTo || IsPlayerMovingTo2)
                    return true;
            }
            else
            {
                var isWaitingForNextUseTime = DateTime.UtcNow < NextUseTime;
                var isPlayerInitiatedMovement = (CurrentMoveToState.RawMotionState.Flags & (RawMotionFlags.ForwardCommand | RawMotionFlags.SideStepCommand | RawMotionFlags.TurnCommand)) != 0;

                if (isPlayerInitiatedMovement || HasPerformedActionsSinceLastMovementUpdate || IsJumping || PhysicsObj.IsMovingOrAnimating || IsMoving || IsPlayerMovingTo || IsPlayerMovingTo2 || isWaitingForNextUseTime)
                    return true;
            }
            return false;
        }

        private static HashSet<uint> buggedCells = new HashSet<uint>()
        {
            0xD6990112,
            0xD599012C
        };

        public bool ValidateMovement(ACE.Entity.Position newPosition)
        {
            if (CurrentLandblock == null)
                return false;

            if (!Teleporting && Location.Landblock != newPosition.Cell >> 16)
            {
                if ((Location.Cell & 0xFFFF) >= 0x100 && (newPosition.Cell & 0xFFFF) >= 0x100)
                {
                    if (!buggedCells.Contains(Location.Cell) || !buggedCells.Contains(newPosition.Cell))
                        return false;
                }

                if (CurrentLandblock.IsDungeon)
                {
                    var destBlock = LScape.get_landblock(newPosition.Cell);
                    if (destBlock != null && destBlock.IsDungeon)
                        return false;
                }
            }
            return true;
        }


        public bool SyncLocationWithPhysics()
        {
            if (PhysicsObj.CurCell == null)
            {
                Console.WriteLine($"{Name}.SyncLocationWithPhysics(): CurCell is null!");
                return false;
            }

            var blockcell = PhysicsObj.Position.ObjCellID;
            var pos = PhysicsObj.Position.Frame.Origin;
            var rotate = PhysicsObj.Position.Frame.Orientation;

            var landblockUpdate = blockcell << 16 != CurrentLandblock.Id.Landblock;

            Location = new ACE.Entity.Position(blockcell, pos, rotate);

            return landblockUpdate;
        }

        private bool gagNoticeSent = false;

        public void GagsTick()
        {
            if (IsGagged)
            {
                if (!gagNoticeSent)
                {
                    SendGagNotice();
                    gagNoticeSent = true;
                }

                // check for gag expiration, if expired, remove gag.
                GagDuration -= CachedHeartbeatInterval;

                if (GagDuration <= 0)
                {
                    IsGagged = false;
                    GagTimestamp = 0;
                    GagDuration = 0;
                    SaveBiotaToDatabase();
                    SendUngagNotice();
                    gagNoticeSent = false;
                }
            }
        }

        /// <summary>
        /// Prepare new action to run on this player
        /// </summary>
        public override void EnqueueAction(IAction action)
        {
            actionQueue.EnqueueAction(action);
        }

        public void LeyLineAmuletsTick(double currentUnixTime)
        {
            if (EquippedObjectsLoaded && InventoryLoaded)
            {
                var list = EquippedObjects.Values.Where(i => i.WeenieType == WeenieType.LeyLineAmulet).ToList();
                list = list.Concat(Inventory.Values).Where(i => i.WeenieType == WeenieType.LeyLineAmulet).ToList();

                foreach (var item in list)
                {
                    LeyLineAmulet amulet = item as LeyLineAmulet;
                    if(amulet != null)
                        amulet.CheckAlignmentDecay(this, currentUnixTime);
                }
            }
        }

        private static List<string> PvPInciteMessages = new List<string>
        {
            "It would be a shame if someone would kill them...",
            "Be a good boy and do your thing would you?",
            "Just hanging around without a care in the world..."
        };
        public void PvPInciteTick(double currentUnixTime)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return;

            if (!PropertyManager.GetBool("bz_whispers_enabled").Item)
                return;

            if ((!IsPK && !IsPKL) || ThreadSafeRandom.Next(0.0f, 1.0f) > PropertyManager.GetDouble("bz_whispers_chance").Item)
                return;

            List<Player> validPlayers;
            if (GameplayMode == GameplayModes.HardcorePK)
                validPlayers = PlayerManager.GetAllOnline().Where(e => e.Guid != Guid && e.GameplayMode == GameplayModes.HardcorePK &&!e.IsOvertlyPlussed).ToList();
            else
                validPlayers = PlayerManager.GetAllOnline().Where(e => e.Guid != Guid && e.GameplayMode == GameplayModes.Regular && e.IsPK && !e.IsOvertlyPlussed).ToList();

            if (validPlayers.Count + 1 < PropertyManager.GetLong("bz_whispers_min_pop").Item)
                return;

            List<Player> possiblePlayers = validPlayers.Where(e => e.Level >= Level && e.Level <= Level + 5 && !e.IsOvertlyPlussed).ToList();
            if (possiblePlayers.Count() > 0)
            {
                var validPossiblePlayers = new List<Player>();
                foreach (var entry in possiblePlayers)
                {
                    if (!LandblockManager.apartmentLandblocks.Contains((uint)entry.Location.LandblockId.Landblock << 16 ^ 0x0000FFFF) && !NoDamage_Landblocks.Contains(entry.Location.LandblockId.Landblock) && (Allegiance == null || entry.Allegiance != Allegiance) && (Fellowship == null || entry.Fellowship != Fellowship))
                        validPossiblePlayers.Add(entry);
                }

                if (validPossiblePlayers.Count() > 0)
                {
                    var rolledPlayer = validPossiblePlayers[ThreadSafeRandom.Next(0, validPossiblePlayers.Count() - 1)];

                    var position = rolledPlayer.Biota.GetPosition(PositionType.Location, rolledPlayer.BiotaDatabaseLock);

                    string locationString = Server.Entity.Landblock.GetLocationString(position.LandblockId.Landblock);

                    if (locationString != "")
                    {
                        var message = PvPInciteMessages[ThreadSafeRandom.Next(0, PvPInciteMessages.Count() - 1)];
                        Session.Network.EnqueueSend(new GameMessageSound(Guid, Sound.HealthDownVoid));
                        Session.Network.EnqueueSend(new GameMessageSystemChat($"Bael'zharon whispers in your ear, \"{rolledPlayer.Name} is currently{locationString}. {(rolledPlayer.Gender == 2 ? "She" : "He")} is level {rolledPlayer.Level}. {message}\"", ChatMessageType.Tell));
                    }
                }
            }
        }

        /// <summary>
        /// Called every ~5 secs for equipped mana consuming items
        /// </summary>
        public void ManaConsumersTick()
        {
            if (!EquippedObjectsLoaded) return;

            foreach (var item in EquippedObjects.Values)
            {
                if (!item.IsAffecting && Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                    continue;

                if (item.ItemCurMana == null || item.ItemMaxMana == null || item.ManaRate == null)
                    continue;

                var burnRate = -item.ManaRate.Value;

                if (LumAugItemManaUsage != 0)
                    burnRate *= GetNegativeRatingMod(LumAugItemManaUsage * 5);

                item.ItemManaRateAccumulator += (float)(burnRate * CachedHeartbeatInterval);

                if (item.ItemManaRateAccumulator < 1)
                    continue;

                var manaToBurn = (int)Math.Floor(item.ItemManaRateAccumulator);

                if (manaToBurn > item.ItemCurMana)
                    manaToBurn = item.ItemCurMana.Value;

                item.ItemManaRateAccumulator -= manaToBurn;

                if (Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.CustomDM)
                {
                    if (!item.IsAffecting)
                    {
                        if (CanActivateItemSpells(item, true))
                        {
                            Session.Network.EnqueueSend(new GameMessageSystemChat($"You now meet the requirements to activate the {item.NameWithMaterial}!", ChatMessageType.Magic));
                            ActivateItemSpells(item);
                        }
                        continue;
                    }
                    else
                    {
                        var result = item.CheckUseRequirements(this, true);
                        if (!result.Success)
                        {
                            Session.Network.EnqueueSend(new GameMessageSystemChat($"You no longer meet the requirements to activate the {item.NameWithMaterial}!", ChatMessageType.Magic));
                            DeactivateItemSpells(item);
                            continue;
                        }
                    }
                }

                item.ItemCurMana -= manaToBurn;

                if (item.ItemCurMana > 0)
                    CheckLowMana(item, burnRate);
                else
                    HandleManaDepleted(item);
            }
        }

        private bool CheckLowMana(WorldObject item, double burnRate)
        {
            const int lowManaWarningSeconds = 120;

            var secondsUntilEmpty = item.ItemCurMana / burnRate;

            if (secondsUntilEmpty > lowManaWarningSeconds)
            {
                item.ItemManaDepletionMessage = false;
                return false;
            }
            if (!item.ItemManaDepletionMessage)
            {
                Session.Network.EnqueueSend(new GameMessageSystemChat($"Your {item.NameWithMaterial} is low on Mana.", ChatMessageType.Magic));
                item.ItemManaDepletionMessage = true;
            }
            return true;
        }

        private void HandleManaDepleted(WorldObject item)
        {
            var msg = new GameMessageSystemChat($"Your {item.NameWithMaterial} is out of Mana.", ChatMessageType.Magic);
            var sound = new GameMessageSound(Guid, Sound.ItemManaDepleted);
            Session.Network.EnqueueSend(msg, sound);

            // unsure if these messages / sounds were ever sent in retail,
            // or if it just purged the enchantments invisibly
            // doing a delay here to prevent 'SpellExpired' sounds from overlapping with 'ItemManaDepleted'
            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(2.0f);
            actionChain.AddAction(this, () => DeactivateItemSpells(item));
            actionChain.EnqueueChain();
        }

        public override void OnMotionDone(uint motionID, bool success)
        {
            //Console.WriteLine($"{Name}.HandleMotionDone({(MotionCommand)motionID}, {success})");

            if (!FastTick) return;

            if (FoodState.IsChugging)
                HandleMotionDone_UseConsumable(motionID, success);

            if (MagicState.IsCasting)
                HandleMotionDone_Magic(motionID, success);
        }
    }
}
