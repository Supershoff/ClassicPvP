using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ACE.Database.Models.Log
{
    public partial class ArenaCharacterStats
    {
        public uint Id { get; set; }
        public uint CharacterId { get; set; }
        public string CharacterName { get; set; }

        [NotMapped]
        public uint CharacterLevel { get; set; }

        public string EventType { get; set; }

        // Raw ELO rating — used by 1v1 and 2v2 individual rankings.
        // Updated per-match.  ELO decay is applied daily by the background job
        // and written back to this column, so the stored value is always the
        // current effective rating — no run-time adjustment is needed.
        // FFA/Tugak leave this at its default (1500) and do not use it.
        public uint Elo { get; set; }

        // Accumulated placement points — used by FFA and Tugak rankings.
        // For 1v1/2v2 this column mirrors Elo, which is the leaderboard score
        // for those formats; ranking reads Elo directly.
        public uint RankPoints { get; set; }

        // Timestamp of the most recent ranked match in this event category.
        // Playing a 1v1 does NOT reset the 2v2 decay clock; each event type
        // is tracked independently.
        public DateTime? LastMatchDatetime { get; set; }

        // Timestamp through which ELO decay has been settled — stamped by the
        // daily background job whether or not decay was owed, and by a match
        // result.  The job skips any row already settled today, so a server
        // restart cannot apply a second day of decay.
        public DateTime? LastDecayDatetime { get; set; }

        // Number of 2v2 matches where this player survived (was not eliminated)
        // as part of the winning team.  Tracked as a stat only — it does not
        // affect the leaderboard score.
        public uint TotalSurvived { get; set; }

        // Computed in-memory by the database layer (not persisted).
        // For 1v1/2v2: the Elo rating.  For FFA/Tugak: same as RankPoints.
        [NotMapped]
        public uint CompositeScore { get; set; }

        public uint TotalMatches { get; set; }
        public uint TotalWins { get; set; }
        public uint TotalDraws { get; set; }
        public uint TotalLosses { get; set; }
        public uint TotalDisqualified { get; set; }
        public uint TotalKills { get; set; }
        public uint TotalDeaths { get; set; }
        public uint TotalDmgReceived { get; set; }
        public uint TotalDmgDealt { get; set; }
    }
}
