using System;
using ACE.Database.Models.Log;

namespace ACE.Database
{
    public static class ArenaRanking
    {
        // -----------------------------------------------------------------------
        // ELO helpers (shared by 1v1 and 2v2)
        // -----------------------------------------------------------------------

        public static float GetProbabilityWinning(float ratingPlayer1, float ratingPlayer2)
        {
            return 1f / (1f + MathF.Pow(10f, (ratingPlayer2 - ratingPlayer1) / 400f));
        }

        /// <summary>
        /// Returns how many ELO points transfer from the loser to the winner.
        /// </summary>
        public static int GetRankChange(uint winnerCurrentRank, uint loserCurrentRank, int multiplier)
        {
            float probabilityWinPlayer1 = GetProbabilityWinning(winnerCurrentRank, loserCurrentRank);
            return Convert.ToInt32(Math.Round(multiplier * (1 - probabilityWinPlayer1)));
        }

        // -----------------------------------------------------------------------
        // Leaderboard score (1v1, 2v2 individual, 2v2 teams)
        // -----------------------------------------------------------------------
        //
        // The leaderboard score IS the ELO rating.  Activity counters (wins,
        // matches played, 2v2 survivals) no longer contribute — activity is
        // rewarded through the decay tiers below instead, which let an active
        // player hold a rating that an inactive one bleeds away.

        // -----------------------------------------------------------------------
        // ELO decay (persisted to the database once per day by ArenaManager)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Starting ELO, and the baseline decay works against.  Only the portion of
        /// a rating above this value decays, and no amount of decay drops a rating
        /// below it.
        /// </summary>
        public const uint EloBaseline = 1500;

        /// <summary>How far back the activity window for the decay tiers reaches.</summary>
        public const int EloDecayWindowDays = 7;

        /// <summary>
        /// Daily decay tiers for 1v1, keyed by how many 1v1 matches the player
        /// completed in the last <see cref="EloDecayWindowDays"/> days.  The first
        /// tier whose threshold the match count falls under wins; a count at or
        /// above the last threshold decays nothing.
        /// </summary>
        private static readonly (int maxMatchesExclusive, double rate)[] DecayTiers1v1 =
        {
            ( 1, 0.05),   // no matches at all
            ( 3, 0.03),   // 1 - 2 matches
            (10, 0.01),   // 3 - 9 matches
            // 10+ matches — no decay
        };

        /// <summary>
        /// Daily decay tiers for 2v2.  Gentler than 1v1 and reaching zero sooner,
        /// because the format draws fewer players and a partner has to be available.
        /// </summary>
        private static readonly (int maxMatchesExclusive, double rate)[] DecayTiers2v2 =
        {
            (1, 0.03),    // no matches at all
            (3, 0.01),    // 1 - 2 matches
            // 3+ matches — no decay
        };

        /// <summary>
        /// Returns the fraction of the above-baseline rating that decays today for a
        /// player with <paramref name="matchesLast7Days"/> completed matches in that
        /// same event category.  Matches in another category do not count.
        /// </summary>
        public static double GetDailyDecayRate(string eventType, int matchesLast7Days)
        {
            var tiers = eventType == "2v2" ? DecayTiers2v2 : DecayTiers1v1;

            foreach (var tier in tiers)
            {
                if (matchesLast7Days < tier.maxMatchesExclusive)
                    return tier.rate;
            }

            return 0.0;
        }

        /// <summary>
        /// Applies one day of decay and returns the new ELO, or null when nothing is
        /// owed (rating at or below the baseline, an activity tier that does not
        /// decay, or a change too small to move the stored value).
        ///
        /// <para>Decay is taken from the rating *above* <see cref="EloBaseline"/>, not
        /// from the whole rating: at 1800 with no matches in the last week, a 5% 1v1
        /// tier removes 5% of 300, so 15 points, not 90.</para>
        ///
        /// <para>The caller persists the result and is responsible for running this at
        /// most once per calendar day per row.</para>
        /// </summary>
        public static uint? ApplyDailyDecay(uint currentElo, string eventType, int matchesLast7Days)
        {
            if (currentElo <= EloBaseline)
                return null;

            double rate = GetDailyDecayRate(eventType, matchesLast7Days);
            if (rate <= 0.0)
                return null;

            double excess  = currentElo - EloBaseline;
            uint   newElo  = (uint)Math.Max((double)EloBaseline, Math.Round(EloBaseline + excess * (1.0 - rate)));

            return newElo == currentElo ? (uint?)null : newElo;
        }

        /// <summary>
        /// Leaderboard score for a 1v1 or 2v2 individual stats row: the ELO rating
        /// itself.  Decay is applied daily by the background job and already written
        /// back to <see cref="ArenaCharacterStats.Elo"/>, so no run-time adjustment is
        /// needed here.  Never call this for FFA or Tugak.
        /// </summary>
        public static uint ComputeCompositeScore(ArenaCharacterStats stats)
        {
            return stats.Elo;
        }

        /// <summary>
        /// Leaderboard score for a 2v2 team stats row: the team's ELO rating.
        /// Team ratings do not decay.
        /// </summary>
        public static uint ComputeCompositeScore(ArenaTeamStats stats)
        {
            return stats.Elo;
        }

        // -----------------------------------------------------------------------
        // FFA / Tugak placement points
        // -----------------------------------------------------------------------

        /// <summary>1st-place points in FFA / Tugak events.</summary>
        public const uint FfaPoints_1st           = 100;
        /// <summary>2nd-place points.</summary>
        public const uint FfaPoints_2nd           = 50;
        /// <summary>3rd-place points.</summary>
        public const uint FfaPoints_3rd           = 25;
        /// <summary>Participation points (4th place and beyond).</summary>
        public const uint FfaPoints_Participation = 5;

        /// <summary>
        /// Returns how many ranking points to award for the given finish place
        /// in an FFA or Tugak event.  Disqualified players (finishPlace == -1)
        /// receive 0 points.
        /// </summary>
        public static uint GetFfaPlacementPoints(int finishPlace)
        {
            return finishPlace switch
            {
                1  => FfaPoints_1st,
                2  => FfaPoints_2nd,
                3  => FfaPoints_3rd,
                -1 => 0,                    // disqualified
                _  => FfaPoints_Participation
            };
        }
    }
}
