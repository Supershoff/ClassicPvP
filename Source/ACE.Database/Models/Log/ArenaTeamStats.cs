using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ACE.Database.Models.Log
{
    /// <summary>
    /// Aggregate ranking row for a unique 2v2 team pair.
    /// The team is identified by <see cref="TeamKey"/> which is always
    /// the lower character ID followed by the higher one: "{minId}_{maxId}".
    /// </summary>
    public partial class ArenaTeamStats
    {
        public uint Id { get; set; }

        /// <summary>"{minCharacterId}_{maxCharacterId}" — unique per pair.</summary>
        public string TeamKey { get; set; }

        public uint CharacterIdA { get; set; }
        public string CharacterNameA { get; set; }
        public uint CharacterIdB { get; set; }
        public string CharacterNameB { get; set; }

        [NotMapped]
        public string TeamName => $"{CharacterNameA} & {CharacterNameB}";

        /// <summary>
        /// Raw team ELO, and the team's leaderboard score.  Team ratings are
        /// exempt from the daily decay job.
        /// </summary>
        public uint Elo { get; set; }

        /// <summary>
        /// Score snapshot mirroring <see cref="Elo"/> as of the last match.
        /// Useful for raw DB queries; authoritative ranking uses CompositeScore.
        /// </summary>
        public uint RankPoints { get; set; }

        public uint TotalMatches { get; set; }
        public uint TotalWins { get; set; }
        public uint TotalLosses { get; set; }
        public uint TotalDraws { get; set; }
        public uint TotalDisqualified { get; set; }

        /// <summary>
        /// Number of matches where both team members survived (were not eliminated)
        /// as part of the winning result.  Tracked as a stat only — it does not
        /// affect the team's leaderboard score.
        /// </summary>
        public uint TotalSurvived { get; set; }

        /// <summary>
        /// Timestamp of the most recent match for this team pair.
        /// </summary>
        public DateTime? LastMatchDatetime { get; set; }

        /// <summary>
        /// Unused — teams are exempt from ELO decay.  Retained so the existing
        /// column keeps mapping cleanly.
        /// </summary>
        public DateTime? LastDecayDatetime { get; set; }

        // Computed in-memory by the database layer — not persisted.
        [NotMapped]
        public uint CompositeScore { get; set; }
    }
}
