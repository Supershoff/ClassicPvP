using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ACE.Common;
using ACE.DatLoader;

namespace ACE.Server.Managers
{
    /// <summary>
    /// Manages the server-wide rolling XP cap for ClassicPvP (Infiltration ruleset).
    ///
    /// The cap is stored and enforced as a raw total-XP ceiling rather than a level
    /// number, so it can extend naturally past level 126 into the post-cap skill/attribute
    /// grind phase.
    ///
    /// Schedule (relative to rolling_level_cap_start_timestamp):
    ///
    ///   Phase 1  Days  0–14:  +3.00 levels/day  →  level  57 at end of week 2
    ///   Phase 2  Days 15–44:  +1.50 levels/day  →  level 101 at end of week 6
    ///   Phase 3  Days 45–59:  +1.40 levels/day  →  level 126 exactly at day 60
    ///   Phase 4  Days 60–120: linear XP growth from level-126 XP to season_max_xp
    ///   Day 121+:             cap frozen at season_max_xp
    ///
    ///   Week-by-week level milestones:
    ///     Week  1 (day  0): level  15
    ///     Week  2 (day  7): level  36
    ///     Week  3 (day 14): level  57
    ///     Week  4 (day 21): level  69
    ///     Week  5 (day 28): level  80
    ///     Week  6 (day 35): level  90
    ///     Week  7 (day 42): level 101
    ///     Week  8 (day 49): level 111
    ///     Week  9 (day 56): level 121
    ///     Week  9 (day 60): level 126 — post-level-cap XP phase begins
    ///     Week 18 (day120): season_max_xp reached
    ///
    /// Relevant server configs (PropertyManager):
    ///   rolling_level_cap_enabled         (bool)   — master on/off switch
    ///   rolling_level_cap_start_timestamp (long)   — Unix timestamp of season day 0
    ///   season_max_xp                     (long)   — total-XP ceiling at end of season;
    ///                                                should be enough for every template to
    ///                                                max all skills and attributes
    ///   rolling_xp_cap                    (long)   — computed XP cap (managed automatically)
    ///   rolling_xp_cap_timestamp          (long)   — last update time (managed automatically)
    ///   pvp_dmg_mod_preset_applied_level  (long)   — level threshold of last applied preset
    ///                                                (managed automatically)
    ///
    /// Note: allow_xp_at_max_level must be true (it is set automatically for Infiltration)
    /// for players at level 126 to continue earning XP during Phase 4.
    ///
    /// PvP damage modifier presets are defined in pvp_dmg_mod_presets.json in the server
    /// output directory.  The active preset (highest threshold &lt;= current level cap) is
    /// applied once per day alongside the XP cap update.  Use /reloadpvpdmgpresets to
    /// hot-reload the JSON without a server restart.
    /// </summary>
    public static class RollingLevelCapManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Phase 3 ends (and Phase 4 begins) once the level-based computation first
        // reaches or exceeds the dat-file max level.  With the rates above this
        // occurs at exactly day 60; the code computes it dynamically so rate
        // changes above automatically propagate without touching this constant.
        private const int SEASON_END_DAY = 120;

        /// <summary>Exposed for admin status display and tests.</summary>
        public static int SeasonEndDay => SEASON_END_DAY;

        private static DateTime LastTickDateTime = DateTime.MinValue;

        // ── pvp_dmg_mod preset state ──────────────────────────────────────────────

        /// <summary>File name relative to the server's working / output directory.</summary>
        private const string PRESET_FILE_NAME = "pvp_dmg_mod_presets.json";

        /// <summary>In-memory preset list; replaced atomically on reload.</summary>
        private static List<PvpDmgModPreset> _presets = new List<PvpDmgModPreset>();

