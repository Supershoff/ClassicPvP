using System;
using System.Collections.Generic;
using System.Linq;

using log4net;

using ACE.Database;
using ACE.Database.Models.Log;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Managers
{
    public static class ArenaManager
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<uint, ArenaPlayer> queuedPlayers = new Dictionary<uint, ArenaPlayer>();
        private static Dictionary<uint, ArenaLocation> arenaLocations = new Dictionary<uint, ArenaLocation>();

        public static void Initialize()
        {
            arenaLocations = ArenaLocation.InitializeArenaLocations();
        }

        private static DateTime LastTickDateTime  = DateTime.MinValue;
        private static DateTime LastDecayTickDate = DateTime.MinValue;

        public static void Tick()
        {
            if (DateTime.Now.AddSeconds(-3) < LastTickDateTime)
                return;

            // Apply ELO decay once per calendar day regardless of arena enabled/disabled state.
            // Each event category (1v1, 2v2) decays independently, at a rate set by how
            // many matches the player completed in that same category over the last 7 days.
            if (DateTime.Now.Date > LastDecayTickDate.Date)
            {
                LastDecayTickDate = DateTime.Now;
                DatabaseManager.Log.ApplyArenaEloDecay();
                log.Info("[Arena] Daily ELO decay applied.");
            }

            bool isArenasDisabled = PropertyManager.GetBool("disable_arenas").Item;
            if (isArenasDisabled)
            {
                foreach (var arena in arenaLocations)
                {
                    if (arena.Value.HasActiveEvent)
                    {
                        arena.Value.EndEventTimelimitExceeded();
                        arena.Value.ClearPlayersFromArena();
                        arena.Value.ActiveEvent = null;
                    }
                }

                if (queuedPlayers.Count() > 0)
                    queuedPlayers.Clear();

                LastTickDateTime = DateTime.Now;

                return;
            }

            var randomizedLocationList = arenaLocations.Values.OrderBy(x => Guid.NewGuid());
            foreach (var arena in randomizedLocationList)
            {
                arena.Tick();
            }

            LastTickDateTime = DateTime.Now;
        }

        public static List<ArenaEvent> GetActiveEvents()
        {
            var eventList = new List<ArenaEvent>();

            foreach (var arena in arenaLocations.Values)
            {
                if (arena.HasActiveEvent)
                {
                    eventList.Add(arena.ActiveEvent);
                }
            }

            return eventList;
        }

        public static bool IsActiveArenaPlayer(uint characterId)
        {
            bool isPlayerActive = false;

            foreach (var arena in arenaLocations.Values)
            {
                if (arena.HasActiveEvent && arena.ActiveEvent.Status >= 4)
                {
                    isPlayerActive = arena.ActiveEvent.Players.FirstOrDefault(x => x.CharacterId == characterId) != null;
                    if (isPlayerActive)
                        break;
                }
            }

            return isPlayerActive;
        }

        public static bool AddPlayerToQueue(uint characterId, string characterName, int? characterLevel, string eventType, uint monarchId, string monarchName, string playerIP, out string returnMsg, Guid? teamGuid = null, int maxOpposingTeamSize = 9)
        {
            returnMsg = string.Empty;

            if (queuedPlayers.ContainsKey(characterId))
            {
                returnMsg = $"You are actively queued for an arena event, you cannot queue twice";
                return false;
            }

            var existingArenaPlayer = ArenaManager.GetArenaPlayerByCharacterId(characterId);
            if (existingArenaPlayer != null)
            {
                returnMsg = $"You are currently in an active {existingArenaPlayer.EventTypeDisplay} arena event.  You must wait until your current event is over before queuing for another one.";
                return false;
            }

            switch (eventType.ToLower())
            {
                case "1v1":
                    maxOpposingTeamSize = 1;
                    break;
                case "2v2":
                    maxOpposingTeamSize = 2;
                    break;
                case "ffa":
                case "tugak":
                    maxOpposingTeamSize = 1;
                    break;
                case "group":
                    break;
            }

            ArenaPlayer player = new ArenaPlayer();
            player.CharacterId = characterId;
            player.CharacterName = characterName;
            player.CharacterLevel = (uint)(characterLevel.HasValue ? characterLevel.Value : 0);
            player.EventType = eventType.ToLower();
            player.MonarchId = monarchId;
            player.MonarchName = monarchName;
            player.CreateDateTime = DateTime.Now;
            player.PlayerIP = playerIP;
            player.TeamGuid = teamGuid;
            player.MaxOpposingTeamSize = maxOpposingTeamSize;

            queuedPlayers.Add(characterId, player);

            var queueCount = queuedPlayers.Values.Count(x => x.EventType.Equals(eventType));

            if (!eventType.ToLower().Equals("group"))
            {
                PlayerManager.BroadcastToAll(new GameMessageSystemChat($"A new player has queued for a{(eventType.ToLower().Equals("ffa") ? "n" : "")} {eventType} arena match. There {(queueCount > 1 ? "are" : "is")} currently {queueCount} player{(queueCount > 1 ? "s" : "")} queued for {eventType}", ChatMessageType.Broadcast));
            }

            return true;
        }

        public static bool RemovePlayerFromQueue(uint characterId)
        {
            if (queuedPlayers.ContainsKey(characterId))
            {
                queuedPlayers.Remove(characterId);
                return true;
            }

            return false;
        }

        public static bool RemoveTeamFromQueue(Guid teamGuid)
        {
            var teamPlayers = queuedPlayers.Values.Where(x => x.TeamGuid?.Equals(teamGuid) ?? false);
            if (teamPlayers != null && teamPlayers.Count() > 0)
            {
                foreach (var teamPlayer in teamPlayers)
                {
                    queuedPlayers.Remove(teamPlayer.CharacterId);
                }
            }

            return false;
        }

        public static void ReQueuePlayer(ArenaPlayer player)
        {
            player.EventId = null;
            player.TeamGuid = null;
            if (!queuedPlayers.ContainsKey(player.CharacterId))
            {
                queuedPlayers.Add(player.CharacterId, player);
            }
        }

        public static ArenaEvent MatchMake(List<string> supportedEventTypes)
        {
            return MatchMake(supportedEventTypes, new List<uint>());
        }

        public static ArenaEvent MatchMake(List<string> supportedEventTypes, List<uint> excludedPlayers)
        {
            if (excludedPlayers == null)
            {
                excludedPlayers = new List<uint>();
            }

            List<ArenaPlayer> queueCopy = new List<ArenaPlayer>();
            queueCopy.AddRange(queuedPlayers.Values);

            foreach (var arenaPlayer in queueCopy)
            {
                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                bool isPlayerValidState = true;
                if (player != null)
                {
                    if (!player.IsPK)
                    {
                        isPlayerValidState = false;
                    }
                    else if (player.PKTimerActive)
                    {
                        isPlayerValidState = false;
                    }
                    else if (player.IsArenaObserver || player.IsPendingArenaObserver)
                    {
                        isPlayerValidState = false;
                    }
                }
                else
                {
                    isPlayerValidState = false;
                }

                if (!isPlayerValidState)
                {
                    if (arenaPlayer.EventType.ToLower().Equals("group"))
                    {
                        var teamMembers = queuedPlayers.Values.Where(x => x.TeamGuid == arenaPlayer.TeamGuid && x.CharacterId != arenaPlayer.CharacterId);
                        if (teamMembers != null)
                        {
                            foreach (var teamMember in teamMembers)
                            {
                                queuedPlayers.Remove(teamMember.CharacterId);
                                var teamMemberPlayer = PlayerManager.GetOnlinePlayer(teamMember.CharacterId);
                                if (teamMemberPlayer != null)
                                {
                                    teamMemberPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been removed from the arena queue because during match making one of your team mates was found to be either watching another arena event, is not PK status or is PK tagged.  Please join the queue again when your entire team is in a valid state.", ChatMessageType.System));
                                }
                            }
                        }
                    }

                    if (player != null)
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been removed from the arena queue because during match making you were found to be either watching another arena event, are not PK status or you are PK tagged.  Please join the queue again when you're in a valid state.", ChatMessageType.System));

                    queuedPlayers.Remove(arenaPlayer.CharacterId);
                }
            }

            var firstArenaPlayer = queuedPlayers.Values?
                .Where(x => supportedEventTypes.Contains(x.EventType.ToLower()) && !excludedPlayers.Contains(x.CharacterId))?
                .OrderBy(x => x.CreateDateTime)?
                .FirstOrDefault();

            if (firstArenaPlayer != null)
            {
                uint maxMatchLevel = 275;
                uint minMatchLevel = 1;
                if (firstArenaPlayer.CharacterLevel >= 150)
                {
                    maxMatchLevel = 275;
                    minMatchLevel = Math.Max(firstArenaPlayer.CharacterLevel - 75, 130);
                }
                else if (firstArenaPlayer.CharacterLevel >= 80)
                {
                    maxMatchLevel = firstArenaPlayer.CharacterLevel + 40;
                    minMatchLevel = Math.Max(firstArenaPlayer.CharacterLevel - 40, 60);
                }
                else if (firstArenaPlayer.CharacterLevel < 80)
                {
                    maxMatchLevel = firstArenaPlayer.CharacterLevel + 20;
                    // CharacterLevel is unsigned; do the subtraction in signed space so the
                    // Math.Max(..., 1) floor holds for players below level 20 instead of underflowing.
                    minMatchLevel = (uint)Math.Max((int)firstArenaPlayer.CharacterLevel - 20, 1);
                }

                var otherPlayers = queuedPlayers.Values?
                .Where(x =>
                        firstArenaPlayer.EventType.Equals(x.EventType) &&
                        x.CharacterId != firstArenaPlayer.CharacterId &&
                        x.CharacterLevel <= maxMatchLevel &&
                        x.CharacterLevel >= minMatchLevel &&
                        (PropertyManager.GetBool("arena_allow_same_ip_match").Item || !firstArenaPlayer.PlayerIP.Equals(x.PlayerIP)))?
                .OrderBy(x => x.CreateDateTime);

                bool weHaveEnoughPlayers = false;
                List<ArenaPlayer> finalPlayerList = new List<ArenaPlayer>();

                if (otherPlayers != null && otherPlayers.Count() > 0)
                {
                    switch (firstArenaPlayer.EventType.ToLower())
                    {
                        case "1v1":
                            weHaveEnoughPlayers = true;
                            finalPlayerList.Add(firstArenaPlayer);
                            finalPlayerList.Add(otherPlayers.First());
                            foreach (var player in finalPlayerList)
                            {
                                player.TeamGuid = Guid.NewGuid();
                            }
                            break;
                        case "2v2":
                            if (otherPlayers.Count() >= 3)
                            {
                                var foundFirstTeamMatch = false;

                                var firstPlayer = PlayerManager.GetOnlinePlayer(firstArenaPlayer.CharacterId);
                                Fellowship firstPlayerFellowship = null;
                                if (firstPlayer != null)
                                {
                                    firstPlayerFellowship = firstPlayer.Fellowship;
                                }

                                if (firstPlayerFellowship != null)
                                {
                                    foreach (var otherArenaPlayer in otherPlayers)
                                    {
                                        var otherPlayer = PlayerManager.GetOnlinePlayer(otherArenaPlayer.CharacterId);
                                        if (otherPlayer != null && firstPlayerFellowship.FellowshipMembers.ContainsKey(otherPlayer.Guid.Full))
                                        {
                                            foundFirstTeamMatch = true;
                                            firstArenaPlayer.TeamGuid = Guid.NewGuid();
                                            otherArenaPlayer.TeamGuid = firstArenaPlayer.TeamGuid;
                                            finalPlayerList.Add(firstArenaPlayer);
                                            finalPlayerList.Add(otherArenaPlayer);
                                            break;
                                        }
                                    }
                                }

                                if (!foundFirstTeamMatch)
                                {
                                    var sameAllegQueuedPlayers = otherPlayers.Where(x => x.MonarchId == firstArenaPlayer.MonarchId)?.OrderBy(x => x.CreateDateTime);
                                    if (sameAllegQueuedPlayers != null && sameAllegQueuedPlayers.Any())
                                    {
                                        foundFirstTeamMatch = true;
                                        var allegTeamMate = sameAllegQueuedPlayers.First();
                                        firstArenaPlayer.TeamGuid = Guid.NewGuid();
                                        allegTeamMate.TeamGuid = firstArenaPlayer.TeamGuid;
                                        finalPlayerList.Add(firstArenaPlayer);
                                        finalPlayerList.Add(allegTeamMate);
                                    }
                                }

                                var foundSecondTeamMatch = false;
                                var secondTeamCandidates = otherPlayers.Where(x => x.CharacterId != firstArenaPlayer.CharacterId && !x.TeamGuid.HasValue)?.OrderBy(x => x.CreateDateTime);

                                foreach (var secondTeamCandidate in secondTeamCandidates)
                                {
                                    var secondTeamLeaderPlayer = PlayerManager.GetOnlinePlayer(secondTeamCandidate.CharacterId);
                                    Fellowship secondTeamLeaderFellowship = null;
                                    if (secondTeamLeaderPlayer != null)
                                    {
                                        secondTeamLeaderFellowship = secondTeamLeaderPlayer.Fellowship;
                                    }

                                    if (secondTeamLeaderFellowship != null)
                                    {
                                        foreach (var otherArenaPlayer in secondTeamCandidates.Where(x => x.CharacterId != secondTeamCandidate.CharacterId))
                                        {
                                            var otherPlayer = PlayerManager.GetOnlinePlayer(otherArenaPlayer.CharacterId);
                                            if (otherPlayer != null && secondTeamLeaderFellowship.FellowshipMembers.ContainsKey(otherPlayer.Guid.Full))
                                            {
                                                foundSecondTeamMatch = true;
                                                secondTeamCandidate.TeamGuid = Guid.NewGuid();
                                                otherArenaPlayer.TeamGuid = secondTeamCandidate.TeamGuid;
                                                finalPlayerList.Add(secondTeamCandidate);
                                                finalPlayerList.Add(otherArenaPlayer);
                                                break;
                                            }
                                        }
                                    }

                                    if (!foundSecondTeamMatch)
                                    {
                                        var sameAllegQueuedPlayers = secondTeamCandidates.Where(x => x.CharacterId != secondTeamCandidate.CharacterId && x.MonarchId == secondTeamCandidate.MonarchId)?.OrderBy(x => x.CreateDateTime);
                                        if (sameAllegQueuedPlayers != null && sameAllegQueuedPlayers.Any())
                                        {
                                            foundSecondTeamMatch = true;
                                            var allegTeamMate = sameAllegQueuedPlayers.First();
                                            secondTeamCandidate.TeamGuid = Guid.NewGuid();
                                            allegTeamMate.TeamGuid = secondTeamCandidate.TeamGuid;
                                            finalPlayerList.Add(secondTeamCandidate);
                                            finalPlayerList.Add(allegTeamMate);
                                            break;
                                        }
                                    }
                                }

                                if (!foundFirstTeamMatch)
                                {
                                    firstArenaPlayer.TeamGuid = Guid.NewGuid();
                                    var firstPlayerTeamMate = otherPlayers.Where(x => x.CharacterId != firstArenaPlayer.CharacterId && !x.TeamGuid.HasValue).OrderBy(x => Guid.NewGuid()).First();
                                    firstPlayerTeamMate.TeamGuid = firstArenaPlayer.TeamGuid;
                                    finalPlayerList.Add(firstArenaPlayer);
                                    finalPlayerList.Add(firstPlayerTeamMate);
                                    foundFirstTeamMatch = true;
                                }

                                if (!foundSecondTeamMatch)
                                {
                                    var secondTeamPlayers = otherPlayers.Where(x => !x.TeamGuid.HasValue).Take(2);
                                    var secondTeamGuid = Guid.NewGuid();
                                    foreach (var secondTeamPlayer in secondTeamPlayers)
                                    {
                                        secondTeamPlayer.TeamGuid = secondTeamGuid;
                                        finalPlayerList.Add(secondTeamPlayer);
                                    }

                                    foundSecondTeamMatch = true;
                                }

                                if (foundFirstTeamMatch && foundSecondTeamMatch)
                                {
                                    weHaveEnoughPlayers = true;
                                }
                            }
                            break;
                        case "ffa":

                            if (otherPlayers.Count() >= 9 ||
                                (otherPlayers.Count() >= 6 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-1)) ||
                                (otherPlayers.Count() >= 5 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-2)) ||
                                (otherPlayers.Count() >= 4 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-3)))
                            {
                                finalPlayerList.Add(firstArenaPlayer);

                                foreach (var player in otherPlayers)
                                {
                                    if (finalPlayerList.Count(x => x.MonarchId == player.MonarchId) <= 1)
                                    {
                                        finalPlayerList.Add(player);
                                    }

                                    if (finalPlayerList.Count() >= 10)
                                        break;
                                }

                                if (finalPlayerList.Count() >= 10 ||
                                    (finalPlayerList.Count() >= 7 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-1)) ||
                                    (finalPlayerList.Count() >= 6 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-2)) ||
                                    (finalPlayerList.Count() >= 5 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-3)))
                                {
                                    weHaveEnoughPlayers = true;
                                    foreach (var player in finalPlayerList)
                                    {
                                        player.TeamGuid = Guid.NewGuid();
                                    }
                                }
                            }

                            break;
                        case "tugak":

                            if (otherPlayers.Count() >= 9 ||
                                (otherPlayers.Count() >= 8 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-1)) ||
                                (otherPlayers.Count() >= 7 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-2)) ||
                                (otherPlayers.Count() >= 6 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-3)) ||
                                (otherPlayers.Count() >= 5 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-4)) ||
                                (otherPlayers.Count() >= 4 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-5)))
                            {
                                finalPlayerList.Add(firstArenaPlayer);

                                foreach (var player in otherPlayers)
                                {
                                    finalPlayerList.Add(player);
                                    if (finalPlayerList.Count() >= 15)
                                        break;
                                }

                                if (finalPlayerList.Count() >= 10 ||
                                    (finalPlayerList.Count() >= 9 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-1)) ||
                                    (finalPlayerList.Count() >= 8 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-2)) ||
                                    (finalPlayerList.Count() >= 7 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-3)) ||
                                    (finalPlayerList.Count() >= 6 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-4)) ||
                                    (finalPlayerList.Count() >= 5 && firstArenaPlayer.CreateDateTime < DateTime.Now.AddMinutes(-5)))
                                {
                                    weHaveEnoughPlayers = true;
                                    foreach (var player in finalPlayerList)
                                    {
                                        player.TeamGuid = Guid.NewGuid();
                                    }
                                }
                            }

                            break;
                        case "group":

                            var firstPlayerTeamMembers = queueCopy.Where(x => x.TeamGuid == firstArenaPlayer.TeamGuid);
                            Guid? secondTeamGuidGroup = null;

                            var otherTeamPlayers = otherPlayers
                            .Where(x => x.TeamGuid != firstArenaPlayer.TeamGuid)
                            .OrderBy(x => x.CreateDateTime);

                            if (otherTeamPlayers != null && otherTeamPlayers.Count() > 0)
                            {
                                foreach (var opponentPlayer in otherTeamPlayers)
                                {
                                    var opponentPlayerTeam = queueCopy.Where(x => x.TeamGuid == opponentPlayer.TeamGuid);

                                    if (opponentPlayerTeam == null || opponentPlayerTeam.Count() < 3)
                                        continue;

                                    if (opponentPlayer.MaxOpposingTeamSize < firstPlayerTeamMembers.Count() ||
                                        firstArenaPlayer.MaxOpposingTeamSize < opponentPlayerTeam.Count())
                                    {
                                        continue;
                                    }

                                    secondTeamGuidGroup = opponentPlayer.TeamGuid;
                                    weHaveEnoughPlayers = true;
                                }

                                if (weHaveEnoughPlayers && secondTeamGuidGroup.HasValue)
                                {
                                    foreach (var firstTeamPlayer in firstPlayerTeamMembers)
                                    {
                                        finalPlayerList.Add(firstTeamPlayer);
                                    }

                                    var secondTeamMembers = otherTeamPlayers.Where(x => x.TeamGuid == secondTeamGuidGroup.Value);
                                    foreach (var secondTeamPlayer in secondTeamMembers)
                                    {
                                        finalPlayerList.Add(secondTeamPlayer);
                                    }
                                }
                            }

                            break;
                    }

                    if (weHaveEnoughPlayers)
                    {
                        log.Info($"ArenaManager.MatchMake() - we have enough players to start the match");

                        var arenaEvent = new ArenaEvent();
                        arenaEvent.EventType = firstArenaPlayer.EventType;
                        arenaEvent.Players = finalPlayerList;
                        arenaEvent.Status = 1;
                        arenaEvent.CreatedDateTime = DateTime.Now;

                        foreach (var player in finalPlayerList)
                        {
                            queuedPlayers.Remove(player.CharacterId);
                        }

                        return arenaEvent;
                    }
                    else
                    {
                        excludedPlayers.Add(firstArenaPlayer.CharacterId);
                        return MatchMake(supportedEventTypes, excludedPlayers);
                    }
                }
                else
                {
                    excludedPlayers.Add(firstArenaPlayer.CharacterId);
                    return MatchMake(supportedEventTypes, excludedPlayers);
                }
            }
            else
            {
                return null;
            }
        }

        public static void CancelEvent(ArenaEvent arenaEvent)
        {
            log.Info($"ArenaManager.CancelEvent() - ArenaEventId = {arenaEvent.Id}, Location = {arenaEvent.Location}");

            try
            {
                arenaEvent.Status = -1;
                arenaEvent.EndDateTime = DateTime.Now;
                DatabaseManager.Log.SaveArenaEvent(arenaEvent);

                foreach (var arenaPlayer in arenaEvent.Players)
                {
                    var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                    if (player != null)
                    {
                        player.EnqueueBroadcast(new GameMessageSystemChat($"Your pending arena event has been cancelled.\nCancel Reason: {arenaEvent.CancelReason}", ChatMessageType.Broadcast));
                        if (player.IsPK && !player.PKTimerActive)
                        {
                            ReQueuePlayer(arenaPlayer);
                            player.EnqueueBroadcast(new GameMessageSystemChat("You have been added back to the front of the arena queue.", ChatMessageType.Broadcast));
                        }

                        if (player.CurrentLandblock?.IsArenaLandblock ?? false)
                        {
                            player.Teleport(player.Sanctuary);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in ArenaManager.CancelEvent. Ex: {ex}");
            }
        }

        public static void HandlePlayerDeath(uint victimId, uint killerId)
        {
            log.Info($"ArenaManager.HandlePlayerDeath victimId = {victimId}, killerId = {killerId}");
            ArenaPlayer victim = null;
            ArenaPlayer killer = null;
            ArenaLocation arenaLocation = null;
            foreach (var arena in arenaLocations.Values)
            {
                if (arena.HasActiveEvent)
                {
                    if (victim == null)
                    {
                        victim = arena.ActiveEvent.Players.FirstOrDefault(x => x.CharacterId == victimId);
                        killer = arena.ActiveEvent.Players.FirstOrDefault(x => x.CharacterId == killerId);
                        arenaLocation = arena;
                    }

                    if (victim != null && killer != null)
                        break;
                }
            }

            if (victim != null &&
                arenaLocation != null &&
                (arenaLocation.ActiveEvent.EventType.Equals("1v1") ||
                arenaLocation.ActiveEvent.EventType.Equals("2v2") ||
                arenaLocation.ActiveEvent.EventType.Equals("ffa") ||
                arenaLocation.ActiveEvent.EventType.Equals("group")))
            {
                victim.IsEliminated = true;
                victim.TotalDeaths++;

                var notEliminatedPlayers = arenaLocation.ActiveEvent.Players.Where(x => !x.IsEliminated);
                if (notEliminatedPlayers != null)
                {
                    victim.FinishPlace = notEliminatedPlayers.Count() + 1;
                }

                var victimPlayer = PlayerManager.GetOnlinePlayer(victim.CharacterId);
                if (victimPlayer != null)
                {
                    victimPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"You've been eliminated from an arena event by way of death.  You finished in {victim.FinishPlaceDisplay} place.  Stay online until the end of the match to receive rewards.", ChatMessageType.System));
                }
            }

            if (killer != null && !killer.IsEliminated)
            {
                killer.TotalKills++;
            }

            arenaLocation.CheckForArenaWinner(out Guid? winningTeamGuid);
            if (winningTeamGuid.HasValue)
            {
                arenaLocation.EndEventWithWinner(winningTeamGuid.Value);
            }
        }

        public static List<ArenaPlayer> GetQueuedPlayers()
        {
            return queuedPlayers.Values.ToList();
        }

        public static void PlayerCancel(uint characterId)
        {
            var player = PlayerManager.GetOnlinePlayer(characterId);

            var arenaPlayer = ArenaManager.GetArenaPlayerByCharacterId(characterId);

            if (arenaPlayer != null)
            {
                player?.EnqueueBroadcast(new GameMessageSystemChat("Your arena match is already started and cannot be cancelled.  To forfeit, you can leave the arena or log off.", ChatMessageType.Broadcast));
                return;
            }

            if (queuedPlayers.ContainsKey(characterId))
            {
                var thisQueuedPlayer = queuedPlayers[characterId];

                queuedPlayers.Remove(characterId);
                player?.EnqueueBroadcast(new GameMessageSystemChat("You have cancelled and been removed from the arena queue.", ChatMessageType.Broadcast));

                if (thisQueuedPlayer.EventType.ToLower().Equals("group"))
                {
                    var queuedTeammates = queuedPlayers.Where(x => x.Value.TeamGuid == thisQueuedPlayer.TeamGuid);
                    foreach (var teammatePlayer in queuedTeammates)
                    {
                        var thisPlayer = PlayerManager.GetOnlinePlayer(teammatePlayer.Key);

                        queuedPlayers.Remove(teammatePlayer.Key);
                        thisPlayer?.EnqueueBroadcast(new GameMessageSystemChat("You have been removed from the arena queue because one of your team members has cancelled", ChatMessageType.Broadcast));
                    }
                }
            }

            if (player?.IsArenaObserver ?? false)
            {
                ExitArenaObserverMode(player);
                var arenaLoc = arenaLocations.Values.FirstOrDefault(x => x.HasActiveEvent && (x.ActiveEvent.Observers?.Contains(player.Character.Id) ?? false));
                if (arenaLoc != null)
                {
                    arenaLoc.ActiveEvent.Observers.Remove(player.Character.Id);
                }
            }
        }

        public static ArenaPlayer GetArenaPlayerByCharacterId(uint characterId)
        {
            foreach (ArenaLocation loc in arenaLocations.Values)
            {
                if (loc.HasActiveEvent)
                {
                    foreach (var player in loc.ActiveEvent.Players)
                    {
                        if (player.CharacterId == characterId)
                        {
                            return player;
                        }
                    }
                }
            }

            return null;
        }

        public static ArenaEvent GetArenaEventByLandblock(uint landblockId)
        {
            if (arenaLocations.ContainsKey(landblockId))
            {
                if (arenaLocations[landblockId].HasActiveEvent)
                {
                    return arenaLocations[landblockId].ActiveEvent;
                }
            }

            return null;
        }

        public static string GetArenaNameByLandblock(uint landblockId)
        {
            if (arenaLocations.ContainsKey(landblockId))
            {
                return arenaLocations[landblockId].ArenaName;
            }

            return "";
        }

        public static bool IsValidEventType(string eventType)
        {
            switch (eventType.ToLower())
            {
                case "1v1":
                case "2v2":
                case "ffa":
                case "group":
                case "tugak":
                    return true;
                default:
                    return false;
            }
        }

        public static void ClearQueue(string eventType)
        {
            List<ArenaPlayer> playersToRemove = new List<ArenaPlayer>();

            foreach (var arenaPlayer in queuedPlayers.Values)
            {
                if (string.IsNullOrEmpty(eventType) || arenaPlayer.EventType.ToLower().Equals(eventType))
                {
                    playersToRemove.Add(arenaPlayer);
                }
            }

            foreach (var removedPlayer in playersToRemove)
            {
                queuedPlayers.Remove(removedPlayer.CharacterId);
                var player = PlayerManager.GetOnlinePlayer(removedPlayer.CharacterId);
                if (player != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been removed from the arena queue because an admin had to reset the queue for your event type.  Sorry for the inconvenience.", ChatMessageType.System));
                }
            }
        }

        public static void ObserveEvent(Player player, int eventID)
        {
            var onlinePlayer = PlayerManager.GetOnlinePlayer(player.Character.Id);
            if (onlinePlayer != null)
            {
                if (player.PKTimerActive)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been prevented from observing an arena event because you are currently PK tagged.  Please wait until you are not PK tagged to join an event.", ChatMessageType.System));
                    return;
                }
            }
            else
            {
                return;
            }

            if (ArenaManager.GetArenaPlayerByCharacterId(player.Character.Id) != null)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You cannot watch an arena match when you are already a player in an active arena match.", ChatMessageType.System));
                return;
            }

            if (player.IsArenaObserver)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You are already observing an arena event.  Type /arena cancel to leave the event.", ChatMessageType.System));
                return;
            }

            var arenaEvent = ArenaManager.GetActiveEvents()?.FirstOrDefault(x => x.Id == eventID);
            if (arenaEvent == null)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"There is no active arena match with EventID = {eventID}.", ChatMessageType.System));
                return;
            }

            // An event that hasn't started yet has no saved ID, so it matches
            // EventID 0.  Observing one would reveal the matchup to a third party
            // who could relay it to a participant looking to dodge a bad draw.
            if (!arenaEvent.HasStarted)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"That arena match hasn't started yet.  You can watch it once it begins.", ChatMessageType.System));
                return;
            }

            if (arenaEvent.Observers == null)
                arenaEvent.Observers = new List<uint>();

            arenaEvent.Observers.Add(player.Character.Id);

            EnterArenaObserverMode(player, arenaEvent);
        }

        public static void EnterArenaObserverMode(Player player, ArenaEvent arenaEvent)
        {
            if (player == null || arenaEvent == null)
                return;

            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You are about to enter Arena Observer mode. You will be frozen in place for a bit before you are teleported to the arena.", ChatMessageType.System));

            var actionChain = new ActionChain();

            actionChain.AddAction(player, () =>
            {
                player.IsPendingArenaObserver = true;
                player.IsFrozen = true;
                player.EnqueueBroadcastPhysicsState();
            });
            actionChain.AddDelaySeconds(10);
            actionChain.AddAction(player, () =>
            {
                player.HandleCloak();
            });
            actionChain.AddDelaySeconds(.5);
            actionChain.AddAction(player, () =>
            {
                player.IsGagged = true;
                player.IsFrozen = false;
                player.Attackable = false;
                player.EnqueueBroadcastPhysicsState();
            });
            actionChain.AddDelaySeconds(.5);
            actionChain.AddAction(player, () =>
            {
                var startingPositions = ArenaLocation.GetArenaLocationStartingPositions(arenaEvent.Location);
                if (startingPositions != null)
                {
                    player.Teleport(startingPositions[new Random().Next(startingPositions.Count)]);
                }
            });
            actionChain.AddAction(player, () =>
            {
                player.IsPendingArenaObserver = false;
                player.IsArenaObserver = true;
                player.RecallsDisabled = true;
                player.HandleActionChangeCombatMode(CombatMode.NonCombat);
                player.EnqueueBroadcastPhysicsState();
            });
            actionChain.EnqueueChain();
            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have entered Arena Observer mode. You can watch an arena match, but are not visible, cannot talk, and cannot interact with the world.\nTo exit use the command /arena cancel", ChatMessageType.System));
        }

        public static void ExitArenaObserverMode(Player player)
        {
            if (player == null)
                return;

            var actionChain = new ActionChain();

            actionChain.AddAction(player, () =>
            {
                player.IsFrozen = true;
                player.EnqueueBroadcastPhysicsState();
            });
            actionChain.AddDelaySeconds(3);
            actionChain.AddAction(player, () =>
            {
                player.Teleport(player.Sanctuary);
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You've exited observer mode for an arena match and are being teleported to your lifestone.", ChatMessageType.System));
            });
            actionChain.AddDelaySeconds(0.5);
            actionChain.AddAction(player, () =>
            {
                player.RecallsDisabled = false;
                player.IsFrozen = false;
                player.Attackable = true;
                if (player.GagDuration <= 0)
                {
                    player.IsGagged = false;
                }
                player.DeCloak();
                player.IsPendingArenaObserver = false;
                player.IsArenaObserver = false;
            });

            actionChain.EnqueueChain();
        }

        public static void DispelArenaRares(Player player)
        {
            if (player == null)
                return;

            if (player.HasArenaRareDmgBuff)
            {
                if (player.EnchantmentManager.HasSpell(5978))
                {
                    var enchantment = player.EnchantmentManager.GetEnchantment(5978);
                    if (enchantment != null)
                    {
                        player.EnchantmentManager.Dispel(enchantment);
                    }
                }

                player.HasArenaRareDmgBuff = false;
            }

            if (player.HasArenaRareDmgReductionBuff)
            {
                if (player.EnchantmentManager.HasSpell(5192))
                {
                    var enchantment = player.EnchantmentManager.GetEnchantment(5192);
                    if (enchantment != null)
                    {
                        player.EnchantmentManager.Dispel(enchantment);
                    }
                }

                player.HasArenaRareDmgReductionBuff = false;
            }
        }
    }
}
