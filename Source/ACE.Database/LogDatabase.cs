using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using log4net;

using ACE.Database.Models.Log;

namespace ACE.Database
{
    public class LogDatabase
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool IsConfigured => Common.ConfigManager.Config.MySql.Log != null;

        public bool Exists(bool retryUntilFound)
        {
            if (!IsConfigured)
                return false;

            var config = Common.ConfigManager.Config.MySql.Log;

            for (;;)
            {
                using (var context = new LogDbContext())
                {
                    if (((RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>()).Exists())
                    {
                        log.Debug($"[DATABASE] Successfully connected to {config.Database} database on {config.Host}:{config.Port}.");
                        return true;
                    }
                }

                log.Error($"[DATABASE] Attempting to reconnect to {config.Database} database on {config.Host}:{config.Port} in 5 seconds...");

                if (retryUntilFound)
                    Thread.Sleep(5000);
                else
                    return false;
            }
        }

        #region Account Session Log

        public void LogAccountSessionStart(uint accountId, string accountName, string sessionIP)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"INSERT INTO account_session_log (accountId, accountName, sessionIP, loginDateTime)
                            VALUES ({accountId}, {accountName}, {sessionIP}, {DateTime.Now});");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogAccountSessionStart saving session log data to DB. Ex: {ex}");
            }
        }

        public void LogAccountSessionEnd(uint accountId)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"UPDATE account_session_log SET logoutDateTime = {DateTime.Now}
                            WHERE accountId = {accountId} AND logoutDateTime IS NULL;");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogAccountSessionEnd saving session log data to DB for AccountId = {accountId}. Ex: {ex}");
            }
        }

        #endregion

        #region Character Login Log

        public void LogCharacterLogin(uint accountId, string accountName, string sessionIP, uint characterId, string characterName)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"INSERT INTO character_login_log (accountId, accountName, characterId, characterName, sessionIP, loginDateTime)
                            VALUES ({accountId}, {accountName}, {characterId}, {characterName}, {sessionIP}, {DateTime.Now});");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogCharacterLogin saving character login info to DB for AccountId = {accountId}, CharacterId = {characterId}. Ex: {ex}");
            }
        }

        public void LogCharacterLogout(uint characterId)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"UPDATE character_login_log SET logoutDateTime = {DateTime.Now}
                            WHERE characterId = {characterId} AND logoutDateTime IS NULL;");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogCharacterLogout saving session log data to DB for CharacterId = {characterId}. Ex: {ex}");
            }
        }

        #endregion

        #region Tinkering Log

        public void LogTinkeringEvent(uint characterId, string characterName, uint itemBiotaId, float chance, float roll, bool isSuccess, uint itemNumPreviousTinks, uint itemWorkmanship, string salvageType, uint salvageWorkmanship)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"INSERT INTO tinker_log (characterId, characterName, itemBiotaId, tinkDateTime, successChance, roll, isSuccess, itemNumPreviousTinks, itemWorkmanship, salvageType, salvageWorkmanship)
                            VALUES ({characterId}, {characterName}, {itemBiotaId}, {DateTime.Now}, {chance}, {roll}, {isSuccess}, {itemNumPreviousTinks}, {itemWorkmanship}, {salvageType}, {salvageWorkmanship});");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogTinkeringEvent saving data to DB for CharacterId = {characterId}, ItemBiotaId = {itemBiotaId}. Ex: {ex}");
            }
        }

        #endregion

        #region PK Kills Log

        public void LogPkKill(uint victimId, uint killerId, uint? victimMonarchId, uint? killerMonarchId, uint? victimArenaPlayerId = null, uint? killerArenaPlayerId = null)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"INSERT INTO pk_kills_log (killer_id, victim_id, killer_monarch_id, victim_monarch_id, kill_datetime, killer_arena_player_id, victim_arena_player_id)
                            VALUES ({killerId}, {victimId}, {killerMonarchId}, {victimMonarchId}, {DateTime.Now}, {killerArenaPlayerId}, {victimArenaPlayerId});");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogPkKill saving kill data to DB for KillerId = {killerId}, VictimId = {victimId}. Ex: {ex}");
            }
        }

        #endregion

        #region Arenas

        public uint SaveArenaEvent(ArenaEvent arenaEvent)
        {
            if (!IsConfigured) return 0;
            try
            {
                using (var context = new LogDbContext())
                {
                    if (arenaEvent.Id <= 0)
                        context.ArenaEvents.Add(arenaEvent);
                    else
                        context.Entry(arenaEvent).State = EntityState.Modified;

                    context.SaveChanges();

                    foreach (var arenaPlayer in arenaEvent.Players)
                    {
                        arenaPlayer.EventId = arenaEvent.Id;

                        if (arenaPlayer.Id <= 0)
                            context.ArenaPlayers.Add(arenaPlayer);
                        else
                            context.Entry(arenaPlayer).State = EntityState.Modified;
                    }

                    context.SaveChanges();

                    return arenaEvent.Id;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in SaveArenaEvent. Ex: {ex}");
            }

            return 0;
        }

        /// <summary>
        /// Updates or creates the arena stats row for a character after a match.
        /// </summary>
        /// <param name="newElo">
        /// If set, replaces the stored raw ELO (1v1 / 2v2 only).
        /// Pass null for FFA, Tugak, draw results, or any case where ELO should not change.
        /// </param>
        /// <param name="addRankPoints">
        /// Amount to add to the stored RankPoints column (FFA / Tugak placement points).
        /// Not used by 1v1 / 2v2, whose RankPoints column mirrors their ELO rating.
        /// </param>
        /// <param name="survived">
        /// Whether to increment TotalSurvived (2v2 survival stat; always false for other modes).
        /// </param>
        public void AddToArenaStats(
            uint characterId, string characterName, string eventType,
            uint totalMatches, uint totalWins, uint totalDraws, uint totalLosses, uint totalDisqualified,
            uint totalDeaths, uint totalKills, uint totalDmgDealt, uint totalDmgReceived,
            uint? newElo = null, uint addRankPoints = 0, bool survived = false)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    var stats = context.ArenaCharacterStats.FirstOrDefault(x => x.CharacterId == characterId && x.EventType.Equals(eventType));
                    if (stats == null)
                    {
                        stats = new ArenaCharacterStats
                        {
                            CharacterId = characterId,
                            CharacterName = characterName,
                            EventType = eventType,
                            Elo = 1500
                        };
                        context.ArenaCharacterStats.Add(stats);
                    }
                    else
                    {
                        context.Entry(stats).State = EntityState.Modified;
                    }

                    stats.TotalMatches      += totalMatches;
                    stats.TotalWins         += totalWins;
                    stats.TotalDraws        += totalDraws;
                    stats.TotalLosses       += totalLosses;
                    stats.TotalDisqualified += totalDisqualified;
                    stats.TotalDeaths       += totalDeaths;
                    stats.TotalKills        += totalKills;
                    stats.TotalDmgDealt     += totalDmgDealt;
                    stats.TotalDmgReceived  += totalDmgReceived;
                    stats.LastMatchDatetime  = DateTime.Now;
                    // Decay is settled through now: today's daily job must not also
                    // decay this row, and tomorrow's will re-read the 7-day activity
                    // window with this match included.
                    stats.LastDecayDatetime  = DateTime.Now;

                    if (newElo.HasValue)
                    {
                        stats.Elo = newElo.Value;
                        // 1v1 / 2v2 score is the ELO rating; mirror it into the
                        // persisted snapshot column so raw DB queries stay accurate.
                        stats.RankPoints = newElo.Value;
                    }

                    if (addRankPoints > 0)
                        stats.RankPoints += addRankPoints;

                    if (survived)
                        stats.TotalSurvived++;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception saving ArenaCharacterStats. ex: {ex}");
            }
        }

        /// <summary>
        /// Updates or creates the team-pair ranking row for a 2v2 match.
        /// Team members must be supplied in any order; the team key is sorted internally.
        /// </summary>
        public void AddToArenaTeamStats(
            uint charIdA, string charNameA, uint charIdB, string charNameB,
            uint totalMatches, uint totalWins, uint totalDraws, uint totalLosses, uint totalDisqualified,
            uint totalSurvived, uint? newElo = null)
        {
            if (!IsConfigured) return;
            try
            {
                // Canonical key: lower ID first
                var (loId, loName, hiId, hiName) = charIdA <= charIdB
                    ? (charIdA, charNameA, charIdB, charNameB)
                    : (charIdB, charNameB, charIdA, charNameA);
                string teamKey = $"{loId}_{hiId}";

                using (var context = new LogDbContext())
                {
                    var team = context.ArenaTeamStats.FirstOrDefault(x => x.TeamKey == teamKey);
                    if (team == null)
                    {
                        team = new ArenaTeamStats
                        {
                            TeamKey        = teamKey,
                            CharacterIdA   = loId,
                            CharacterNameA = loName,
                            CharacterIdB   = hiId,
                            CharacterNameB = hiName,
                            Elo            = 1500
                        };
                        context.ArenaTeamStats.Add(team);
                    }
                    else
                    {
                        // Keep names current in case of rename
                        team.CharacterNameA = loName;
                        team.CharacterNameB = hiName;
                        context.Entry(team).State = EntityState.Modified;
                    }

                    team.TotalMatches      += totalMatches;
                    team.TotalWins         += totalWins;
                    team.TotalDraws        += totalDraws;
                    team.TotalLosses       += totalLosses;
                    team.TotalDisqualified += totalDisqualified;
                    team.TotalSurvived     += totalSurvived;
                    team.LastMatchDatetime  = DateTime.Now;

                    if (newElo.HasValue)
                        team.Elo = newElo.Value;

                    // Team score is the team's ELO; mirror it into the snapshot column.
                    // Teams are exempt from decay, so LastDecayDatetime is left unused.
                    team.RankPoints = ArenaRanking.ComputeCompositeScore(team);

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception saving ArenaTeamStats. ex: {ex}");
            }
        }

        public ArenaCharacterStats GetCharacterArenaStatsByEvent(uint characterId, string eventType)
        {
            if (!IsConfigured) return null;
            try
            {
                using (var context = new LogDbContext())
                {
                    var stats = context.ArenaCharacterStats.FirstOrDefault(x => x.CharacterId == characterId && x.EventType.Equals(eventType));
                    if (stats != null)
                        stats.CompositeScore = IsEloEventType(eventType)
                            ? ArenaRanking.ComputeCompositeScore(stats)
                            : stats.RankPoints;
                    return stats;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in GetCharacterArenaStatsByEvent. ex:{ex}");
            }

            return null;
        }

        public string GetArenaStatsByCharacterId(uint characterId, string characterName)
        {
            if (!IsConfigured) return "Log database is not configured.";
            var returnMsg = new System.Text.StringBuilder();

            try
            {
                using (var context = new LogDbContext())
                {
                    var stats = context.ArenaCharacterStats.Where(x => x.CharacterId == characterId)?.ToList() ?? new List<ArenaCharacterStats>();

                    returnMsg.Append($"********* Arena Stats for {characterName} *********\n\n");

                    void AppendStats(ArenaCharacterStats s, string label, bool showRank)
                    {
                        bool isElo = IsEloEventType(s.EventType);
                        uint displayScore = isElo
                            ? ArenaRanking.ComputeCompositeScore(s)
                            : s.RankPoints;

                        returnMsg.Append($"{label}\n");
                        if (showRank)
                        {
                            int rank = isElo ? GetArenaRank(s) : GetArenaRankByPoints(s.EventType, s.RankPoints);
                            returnMsg.Append($"  Rank: {rank.ToString("n0")}\n");
                            if (isElo)
                            {
                                returnMsg.Append($"  ELO: {displayScore.ToString("n0")}\n");
                            }
                            else
                            {
                                returnMsg.Append($"  Points: {displayScore.ToString("n0")}\n");
                            }
                        }
                        returnMsg.Append($"  Matches: {s.TotalMatches.ToString("n0")}\n");
                        returnMsg.Append($"  Wins: {s.TotalWins.ToString("n0")}\n");
                        returnMsg.Append($"  Draws: {s.TotalDraws.ToString("n0")}\n");
                        returnMsg.Append($"  Losses: {s.TotalLosses.ToString("n0")}\n");
                        returnMsg.Append($"  Disqualified: {s.TotalDisqualified.ToString("n0")}\n");
                        returnMsg.Append($"  Kills: {s.TotalKills.ToString("n0")}\n");
                        returnMsg.Append($"  Deaths: {s.TotalDeaths.ToString("n0")}\n");
                        returnMsg.Append($"  Damage Dealt: {s.TotalDmgDealt.ToString("n0")}\n");
                        returnMsg.Append($"  Damage Received: {s.TotalDmgReceived.ToString("n0")}\n\n");
                    }

                    AppendStats(stats.FirstOrDefault(x => x.EventType.Equals("1v1"))   ?? new ArenaCharacterStats { EventType = "1v1",   Elo = 1500 }, "1v1",   true);
                    AppendStats(stats.FirstOrDefault(x => x.EventType.Equals("2v2"))   ?? new ArenaCharacterStats { EventType = "2v2",   Elo = 1500 }, "2v2",   true);
                    AppendStats(stats.FirstOrDefault(x => x.EventType.Equals("ffa"))   ?? new ArenaCharacterStats { EventType = "ffa"   }, "FFA",   true);
                    AppendStats(stats.FirstOrDefault(x => x.EventType.Equals("tugak")) ?? new ArenaCharacterStats { EventType = "tugak" }, "Tugak", true);
                    AppendStats(stats.FirstOrDefault(x => x.EventType.Equals("group")) ?? new ArenaCharacterStats { EventType = "group" }, "Group", false);

                    returnMsg.Append($"Totals:\n");
                    returnMsg.Append($"  Total Matches: {stats.Sum(x => x.TotalMatches).ToString("n0")}\n");
                    returnMsg.Append($"  Total Wins: {stats.Sum(x => x.TotalWins).ToString("n0")}\n");
                    returnMsg.Append($"  Total Draws: {stats.Sum(x => x.TotalDraws).ToString("n0")}\n");
                    returnMsg.Append($"  Total Losses: {stats.Sum(x => x.TotalLosses).ToString("n0")}\n");
                    returnMsg.Append($"  Total Disqualified: {stats.Sum(x => x.TotalDisqualified).ToString("n0")}\n");
                    returnMsg.Append($"  Total Kills: {stats.Sum(x => x.TotalKills).ToString("n0")}\n");
                    returnMsg.Append($"  Total Deaths: {stats.Sum(x => x.TotalDeaths).ToString("n0")}\n");
                    returnMsg.Append($"  Total Damage Dealt: {stats.Sum(x => x.TotalDmgDealt).ToString("n0")}\n");
                    returnMsg.Append($"  Total Damage Received: {stats.Sum(x => x.TotalDmgReceived).ToString("n0")}\n\n");
                    returnMsg.Append($"*****************************\n");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetArenaStatsByCharacterId for characterId = {characterId}. ex: {ex}");
            }

            return returnMsg.ToString();
        }

        // 1v1 and 2v2 are ELO-based; ffa, tugak, group are not.
        private static bool IsEloEventType(string eventType) =>
            eventType == "1v1" || eventType == "2v2";

        /// <summary>
        /// Returns the player's leaderboard rank for a 1v1 or 2v2 event type,
        /// which is simply their position by ELO rating.
        /// </summary>
        public int GetArenaRank(ArenaCharacterStats playerStats)
        {
            if (!IsConfigured) return -1;
            try
            {
                uint playerScore = ArenaRanking.ComputeCompositeScore(playerStats);
                using (var context = new LogDbContext())
                {
                    int higher = context.ArenaCharacterStats
                        .Count(x => x.EventType.Equals(playerStats.EventType) && x.Elo > playerScore);
                    return higher + 1;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in GetArenaRank (ELO). ex:{ex}");
            }
            return -1;
        }

        /// <summary>
        /// Returns the player's leaderboard rank for FFA / Tugak (points-based, SQL-side).
        /// </summary>
        public int GetArenaRankByPoints(string eventType, uint rankPoints)
        {
            if (!IsConfigured) return -1;
            try
            {
                using (var context = new LogDbContext())
                {
                    int higher = context.ArenaCharacterStats
                        .Count(x => x.EventType.Equals(eventType) && x.RankPoints > rankPoints);
                    return higher + 1;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in GetArenaRankByPoints. ex:{ex}");
            }
            return -1;
        }

        /// <summary>
        /// Returns the top 10 players for the given event type.
        /// For 1v1/2v2, sorts by ELO rating (decay is already baked into the stored value).
        /// For FFA/Tugak, sorts by accumulated placement points via SQL.
        /// </summary>
        public List<ArenaCharacterStats> GetArenaTopRankedByEventType(string eventType)
        {
            if (!IsConfigured) return new List<ArenaCharacterStats>();
            try
            {
                using (var context = new LogDbContext())
                {
                    if (IsEloEventType(eventType))
                    {
                        var topTen = context.ArenaCharacterStats
                            .Where(x => x.EventType.Equals(eventType))
                            .OrderByDescending(x => x.Elo)
                            .Take(10)
                            .ToList();

                        foreach (var s in topTen)
                            s.CompositeScore = ArenaRanking.ComputeCompositeScore(s);

                        return topTen;
                    }
                    else
                    {
                        // FFA / Tugak: accumulated placement points, sort in SQL.
                        var topTen = context.ArenaCharacterStats
                            .Where(x => x.EventType.Equals(eventType))
                            .OrderByDescending(x => x.RankPoints)
                            .Take(10)
                            .ToList();

                        foreach (var s in topTen)
                            s.CompositeScore = s.RankPoints;

                        return topTen;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in GetArenaTopRankedByEventType. ex:{ex}");
            }

            return new List<ArenaCharacterStats>();
        }

        /// <summary>
        /// Returns the top 10 2v2 team pairs ranked by team ELO rating.
        /// </summary>
        public List<ArenaTeamStats> GetArenaTopRankedTeams()
        {
            if (!IsConfigured) return new List<ArenaTeamStats>();
            try
            {
                using (var context = new LogDbContext())
                {
                    var topTeams = context.ArenaTeamStats
                        .OrderByDescending(x => x.Elo)
                        .Take(10)
                        .ToList();

                    foreach (var t in topTeams)
                        t.CompositeScore = ArenaRanking.ComputeCompositeScore(t);

                    return topTeams;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in GetArenaTopRankedTeams. ex:{ex}");
            }
            return new List<ArenaTeamStats>();
        }

        /// <summary>
        /// Counts completed 1v1 and 2v2 matches per character over the last
        /// <see cref="ArenaRanking.EloDecayWindowDays"/> days, keyed by
        /// (characterId, eventType).  Only events that actually finished count —
        /// status 5 (ended with a winner) and 6 (time limit reached); cancelled
        /// events (status -1) never awarded a match and are excluded here too.
        /// </summary>
        private static Dictionary<(uint CharacterId, string EventType), int> GetRecentArenaMatchCounts(LogDbContext context)
        {
            var cutoff = DateTime.Now.AddDays(-ArenaRanking.EloDecayWindowDays);

            var counts = (from p in context.ArenaPlayers.AsNoTracking()
                          join e in context.ArenaEvents.AsNoTracking()
                            on p.EventId equals (uint?)e.Id
                          where (p.EventType == "1v1" || p.EventType == "2v2")
                             && e.Status >= 5
                             && e.EndDateTime >= cutoff
                          group p by new { p.CharacterId, p.EventType } into g
                          select new { g.Key.CharacterId, g.Key.EventType, Matches = g.Count() })
                         .ToList();

            return counts.ToDictionary(x => (x.CharacterId, x.EventType), x => x.Matches);
        }

        /// <summary>
        /// Applies one day of ELO decay to all 1v1 and 2v2 character stats rows.
        /// Called once per calendar day by ArenaManager.Tick().
        ///
        /// <para>How much decays depends on how many matches the player completed in
        /// that same format over the last <see cref="ArenaRanking.EloDecayWindowDays"/>
        /// days — see <see cref="ArenaRanking.GetDailyDecayRate"/>.  Only the rating
        /// above <see cref="ArenaRanking.EloBaseline"/> decays, and it never drops
        /// below that baseline.  Team ratings do not decay at all.</para>
        ///
        /// <para><c>LastDecayDatetime</c> is stamped on every row examined, whether or
        /// not decay was owed, so a server restart on the same day cannot apply a
        /// second day of decay.</para>
        /// </summary>
        public void ApplyArenaEloDecay()
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    var matchCounts = GetRecentArenaMatchCounts(context);
                    var today       = DateTime.Now.Date;

                    var eloRows = context.ArenaCharacterStats
                        .Where(x => x.EventType == "1v1" || x.EventType == "2v2")
                        .ToList();

                    bool anyChanged = false;
                    foreach (var stats in eloRows)
                    {
                        // Never played this format, or already settled today.
                        if (!stats.LastMatchDatetime.HasValue) continue;
                        if (stats.LastDecayDatetime.HasValue && stats.LastDecayDatetime.Value.Date >= today) continue;

                        matchCounts.TryGetValue((stats.CharacterId, stats.EventType), out int recentMatches);

                        var newElo = ArenaRanking.ApplyDailyDecay(stats.Elo, stats.EventType, recentMatches);

                        stats.LastDecayDatetime = DateTime.Now;

                        if (newElo.HasValue)
                        {
                            stats.Elo        = newElo.Value;
                            stats.RankPoints = newElo.Value;   // keep the persisted snapshot in sync
                        }

                        anyChanged = true;
                    }

                    if (anyChanged)
                        context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in ApplyArenaEloDecay. Ex: {ex}");
            }
        }

        public uint CreateArenaPlayer(ArenaPlayer player)
        {
            if (!IsConfigured) return 0;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.ArenaPlayers.Add(player);
                    context.SaveChanges();
                    return player.Id;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in CreateArenaPlayer. Ex: {ex}");
            }

            return 0;
        }

        public void UpdateArenaPlayer(ArenaPlayer player)
        {
            if (!IsConfigured) return;
            using (var context = new LogDbContext())
            {
                context.Entry(player).State = EntityState.Modified;
                context.SaveChanges();
            }
        }

        public List<ArenaEvent> GetAllArenaEvents()
        {
            if (!IsConfigured) return new List<ArenaEvent>();
            List<ArenaEvent> eventList = null;

            try
            {
                using (var context = new LogDbContext())
                {
                    eventList = context.ArenaEvents
                        .AsNoTracking()
                        .OrderByDescending(r => r.StartDateTime)
                        .Where(r => r.EndDateTime.HasValue)
                        ?.ToList() ?? new List<ArenaEvent>();
                }

                foreach (var arenaEvent in eventList)
                    arenaEvent.Players = GetAllArenaPlayersByEvent(arenaEvent.Id);
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetAllArenaEvents. Ex: {ex}");
            }

            return eventList ?? new List<ArenaEvent>();
        }

        public List<ArenaPlayer> GetAllArenaPlayersByEvent(uint eventId)
        {
            if (!IsConfigured) return new List<ArenaPlayer>();
            List<ArenaPlayer> playerList = null;

            try
            {
                using (var context = new LogDbContext())
                {
                    playerList = context.ArenaPlayers
                        .AsNoTracking()
                        .Where(x => x.EventId == (uint?)eventId)
                        ?.ToList();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetAllArenaPlayersByEvent. Ex: {ex}");
            }

            return playerList ?? new List<ArenaPlayer>();
        }

        #endregion

        #region Rare Log

        public void LogRare(RareLog rareLog)
        {
            if (!IsConfigured || rareLog == null) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"INSERT INTO rare_log (characterName, characterId, itemName, itemBiotaId, itemWeenieId, createDateTime)
                            VALUES ({rareLog.CharacterName}, {rareLog.CharacterId}, {rareLog.ItemName}, {rareLog.ItemBiotaId}, {rareLog.ItemWeenieId}, {DateTime.Now});");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogRare saving rare event to DB. ex: {ex}");
            }
        }

        #endregion

        #region Stuck Character Log

        public void LogStuckCharacter(StuckCharacterLog stuckLog)
        {
            if (!IsConfigured || stuckLog == null) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.Database.ExecuteSql(
                        @$"INSERT INTO stuck_character_log
                        (playerGuid, playerName, accountName, accountId, sessionInfo, landblock, location,
                         isLoggingOut, isInDeathProcess, foundOnLandblock, forcedLogOffRequested,
                         pkLogoutState, materializedLogoutState, logoffPath, createdAtUtc)
                        VALUES
                        ({stuckLog.PlayerGuid},
                         {stuckLog.PlayerName ?? (object)DBNull.Value},
                         {stuckLog.AccountName ?? (object)DBNull.Value},
                         {stuckLog.AccountId},
                         {stuckLog.SessionInfo ?? (object)DBNull.Value},
                         {stuckLog.Landblock ?? (object)DBNull.Value},
                         {stuckLog.Location ?? (object)DBNull.Value},
                         {stuckLog.IsLoggingOut},
                         {stuckLog.IsInDeathProcess},
                         {stuckLog.FoundOnLandblock},
                         {stuckLog.ForcedLogOffRequested},
                         {stuckLog.PkLogoutState},
                         {stuckLog.MaterializedLogoutState},
                         {stuckLog.LogoffPath ?? (object)DBNull.Value},
                         {DateTime.Now});");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogStuckCharacter saving stuck character event to DB. ex: {ex}");
            }
        }

        #endregion

        #region Movement Violation

        /// <summary>
        /// Records a single movement anti-cheat violation event for long-term ban evidence.
        /// Fire-and-forget: failures are logged but never thrown to the caller.
        /// </summary>
        /// <param name="violationType">
        /// Short identifier for the check that fired, e.g. "speed_packet", "script_timing",
        /// "geometry", "door_ghost".  See MovementViolationLog.ViolationType for the full list.
        /// </param>
        public void LogMovementViolation(uint characterId, string characterName, string accountName,
            string violationType, float observedSpeed, float allowedSpeed, float suspicionScore, string location)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    context.MovementViolationLogs.Add(new MovementViolationLog
                    {
                        CharacterId       = characterId,
                        CharacterName     = characterName,
                        AccountName       = accountName,
                        ViolationType     = violationType ?? "unknown",
                        ObservedSpeed     = observedSpeed,
                        AllowedSpeed      = allowedSpeed,
                        SuspicionScore    = suspicionScore,
                        Location          = location,
                        ViolationDateTime = DateTime.UtcNow
                    });
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LogMovementViolation for character {characterName} ({characterId}). Ex: {ex}");
            }
        }

        #endregion

        #region Allegiance Hometown

        public List<AllegianceHometownTown> GetAllAllegianceHometownTowns()
        {
            if (!IsConfigured) return new List<AllegianceHometownTown>();
            try
            {
                using (var context = new LogDbContext())
                    return context.AllegianceHometownTowns.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetAllAllegianceHometownTowns. Ex: {ex}");
            }
            return new List<AllegianceHometownTown>();
        }

        public void UpdateAllegianceHometownTown(AllegianceHometownTown town)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    var rec = context.AllegianceHometownTowns.FirstOrDefault(x => x.TownId == town.TownId);
                    if (rec == null) return;

                    rec.OwnerMonarchId             = town.OwnerMonarchId;
                    rec.OwnerAllegianceName        = town.OwnerAllegianceName;
                    rec.CapturedAt                 = town.CapturedAt;
                    rec.ConflictPhase              = town.ConflictPhase;
                    rec.ConflictAttackerMonarchId  = town.ConflictAttackerMonarchId;
                    rec.ConflictAttackerName       = town.ConflictAttackerName;
                    rec.ConflictStartTime          = town.ConflictStartTime;
                    rec.Phase2StartTime            = town.Phase2StartTime;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in UpdateAllegianceHometownTown for TownId={town.TownId}. Ex: {ex}");
            }
        }

        public AllegianceHometownEvent StartAllegianceHometownEvent(byte townId, uint attackerMonarchId,
            string attackerAllegianceName, uint? defenderMonarchId, string defenderAllegianceName)
        {
            if (!IsConfigured) return null;
            try
            {
                var evt = new AllegianceHometownEvent
                {
                    TownId                  = townId,
                    AttackerMonarchId       = attackerMonarchId,
                    AttackerAllegianceName  = attackerAllegianceName,
                    DefenderMonarchId       = defenderMonarchId,
                    DefenderAllegianceName  = defenderAllegianceName,
                    EventStartTime          = DateTime.UtcNow,
                };

                using (var context = new LogDbContext())
                {
                    context.AllegianceHometownEvents.Add(evt);
                    context.SaveChanges();
                }

                return evt;
            }
            catch (Exception ex)
            {
                log.Error($"Exception in StartAllegianceHometownEvent for TownId={townId}. Ex: {ex}");
            }
            return null;
        }

        public void UpdateAllegianceHometownEvent(AllegianceHometownEvent evt)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    var rec = context.AllegianceHometownEvents.FirstOrDefault(x => x.EventId == evt.EventId);
                    if (rec == null) return;

                    rec.Phase2StartTime      = evt.Phase2StartTime;
                    rec.EventEndTime         = evt.EventEndTime;
                    rec.Outcome              = evt.Outcome;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in UpdateAllegianceHometownEvent for EventId={evt.EventId}. Ex: {ex}");
            }
        }

        public List<AllegianceHometownBlacklist> GetAllAllegianceHometownBlacklist()
        {
            if (!IsConfigured) return new List<AllegianceHometownBlacklist>();
            try
            {
                using (var context = new LogDbContext())
                    return context.AllegianceHometownBlacklists.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetAllAllegianceHometownBlacklist. Ex: {ex}");
            }
            return new List<AllegianceHometownBlacklist>();
        }

        public void AddAllegianceHometownBlacklist(AllegianceHometownBlacklist entry)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    if (!context.AllegianceHometownBlacklists.Any(x => x.MonarchId == entry.MonarchId))
                        context.AllegianceHometownBlacklists.Add(entry);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in AddAllegianceHometownBlacklist for MonarchId={entry.MonarchId}. Ex: {ex}");
            }
        }

        public void RemoveAllegianceHometownBlacklist(uint monarchId)
        {
            if (!IsConfigured) return;
            try
            {
                using (var context = new LogDbContext())
                {
                    var rec = context.AllegianceHometownBlacklists.FirstOrDefault(x => x.MonarchId == monarchId);
                    if (rec != null)
                    {
                        context.AllegianceHometownBlacklists.Remove(rec);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in RemoveAllegianceHometownBlacklist for MonarchId={monarchId}. Ex: {ex}");
            }
        }

        #endregion

        // ====================================================================
        #region Season

        // ── Kill / Death / Bounty increments ────────────────────────────────

        /// <summary>
        /// Increments pk_kills and updates the kill streak columns for the killer.
        /// Upserts the row if it does not yet exist.
        /// </summary>
        public void UpdateSeasonKill(uint characterId, string characterName, uint newCurrentStreak)
        {
            if (!IsConfigured) return;
            try
            {
                using var context = new LogDbContext();
                var row = GetOrCreateSeasonStats(context, characterId, characterName);

                row.PkKills++;
                row.PkKillStreakCur = newCurrentStreak;
                if (newCurrentStreak > row.PkKillStreakBest)
                    row.PkKillStreakBest = newCurrentStreak;

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in UpdateSeasonKill for characterId={characterId}. Ex: {ex}");
            }
        }

        /// <summary>
        /// Increments pk_deaths and resets the current kill streak to 0.
        /// </summary>
        public void UpdateSeasonDeath(uint characterId, string characterName)
        {
            if (!IsConfigured) return;
            try
            {
                using var context = new LogDbContext();
                var row = GetOrCreateSeasonStats(context, characterId, characterName);

                row.PkDeaths++;
                row.PkKillStreakCur = 0;

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in UpdateSeasonDeath for characterId={characterId}. Ex: {ex}");
            }
        }

        /// <summary>
        /// Increments bounties_completed for the given character.
        /// </summary>
        public void UpdateSeasonBounty(uint characterId, string characterName)
        {
            if (!IsConfigured) return;
            try
            {
                using var context = new LogDbContext();
                var row = GetOrCreateSeasonStats(context, characterId, characterName);
                row.BountiesCompleted++;
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in UpdateSeasonBounty for characterId={characterId}. Ex: {ex}");
            }
        }

        private static Models.Log.SeasonCharacterStats GetOrCreateSeasonStats(
            LogDbContext context, uint characterId, string characterName)
        {
            var row = context.SeasonCharacterStats
                .FirstOrDefault(x => x.CharacterId == characterId);

            if (row == null)
            {
                row = new Models.Log.SeasonCharacterStats
                {
                    CharacterId   = characterId,
                    CharacterName = characterName
                };
                context.SeasonCharacterStats.Add(row);
            }
            else
            {
                // Keep name current in case of rename
                row.CharacterName = characterName;
                context.Entry(row).State = EntityState.Modified;
            }

            return row;
        }

        // ── Streak initialisation (called on server start) ───────────────────

        /// <summary>
        /// Loads all current kill streaks from the DB into a dictionary keyed by
        /// character_id.  Used by SeasonManager to restore in-memory state on startup.
        /// </summary>
        public Dictionary<uint, uint> LoadAllCurrentStreaks()
        {
            if (!IsConfigured) return new Dictionary<uint, uint>();
            try
            {
                using var context = new LogDbContext();
                return context.SeasonCharacterStats
                    .AsNoTracking()
                    .Where(x => x.PkKillStreakCur > 0)
                    .ToDictionary(x => x.CharacterId, x => x.PkKillStreakCur);
            }
            catch (Exception ex)
            {
                log.Error($"Exception in LoadAllCurrentStreaks. Ex: {ex}");
            }
            return new Dictionary<uint, uint>();
        }

        // ── Leaderboard queries ──────────────────────────────────────────────

        /// <summary>
        /// Returns the top <paramref name="count"/> rows for the given category.
        /// Arena categories are read from arena_character_stats (aggregated where needed).
        /// PK/bounty categories are read from season_character_stats.
        /// The overall category is computed from rank-points across all other categories.
        /// </summary>
        public List<SeasonLeaderEntry> GetSeasonTopForCategory(string category, int count = 10)
        {
            if (!IsConfigured) return new List<SeasonLeaderEntry>();
            try
            {
                using var context = new LogDbContext();
                return category switch
                {
                    SeasonConfig.Cat_1v1 or SeasonConfig.Cat_2v2 =>
                        GetTopArenaElo(context, category, count),

                    SeasonConfig.Cat_Ffa or SeasonConfig.Cat_Tugak =>
                        GetTopArenaPoints(context, category, count),

                    SeasonConfig.Cat_Group =>
                        GetTopArenaGroupWins(context, count),

                    SeasonConfig.Cat_ArenaWins =>
                        GetTopArenaAggregate(context, "total_wins", count),

                    SeasonConfig.Cat_ArenaMatches =>
                        GetTopArenaAggregate(context, "total_matches", count),

                    SeasonConfig.Cat_Bounty =>
                        GetTopSeasonColumn(context, category,
                            q => q.OrderByDescending(x => x.BountiesCompleted),
                            x => (ulong)x.BountiesCompleted, count),

                    SeasonConfig.Cat_PkKills =>
                        GetTopSeasonColumn(context, category,
                            q => q.OrderByDescending(x => x.PkKills),
                            x => (ulong)x.PkKills, count),

                    SeasonConfig.Cat_PkKd =>
                        GetTopPkKd(context, count),

                    SeasonConfig.Cat_PkStreak =>
                        GetTopSeasonColumn(context, category,
                            q => q.OrderByDescending(x => x.PkKillStreakBest),
                            x => (ulong)x.PkKillStreakBest, count),

                    SeasonConfig.Cat_Overall =>
                        GetTopOverall(context, count),

                    _ => new List<SeasonLeaderEntry>()
                };
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetSeasonTopForCategory('{category}'). Ex: {ex}");
            }
            return new List<SeasonLeaderEntry>();
        }

        // Arena — ELO-based (1v1, 2v2)
        private static List<SeasonLeaderEntry> GetTopArenaElo(
            LogDbContext context, string category, int count)
        {
            var top = context.ArenaCharacterStats
                .AsNoTracking()
                .Where(x => x.EventType == category)
                .OrderByDescending(x => x.Elo)
                .Take(count)
                .ToList();

            foreach (var s in top)
                s.CompositeScore = ArenaRanking.ComputeCompositeScore(s);

            return top
                .Select((s, i) => new SeasonLeaderEntry
                {
                    Rank          = i + 1,
                    CharacterId   = s.CharacterId,
                    CharacterName = s.CharacterName,
                    Score         = s.CompositeScore,
                    ScoreDisplay  = $"{s.CompositeScore:n0} ELO"
                })
                .ToList();
        }

        // Arena — placement-points (ffa, tugak)
        private static List<SeasonLeaderEntry> GetTopArenaPoints(
            LogDbContext context, string category, int count)
        {
            return context.ArenaCharacterStats
                .AsNoTracking()
                .Where(x => x.EventType == category)
                .OrderByDescending(x => x.RankPoints)
                .Take(count)
                .AsEnumerable()
                .Select((s, i) => new SeasonLeaderEntry
                {
                    Rank          = i + 1,
                    CharacterId   = s.CharacterId,
                    CharacterName = s.CharacterName,
                    Score         = s.RankPoints,
                    ScoreDisplay  = s.RankPoints.ToString("n0")
                })
                .ToList();
        }

        // Arena — group wins
        private static List<SeasonLeaderEntry> GetTopArenaGroupWins(
            LogDbContext context, int count)
        {
            return context.ArenaCharacterStats
                .AsNoTracking()
                .Where(x => x.EventType == SeasonConfig.Cat_Group)
                .OrderByDescending(x => x.TotalWins)
                .Take(count)
                .AsEnumerable()
                .Select((s, i) => new SeasonLeaderEntry
                {
                    Rank          = i + 1,
                    CharacterId   = s.CharacterId,
                    CharacterName = s.CharacterName,
                    Score         = s.TotalWins,
                    ScoreDisplay  = s.TotalWins.ToString("n0")
                })
                .ToList();
        }

        // Arena — aggregate across all event types (wins, kills, or matches)
        private static List<SeasonLeaderEntry> GetTopArenaAggregate(
            LogDbContext context, string column, int count)
        {
            // EF Core doesn't support dynamic column selection cleanly, so we
            // load all rows for all event types and aggregate in-memory.
            // This is acceptably cheap given the small row counts in arena_character_stats.
            var grouped = context.ArenaCharacterStats
                .AsNoTracking()
                .ToList()
                .GroupBy(x => new { x.CharacterId, x.CharacterName })
                .Select(g => new
                {
                    g.Key.CharacterId,
                    g.Key.CharacterName,
                    Total = column switch
                    {
                        "total_wins"    => (ulong)g.Sum(x => (long)x.TotalWins),
                        "total_kills"   => (ulong)g.Sum(x => (long)x.TotalKills),
                        "total_matches" => (ulong)g.Sum(x => (long)x.TotalMatches),
                        _               => 0UL
                    }
                })
                .OrderByDescending(x => x.Total)
                .Take(count)
                .ToList();

            return grouped.Select((x, i) => new SeasonLeaderEntry
            {
                Rank          = i + 1,
                CharacterId   = x.CharacterId,
                CharacterName = x.CharacterName,
                Score         = x.Total,
                ScoreDisplay  = x.Total.ToString("n0")
            }).ToList();
        }

        // Season — generic column accessor for season_character_stats
        private static List<SeasonLeaderEntry> GetTopSeasonColumn(
            LogDbContext context,
            string category,
            Func<IQueryable<Models.Log.SeasonCharacterStats>,
                 IOrderedQueryable<Models.Log.SeasonCharacterStats>> orderBy,
            Func<Models.Log.SeasonCharacterStats, ulong> scoreSelector,
            int count)
        {
            return orderBy(context.SeasonCharacterStats.AsNoTracking())
                .Take(count)
                .AsEnumerable()
                .Select((x, i) => new SeasonLeaderEntry
                {
                    Rank          = i + 1,
                    CharacterId   = x.CharacterId,
                    CharacterName = x.CharacterName,
                    Score         = scoreSelector(x),
                    ScoreDisplay  = scoreSelector(x).ToString("n0")
                })
                .ToList();
        }

        // PK K/D ratio (min SeasonConfig.PkKd_MinKills kills required)
        private static List<SeasonLeaderEntry> GetTopPkKd(LogDbContext context, int count)
        {
            return context.SeasonCharacterStats
                .AsNoTracking()
                .Where(x => x.PkKills >= SeasonConfig.PkKd_MinKills)
                .ToList()
                .OrderByDescending(x => x.KdRatio)
                .Take(count)
                .Select((x, i) =>
                {
                    var kd = x.KdRatio;
                    return new SeasonLeaderEntry
                    {
                        Rank          = i + 1,
                        CharacterId   = x.CharacterId,
                        CharacterName = x.CharacterName,
                        Score         = (ulong)(kd * 1000),      // scaled for integer storage
                        ScoreDisplay  = $"{kd:0.00}  ({x.PkKills:n0} K / {x.PkDeaths:n0} D)"
                    };
                })
                .ToList();
        }

        // Overall — weighted rank-point sum across all ScoredCategories
        private List<SeasonLeaderEntry> GetTopOverall(LogDbContext context, int count)
        {
            // Build rank-lists for all scored categories then compute weighted totals.
            var scores = new Dictionary<uint, (string name, double total)>();

            foreach (var cat in SeasonConfig.ScoredCategories)
            {
                if (!SeasonConfig.CategoryWeights.TryGetValue(cat, out var weight))
                    continue;

                var top = GetSeasonTopForCategory(cat, 9999);   // full list for rank computation
                for (int i = 0; i < top.Count; i++)
                {
                    var entry = top[i];
                    var pts   = SeasonConfig.RankToPoints(i + 1) * weight;

                    if (scores.TryGetValue(entry.CharacterId, out var existing))
                        scores[entry.CharacterId] = (existing.name, existing.total + pts);
                    else
                        scores[entry.CharacterId] = (entry.CharacterName, pts);
                }
            }

            return scores
                .OrderByDescending(kv => kv.Value.total)
                .Take(count)
                .Select((kv, i) => new SeasonLeaderEntry
                {
                    Rank          = i + 1,
                    CharacterId   = kv.Key,
                    CharacterName = kv.Value.name,
                    Score         = (ulong)(kv.Value.total * 100), // scaled to avoid float storage
                    ScoreDisplay  = $"{kv.Value.total:0.0} pts"
                })
                .ToList();
        }

        // ── Per-player standings ─────────────────────────────────────────────

        /// <summary>
        /// Returns the rank and score of <paramref name="characterId"/> in every
        /// scored category plus overall.  Unranked categories show rank 0.
        /// </summary>
        public SeasonPlayerStanding GetSeasonPlayerStanding(uint characterId, string characterName)
        {
            if (!IsConfigured)
                return new SeasonPlayerStanding { CharacterId = characterId, CharacterName = characterName };

            var standing = new SeasonPlayerStanding
            {
                CharacterId   = characterId,
                CharacterName = characterName
            };

            try
            {
                // Pull a full top list for every category and find where this player sits.
                foreach (var cat in SeasonConfig.ScoredCategories)
                {
                    var top  = GetSeasonTopForCategory(cat, 9999);
                    var mine = top.FirstOrDefault(x => x.CharacterId == characterId);
                    standing.CategoryStandings[cat] = mine ?? new SeasonLeaderEntry
                    {
                        Rank          = 0,
                        CharacterId   = characterId,
                        CharacterName = characterName,
                        Score         = 0,
                        ScoreDisplay  = "0"
                    };
                }

                // Overall
                var overallTop  = GetSeasonTopForCategory(SeasonConfig.Cat_Overall, 9999);
                var overallMine = overallTop.FirstOrDefault(x => x.CharacterId == characterId);
                standing.CategoryStandings[SeasonConfig.Cat_Overall] = overallMine ?? new SeasonLeaderEntry
                {
                    Rank          = 0,
                    CharacterId   = characterId,
                    CharacterName = characterName,
                    Score         = 0,
                    ScoreDisplay  = "0 pts"
                };
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetSeasonPlayerStanding for characterId={characterId}. Ex: {ex}");
            }

            return standing;
        }

        // ── Milestone snapshot ───────────────────────────────────────────────

        /// <summary>
        /// Captures the current top-10 for every scored category (plus overall)
        /// into a new season_milestone + season_milestone_leader rows.
        /// Returns the new milestone ID, or 0 on failure.
        /// </summary>
        public ushort CaptureSeasonMilestone(ushort weekNumber)
        {
            if (!IsConfigured) return 0;
            try
            {
                // Insert the milestone header
                var milestone = new Models.Log.SeasonMilestone
                {
                    WeekNumber       = weekNumber,
                    SnapshotDatetime = DateTime.Now
                };

                using var context = new LogDbContext();
                context.SeasonMilestones.Add(milestone);
                context.SaveChanges();

                var milestoneId = milestone.Id;

                // Snapshot every category
                var allCategories = new List<string>(SeasonConfig.ScoredCategories) { SeasonConfig.Cat_Overall };
                var leaderRows    = new List<Models.Log.SeasonMilestoneLeader>();

                foreach (var cat in allCategories)
                {
                    var top = GetSeasonTopForCategory(cat, 10);
                    foreach (var entry in top)
                    {
                        leaderRows.Add(new Models.Log.SeasonMilestoneLeader
                        {
                            MilestoneId   = milestoneId,
                            WeekNumber    = weekNumber,
                            Category      = cat,
                            Rank          = (byte)entry.Rank,
                            CharacterId   = entry.CharacterId,
                            CharacterName = entry.CharacterName,
                            Score         = entry.Score,
                            RewardClaimed = false
                        });
                    }
                }

                context.SeasonMilestoneLeaders.AddRange(leaderRows);
                context.SaveChanges();

                return milestoneId;
            }
            catch (Exception ex)
            {
                log.Error($"Exception in CaptureSeasonMilestone (week {weekNumber}). Ex: {ex}");
            }
            return 0;
        }

        // ── Reward claim ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns all unclaimed milestone leader rows for a given character.
        /// </summary>
        public List<Models.Log.SeasonMilestoneLeader> GetUnclaimedMilestoneLeaders(uint characterId)
        {
            if (!IsConfigured) return new List<Models.Log.SeasonMilestoneLeader>();
            try
            {
                using var context = new LogDbContext();
                return context.SeasonMilestoneLeaders
                    .AsNoTracking()
                    .Where(x => x.CharacterId == characterId && !x.RewardClaimed)
                    .OrderBy(x => x.WeekNumber)
                    .ThenBy(x => x.Category)
                    .ToList();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetUnclaimedMilestoneLeaders for characterId={characterId}. Ex: {ex}");
            }
            return new List<Models.Log.SeasonMilestoneLeader>();
        }

        /// <summary>
        /// Marks a single milestone leader row as claimed.
        /// </summary>
        public void MarkMilestoneLeaderClaimed(uint rowId)
        {
            if (!IsConfigured) return;
            try
            {
                using var context = new LogDbContext();
                var row = context.SeasonMilestoneLeaders.FirstOrDefault(x => x.Id == rowId);
                if (row == null) return;
                row.RewardClaimed   = true;
                row.ClaimedDatetime = DateTime.Now;
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in MarkMilestoneLeaderClaimed for rowId={rowId}. Ex: {ex}");
            }
        }

        /// <summary>
        /// Returns the highest week_number recorded in season_milestone, or 0 if none.
        /// Used by SeasonManager on startup to restore the week counter.
        /// </summary>
        public ushort GetLastMilestoneWeekNumber()
        {
            if (!IsConfigured) return 0;
            try
            {
                using var context = new LogDbContext();
                if (!context.SeasonMilestones.Any()) return 0;
                return context.SeasonMilestones.Max(x => x.WeekNumber);
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetLastMilestoneWeekNumber. Ex: {ex}");
            }
            return 0;
        }

        /// <summary>
        /// Returns the snapshot_datetime of the most recent milestone, or null if none.
        /// Used by SeasonManager on startup to avoid re-firing the same Sunday's snapshot.
        /// </summary>
        public DateTime? GetLastMilestoneDatetime()
        {
            if (!IsConfigured) return null;
            try
            {
                using var context = new LogDbContext();
                if (!context.SeasonMilestones.Any()) return null;
                return context.SeasonMilestones
                    .OrderByDescending(x => x.Id)
                    .Select(x => (DateTime?)x.SnapshotDatetime)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                log.Error($"Exception in GetLastMilestoneDatetime. Ex: {ex}");
            }
            return null;
        }

        #endregion
    }
}
