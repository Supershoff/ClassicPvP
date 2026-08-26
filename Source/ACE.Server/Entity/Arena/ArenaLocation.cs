using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using log4net;

using ACE.Common;
using ACE.Database;
using ACE.Database.Models.Log;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity.PKQuests;
using ACE.Server.Factories;
using PKQuestDefs = ACE.Server.Entity.PKQuests.PKQuests;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Handlers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity
{
    public class ArenaLocation
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public uint LandblockId { get; set; }

        public List<string> SupportedEventTypes { get; set; }

        public ArenaEvent ActiveEvent { get; set; }

        public bool HasActiveEvent { get { return ActiveEvent != null; } }

        private DateTime lastTickDateTime = DateTime.MinValue;
        private DateTime lastEventTimerMessage = DateTime.MinValue;

        public string ArenaName { get; set; }

        // Tugak War permits damage only from the Health Bolt line (Martyr's Hecatomb I-VII, spell ids 2760-2766).
        // Classic has no Curse of Raven Fury ("Tugak") spell, so the Health Bolt line stands in for it.
        public static readonly HashSet<uint> TugakAllowedSpellIds = new HashSet<uint>
        {
            (uint)SpellId.HealthBolt1,
            (uint)SpellId.HealthBolt2,
            (uint)SpellId.HealthBolt3,
            (uint)SpellId.HealthBolt4,
            (uint)SpellId.HealthBolt5,
            (uint)SpellId.HealthBolt6,
            (uint)SpellId.HealthBolt7,
        };

        /// <summary>
        /// Returns true if the given spell id is a permitted damage spell in a Tugak War event
        /// (the Health Bolt / Martyr's Hecatomb line, tiers I-VII).
        /// </summary>
        public static bool IsTugakAllowedSpell(uint spellId) => TugakAllowedSpellIds.Contains(spellId);

        public ArenaLocation()
        {
            lastTickDateTime = DateTime.MinValue;
        }

        public void Tick()
        {
            if (!HasActiveEvent && lastTickDateTime > DateTime.Now.AddSeconds(-10))
                return;

            if (HasActiveEvent)
            {
                if (PropertyManager.GetBool("disable_arenas").Item)
                {
                    ActiveEvent.Status = -1;
                    EndEventCancel();
                    ClearPlayersFromArena();
                    ActiveEvent = null;
                    return;
                }

                if (!PropertyManager.GetBool("arena_allow_observers").Item)
                {
                    if ((ActiveEvent.Observers?.Count ?? 0) > 0)
                    {
                        List<uint> activeObservers = new List<uint>();
                        foreach (var observer in ActiveEvent.Observers)
                        {
                            var observerPlayer = PlayerManager.GetOnlinePlayer(observer);
                            if (observerPlayer != null)
                                activeObservers.Add(observerPlayer.Character.Id);
                        }

                        foreach (var observer in activeObservers)
                        {
                            ActiveEvent.Observers.Remove(observer);
                            var observerPlayer = PlayerManager.GetOnlinePlayer(observer);
                            if (observerPlayer != null)
                                ArenaManager.ExitArenaObserverMode(observerPlayer);
                        }
                    }
                }

                switch (ActiveEvent.Status)
                {
                    case -1:
                        ClearPlayersFromArena();
                        EndEventCancel();
                        ActiveEvent = null;
                        break;

                    case 1:
                        if (!ValidateArenaEventPlayers(out string resultMsg))
                        {
                            log.Info($"ArenaLocation.Tick() - {ArenaName} status = 1 - Invalid Player State, canceling event. Reason = {resultMsg}");
                            ActiveEvent.CancelReason = resultMsg;
                            ArenaManager.CancelEvent(ActiveEvent);
                        }

                        foreach (var arenaPlayer in ActiveEvent.Players)
                        {
                            var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                            if (player != null)
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"{player.Name} - You have been matched for a {ActiveEvent.EventTypeDisplay} arena event.  Prepare yourself, you will be teleported to the arena shortly.", ChatMessageType.System));
                        }

                        ActiveEvent.PreEventCountdownStartDateTime = DateTime.Now;
                        ActiveEvent.Status = ActiveEvent.Status == -1 ? -1 : 2;
                        break;

                    case 2:
                        if (!ValidateArenaEventPlayers(out string resultMsg2))
                        {
                            log.Info($"ArenaLocation.Tick() - {ArenaName} status = 2 - Invalid Player State, canceling event. Reason = {resultMsg2}");
                            ActiveEvent.CancelReason = resultMsg2;
                            ArenaManager.CancelEvent(ActiveEvent);
                            return;
                        }

                        if (DateTime.Now.AddSeconds(-10) > ActiveEvent.PreEventCountdownStartDateTime)
                        {
                            List<Position> positions = GetArenaLocationStartingPositions(ActiveEvent.Location);
                            var playerList = new List<Player>();
                            foreach (var arenaPlayer in ActiveEvent.Players)
                            {
                                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                                if (player != null)
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Players are now being teleported to the {ActiveEvent.EventTypeDisplay} arena event.\nAfter a brief pause to allow everyone to arrive, the event will begin.", ChatMessageType.System));
                                    playerList.Add(player);
                                }
                                else
                                {
                                    log.Info($"ArenaLocation.Tick() - {ArenaName} status = 2 - PreEventCountdown is complete but player {arenaPlayer.CharacterName} is offline.  Canceling the event.");
                                    ActiveEvent.CancelReason = $"{arenaPlayer.CharacterName} logged off before being teleported to the arena";
                                    ArenaManager.CancelEvent(ActiveEvent);
                                    break;
                                }
                            }

                            if (ActiveEvent.EventType.Equals("2v2"))
                                CreateTeamFellowships();

                            foreach (var player in playerList)
                            {
                                if (player.IsArenaObserver || player.IsPendingArenaObserver || player.CloakStatus == CloakStatus.On)
                                {
                                    player.RecallsDisabled = false;
                                    player.IsFrozen = false;
                                    player.Attackable = true;
                                    if (player.GagDuration <= 0)
                                        player.IsGagged = false;
                                    player.DeCloak();
                                    player.IsPendingArenaObserver = false;
                                    player.IsArenaObserver = false;
                                }
                            }

                            if (ActiveEvent.EventType.ToLower().Equals("2v2") ||
                                ActiveEvent.EventType.ToLower().Equals("group"))
                            {
                                var teamPositions = new Dictionary<Guid, Position>();
                                foreach (var arenaPlayer in ActiveEvent.Players)
                                {
                                    if (arenaPlayer.TeamGuid.HasValue && !teamPositions.Keys.Contains(arenaPlayer.TeamGuid.Value))
                                    {
                                        var posIndex = new Random().Next(positions.Count());
                                        teamPositions.Add(arenaPlayer.TeamGuid.Value, positions[posIndex]);
                                    }
                                }

                                foreach (var teamPosition in teamPositions)
                                {
                                    var teamArenaPlayers = ActiveEvent.Players.Where(x => x.TeamGuid == teamPosition.Key);
                                    foreach (var teamArenaPlayer in teamArenaPlayers)
                                    {
                                        var teamPlayer = playerList.FirstOrDefault(x => x.Character.Id == teamArenaPlayer.CharacterId);
                                        if (teamPlayer != null)
                                        {
                                            log.Info($"ArenaLocation.Tick() - {ArenaName} status = 2 - teleporting {teamPlayer.Name} to position {teamPosition.Value.ToLOCString}");
                                            teamPlayer.Teleport(teamPosition.Value);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < playerList.Count; i++)
                                {
                                    var j = i < positions.Count ? i : positions.Count - 1;
                                    log.Info($"ArenaLocation.Tick() - {ArenaName} status = 2 - teleporting {playerList[i].Name} to position {positions[j].ToLOCString}");
                                    playerList[i].Teleport(positions[j]);
                                }
                            }

                            ActiveEvent.CountdownStartDateTime = DateTime.Now;
                            ActiveEvent.Status = ActiveEvent.Status == -1 ? -1 : 3;
                        }
                        break;

                    case 3:
                        if (DateTime.Now.AddSeconds(-15) > ActiveEvent.CountdownStartDateTime)
                        {
                            log.Info($"ArenaLocation.Tick() - {ArenaName} status = 3, countdown is complete, start the fight");
                            StartEvent();
                            foreach (var arenaPlayer in ActiveEvent.Players)
                            {
                                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                                if (player != null)
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The event has started!\nRemaining Event Time: {ActiveEvent.TimeRemainingDisplay}", ChatMessageType.System));
                                    lastEventTimerMessage = DateTime.Now;
                                }
                            }
                        }
                        break;

                    case 4:
                        var notEliminatedPlayers = ActiveEvent.Players.Where(x => !x.IsEliminated)?.ToList() ?? new List<ArenaPlayer>();
                        foreach (var arenaPlayer in ActiveEvent.Players)
                        {
                            if (!arenaPlayer.IsEliminated)
                            {
                                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                                if (player == null)
                                {
                                    arenaPlayer.IsEliminated = true;
                                    arenaPlayer.IsDisqualified = true;
                                    notEliminatedPlayers.Remove(arenaPlayer);
                                }
                                else
                                {
                                    if (!player.IsPK)
                                    {
                                        arenaPlayer.IsEliminated = true;
                                        ArenaManager.DispelArenaRares(player);
                                        notEliminatedPlayers.Remove(arenaPlayer);
                                    }

                                    if (player.Location.Landblock != ActiveEvent.Location)
                                    {
                                        arenaPlayer.IsEliminated = true;
                                        ArenaManager.DispelArenaRares(player);
                                        notEliminatedPlayers.Remove(arenaPlayer);
                                        if (!player.IsInDeathProcess)
                                        {
                                            arenaPlayer.IsDisqualified = true;
                                            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been disqualified from the {arenaPlayer.EventType} arena match because you left the arena.", ChatMessageType.System));
                                        }
                                    }
                                }

                                if (arenaPlayer.IsEliminated && !arenaPlayer.IsDisqualified)
                                {
                                    arenaPlayer.FinishPlace = notEliminatedPlayers.Count() + 1;
                                    if (player != null)
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been eliminated from the {ActiveEvent.EventTypeDisplay} arena match in {ArenaName}.  You finished in {arenaPlayer.FinishPlaceDisplay} place.", ChatMessageType.System));
                                }
                            }
                        }

                        if (ActiveEvent.WinningTeamGuid.HasValue)
                        {
                            log.Info($"ArenaLocation.Tick() - {ArenaName} status = 4, WinningTeamGuid is already set, ending event with winner");
                            EndEventWithWinner(ActiveEvent.WinningTeamGuid.Value);
                            break;
                        }
                        else
                        {
                            if (CheckForArenaWinner(out Guid? winningTeamGuid) && winningTeamGuid.HasValue)
                            {
                                log.Info($"ArenaLocation.Tick() - {ArenaName} status = 4, winner found, ending event with winner");
                                EndEventWithWinner(winningTeamGuid.Value);
                                break;
                            }
                        }

                        if (!ActiveEvent.IsOvertime && ActiveEvent.TimeRemaining <= TimeSpan.Zero)
                        {
                            log.Info($"ArenaLocation.Tick() - {ArenaName} status = 4, event time limit exceeded, going to overtime");
                            ActiveEvent.IsOvertime = true;
                            foreach (var arenaPlayer in ActiveEvent.Players)
                            {
                                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                                if (player != null)
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"OVERTIME! Chugs are disabled and all healing will be incrementally less effective as time goes on.  Overtime Remaining: {ActiveEvent.OvertimeRemainingDisplay}", ChatMessageType.System));
                            }

                            if (ActiveEvent.Observers != null)
                            {
                                foreach (var observer in ActiveEvent.Observers)
                                {
                                    var player = PlayerManager.GetOnlinePlayer(observer);
                                    if (player != null)
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"OVERTIME! Chugs are disabled and all healing will be incrementally less effective as time goes on.  Overtime Remaining: {ActiveEvent.OvertimeRemainingDisplay}", ChatMessageType.System));
                                }
                            }
                            break;
                        }

                        if (ActiveEvent.IsOvertime && ActiveEvent.OvertimeRemaining <= TimeSpan.Zero)
                        {
                            log.Info($"ArenaLocation.Tick() - {ArenaName} status = 4, event time limit exceeded, ending in draw");
                            EndEventTimelimitExceeded();
                            break;
                        }

                        if (DateTime.Now.AddSeconds(-30) > lastEventTimerMessage)
                        {
                            foreach (var arenaPlayer in ActiveEvent.Players)
                            {
                                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                                if (player != null)
                                {
                                    if (ActiveEvent.IsOvertime)
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Overtime Remaining: {ActiveEvent.OvertimeRemainingDisplay}\nHealing Reduction: {ActiveEvent.OvertimeHealingModifierDisplay}", ChatMessageType.System));
                                    else
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Remaining Event Time: {ActiveEvent.TimeRemainingDisplay}", ChatMessageType.System));
                                }
                            }

                            if (ActiveEvent.Observers != null)
                            {
                                foreach (var observer in ActiveEvent.Observers)
                                {
                                    var player = PlayerManager.GetOnlinePlayer(observer);
                                    if (player != null)
                                    {
                                        if (ActiveEvent.IsOvertime)
                                            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Overtime Remaining: {ActiveEvent.OvertimeRemainingDisplay}\nHealing Reduction: {ActiveEvent.OvertimeHealingModifierDisplay}", ChatMessageType.System));
                                        else
                                            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Remaining Event Time: {ActiveEvent.TimeRemainingDisplay}", ChatMessageType.System));
                                    }
                                }
                            }

                            lastEventTimerMessage = DateTime.Now;
                        }
                        break;

                    case 5:
                    case 6:
                        if (DateTime.Now.AddSeconds(-45) > ActiveEvent.EndDateTime)
                        {
                            foreach (var arenaPlayer in ActiveEvent.Players)
                            {
                                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                                if (player != null)
                                {
                                    if (player.CurrentLandblock?.IsArenaLandblock ?? false)
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Thank you for playing arenas.  You've loitered a bit too long after the event.  Have a nice trip to your Lifestone!", ChatMessageType.System));
                                        player.Teleport(player.Sanctuary);
                                    }
                                }
                            }

                            ActiveEvent = null;
                        }
                        else
                        {
                            bool hasPlayers = false;
                            foreach (var arenaPlayer in ActiveEvent.Players)
                            {
                                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                                if (player != null && player.Location.Landblock == LandblockId)
                                    hasPlayers = true;
                            }

                            if (!hasPlayers)
                                ActiveEvent = null;
                        }
                        break;

                    default:
                        break;
                }
            }
            else
            {
                ClearPlayersFromArena();
                MatchMake();
            }

            lastTickDateTime = DateTime.Now;
        }

        private void MatchMake()
        {
            var arenaEvent = ArenaManager.MatchMake(SupportedEventTypes);
            if (arenaEvent != null)
            {
                ActiveEvent = arenaEvent;
                ActiveEvent.Location = LandblockId;
            }
        }

        private bool ValidateArenaEventPlayers(out string resultMsg)
        {
            var isPlayerNpk = false;
            var isPlayerPkTagged = false;
            var isPlayerMissing = false;
            resultMsg = "";
            foreach (var arenaPlayer in ActiveEvent.Players)
            {
                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                if (player != null)
                {
                    if (!player.IsPK)
                    {
                        isPlayerNpk = true;
                        resultMsg += $"\n{arenaPlayer.CharacterName} is not PK";
                    }
                    else if (player.PKTimerActive)
                    {
                        isPlayerPkTagged = true;
                        resultMsg += $"\n{arenaPlayer.CharacterName} is PvP tagged";
                    }
                }
                else
                {
                    isPlayerMissing = true;
                    resultMsg += $"\n{arenaPlayer.CharacterName} is not online";
                }
            }

            if (resultMsg.StartsWith("\n"))
                resultMsg = resultMsg.Remove(0, 1);

            return !isPlayerMissing && !isPlayerNpk && !isPlayerPkTagged;
        }

        public void CreateTeamFellowships()
        {
            if (ActiveEvent == null || ActiveEvent.Players == null || ActiveEvent.Players.Count < 4)
                return;

            List<Guid> teamIds = new List<Guid>();
            foreach (var arenaPlayer in ActiveEvent.Players)
            {
                if (arenaPlayer.TeamGuid.HasValue && !teamIds.Contains(arenaPlayer.TeamGuid.Value))
                    teamIds.Add(arenaPlayer.TeamGuid.Value);
            }

            foreach (var teamId in teamIds)
            {
                var teamArenaPlayers = ActiveEvent.Players.Where(x => x.TeamGuid == teamId);
                var teamLeadArenaPlayer = teamArenaPlayers?.FirstOrDefault();

                if (teamLeadArenaPlayer != null)
                {
                    var teamLeadPlayer = PlayerManager.GetOnlinePlayer(teamLeadArenaPlayer.CharacterId);
                    if (teamLeadPlayer != null)
                    {
                        teamLeadPlayer.FellowshipQuit(true);
                        teamLeadPlayer.FellowshipCreate(teamLeadPlayer.Name, false);

                        foreach (var teamArenaPlayer in teamArenaPlayers)
                        {
                            if (teamArenaPlayer.CharacterId == teamLeadArenaPlayer.CharacterId)
                                continue;

                            var teamPlayer = PlayerManager.GetOnlinePlayer(teamArenaPlayer.CharacterId);
                            if (teamPlayer != null)
                            {
                                teamPlayer.FellowshipQuit(true);
                                teamPlayer.SetCharacterOption(CharacterOption.AutomaticallyAcceptFellowshipRequests, true);
                                teamPlayer.SetCharacterOption(CharacterOption.IgnoreFellowshipRequests, false);
                                teamLeadPlayer.FellowshipRecruit(teamPlayer);
                            }
                        }
                    }
                }
            }
        }

        public bool CheckForArenaWinner(out Guid? winningTeamGuid)
        {
            winningTeamGuid = null;

            if (ActiveEvent == null || ActiveEvent.Players == null || ActiveEvent.Players.Count < 2 || ActiveEvent.Status < 4)
                return false;

            List<Guid> teamsStillAlive = new List<Guid>();
            foreach (var arenaPlayer in ActiveEvent.Players)
            {
                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                if (player != null && player.IsPK && (player.CurrentLandblock?.IsArenaLandblock ?? false))
                {
                    if (arenaPlayer.TeamGuid.HasValue && !teamsStillAlive.Contains(arenaPlayer.TeamGuid.Value))
                        teamsStillAlive.Add(arenaPlayer.TeamGuid.Value);
                }
            }

            if (teamsStillAlive.Count == 1)
            {
                if (ActiveEvent.EventType.Equals("ffa") || ActiveEvent.EventType.Equals("tugak"))
                {
                    var winner = ActiveEvent.Players.FirstOrDefault(x => x.TeamGuid == teamsStillAlive[0]);
                    if (winner != null)
                        winner.FinishPlace = 1;
                }

                winningTeamGuid = teamsStillAlive[0];
                return true;
            }

            if (teamsStillAlive.Count == 0)
            {
                winningTeamGuid = ActiveEvent.Players.First().TeamGuid;
                return true;
            }

            return false;
        }

        public void StartEvent()
        {
            ActiveEvent.StartDateTime = DateTime.Now;
            ActiveEvent.Status = ActiveEvent.Status == -1 ? -1 : 4;

            DatabaseManager.Log.SaveArenaEvent(ActiveEvent);

            var msg = $"Arena Match Started: Event Type = {ActiveEvent.EventTypeDisplay}, Players = {ActiveEvent.PlayersDisplay}, EventID = {ActiveEvent.Id}. To watch the event, type /arena watch {ActiveEvent.Id}";
            PlayerManager.BroadcastToAll(new GameMessageSystemChat(msg, ChatMessageType.Broadcast));
            try
            {
                var webhookUrl = PropertyManager.GetString("arena_globals_webhook").Item;
                if (!string.IsNullOrEmpty(webhookUrl))
                    _ = TurbineChatHandler.SendWebhookedChat("Arenas", msg, webhookUrl, "Global");
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed sending Arena global message to webhook. Ex:{0}", ex);
            }
        }

        public void EndEventWithWinner(Guid winningTeamGuid)
        {
            log.Info($"ArenaLocation.EndEventWithWinner() - {ArenaName} - WinningTeamGuid = {winningTeamGuid}");

            if (ActiveEvent.Status > 4)
                return;

            ActiveEvent.Status = 5;
            ActiveEvent.EndDateTime = DateTime.Now;
            ActiveEvent.WinningTeamGuid = winningTeamGuid;

            var livingWinners = ActiveEvent.Players.Where(x => x.TeamGuid == winningTeamGuid && !x.IsEliminated && !x.IsDisqualified);
            if (livingWinners != null)
            {
                foreach (var winner in livingWinners)
                    winner.FinishPlace = 1;
            }

            DatabaseManager.Log.SaveArenaEvent(ActiveEvent);

            string winnerList = "";
            var winners = ActiveEvent.Players.Where(x => x.TeamGuid == winningTeamGuid)?.ToList();
            string loserList = "";
            var losers = ActiveEvent.Players.Where(x => x.TeamGuid != winningTeamGuid)?.ToList();

            bool sameClanFight = false;
            foreach (var winner in winners)
            {
                var clanMatesOnOtherTeam = losers.Where(x => x.MonarchId == winner.MonarchId);
                if (clanMatesOnOtherTeam != null && clanMatesOnOtherTeam.Count() > 0)
                {
                    sameClanFight = true;
                    break;
                }
            }

            winners.ForEach(x => winnerList += string.IsNullOrEmpty(winnerList) ? x.CharacterName : $", {x.CharacterName}");
            losers.ForEach(x => loserList += string.IsNullOrEmpty(loserList) ? x.CharacterName : $", {x.CharacterName}");

            bool underageViolation = false;
            var underageCount = 0;
            foreach (var arenaPlayer in ActiveEvent.Players)
            {
                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                if (player != null && player.Age <= PropertyManager.GetLong("arenas_reward_min_age").Item)
                    underageCount++;
            }

            if (underageCount > 0 && (ActiveEvent.EventType.Equals("1v1") || ActiveEvent.EventType.Equals("2v2")))
                underageViolation = true;
            else if (underageCount > 2 && ActiveEvent.EventType.Equals("ffa"))
                underageViolation = true;

            // Build per-player new ELO values (1v1 and 2v2).
            // FFA / Tugak use placement points instead; their entry stays null here.
            var newEloMap = new Dictionary<uint, uint>();

            if (ActiveEvent.EventType.Equals("1v1"))
            {
                var winner = winners.FirstOrDefault();
                var loser  = losers.FirstOrDefault();
                if (winner != null && loser != null)
                {
                    var winnerElo = DatabaseManager.Log.GetCharacterArenaStatsByEvent(winner.CharacterId, "1v1")?.Elo ?? 1500;
                    var loserElo  = DatabaseManager.Log.GetCharacterArenaStatsByEvent(loser.CharacterId,  "1v1")?.Elo ?? 1500;
                    var rankChange = ArenaRanking.GetRankChange(winnerElo, loserElo, 32);

                    newEloMap[winner.CharacterId] = (uint)Math.Max(1, (int)winnerElo + rankChange);
                    newEloMap[loser.CharacterId]  = (uint)Math.Max(1, (int)loserElo  - rankChange);
                }
            }
            else if (ActiveEvent.EventType.Equals("2v2"))
            {
                // Average-ELO team comparison; each individual gains/loses the same delta.
                double winnerTeamEloAvg = winners.Average(w =>
                    (double)(DatabaseManager.Log.GetCharacterArenaStatsByEvent(w.CharacterId, "2v2")?.Elo ?? 1500));
                double loserTeamEloAvg  = losers.Average(l =>
                    (double)(DatabaseManager.Log.GetCharacterArenaStatsByEvent(l.CharacterId,  "2v2")?.Elo ?? 1500));

                var rankChange = ArenaRanking.GetRankChange((uint)winnerTeamEloAvg, (uint)loserTeamEloAvg, 32);

                foreach (var w in winners)
                {
                    var currentElo = DatabaseManager.Log.GetCharacterArenaStatsByEvent(w.CharacterId, "2v2")?.Elo ?? 1500;
                    newEloMap[w.CharacterId] = (uint)Math.Max(1, (int)currentElo + rankChange);
                }
                foreach (var l in losers)
                {
                    var currentElo = DatabaseManager.Log.GetCharacterArenaStatsByEvent(l.CharacterId, "2v2")?.Elo ?? 1500;
                    newEloMap[l.CharacterId] = (uint)Math.Max(1, (int)currentElo - rankChange);
                }

                // Update team-pair standings
                uint winnerTeamSurvived = (uint)winners.Count(w => !w.IsEliminated && !w.IsDisqualified);
                uint loserTeamSurvived  = 0;

                var winnersSorted = winners.OrderBy(w => w.CharacterId).ToList();
                var losersSorted  = losers .OrderBy(l => l.CharacterId).ToList();

                if (winnersSorted.Count == 2)
                {
                    var eloA = newEloMap.TryGetValue(winnersSorted[0].CharacterId, out var eA) ? eA : 1500u;
                    var eloB = newEloMap.TryGetValue(winnersSorted[1].CharacterId, out var eB) ? eB : 1500u;
                    var teamWinnerElo = (eloA + eloB) / 2;
                    DatabaseManager.Log.AddToArenaTeamStats(
                        winnersSorted[0].CharacterId, winnersSorted[0].CharacterName,
                        winnersSorted[1].CharacterId, winnersSorted[1].CharacterName,
                        1, 1, 0, 0, 0, winnerTeamSurvived, teamWinnerElo);
                }
                if (losersSorted.Count == 2)
                {
                    var teamLoserEloA = newEloMap.TryGetValue(losersSorted[0].CharacterId, out var eLa) ? eLa : 1500u;
                    var teamLoserEloB = newEloMap.TryGetValue(losersSorted[1].CharacterId, out var eLb) ? eLb : 1500u;
                    var teamLoserElo  = (teamLoserEloA + teamLoserEloB) / 2;
                    DatabaseManager.Log.AddToArenaTeamStats(
                        losersSorted[0].CharacterId, losersSorted[0].CharacterName,
                        losersSorted[1].CharacterId, losersSorted[1].CharacterName,
                        1, 0, 0, 1, 0, 0, teamLoserElo);
                }
            }

            foreach (var winner in winners)
            {
                uint? newElo = newEloMap.TryGetValue(winner.CharacterId, out var e) ? e : (uint?)null;

                // FFA / Tugak winners always finish 1st
                uint ffaPoints = (ActiveEvent.EventType.Equals("ffa") || ActiveEvent.EventType.Equals("tugak"))
                    ? ArenaRanking.GetFfaPlacementPoints(1)
                    : 0;

                // 2v2 survival: winner survived if not eliminated
                bool survived2v2 = ActiveEvent.EventType.Equals("2v2") && !winner.IsEliminated && !winner.IsDisqualified;

                DatabaseManager.Log.AddToArenaStats(
                    winner.CharacterId, winner.CharacterName, winner.EventType,
                    1, 1, 0, 0, 0,
                    winner.TotalDeaths, winner.TotalKills, winner.TotalDmgDealt, winner.TotalDmgReceived,
                    newElo, ffaPoints, survived2v2);

                var player = PlayerManager.GetOnlinePlayer(winner.CharacterId);
                if (player != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Congratulations, you've won the {ActiveEvent.EventTypeDisplay} arena event against {loserList}!\nIf you're still in {ArenaName} you have a short period before you're teleported to your Lifestone so hurry up and loot.", ChatMessageType.System));

                    var shouldReward = IsPlayerRewardEligible(player, winner, ActiveEvent.Players) && !underageViolation;

                    if (shouldReward)
                    {
                        switch (ActiveEvent.EventType)
                        {
                            case "1v1":
                            case "2v2":
                                // 1v1 winners receive 10% of a level; 2v2 winners 15%.
                                player.GrantLevelProportionalXpNoModifier(ActiveEvent.EventType.Equals("1v1") ? 0.1 : 0.15, 0, 0, XpType.PvP);

                                if (player.MaximumLuminance != null)
                                    player.GrantLuminance(30000, XpType.PvP, ShareType.None);

                                var pkTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PkTrophy);
                                pkTrophy?.SetStackSize(5);
                                if (pkTrophy != null && player.TryCreateInInventoryWithNetworking(pkTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(pkTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received 5 PK Trophies", ChatMessageType.Broadcast));
                                }

                                var arenaTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PhialOfBloodyTears);
                                arenaTrophy?.SetStackSize(1);
                                if (arenaTrophy != null && player.TryCreateInInventoryWithNetworking(arenaTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(arenaTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received a Phial of Bloody Tears", ChatMessageType.Broadcast));
                                }

                                var arenaKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                arenaKey?.SetStackSize(1);
                                if (arenaKey != null && player.TryCreateInInventoryWithNetworking(arenaKey))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(arenaKey));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                }

                                if (new Random().NextDouble() > 0.95)
                                {
                                    var bonusCount = new Random().Next(1, 3);
                                    for (int i = 0; i < bonusCount; i++)
                                    {
                                        var bonusKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                        bonusKey?.SetStackSize(1);
                                        if (bonusKey != null && player.TryCreateInInventoryWithNetworking(bonusKey))
                                        {
                                            player.Session.Network.EnqueueSend(new GameMessageCreateObject(bonusKey));
                                            player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received a bonus Darkbeat's Lost Storage Key", ChatMessageType.Broadcast));
                                        }
                                    }
                                }
                                break;

                            case "ffa":
                            case "tugak":
                                player.GrantLevelProportionalXpNoModifier(0.35, 0, 0, XpType.PvP);

                                if (player.MaximumLuminance != null)
                                    player.GrantLuminance(80000, XpType.PvP, ShareType.None);

                                var ffaWinnerPkTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PkTrophy);
                                ffaWinnerPkTrophy?.SetStackSize(5);
                                if (ffaWinnerPkTrophy != null && player.TryCreateInInventoryWithNetworking(ffaWinnerPkTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaWinnerPkTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received 5 PK Trophies", ChatMessageType.Broadcast));
                                }

                                var ffaWinnerArenaTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PhialOfBloodyTears);
                                ffaWinnerArenaTrophy?.SetStackSize(3);
                                if (ffaWinnerArenaTrophy != null && player.TryCreateInInventoryWithNetworking(ffaWinnerArenaTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaWinnerArenaTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received 3 Phials of Bloody Tears", ChatMessageType.Broadcast));
                                }

                                for (int i = 0; i < 5; i++)
                                {
                                    var ffaWinnerKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                    ffaWinnerKey?.SetStackSize(1);
                                    if (ffaWinnerKey != null && player.TryCreateInInventoryWithNetworking(ffaWinnerKey))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaWinnerKey));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received five of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                    }
                                }
                                break;

                            case "group":
                                var rewardMultiplier = winner.FinishPlace == 1 && !sameClanFight ? 3 : 1;
                                var groupWinXp = winner.FinishPlace == 1 && !sameClanFight ? 0.6 : 0.3;
                                player.GrantLevelProportionalXpNoModifier(groupWinXp, 0, 0, XpType.PvP);

                                if (player.MaximumLuminance != null)
                                    player.GrantLuminance(20000 * rewardMultiplier, XpType.PvP, ShareType.None);

                                var groupWinnerPkTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PkTrophy);
                                groupWinnerPkTrophy?.SetStackSize(5 * rewardMultiplier);
                                if (groupWinnerPkTrophy != null && player.TryCreateInInventoryWithNetworking(groupWinnerPkTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(groupWinnerPkTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have received {5 * rewardMultiplier} PK Trophies", ChatMessageType.Broadcast));
                                }

                                var groupWinnerArenaTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PhialOfBloodyTears);
                                groupWinnerArenaTrophy?.SetStackSize(1 * rewardMultiplier);
                                if (groupWinnerArenaTrophy != null && player.TryCreateInInventoryWithNetworking(groupWinnerArenaTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(groupWinnerArenaTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have received {1 * rewardMultiplier} Phials of Bloody Tears", ChatMessageType.Broadcast));
                                }

                                for (int i = 0; i < 2 * rewardMultiplier; i++)
                                {
                                    var groupWinnerKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                    groupWinnerKey?.SetStackSize(1);
                                    if (groupWinnerKey != null && player.TryCreateInInventoryWithNetworking(groupWinnerKey))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(groupWinnerKey));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                    }
                                }
                                break;
                        }

                        SetPlayerRewardLimitProperties(player, winner);
                    }

                    //Handle PK quests for winners
                    // Arena participation/win quests only require a legit cross-allegiance match
                    // (an opponent whose monarch differs from yours), not membership in a
                    // whitelisted allegiance. The whitelist gate is for open-world PK kill quests
                    // (see Player_Death.cs); applying it here silently denied 2v2/group/ffa/tugak
                    // credit to every unsworn or non-whitelisted player. Unsworn players carry their
                    // own character id as MonarchId, so this still blocks same-allegiance staged fights.
                    var winnerMonarchId = winner.MonarchId;
                    var hasDifferentAllegianceOpponent = losers.FirstOrDefault(x => x.MonarchId != winnerMonarchId) != null;
                    if (hasDifferentAllegianceOpponent || ActiveEvent.EventType.ToLower().Equals("1v1"))
                    {
                        player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateAnyArena);
                        player.CompletePkQuestTasks(PKQuestDefs.PKQuests_WinAnyArena);
                        player.CompletePkQuestTask("ARENA_DMG20K", (int)winner.TotalDmgDealt);
                        if (winner.TotalDmgReceived <= 800)
                            player.CompletePkQuestTask("ARENA_RECDMG800", 1);

                        switch (ActiveEvent.EventType)
                        {
                            case "1v1":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Participate1v1Arena);
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Win1v1Arena);
                                break;
                            case "2v2":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Participate2v2Arena);
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Win2v2Arena);
                                break;
                            case "ffa":
                                player.CompletePkQuestTask("ARENA_FFA_2");
                                player.CompletePkQuestTask("ARENA_FFA_WIN_1");
                                player.CompletePkQuestTask("ARENA_FFA_TOP3");
                                break;
                            case "tugak":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateTugakArena);
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_WinTugakArena);
                                player.CompletePkQuestTask("ARENA_TUGAK_TOP3");
                                break;
                            case "group":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateGroupArena);
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_WinGroupArena);
                                break;
                            default:
                                break;
                        }
                    }

                    ArenaManager.DispelArenaRares(player);
                }
            }

            foreach (var loser in losers)
            {
                bool isFFA      = loser.EventType.Equals("ffa") || loser.EventType.Equals("tugak");
                bool isOvertime = this.ActiveEvent.IsOvertime;
                bool isDraw     = (!isFFA && isOvertime) || (isFFA && loser.FinishPlace <= 3 && loser.FinishPlace > 0);

                uint? newElo = newEloMap.TryGetValue(loser.CharacterId, out var el) ? el : (uint?)null;

                uint ffaPoints = isFFA ? ArenaRanking.GetFfaPlacementPoints(loser.FinishPlace) : 0;

                DatabaseManager.Log.AddToArenaStats(
                    loser.CharacterId, loser.CharacterName, loser.EventType,
                    1, 0,
                    isDraw || isOvertime ? 1 : (uint)0,
                    isDraw || isOvertime ? 0 : (uint)1,
                    loser.FinishPlace == -1 ? 1 : (uint)0,
                    loser.TotalDeaths, loser.TotalKills, loser.TotalDmgDealt, loser.TotalDmgReceived,
                    newElo, ffaPoints);

                var player = PlayerManager.GetOnlinePlayer(loser.CharacterId);
                if (player != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Tough luck, you've lost the {ActiveEvent.EventTypeDisplay} arena event to {winnerList}\nIf you're still in the {ArenaName} arena you have a short period before you're teleported to your lifestone.", ChatMessageType.System));

                    if (loser.EventType.Equals("ffa") || loser.EventType.Equals("tugak"))
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {loser.EventTypeDisplay} arena match in {ArenaName} has finished and you placed {loser.FinishPlaceDisplay}\nIf you're still in the {ArenaName} arena you have a short period before you're teleported to your lifestone.", ChatMessageType.System));

                    var shouldReward = IsPlayerRewardEligible(player, loser, ActiveEvent.Players) && !underageViolation;

                    if (shouldReward)
                    {
                        switch (ActiveEvent.EventType)
                        {
                            case "1v1":
                            case "2v2":
                                player.GrantLevelProportionalXpNoModifier(0.035, 0, 0, XpType.PvP);

                                if (player.MaximumLuminance != null)
                                    player.GrantLuminance(5000, XpType.PvP, ShareType.None);

                                var pkTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PkTrophy);
                                pkTrophy?.SetStackSize(1);
                                if (pkTrophy != null && player.TryCreateInInventoryWithNetworking(pkTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(pkTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received a PK Trophy", ChatMessageType.Broadcast));
                                }

                                if (new Random().NextDouble() > 0.75)
                                {
                                    var arenaKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                    arenaKey?.SetStackSize(1);
                                    if (arenaKey != null && player.TryCreateInInventoryWithNetworking(arenaKey))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(arenaKey));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                    }
                                }
                                break;

                            case "ffa":
                            case "tugak":
                                if (loser.FinishPlace == 2)
                                {
                                    player.GrantLevelProportionalXpNoModifier(0.25, 0, 0, XpType.PvP);

                                    if (player.MaximumLuminance != null)
                                        player.GrantLuminance(12000, XpType.PvP, ShareType.None);

                                    var ffaLoserPkTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PkTrophy);
                                    ffaLoserPkTrophy?.SetStackSize(3);
                                    if (ffaLoserPkTrophy != null && player.TryCreateInInventoryWithNetworking(ffaLoserPkTrophy))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaLoserPkTrophy));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received 3 PK Trophies", ChatMessageType.Broadcast));
                                    }

                                    var ffaLoserArenaTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PhialOfBloodyTears);
                                    ffaLoserArenaTrophy?.SetStackSize(2);
                                    if (ffaLoserArenaTrophy != null && player.TryCreateInInventoryWithNetworking(ffaLoserArenaTrophy))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaLoserArenaTrophy));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received 2 Phials of Bloody Tears", ChatMessageType.Broadcast));
                                    }

                                    var ffaLoserKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                    ffaLoserKey?.SetStackSize(1);
                                    if (ffaLoserKey != null && player.TryCreateInInventoryWithNetworking(ffaLoserKey))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaLoserKey));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                    }
                                }
                                else if (loser.FinishPlace == 3)
                                {
                                    player.GrantLevelProportionalXpNoModifier(0.15, 0, 0, XpType.PvP);

                                    if (player.MaximumLuminance != null)
                                        player.GrantLuminance(8000, XpType.PvP, ShareType.None);

                                    var ffaLoserPkTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PkTrophy);
                                    ffaLoserPkTrophy?.SetStackSize(1);
                                    if (ffaLoserPkTrophy != null && player.TryCreateInInventoryWithNetworking(ffaLoserPkTrophy))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaLoserPkTrophy));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received a PK Trophy", ChatMessageType.Broadcast));
                                    }

                                    var ffaLoserArenaTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PhialOfBloodyTears);
                                    ffaLoserArenaTrophy?.SetStackSize(1);
                                    if (ffaLoserArenaTrophy != null && player.TryCreateInInventoryWithNetworking(ffaLoserArenaTrophy))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaLoserArenaTrophy));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received a Phial of Bloody Tears", ChatMessageType.Broadcast));
                                    }

                                    var ffaLoserKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                    ffaLoserKey?.SetStackSize(1);
                                    if (ffaLoserKey != null && player.TryCreateInInventoryWithNetworking(ffaLoserKey))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaLoserKey));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                    }
                                }
                                else
                                {
                                    player.GrantLevelProportionalXpNoModifier(0.035, 0, 0, XpType.PvP);

                                    if (player.MaximumLuminance != null)
                                        player.GrantLuminance(5000, XpType.PvP, ShareType.None);

                                    if (new Random().NextDouble() > 0.75)
                                    {
                                        var ffaLoserKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                        ffaLoserKey?.SetStackSize(1);
                                        if (ffaLoserKey != null && player.TryCreateInInventoryWithNetworking(ffaLoserKey))
                                        {
                                            player.Session.Network.EnqueueSend(new GameMessageCreateObject(ffaLoserKey));
                                            player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                        }
                                    }
                                }
                                break;

                            case "group":
                                player.GrantLevelProportionalXpNoModifier(0.1, 0, 0, XpType.PvP);

                                if (player.MaximumLuminance != null)
                                    player.GrantLuminance(20000, XpType.PvP, ShareType.None);

                                var groupLoserPkTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PkTrophy);
                                groupLoserPkTrophy?.SetStackSize(2);
                                if (groupLoserPkTrophy != null && player.TryCreateInInventoryWithNetworking(groupLoserPkTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(groupLoserPkTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received 2 PK Trophies", ChatMessageType.Broadcast));
                                }

                                var groupLoserArenaTrophy = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.PhialOfBloodyTears);
                                groupLoserArenaTrophy?.SetStackSize(1);
                                if (groupLoserArenaTrophy != null && player.TryCreateInInventoryWithNetworking(groupLoserArenaTrophy))
                                {
                                    player.Session.Network.EnqueueSend(new GameMessageCreateObject(groupLoserArenaTrophy));
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received 1 Phial of Bloody Tears", ChatMessageType.Broadcast));
                                }

                                if (new Random().NextDouble() > 0.75)
                                {
                                    var groupLoserKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                                    groupLoserKey?.SetStackSize(1);
                                    if (groupLoserKey != null && player.TryCreateInInventoryWithNetworking(groupLoserKey))
                                    {
                                        player.Session.Network.EnqueueSend(new GameMessageCreateObject(groupLoserKey));
                                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                                    }
                                }
                                break;
                        }

                        SetPlayerRewardLimitProperties(player, loser);
                    }

                    //Handle PK quests for losers
                    // See winner block above: arena quest credit only requires a cross-allegiance
                    // opponent, not a whitelisted allegiance.
                    var loserMonarchId = loser.MonarchId;
                    var hasDifferentAllegianceWinner = winners.FirstOrDefault(x => x.MonarchId != loserMonarchId) != null;
                    if (hasDifferentAllegianceWinner || ActiveEvent.EventType.ToLower().Equals("1v1"))
                    {
                        player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateAnyArena);
                        player.CompletePkQuestTask("ARENA_DMG20K", (int)loser.TotalDmgDealt);

                        switch (ActiveEvent.EventType)
                        {
                            case "1v1":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Participate1v1Arena);
                                break;
                            case "2v2":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Participate2v2Arena);
                                break;
                            case "ffa":
                                player.CompletePkQuestTask("ARENA_FFA_2");
                                if (loser.FinishPlace > 0 && loser.FinishPlace <= 3)
                                    player.CompletePkQuestTask("ARENA_FFA_TOP3");
                                break;
                            case "tugak":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateTugakArena);
                                if (loser.FinishPlace > 0 && loser.FinishPlace <= 3)
                                    player.CompletePkQuestTask("ARENA_TUGAK_TOP3");
                                break;
                            case "group":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateGroupArena);
                                break;
                            default:
                                break;
                        }
                    }

                    ArenaManager.DispelArenaRares(player);
                }
            }

            var globalMsg = $"{winnerList} just won a {ActiveEvent.EventTypeDisplay} arena event against {loserList} in {ArenaName}";
            PlayerManager.BroadcastToAll(new GameMessageSystemChat(globalMsg, ChatMessageType.Broadcast));
            try
            {
                var webhookUrl = PropertyManager.GetString("arena_globals_webhook").Item;
                if (!string.IsNullOrEmpty(webhookUrl))
                    _ = TurbineChatHandler.SendWebhookedChat("Arenas", globalMsg, webhookUrl, "Global");
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed sending Arena global message to webhook. Ex:{0}", ex);
            }

            SeasonManager.InvalidateArenaCache();
        }

        public void SetPlayerRewardLimitProperties(Player player, ArenaPlayer arenaPlayer)
        {
            if (!player.ArenaDailyRewardTimestamp.HasValue || Time.GetDateTimeFromTimestamp(player.ArenaDailyRewardTimestamp.Value) < DateTime.Today)
            {
                player.ArenaDailyRewardTimestamp = Time.GetUnixTime(DateTime.Today);
                player.ArenaDailyRewardCount = 0;
                player.ArenaSameClanDailyRewardCount = 0;
                player.ArenaRewardsByOpponent = null;
            }

            player.ArenaDailyRewardCount++;

            var sameClanOpponents = ActiveEvent.Players.Where(x => x.MonarchId == arenaPlayer.MonarchId && x.TeamGuid != arenaPlayer.TeamGuid);
            if (sameClanOpponents != null && sameClanOpponents.Count() > 0)
                player.ArenaSameClanDailyRewardCount++;

            DateTime thisHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            if (!player.ArenaHourlyTimestamp.HasValue || Time.GetDateTimeFromTimestamp(player.ArenaHourlyTimestamp.Value) < thisHour)
            {
                player.ArenaHourlyTimestamp = Time.GetUnixTime(thisHour);
                player.ArenaHourlyCount = 0;
            }

            player.ArenaHourlyCount++;

            var opponents = ActiveEvent.Players.Where(x => x.TeamGuid != arenaPlayer.TeamGuid);
            if (opponents != null)
            {
                var opponentRewards = player.ArenaRewardsByOpponent;
                foreach (var opponent in opponents)
                {
                    if (opponentRewards != null && opponentRewards.ContainsKey(opponent.CharacterId))
                        opponentRewards[opponent.CharacterId]++;
                    else
                        opponentRewards.Add(opponent.CharacterId, 1);
                }
                player.ArenaRewardsByOpponent = opponentRewards;
            }
        }

        public bool IsPlayerRewardEligible(Player player, ArenaPlayer arenaPlayer, List<ArenaPlayer> allArenaPlayers)
        {
            if (arenaPlayer.FinishPlace == -1)
                return false;

            if (player == null || player.Age <= PropertyManager.GetLong("arenas_reward_min_age").Item)
                return false;

            if (player.ArenaDailyRewardCount >= 40 && (player.ArenaDailyRewardTimestamp ?? 0) >= Time.GetUnixTime(DateTime.Today))
                return false;

            DateTime thisHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            if (player.ArenaHourlyCount >= 12 && (player.ArenaHourlyTimestamp ?? 0) >= Time.GetUnixTime(thisHour))
                return false;

            if (player.ArenaSameClanDailyRewardCount >= 15 && (player.ArenaDailyRewardTimestamp ?? 0) >= Time.GetUnixTime(DateTime.Today))
            {
                var sameClanOpponents = allArenaPlayers
                    .Where(x =>
                        x.CharacterId != arenaPlayer.CharacterId &&
                        x.TeamGuid != arenaPlayer.TeamGuid &&
                        x.MonarchId == arenaPlayer.MonarchId);

                if (sameClanOpponents != null && sameClanOpponents.Count() > 0)
                    return false;
            }

            // Anti-alt-farming: deny rewards if an opposing arena player is a throwaway parked on an
            // allegiance-mate's account. In arenas we deliberately apply this ONLY to opponents who
            // are not themselves in your allegiance (different monarch) — legitimate same-allegiance
            // matches are allowed to reward, still governed by the 15/day same-allegiance cap above.
            // (Excluding same-monarch opponents here also avoids false positives when a genuine
            // allegiance-mate simply happens to have another of their own characters in the allegiance.)
            foreach (var opponent in allArenaPlayers.Where(x =>
                         x.CharacterId != arenaPlayer.CharacterId &&
                         x.TeamGuid != arenaPlayer.TeamGuid &&
                         x.MonarchId != arenaPlayer.MonarchId))
            {
                var opponentPlayer = PlayerManager.FindByGuid(new ObjectGuid(opponent.CharacterId));
                if (opponentPlayer != null && player.VictimIsAllegianceMateAlt(opponentPlayer))
                    return false;
            }

            return true;
        }

        public void EndEventTimelimitExceeded()
        {
            log.Info($"ArenaLocation.EndEventTimelimitExceeded() - {ArenaName}");
            ActiveEvent.EndDateTime = DateTime.Now;
            ActiveEvent.Status = 6;

            var remainingPlayers = ActiveEvent.Players.Where(x => !x.IsDisqualified && !x.IsEliminated);
            if (remainingPlayers != null && remainingPlayers.Count() > 0)
            {
                foreach (var arenaPlayer in remainingPlayers)
                    arenaPlayer.FinishPlace = remainingPlayers.Count();
            }

            DatabaseManager.Log.SaveArenaEvent(ActiveEvent);

            bool underageViolation = false;
            var underageCount = 0;
            foreach (var arenaPlayer in ActiveEvent.Players)
            {
                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                if (player != null && player.Age <= PropertyManager.GetLong("arenas_reward_min_age").Item)
                    underageCount++;
            }

            if (underageCount > 0 && (ActiveEvent.EventType.Equals("1v1") || ActiveEvent.EventType.Equals("2v2")))
                underageViolation = true;
            else if (underageCount > 2 && (ActiveEvent.EventType.Equals("ffa") || ActiveEvent.EventType.Equals("tugak")))
                underageViolation = true;

            foreach (var arenaPlayer in ActiveEvent.Players)
            {
                bool isFFA  = arenaPlayer.EventType.Equals("ffa") || arenaPlayer.EventType.Equals("tugak");
                var isLoss  = (arenaPlayer.FinishPlace > 3 || arenaPlayer.FinishPlace < 1) &&
                    !arenaPlayer.EventType.ToLower().Equals("group");
                var isDq = arenaPlayer.FinishPlace == -1;

                // FFA / Tugak: award placement points even on timeout
                uint ffaPoints = isFFA ? ArenaRanking.GetFfaPlacementPoints(arenaPlayer.FinishPlace) : 0;

                // 1v1 / 2v2 draw: ELO unchanged but LastMatchDatetime still refreshes (newElo = null)
                DatabaseManager.Log.AddToArenaStats(
                    arenaPlayer.CharacterId, arenaPlayer.CharacterName, arenaPlayer.EventType,
                    1, 0,
                    isLoss ? 0 : (uint)1,
                    isLoss ? 1 : (uint)0,
                    isDq ? 1 : (uint)0,
                    arenaPlayer.TotalDeaths, arenaPlayer.TotalKills, arenaPlayer.TotalDmgDealt, arenaPlayer.TotalDmgReceived,
                    null, ffaPoints);

                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                if (player != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your {ActiveEvent.EventTypeDisplay} arena event has ended in a draw.  If you are still in the arena you can recall now or have a short period before you are teleported to your lifestone.", ChatMessageType.System));

                    var shouldReward = IsPlayerRewardEligible(player, arenaPlayer, ActiveEvent.Players) && !underageViolation;

                    if (shouldReward)
                    {
                        player.GrantLevelProportionalXpNoModifier(0.035, 1, long.MaxValue, XpType.PvP);

                        if (new Random().NextDouble() > 0.75)
                        {
                            var drawKey = WorldObjectFactory.CreateNewWorldObject(CustomWeenieId.DarkbeatKey);
                            drawKey?.SetStackSize(1);
                            if (drawKey != null && player.TryCreateInInventoryWithNetworking(drawKey))
                            {
                                player.Session.Network.EnqueueSend(new GameMessageCreateObject(drawKey));
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat("You have received one of Darkbeat's Lost Storage Keys", ChatMessageType.Broadcast));
                            }
                        }

                        SetPlayerRewardLimitProperties(player, arenaPlayer);
                    }

                    //Handle PK quests for draw participants
                    // See EndEventWithWinner: arena quest credit only requires a cross-allegiance
                    // opponent on the other team, not a whitelisted allegiance.
                    var drawPlayerMonarchId = arenaPlayer.MonarchId;
                    var hasDifferentAllegianceDrawOpponent = ActiveEvent.Players.FirstOrDefault(x => x.TeamGuid != arenaPlayer.TeamGuid && x.MonarchId != drawPlayerMonarchId) != null;
                    if (hasDifferentAllegianceDrawOpponent || ActiveEvent.EventType.ToLower().Equals("1v1"))
                    {
                        player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateAnyArena);
                        player.CompletePkQuestTask("ARENA_DMG20K", (int)arenaPlayer.TotalDmgDealt);

                        switch (ActiveEvent.EventType)
                        {
                            case "1v1":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Participate1v1Arena);
                                break;
                            case "2v2":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_Participate2v2Arena);
                                break;
                            case "ffa":
                                player.CompletePkQuestTask("ARENA_FFA_2");
                                if (!isLoss)
                                    player.CompletePkQuestTask("ARENA_FFA_TOP3");
                                break;
                            case "tugak":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateTugakArena);
                                if (!isLoss)
                                    player.CompletePkQuestTask("ARENA_TUGAK_TOP3");
                                break;
                            case "group":
                                player.CompletePkQuestTasks(PKQuestDefs.PKQuests_ParticipateGroupArena);
                                break;
                            default:
                                break;
                        }
                    }

                    ArenaManager.DispelArenaRares(player);
                }
            }

            var drawMsg = $"Arena event ended in a draw: {ActiveEvent.EventTypeDisplay} - {ActiveEvent.PlayersDisplay} - {ArenaName}";
            PlayerManager.BroadcastToAll(new GameMessageSystemChat(drawMsg, ChatMessageType.Broadcast));
            try
            {
                var webhookUrl = PropertyManager.GetString("arena_globals_webhook").Item;
                if (!string.IsNullOrEmpty(webhookUrl))
                    _ = TurbineChatHandler.SendWebhookedChat("Arenas", drawMsg, webhookUrl, "Global");
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed sending Arena global message to webhook. Ex:{0}", ex);
            }

            SeasonManager.InvalidateArenaCache();
        }

        public void EndEventCancel()
        {
            log.Info($"ArenaLocation.EndEventCancel() - {ArenaName}");
            ActiveEvent.EndDateTime = DateTime.Now;
            ActiveEvent.Status = -1;

            DatabaseManager.Log.SaveArenaEvent(ActiveEvent);

            foreach (var arenaPlayer in ActiveEvent.Players)
            {
                var player = PlayerManager.GetOnlinePlayer(arenaPlayer.CharacterId);
                if (player != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your {ActiveEvent.EventTypeDisplay} arena event was cancelled before it started.", ChatMessageType.System));
                    ArenaManager.DispelArenaRares(player);
                }
            }

            ActiveEvent = null;
        }

        public void ClearPlayersFromArena()
        {
            try
            {
                var arenaLandblock = LandblockManager.GetLandblock(new LandblockId(LandblockId << 16), false);
                var playerList = arenaLandblock.GetCurrentLandblockPlayers();
                foreach (var player in playerList)
                {
                    if (player.IsAdmin)
                        continue;

                    if (player.IsPendingArenaObserver || player.IsArenaObserver)
                    {
                        ArenaManager.ExitArenaObserverMode(player);
                        continue;
                    }

                    player.Teleport(player.Sanctuary);
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat("You've been teleported to your lifestone because you were inside an arena location without being an active participant in an arena event", ChatMessageType.System));
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in ArenaLocation.ClearPlayersFromArena. ex: {ex}");
            }
        }

        public static Dictionary<uint, ArenaLocation> InitializeArenaLocations()
        {
            log.Info($"ArenaLocation.InitializeArenaLocations()");
            var locList = new Dictionary<uint, ArenaLocation>();

            var aCave = new ArenaLocation();
            aCave.LandblockId = 0x018B;
            aCave.SupportedEventTypes = new List<string>() { "1v1", "2v2", "ffa", "group", "tugak" };
            aCave.ArenaName = "A Cave";
            locList.Add(aCave.LandblockId, aCave);

            var ratLair = new ArenaLocation();
            ratLair.LandblockId = 0x01D9;
            ratLair.SupportedEventTypes = new List<string>() { "1v1", "2v2", "tugak" };
            ratLair.ArenaName = "Rat Lair";
            locList.Add(ratLair.LandblockId, ratLair);

            var xarabydunLifestone = new ArenaLocation();
            xarabydunLifestone.LandblockId = 0x02D3;
            xarabydunLifestone.SupportedEventTypes = new List<string>() { "2v2", "ffa", "group", "tugak" };
            xarabydunLifestone.ArenaName = "Xarabydun Lifestone";
            locList.Add(xarabydunLifestone.LandblockId, xarabydunLifestone);

            //var pklArena = new ArenaLocation();
            //pklArena.LandblockId = 0x0067;
            //pklArena.SupportedEventTypes = new List<string>() { "ffa", "group", "tugak" };
            //pklArena.ArenaName = "PKL Arena";
            //locList.Add(pklArena.LandblockId, pklArena);            

            //var boneLair = new ArenaLocation();
            //boneLair.LandblockId = 0x0145;
            //boneLair.SupportedEventTypes = new List<string>() { "ffa", "group" };
            //boneLair.ArenaName = "Bone Lair";
            //locList.Add(boneLair.LandblockId, boneLair);

            //var galleyTower = new ArenaLocation();
            //galleyTower.LandblockId = 0x01AD;
            //galleyTower.SupportedEventTypes = new List<string>() { "ffa", "group" };
            //galleyTower.ArenaName = "Galley Tower";
            //locList.Add(galleyTower.LandblockId, galleyTower);

            //var ypk = new ArenaLocation();
            //ypk.LandblockId = 0x02E3;
            //ypk.SupportedEventTypes = new List<string>() { "ffa", "group" };
            //ypk.ArenaName = "Yaraq PK Arena";
            //locList.Add(ypk.LandblockId, ypk);            

            return locList;
        }

        // HashSet rather than List: IsArenaLandblock is called on every damage calculation
        private static HashSet<uint> _arenaLandblocks;
        public static HashSet<uint> ArenaLandblocks
        {
            get
            {
                if (_arenaLandblocks == null)
                {
                    _arenaLandblocks = new HashSet<uint>()
                    {
                        0x018B, //A Cave
                        0x01D9, //Rat Lair
                        0x02D3, //Xarabydun Lifestone
                        //0x0067, // PKL Arena
                        //0x007F, // Binding Realm
                        //0x0145, // Bone Lair
                        //0x01AD, // Dungeon Galley Tower
                        //0x02E3, // Yaraq PK Arena
                        //0x596A, // Fowl Basement
                        //0x039D, // One Ten
                    };
                }
                return _arenaLandblocks;
            }
        }

        private static Dictionary<uint, List<Position>> _arenaLocationStartingPositions = null;
        private static Dictionary<uint, List<Position>> arenaLocationStartingPositions
        {
            get
            {
                if (_arenaLocationStartingPositions == null)
                {
                    _arenaLocationStartingPositions = new Dictionary<uint, List<Position>>();

                    _arenaLocationStartingPositions.Add(0x0067, new List<Position>()
                    {
                        new Position(0x00670103, 0f, -30f, 0.004999995f, 0f, 0f, -0.699713f, 0.714424f),
                        new Position(0x00670127, 62.829544f, -28.933987f, 0.005000f, 0f, 0f, 0.654449f, 0.756106f),
                        new Position(0x00670117, 29.432631f, -49.517181f, 0.005000f, 0f, 0f, -0.036533f, 0.999332f),
                        new Position(0x00670112, 29.911026f, -0.265875f, 0.005000f, 0f, 0f, 0.999908f, 0.013594f),
                        new Position(0x00670105, 2.247503f, -48.398197f, 0.005000f, 0f, 0f, -0.440161f, 0.897919f),
                        new Position(0x00670124, 58.684017f, -2.170248f, 0.005000f, 0f, 0f, 0.902263f, 0.431186f),
                        new Position(0x00670129, 58.118492f, -47.702240f, 0.005000f, 0f, 0f, 0.426291f, 0.904586f),
                        new Position(0x00670100, 1.902291f, -2.042494f, 0.005000f, 0f, 0f, 0.914624f, -0.404305f),
                        new Position(0x00670109, 14.541056f, -26.421442f, 0.005000f, 0f, 0f, 0.701743f, -0.712430f),
                        new Position(0x0067011A, 37.242702f, -24.179514f, 0.005000f, 0f, 0f, 0.728694f, 0.684840f),
                    });

                    _arenaLocationStartingPositions.Add(0x007F, new List<Position>()
                    {
                        new Position(0x007F0101, 236.75943f, -22.896727f, -59.995f, 0f, 0f, -0.6899546f, 0.72385263f),
                        new Position(0x007F0107, 262.50168f, -16.419994f, -59.995f, 0f, 0f, -0.8109761f, -0.58507925f),
                        new Position(0x007F0103, 250.935226f, -5.749045f, -59.994999f, 0f, 0f, -0.999384f, -0.035106f),
                        new Position(0x007F0105, 252.860062f, -33.350712f, -59.994999f, 0f, 0f, -0.043396f, -0.999058f),
                    });

                    _arenaLocationStartingPositions.Add(0x02E3, new List<Position>()
                    {
                        new Position(0x02E30100, 54.954102f, -53.559502f, 0.005000f, 0f, 0f, -0.706410f, 0.707802f),
                        new Position(0x02E30185, 87.640144f, -54.995811f, 12.004999f, 0f, 0f, -0.701326f, -0.712840f),
                        new Position(0x02E3014A, 54.859180f, -20.170738f, 12.004999f, 0f, 0f, -0.999738f, -0.022893f),
                        new Position(0x02E30131, 21.610455f, -54.913197f, 12.004999f, 0f, 0f, -0.714008f, 0.700137f),
                        new Position(0x02E3015D, 55.055714f, -88.516785f, 12.004999f, 0f, 0f, 0.008961f, 0.999960f),
                        new Position(0x02E30107, 37.609787f, -55.014618f, 6.005000f, 0f, 0f, -0.714715f, 0.699416f),
                        new Position(0x02E30112, 55.120712f, -72.074440f, 6.005000f, 0f, 0f, 0.013379f, 0.999910f),
                        new Position(0x02E30119, 71.587006f, -55.205040f, 6.005000f, 0f, 0f, 0.688900f, 0.724856f),
                        new Position(0x02E3010F, 55.080246f, -37.959023f, 6.005000f, 0f, 0f, 0.999951f, 0.009948f),
                        new Position(0x02E3015A, 59.596912f, -55.734844f, 12.054999f, 0f, 0f, -0.717913f, -0.696133f),
                    });

                    _arenaLocationStartingPositions.Add(0x01AD, new List<Position>()
                    {
                        new Position(0x01AD0116, 29.940001f, -32.599998f, 0.005000f, 0f, 0f, -0.008727f, 0.999962f),
                        new Position(0x01AD011A, 40.046265f, -1.053921f, 0.005000f, 0f, 0f, -0.902557f, -0.430570f),
                        new Position(0x01AD0100, 0.060270f, 2.052596f, 0.005000f, 0f, 0f, -0.929969f, 0.367637f),
                        new Position(0x01AD0142, 39.877190f, -9.612027f, 6.005000f, 0f, 0f, -0.999993f, 0.003741f),
                        new Position(0x01AD014B, 61.338745f, 1.616516f, 6.005000f, 0f, 0f, -0.923844f, -0.382769f),
                        new Position(0x01AD014E, 59.279133f, -31.289797f, 6.005000f, 0f, 0f, -0.310898f, -0.950443f),
                        new Position(0x01AD0144, 40.064552f, -25.380503f, 6.005000f, 0f, 0f, -0.017525f, -0.999846f),
                        new Position(0x01AD0135, 18.353422f, -20.375107f, 6.005000f, 0f, 0f, 0.872478f, -0.488653f),
                        new Position(0x01AD016E, 40.068432f, 0.782179f, 18.004999f, 0f, 0f, -0.919731f, -0.392550f),
                        new Position(0x01AD0155, 30.068464f, -20.947460f, 12.004999f, 0f, 0f, -0.999852f, 0.017178f),
                    });

                    _arenaLocationStartingPositions.Add(0x0145, new List<Position>()
                    {
                        new Position(0x014501A3, 96.569344f, -50.926197f, 6.005000f, 0f, 0f, -0.189269f, -0.981925f),
                        new Position(0x014501A1, 97.130882f, -27.609722f, 6.005000f, 0f, 0f, -0.953241f, -0.302211f),
                        new Position(0x0145010E, 100.117851f, -50.360348f, -5.995000f, 0f, 0f, 0.003506f, 0.999994f),
                        new Position(0x0145010B, 99.959366f, -29.854738f, -5.995000f, 0f, 0f, 0.999639f, 0.026883f),
                        new Position(0x01450149, 71.072929f, -26.698454f, 0.005000f, 0f, 0f, -0.996001f, 0.089337f),
                        new Position(0x0145014B, 72.077400f, -52.935970f, 0.005000f, 0f, 0f, -0.170279f, 0.985396f),
                        new Position(0x01450117, 29.986937f, -13.254610f, 0.005000f, 0f, 0f, 0.999936f, 0.011354f),
                        new Position(0x01450185, 29.381468f, -40.134899f, 6.005000f, 0f, 0f, 0.686305f, -0.727314f),
                        new Position(0x01450121, 29.054979f, -57.933998f, 0.005000f, 0f, 0f, 0.015373f, 0.999882f),
                        new Position(0x01450114, 5.084766f, -76.363258f, 0.005000f, 0f, 0f, -0.394789f, 0.918772f),
                    });

                    _arenaLocationStartingPositions.Add(0x596A, new List<Position>()
                    {
                        new Position(0x596A010C, 35.238209f, -18.218103f, 0.005000f, 0f, 0f, -0.707107f, -0.707107f),
                        new Position(0x596A0102, 5.035136f, -18.326546f, 0.005000f, 0f, 0f, -0.695197f, 0.718819f),
                        new Position(0x596A010A, 34.787258f, -21.811909f, 0.005000f, 0f, 0f, 0.713734f, 0.700416f),
                        new Position(0x596A0102, 5.146578f, -21.661127f, 0.005000f, 0f, 0f, -0.695197f, 0.718819f),
                    });

                    _arenaLocationStartingPositions.Add(0x039D, new List<Position>()
                    {
                        new Position(0x039D02A4, 104.028847f, -46.040611f, 48.005001f, 0f, 0f, -0.925397f, -0.379000f),
                        new Position(0x039D02A4, 103.830605f, -53.779991f, 48.005001f, 0f, 0f, -0.401763f, -0.915744f),
                        new Position(0x039D02A4, 96.026833f, -53.870930f, 48.005001f, 0f, 0f, 0.449561f, -0.893250f),
                        new Position(0x039D02A4, 95.942345f, -45.941307f, 48.005001f, 0f, 0f, 0.924836f, -0.380366f),
                    });

                    _arenaLocationStartingPositions.Add(0x018B, new List<Position>()
                    {
                        new Position(0x018B01A4, 57.098507f, -3.865624f, 0.005000f, 0f, 0f, -0.921203f, -0.389081f), // 0x018B01A4 [57.098507 -3.865624 0.005000] -0.389081 0.000000 0.000000 -0.921203
                        new Position(0x018B0188, 42.685162f, -2.631094f, 0.005000f, 0f, 0f, -0.893269f, 0.449522f), // 0x018B0188 [42.685162 -2.631094 0.005000] 0.449522 0.000000 0.000000 -0.893269
                        new Position(0x018B018A, 41.873562f, -17.906517f, 0.005000f, 0f, 0f, -0.379880f, 0.925036f), // 0x018B018A [41.873562 -17.906517 0.005000] 0.925036 0.000000 0.000000 -0.379880
                        new Position(0x018B01A7, 58.693562f, -18.457191f, 0.005000f, 0f, 0f, 0.319989f, 0.947421f), // 0x018B01A7 [58.693562 -18.457191 0.005000] 0.947421 0.000000 0.000000 0.319989
                    });//A Cave

                    _arenaLocationStartingPositions.Add(0x01D9, new List<Position>()
                    {
                        new Position(0x01D90102, 22.458916f, -24.787766f, 0.005000f, 0f, 0f, -0.690152f, 0.723665f), // 0x01D90102 [22.458916 -24.787766 0.005000] 0.723665 0.000000 0.000000 -0.690152
                        new Position(0x01D90112, 38.709148f, -25.670763f, 0.005000f, 0f, 0f, -0.728054f, -0.685520f), // 0x01D90112 [38.709148 -25.670763 0.005000] -0.685520 0.000000 0.000000 -0.728054
                        new Position(0x01D9010A, 30.201702f, -16.127213f, 0.005000f, 0f, 0f, -0.999959f, -0.009022f), // 0x01D9010A [30.201702 -16.127213 0.005000] -0.009022 0.000000 0.000000 -0.999959
                        new Position(0x01D9010B, 29.965298f, -33.662392f, 0.005000f, 0f, 0f, 0.003121f, -0.999995f), // 0x01D9010B [29.965298 -33.662392 0.005000] -0.999995 0.000000 0.000000 0.003121
                    });//Rat Lair

                    _arenaLocationStartingPositions.Add(0x02D3, new List<Position>()
                    {
                        new Position(0x02D30116, 44.519798f, -41.301655f, 0.005000f, 0f, 0f, -0.848320f, -0.529483f), // 0x02D30116 [44.519798 -41.301655 0.005000] -0.529483 0.000000 0.000000 -0.848320
                        new Position(0x02D30117, 38.992039f, -52.372768f, 0.005000f, 0f, 0f, -0.513071f, -0.858346f), // 0x02D30117 [38.992039 -52.372768 0.005000] -0.858346 0.000000 0.000000 -0.513071
                        new Position(0x02D30112, 27.737848f, -64.324081f, 0.005000f, 0f, 0f, -0.106830f, -0.994277f), // 0x02D30112 [27.737848 -64.324081 0.005000] -0.994277 0.000000 0.000000 -0.106830
                        new Position(0x02D30106, 12.533463f, -55.252972f, 0.005000f, 0f, 0f, 0.408943f, -0.912560f), // 0x02D30106 [12.533463 -55.252972 0.005000] -0.912560 0.000000 0.000000 0.408943
                        new Position(0x02D30104, 6.508535f, -45.134563f, 0.005000f, 0f, 0f, 0.697205f, -0.716872f), // 0x02D30104 [6.508535 -45.134563 0.005000] -0.716872 0.000000 0.000000 0.697205
                        new Position(0x02D30103, 9.816286f, -35.866894f, 0.005000f, 0f, 0f, 0.835337f, -0.549738f), // 0x02D30103 [9.816286 -35.866894 0.005000] -0.549738 0.000000 0.000000 0.835337
                        new Position(0x02D30109, 19.511150f, -26.370974f, 0.005000f, 0f, 0f, 0.983301f, -0.181984f), // 0x02D30109 [19.511150 -26.370974 0.005000] -0.181984 0.000000 0.000000 0.983301
                        new Position(0x02D3010E, 27.721006f, -24.671103f, 0.005000f, 0f, 0f, 0.998709f, 0.050804f), // 0x02D3010E [27.721006 -24.671103 0.005000] 0.050804 0.000000 0.000000 0.998709
                        new Position(0x02D3010F, 32.411823f, -30.301567f, 0.005000f, 0f, 0f, 0.977071f, 0.212911f), // 0x02D3010F [32.411823 -30.301567 0.005000] 0.212911 0.000000 0.000000 0.977071
                        new Position(0x02D30118, 37.262321f, -58.807224f, 0.005000f, 0f, 0f, 0.355590f, 0.934642f), // 0x02D30118 [37.262321 -58.807224 0.005000] 0.934642 0.000000 0.000000 0.355590
                    });//Xarabydun Lifestone
                }

                return _arenaLocationStartingPositions;
            }
        }

        public static List<Position> GetArenaLocationStartingPositions(uint landblockId)
        {
            return arenaLocationStartingPositions.ContainsKey(landblockId) ? arenaLocationStartingPositions[landblockId] : new List<Position>();
        }

        public static bool IsArenaLandblock(uint landblockId)
        {
            return ArenaLandblocks.Contains(landblockId);
        }
    }
}