        // ── Tick ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called from WorldManager.Tick(). Throttled to once every 15 minutes.
        /// Recomputes and persists rolling_xp_cap if a new calendar day has started,
        /// then applies any pending pvp_dmg_mod preset.
        /// </summary>
        public static void Tick()
        {
            if (DateTime.Now.AddMinutes(-15) < LastTickDateTime)
                return;

            LastTickDateTime = DateTime.Now;

            if (!PropertyManager.GetBool("rolling_level_cap_enabled").Item)
                return;

            var startTimestamp = PropertyManager.GetLong("rolling_level_cap_start_timestamp").Item;
            if (startTimestamp <= 0)
                return;

            // Only recalculate once per UTC calendar day.
            var lastUpdateTimestamp = PropertyManager.GetLong("rolling_xp_cap_timestamp").Item;
            var todayMidnightUtc = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).ToUnixTimeSeconds();
            if (lastUpdateTimestamp >= todayMidnightUtc)
                return;

            UpdateXpCap(startTimestamp);
        }

        // ── Core computation ─────────────────────────────────────────────────────

        private static void UpdateXpCap(long startTimestamp)
        {
            try
            {
                var startDate = DateTimeOffset.FromUnixTimeSeconds(startTimestamp).UtcDateTime.Date;
                var daysSinceStart = Math.Max(0, (DateTime.UtcNow.Date - startDate).Days);

                var xpTable        = DatManager.PortalDat.XpTable.CharacterLevelXPList;
                var maxPossibleLvl = (int)xpTable.Count - 1;           // 126 for Infiltration dat
                var maxLevelXp     = (long)xpTable[maxPossibleLvl];    // XP required to reach level 126

                var seasonMaxXp = PropertyManager.GetLong("season_max_xp").Item;
                if (seasonMaxXp < maxLevelXp)
                    seasonMaxXp = maxLevelXp;   // floor: season cap is at least the level-126 threshold

                // ── Phase 1–3: level-based portion ───────────────────────────────
                // Compute what level the schedule would yield on this day.
                double newLevelCap = 15.0;
                for (int i = 0; i < daysSinceStart; i++)
                {
                    if      (i < 15) newLevelCap += 3.00;  // Phase 1: fast early gains
                    else if (i < 45) newLevelCap += 1.50;  // Phase 2: steady mid-game
                    else if (i < 60) newLevelCap += 1.40;  // Phase 3: approach to 126
                    // i >= 60: level increments exhausted; phase 4 handles XP directly
                }

                var levelCap = (int)Math.Ceiling(newLevelCap);
                if (levelCap > maxPossibleLvl)
                    levelCap = maxPossibleLvl;

                long xpCap;

                if (levelCap < maxPossibleLvl)
                {
                    // ── Phases 1–3: cap is a level-table XP milestone ─────────────
                    xpCap = (long)xpTable[levelCap];
                }
                else
                {
                    // ── Phase 4: post-level-cap linear XP growth ──────────────────
                    // Find the first day the level-based schedule hit maxPossibleLvl.
                    // We walk the same loop to locate it rather than hard-coding a magic
                    // constant, so changes to rates above automatically propagate.
                    int postCapStartDay = ComputePostCapStartDay(maxPossibleLvl);

                    int postCapDays      = daysSinceStart - postCapStartDay;
                    int totalPostCapDays = SEASON_END_DAY - postCapStartDay;  // window length

                    if (totalPostCapDays <= 0)
                    {
                        // Degenerate: schedule hit max level on or after the last day.
                        xpCap = seasonMaxXp;
                    }
                    else
                    {
                        double fraction = Math.Min(1.0, (double)postCapDays / totalPostCapDays);
                        xpCap = maxLevelXp + (long)(fraction * (seasonMaxXp - maxLevelXp));
                    }
                }

                PropertyManager.ModifyLong("rolling_xp_cap", xpCap);
                PropertyManager.ModifyLong("rolling_xp_cap_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                log.Info($"RollingLevelCapManager: Updated rolling_xp_cap to {xpCap:N0} " +
                         $"(day {daysSinceStart}, {GetCapDescription(xpCap)}).");

                // ── Auto-update xp_modifier along the rolling curve ───────────────────
                UpdateXpModifier(daysSinceStart);

                // ── Apply pvp_dmg_mod preset if the level cap has reached a new threshold ──
                ApplyPresetIfNeeded(levelCap);
            }
            catch (Exception ex)
            {
                log.Error($"RollingLevelCapManager.UpdateXpCap exception: {ex}");
            }
        }

        /// <summary>
        /// Returns the first daysSinceStart value at which the level-based schedule
        /// reaches or exceeds <paramref name="maxLevel"/>.  Used to locate the
        /// Phase 3 → Phase 4 transition day.
        /// </summary>
        private static int ComputePostCapStartDay(int maxLevel)
        {
            double v = 15.0;
            for (int i = 0; i < 200; i++)   // 200 is a safe upper bound
            {
                if      (i < 15) v += 3.00;
                else if (i < 45) v += 1.50;
                else if (i < 60) v += 1.40;
                else break;     // phases 1–3 are exhausted; post-cap already active

                if ((int)Math.Ceiling(v) >= maxLevel)
                    return i + 1;   // day *after* this increment
            }
            return 56;  // fallback — phases exhausted without hitting maxLevel
        }

        // ── Rolling XP Modifier ──────────────────────────────────────────────────

        /// <summary>
        /// If <c>rolling_xp_modifier_enabled</c> is true, computes and persists
        /// <c>xp_modifier</c> for the given season day using a quadratic curve:
        /// <list type="bullet">
        ///   <item>Day 0 → 0.25</item>
        ///   <item>Day ~44 (~36 % through the season, level cap ~101) → 1.0</item>
        ///   <item>Day 96 (80 % through the season) → <c>rolling_xp_modifier_max</c></item>
        ///   <item>Day 97–120 → capped at <c>rolling_xp_modifier_max</c></item>
        /// </list>
        /// The curve coefficients are re-derived each call so that changing
        /// <c>rolling_xp_modifier_max</c> live is reflected on the next daily tick.
        /// </summary>
        private static void UpdateXpModifier(int daysSinceStart)
        {
            if (!PropertyManager.GetBool("rolling_xp_modifier_enabled").Item)
                return;

            var maxModifier = PropertyManager.GetDouble("rolling_xp_modifier_max").Item;
            if (maxModifier <= 0) maxModifier = 3.0;

            var modifier = ComputeRollingXpModifier(daysSinceStart, SEASON_END_DAY, maxModifier);
            PropertyManager.ModifyDouble("xp_modifier", modifier);

            log.Info($"RollingLevelCapManager: Updated xp_modifier to {modifier:F3} " +
                     $"(day {daysSinceStart}, max={maxModifier:F2}).");
        }

        /// <summary>
        /// Computes the rolling XP modifier for a given season day using a
        /// quadratic curve anchored to three design points:
        /// <list type="bullet">
        ///   <item><c>t = 0.000</c>   → 0.25  (season start)</item>
        ///   <item><c>t ≈ 0.364</c>   → 1.0   (~36 % through season, day ~44)</item>
        ///   <item><c>t = 0.800</c>   → <paramref name="maxModifier"/> (80 % through season, day 96)</item>
        /// </list>
        /// The t-breakpoints 0.364 and 0.800 are derived from the level-100 and
        /// level-220 milestones on a 275-level scale (the original design reference
        /// values), but the implementation uses season-day progress as the axis so
        /// it works correctly in the Infiltration ruleset where max level is 126.
        /// <para>The curve is capped at <paramref name="maxModifier"/> for days
        /// beyond the 80 % point, and floored at 0.25 to prevent a mis-configured
        /// start-day from driving the modifier below its intended floor.</para>
        /// </summary>
        /// <param name="daysSinceStart">Days elapsed since season day 0 (0-based, UTC).</param>
        /// <param name="seasonEndDay">Total season length in days (<see cref="SEASON_END_DAY"/>).</param>
        /// <param name="maxModifier">Configurable end-of-ramp modifier (e.g. 3.0).</param>
        public static double ComputeRollingXpModifier(int daysSinceStart, int seasonEndDay, double maxModifier)
        {
            if (seasonEndDay <= 0)
                return maxModifier;

            double t = Math.Min(1.0, Math.Max(0.0, (double)daysSinceStart / seasonEndDay));

            // Design anchor points (as fractions of season length).
            // t1 / t2 map to "level 100" and "level 220" on a 275-level scale —
            // coincidentally t1 ≈ 0.364 maps to day ~44 / level cap ~101, and
            // t2 = 0.800 maps to day 96 in a 120-day season.
            const double c  = 0.25;
            const double t1 = 100.0 / 275.0;   // ≈ 0.3636
            const double t2 = 220.0 / 275.0;   // = 0.8000
            const double y1 = 1.0;
            double       y2 = maxModifier;

            // Solve the 2×2 system:  a*t1² + b*t1 = y1-c,  a*t2² + b*t2 = y2-c
            double r1  = y1 - c;
            double r2  = y2 - c;
            double det = t1 * t1 * t2 - t2 * t2 * t1;   // t1*t2*(t1 - t2), always ≠ 0
            double a   = (r1 * t2 - r2 * t1) / det;
            double b   = (r2 * t1 * t1 - r1 * t2 * t2) / det;

            double raw = a * t * t + b * t + c;
            return Math.Min(maxModifier, Math.Max(0.25, raw));
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Pure computation: returns the raw XP cap that the rolling schedule produces
        /// for <paramref name="daysSinceStart"/> without touching any PropertyManager state.
        /// Returns 0 if the dat is unavailable.
        /// </summary>
        public static long ComputeXpCapForDay(int daysSinceStart)
        {
            try
            {
                var xpTable        = DatManager.PortalDat.XpTable.CharacterLevelXPList;
                var maxPossibleLvl = (int)xpTable.Count - 1;
                var maxLevelXp     = (long)xpTable[maxPossibleLvl];

                var seasonMaxXp = PropertyManager.GetLong("season_max_xp").Item;
                if (seasonMaxXp < maxLevelXp) seasonMaxXp = maxLevelXp;

                double newLevelCap = 15.0;
                for (int i = 0; i < daysSinceStart; i++)
                {
                    if      (i < 15) newLevelCap += 3.00;
                    else if (i < 45) newLevelCap += 1.50;
                    else if (i < 60) newLevelCap += 1.40;
                }

                var levelCap = (int)Math.Ceiling(newLevelCap);
                if (levelCap > maxPossibleLvl) levelCap = maxPossibleLvl;

                if (levelCap < maxPossibleLvl)
                    return (long)xpTable[levelCap];

                int postCapStartDay  = ComputePostCapStartDay(maxPossibleLvl);
                int postCapDays      = daysSinceStart - postCapStartDay;
                int totalPostCapDays = SEASON_END_DAY - postCapStartDay;
                if (totalPostCapDays <= 0) return seasonMaxXp;
                double fraction = Math.Min(1.0, (double)postCapDays / totalPostCapDays);
                return maxLevelXp + (long)(fraction * (seasonMaxXp - maxLevelXp));
            }
            catch { return 0; }
        }

        /// <summary>
        /// Returns the current raw total-XP cap, or 0 if the system is disabled
        /// or not yet configured.  The caller should treat 0 as "no active cap".
        /// </summary>
        public static long GetCurrentXpCap()
        {
            if (!PropertyManager.GetBool("rolling_level_cap_enabled").Item)
                return 0;

            if (PropertyManager.GetLong("rolling_level_cap_start_timestamp").Item <= 0)
                return 0;

            return PropertyManager.GetLong("rolling_xp_cap").Item;
        }

        /// <summary>
        /// Returns a human-readable description of what XP milestone <paramref name="xpCap"/>
        /// corresponds to — used in player-facing cap messages and admin status output.
        /// </summary>
        public static string GetCapDescription(long xpCap)
        {
            if (xpCap <= 0)
                return "no cap";

            try
            {
                var xpTable        = DatManager.PortalDat.XpTable.CharacterLevelXPList;
                var maxPossibleLvl = xpTable.Count - 1;

                // Walk the XP table to find the highest level whose XP ≤ xpCap.
                int impliedLevel = 0;
                for (int lvl = 1; lvl <= maxPossibleLvl; lvl++)
                {
                    if ((long)xpTable[lvl] <= xpCap)
                        impliedLevel = lvl;
                    else
                        break;
                }

                if (impliedLevel >= maxPossibleLvl)
                {
                    // Cap is at or above the dat's max level.  Express it as the retail
                    // level-equivalent on the 275-level curve (e.g. "level 150"), even
                    // though the in-game level is capped at 126.
                    int retailLevel = GetDisplayLevelCap(xpCap);
                    return $"level {retailLevel} (XP: {xpCap:N0})";
                }

                return $"level {impliedLevel} (XP: {xpCap:N0})";
            }
            catch
            {
                return $"XP {xpCap:N0}";
            }
        }

        /// <summary>
        /// Forces an immediate recalculation of the XP cap and persists it.
        /// Useful after changing rolling_level_cap_start_timestamp via admin command.
        /// </summary>
        public static void ForceRecalculate()
        {
            var startTimestamp = PropertyManager.GetLong("rolling_level_cap_start_timestamp").Item;
            if (startTimestamp <= 0)
            {
                log.Warn("RollingLevelCapManager.ForceRecalculate: rolling_level_cap_start_timestamp is not set.");
                return;
            }

            // Reset the timestamp so UpdateXpCap always runs.
            PropertyManager.ModifyLong("rolling_xp_cap_timestamp", 0);
            UpdateXpCap(startTimestamp);
        }

        /// <summary>
        /// Returns the current season day number (0-based, UTC).
        /// Returns -1 if the season has not been started.
        /// </summary>
        public static int GetCurrentSeasonDay()
        {
            var startTimestamp = PropertyManager.GetLong("rolling_level_cap_start_timestamp").Item;
            if (startTimestamp <= 0) return -1;
            var startDate = DateTimeOffset.FromUnixTimeSeconds(startTimestamp).UtcDateTime.Date;
            return Math.Max(0, (DateTime.UtcNow.Date - startDate).Days);
        }

        /// <summary>
        /// Returns the maximum character level implied by <paramref name="xpCap"/> using
        /// the dat-file XP table.  Returns 0 if the dat is unavailable or xpCap is 0.
        /// </summary>
        public static int GetCurrentLevelCap(long xpCap)
        {
            if (xpCap <= 0) return 0;
            try
            {
                var xpTable        = DatManager.PortalDat.XpTable.CharacterLevelXPList;
                var maxPossibleLvl = (int)xpTable.Count - 1;
                int impliedLevel   = 0;
                for (int lvl = 1; lvl <= maxPossibleLvl; lvl++)
                {
                    if ((long)xpTable[lvl] <= xpCap)
                        impliedLevel = lvl;
                    else
                        break;
                }
                return impliedLevel;
            }
            catch { return 0; }
        }

        // ── Retail level-equivalent display (post level 126) ──────────────────────
        //
        // ClassicPvP runs on an Infiltration-era dat whose character XP table stops at
        // level 126.  The season XP cap, however, keeps climbing past that point into
        // the skill/attribute grind phase.  For status displays we express that extra
        // XP as the level it *would* correspond to on the End-of-Retail (Throne of
        // Destiny) 275-level curve — e.g. a cap worth 9,940,676,567 XP shows as
        // "level 150" even though the in-game level remains 126.
        //
        // Values below are the retail total-XP thresholds for levels 127–275, indexed
        // so that RetailPostCapLevelXp[0] == level 127.  The 0–126 portion still comes
        // from the live dat table, so this only supplements the post-126 range.
        private const int RetailPostCapFirstLevel = 127;

        private static readonly long[] RetailPostCapLevelXp =
        {
            4452737184L,   4623976457L,   4800443961L,   4982258511L,   5169540711L,   // 127-131
            5362412965L,   5560999488L,   5765426325L,   5975821358L,   6192314325L,   // 132-136
            6415036828L,   6644122352L,   6879706272L,   7121925872L,   7370920356L,   // 137-141
            7626830859L,   7889800466L,   8159974219L,   8437499136L,   8722524219L,   // 142-146
            9015200473L,   9315680913L,   9624120583L,   9940676567L,   10265508000L,  // 147-151
            10598776087L,  10940644110L,  11291277447L,  11650843580L,  12019512114L,  // 152-156
            12397454784L,  12784845474L,  13181860228L,  13588677261L,  14005476978L,  // 157-161
            14432441981L,  14869757088L,  15317609341L,  15776188025L,  16245684675L,  // 162-166
            16726293095L,  17218209369L,  17721631872L,  18236761289L,  18763800622L,  // 167-171
            19302955209L,  19854432732L,  20418443236L,  20995199136L,  21584915236L,  // 172-176
            22187808740L,  22804099263L,  23434008850L,  24077761983L,  24735585600L,  // 177-181
            25407709103L,  26094364377L,  26795785797L,  27512210247L,  28243877131L,  // 182-186
            28991028384L,  29753908491L,  30532764494L,  31327846011L,  32139405244L,  // 187-191
            32967696998L,  33812978688L,  34675510358L,  35555554692L,  36453377025L,  // 192-196
            37369245362L,  38303430385L,  39256205472L,  40227846705L,  41218632889L,  // 197-201
            42228845559L,  43258768999L,  44308690253L,  45378899136L,  46469688253L,  // 202-206
            47581353006L,  48714191613L,  49868505116L,  51044597400L,  52242775200L,  // 207-211
            53463348120L,  54706628644L,  55972932147L,  57262576914L,  58575884147L,  // 212-216
            59913177984L,  61274785507L,  62661036761L,  64072264761L,  65508805511L,  // 217-221
            66970998015L,  68459184288L,  69973709375L,  71514921358L,  73083171375L,  // 222-226
            74678813628L,  76302205402L,  77953707072L,  79633682122L,  81342497156L,  // 227-231
            83080521909L,  84848129266L,  86645695269L,  88473599136L,  90332223269L,  // 232-236
            92221953273L,  94143177963L,  96096289383L,  98081682817L,  100099756800L, // 237-241
            102150913137L, 104235556910L, 106354096497L, 108506943580L, 110694513164L, // 242-246
            112917223584L, 115175496524L, 117469757028L, 119800433511L, 122167957778L, // 247-251
            124572765031L, 127015293888L, 129495986391L, 132015288025L, 134573647725L, // 252-256
            137171517895L, 139809354419L, 142487616672L, 145206767539L, 147967273422L, // 257-261
            150769604259L, 153614233532L, 156501638286L, 159432299136L, 162406700286L, // 262-266
            165425329540L, 168488678313L, 171597241650L, 174751518233L, 177952010400L, // 267-271
            181199224153L, 184493669177L, 187835858847L, 191226310247L,                // 272-275
        };

        /// <summary>
        /// Returns the level that <paramref name="xpCap"/> corresponds to for status
        /// displays, extending past the dat's level-126 ceiling onto the retail
        /// (Throne of Destiny) 275-level curve.  For caps at or below the dat's max
        /// level this is identical to <see cref="GetCurrentLevelCap"/>; above it, the
        /// embedded retail table is consulted so a post-126 cap reads as its retail
        /// level-equivalent (e.g. "level 150").  This is a *display* value only — the
        /// in-game level remains capped at 126.
        /// </summary>
        public static int GetDisplayLevelCap(long xpCap)
        {
            if (xpCap <= 0) return 0;

            int datLevel = GetCurrentLevelCap(xpCap);

            try
            {
                var datMaxLevel = DatManager.PortalDat.XpTable.CharacterLevelXPList.Count - 1;

                // Only extend when the loaded dat is the Infiltration table (max 126)
                // and the cap has reached that ceiling.  A retail dat already covers
                // the full range, so GetCurrentLevelCap is authoritative there.
                if (datMaxLevel != 126 || datLevel < datMaxLevel)
                    return datLevel;

                int level = datMaxLevel;
                for (int i = 0; i < RetailPostCapLevelXp.Length; i++)
                {
                    if (RetailPostCapLevelXp[i] <= xpCap)
                        level = RetailPostCapFirstLevel + i;
                    else
                        break;
                }
                return level;
            }
            catch { return datLevel; }
        }

        // ── Catch-up XP boost ────────────────────────────────────────────────────
        //
        // Characters whose lifetime total XP sits well below the current season cap earn
        // boosted XP so a late start (or a fresh reroll) is not a season-ending handicap.
        // The boost scales linearly with how far behind the cap the character is:
        //
        //   totalXp = 0                     → catchup_xp_max_multiplier (default 5.0×)
        //   totalXp → threshold × xpCap     → catchup_xp_min_multiplier (default 2.0×)
        //   totalXp ≥ threshold × xpCap     → 1.0× (no boost)
        //
        // The step down from the minimum multiplier to 1.0× at the threshold is intentional:
        // the boost is a catch-up mechanic for players who are behind, not a gentle taper for
        // players who have already caught up.

        /// <summary>
        /// Returns the catch-up XP multiplier for a character whose lifetime total XP is
        /// <paramref name="totalXp"/>.  Returns 1.0 (no boost) when the feature is disabled,
        /// when no season cap is active, or when the character is at or above
        /// <c>catchup_xp_threshold</c> of the cap.
        /// </summary>
        public static double GetCatchUpXpMultiplier(long totalXp)
        {
            if (!PropertyManager.GetBool("catchup_xp_enabled").Item)
                return 1.0;

            var xpCap = GetCurrentXpCap();
            if (xpCap <= 0)
                return 1.0;

            var threshold = Math.Min(1.0, PropertyManager.GetDouble("catchup_xp_threshold").Item);
            if (threshold <= 0.0)
                return 1.0;

            var progress = Math.Max(0.0, (double)totalXp / xpCap);
            if (progress >= threshold)
                return 1.0;

            var maxMultiplier = PropertyManager.GetDouble("catchup_xp_max_multiplier").Item;
            var minMultiplier = PropertyManager.GetDouble("catchup_xp_min_multiplier").Item;

            // Tolerate a swapped configuration rather than inverting the ramp.
            if (maxMultiplier < minMultiplier)
                (maxMultiplier, minMultiplier) = (minMultiplier, maxMultiplier);

            // Linear ramp across the boosted band: 0 % of the threshold → max, 100 % → min.
            var bandProgress = progress / threshold;
            var multiplier   = maxMultiplier - bandProgress * (maxMultiplier - minMultiplier);

            return Math.Max(1.0, multiplier);
        }

        /// <summary>
        /// Returns the fraction (0.0–1.0+) of the current season XP cap that
        /// <paramref name="totalXp"/> represents, or -1 when no cap is active.
        /// Used by status displays alongside <see cref="GetCatchUpXpMultiplier"/>.
        /// </summary>
        public static double GetSeasonCapProgress(long totalXp)
        {
            var xpCap = GetCurrentXpCap();
            if (xpCap <= 0) return -1.0;
            return Math.Max(0.0, (double)totalXp / xpCap);
        }

        /// <summary>
        /// Returns the time remaining until the cap next advances (next UTC midnight).
        /// Returns <see cref="TimeSpan.Zero"/> if the season has ended or has not started.
        /// </summary>
        public static TimeSpan GetTimeUntilNextCapIncrease()
        {
            int day = GetCurrentSeasonDay();
            if (day < 0 || day >= SEASON_END_DAY) return TimeSpan.Zero;
            return DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
        }

        // ── pvp_dmg_mod preset management ─────────────────────────────────────────

        /// <summary>
        /// Loads (or reloads) pvp_dmg_mod_presets.json from the server's working directory.
        /// Called once at startup and on demand via /reloadpvpdmgpresets.
        /// Returns a human-readable status string suitable for admin output.
        /// </summary>
        public static string LoadPresets()
        {
            string path = ResolvePresetFilePath();

            if (!File.Exists(path))
            {
                _presets = new List<PvpDmgModPreset>();
                log.Info($"RollingLevelCapManager: {PRESET_FILE_NAME} not found at '{path}'. No presets loaded.");
                return $"{PRESET_FILE_NAME} not found at '{path}'. No presets active.";
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<PvpDmgModPresetConfig>(json, ConfigManager.SerializerOptions);

                if (config?.Presets == null || config.Presets.Count == 0)
                {
                    _presets = new List<PvpDmgModPreset>();
                    log.Info($"RollingLevelCapManager: {PRESET_FILE_NAME} loaded — no presets defined.");
                    return $"{PRESET_FILE_NAME} loaded — no presets defined.";
                }

                // Sort ascending by level threshold so the "highest ≤ cap" search is easy.
                config.Presets.Sort((a, b) => a.LevelThreshold.CompareTo(b.LevelThreshold));
                _presets = config.Presets;

                log.Info($"RollingLevelCapManager: Loaded {_presets.Count} pvp_dmg_mod preset(s) from '{path}'. " +
                         $"Thresholds: [{string.Join(", ", _presets.Select(p => p.LevelThreshold))}]");

                return $"Loaded {_presets.Count} preset(s). Thresholds: [{string.Join(", ", _presets.Select(p => p.LevelThreshold))}]";
            }
            catch (Exception ex)
            {
                log.Error($"RollingLevelCapManager: Failed to load {PRESET_FILE_NAME}: {ex.Message}");
                return $"Error loading {PRESET_FILE_NAME}: {ex.Message}";
            }
        }

        /// <summary>
        /// Returns a snapshot of the currently loaded presets (for admin display).
        /// </summary>
        public static IReadOnlyList<PvpDmgModPreset> GetLoadedPresets() => _presets;

        /// <summary>
        /// Returns the active preset for the given level cap, or null if none applies.
        /// The active preset is the one with the highest LevelThreshold &lt;= levelCap.
        /// </summary>
        public static PvpDmgModPreset GetActivePreset(int levelCap)
        {
            PvpDmgModPreset active = null;
            foreach (var p in _presets)
            {
                if (p.LevelThreshold <= levelCap)
                    active = p;
                else
                    break;  // list is sorted ascending; no point continuing
            }
            return active;
        }

        /// <summary>
        /// Checks whether the active preset for <paramref name="levelCap"/> differs from
        /// the last-applied one, and if so applies it.  Called automatically from
        /// <see cref="UpdateXpCap"/> each daily tick.
        /// </summary>
        private static void ApplyPresetIfNeeded(int levelCap)
        {
            if (_presets.Count == 0)
                return;

            var activePreset = GetActivePreset(levelCap);
            if (activePreset == null)
                return;

            var lastAppliedLevel = PropertyManager.GetLong("pvp_dmg_mod_preset_applied_level").Item;
            if (activePreset.LevelThreshold == lastAppliedLevel)
                return;  // already applied — nothing to do

            ApplyPreset(activePreset, reason: $"level cap reached {levelCap} (threshold {activePreset.LevelThreshold})");
        }

        /// <summary>
        /// Applies all properties in <paramref name="preset"/> via PropertyManager and
        /// records the threshold in pvp_dmg_mod_preset_applied_level.
        /// Returns a summary string suitable for admin output.
        /// </summary>
        public static string ApplyPreset(PvpDmgModPreset preset, string reason = "manual")
        {
            if (preset == null)
                return "No preset to apply.";

            int applied = 0, skipped = 0;
            var skippedKeys = new List<string>();

            foreach (var kvp in preset.Properties)
            {
                if (PropertyManager.ModifyDouble(kvp.Key, kvp.Value))
                    applied++;
                else
                {
                    skipped++;
                    skippedKeys.Add(kvp.Key);
                }
            }

            PropertyManager.ModifyLong("pvp_dmg_mod_preset_applied_level", preset.LevelThreshold);

            var desc = string.IsNullOrWhiteSpace(preset.Description) ? "(no description)" : preset.Description;
            var summary = $"Applied pvp_dmg_mod preset (threshold {preset.LevelThreshold} — \"{desc}\") " +
                          $"via {reason}: {applied} propert{(applied == 1 ? "y" : "ies")} set";

            if (skipped > 0)
                summary += $", {skipped} unknown key(s) skipped: [{string.Join(", ", skippedKeys)}]";

            log.Info($"RollingLevelCapManager: {summary}.");
            return summary;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string ResolvePresetFilePath()
        {
            // Prefer the directory of the executing assembly (the server output dir),
            // fall back to the current working directory.
            var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                var p = Path.Combine(assemblyDir, PRESET_FILE_NAME);
                if (File.Exists(p)) return p;
            }
            return Path.Combine(Environment.CurrentDirectory, PRESET_FILE_NAME);
        }
    }
}
