# ClassicPvP — Admin Guide

All server properties are stored in `shard_config` and are readable/writable at runtime via `/modifybool`, `/modifylong`, `/modifydouble`, and `/modifystring`. Changes take effect within seconds — no restart needed unless noted. `GodState` accounts bypass most enforcement checks.

---

## Table of Contents

1. [One-Account-Per-IP Enforcement](#1-one-account-per-ip-enforcement)
2. [Rolling Level Cap](#2-rolling-level-cap)
3. [Rolling XP Modifier](#3-rolling-xp-modifier)
4. [PvP Damage Modifier Presets](#4-pvp-damage-modifier-presets)
5. [PvP XP on Player Kills](#5-pvp-xp-on-player-kills)
6. [XP Cap Categories](#6-xp-cap-categories)
7. [Hot Dungeons](#7-hot-dungeons)
8. [Town Control](#8-town-control)
9. [Season Leaderboard](#9-season-leaderboard)
10. [Tinkering Lotto](#10-tinkering-lotto)
11. [Tinker Character Designation](#11-tinker-character-designation)
12. [Discord Webhooks](#12-discord-webhooks)
13. [Anti-Cheat (Movement Enforcement)](#13-anti-cheat-movement-enforcement)
14. [Bounty System](#14-bounty-system)
15. [Admin Command Quick Reference](#15-admin-command-quick-reference)
16. [Loot-to-Weenie Export](#16-loot-to-weenie-export)
17. [Spell Management](#17-spell-management)
18. [Dungeon Bosses](#18-dungeon-bosses)
19. [Missile Tracking (Experimental)](#19-missile-tracking-experimental)

---

## 1. One-Account-Per-IP Enforcement

Each IP address may only be associated with one account. Accounts accumulate every IP they have ever logged in from; if any of those IPs is later used by a different account, that login is rejected. Players whose ISP rotates their IP, or who occasionally connect through a VPN by mistake, are not penalised — they simply add a new IP to their account's known set.

### How It Works

| Scenario | Behavior |
|---|---|
| First login from any IP | IP is bound to the account silently |
| Login from a previously seen IP | Login proceeds normally |
| Login from a new IP (not yet seen for this account) | New IP added to account's known-IP set; login proceeds |
| IP already claimed by a *different* account | Session terminated; player told to contact admin |

- Localhost (`127.0.0.1` / `::1`) and `Admin+` accounts are always exempt from all checks.
- Every new IP is logged to `account_ip_change_log` for audit purposes.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `enforce_account_ip_binding` | bool | `true` | Master on/off for the IP binding system |
| `ip_binding_ip_whitelist` | string | `""` | Comma-separated list of IPs exempt from all binding enforcement (e.g. `192.168.1.1,10.0.0.5`). Accounts logging in from a whitelisted IP bypass the conflict check entirely — **unlimited** accounts. Use for LAN setups or trusted staff locations. |
| `ip_binding_ip_allowance` | string | `""` | Comma-separated `ip:count` overrides that allow a **capped** number of accounts on an IP (e.g. `203.0.113.42:2`). IPs not listed use the default of 1. Prefer this over the whitelist for households (father/son) where you want exactly N, not unlimited. |

### Whitelist vs. Allowance — which to use

Both let more than one account share an IP, but they differ in the critical way:

| | `ip_binding_ip_whitelist` | `ip_binding_ip_allowance` |
|---|---|---|
| Accounts per IP | **Unlimited** | **Hard cap** (the `:count` you set) |
| Use for | LAN / staff / café where any number is fine | A specific household that should get *exactly* N |
| Risk | Opens the door to unlimited accounts on that IP | Third account on the IP is rejected automatically |

For the "father and son, same house" case, use the **allowance** — set the household's IP to `:2`. A third account attempting to bind that IP is rejected at login, so a truthful two-player household is served without opening the floodgates to unlimited alts.

### Setting an allowance

Use the dedicated command (recommended — avoids CSV formatting mistakes):

```
/setipallowance 203.0.113.42 2      — allow up to 2 accounts on this IP
/setipallowance 203.0.113.42 1      — remove the override (back to default 1)
```

Or edit the property directly:

```
/modifystring ip_binding_ip_allowance 203.0.113.42:2, 198.51.100.7:2
```

Both take effect immediately — no restart required.

> **Note on existing bindings:** the allowance is checked when a *new* account first binds an IP. Accounts already bound to an IP are unaffected. Lowering an allowance below the number of accounts already on an IP does not evict them — use `/clearipbinding` to remove a specific account's binding.

### IP Whitelist

To allow **unlimited** accounts from a shared IP (e.g. a large LAN, internet café, or staff office):

```
/modifystring ip_binding_ip_whitelist 192.168.1.100,203.0.113.42
```

To clear the whitelist:

```
/modifystring ip_binding_ip_whitelist 
```

Changes take effect immediately — no restart required.

### Admin Commands

| Command | Description |
|---|---|
| `/checkipbinding <account>` | Lists all known IPs for the account, how many accounts share each IP, its allowance, and recent IP change history |
| `/clearipbinding <account>` | Removes all IP bindings for the account. Player's next login creates a fresh binding. Use when an account is legitimately moving to a new household. |
| `/setipallowance <ip> <count>` | Sets how many distinct accounts may bind to `<ip>`. `count` ≤ 1 removes the override (back to default 1). Edits `ip_binding_ip_allowance`. |

### Database Tables (`ace_auth`)

| Table | Contents |
|---|---|
| `account_ip_binding` | One row per IP per account — accumulates every IP the account has ever used |
| `account_ip_change_log` | Audit log of every new IP seen per account |

---

## 2. Rolling Level Cap

The rolling cap advances the server-wide XP ceiling once per day using a three-phase schedule. Players at the cap stop earning XP until the cap advances and are notified when it does.

### Schedule

| Phase | Days | Rate | Milestone |
|---|---|---|---|
| Phase 1 | 0–14 | +3.00 levels/day | Level 57 at end of day 14 |
| Phase 2 | 15–44 | +1.50 levels/day | Level 101 at end of day 42 |
| Phase 3 | 45–59 | +1.40 levels/day | Level 126 (cap) at day 60 |
| Phase 4 | 60–120 | Linear XP growth | Season max XP reached at day 120 |
| Day 121+ | — | Frozen at `season_max_xp` | — |

The cap starts at **level 15** on day 0 (season launch). After level 126 the cap continues as a raw total-XP ceiling to cover the post-cap skill/attribute grind.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `rolling_level_cap_enabled` | bool | `false` | Master on/off switch |
| `rolling_level_cap_start_timestamp` | long | `0` | Unix timestamp of season day 0 (UTC midnight). Set before starting |
| `season_max_xp` | long | `80,000,000,000` | Total XP ceiling at end of season (day 120) |
| `rolling_xp_cap` | long | — | **Auto-managed.** Current computed XP cap. Do not set manually |
| `rolling_xp_cap_timestamp` | long | — | **Auto-managed.** Timestamp of last recalculation |
| `pvp_dmg_mod_preset_applied_level` | long | `-1` | **Auto-managed.** Threshold of the last applied pvp_dmg_mod preset |

### Startup Procedure

1. Set `season_max_xp` to the desired end-of-season XP ceiling.
2. Optionally configure `rolling_xp_modifier_enabled` and `rolling_xp_modifier_max` (see Section 3).
3. Optionally create `pvp_dmg_mod_presets.json` (see Section 4).
4. Run `/startrollingcap` on launch day.

### Admin Commands

| Command | Description |
|---|---|
| `/startrollingcap` | Starts the season from today (UTC midnight). Enables the cap, sets `rolling_level_cap_start_timestamp`, forces immediate recalculation |
| `/forcerollingcap` | Forces an immediate recalculation. Use after changing `rolling_level_cap_start_timestamp` or `season_max_xp`. Also re-applies the rolling XP modifier and pvp_dmg_mod preset if enabled |
| `/rollingcapstatus` | Full status: enabled flag, start date, current XP cap, season day, progress %, and rolling XP modifier state |

### Tick Behavior

The manager runs every 15 minutes (`WorldManager.Tick`). It only takes action once per UTC calendar day. Each daily update:
1. Recalculates `rolling_xp_cap`
2. Updates `xp_modifier` if `rolling_xp_modifier_enabled` (Section 3)
3. Applies any pending pvp_dmg_mod preset (Section 4)

---

## 3. Rolling XP Modifier

Automatically adjusts the global `xp_modifier` each day as the season progresses. The modifier follows a quadratic curve — slow early, accelerating late — to reward players who stay active through the end of the season.

### Curve (default max = 3.0)

| Season Day | Approx. Level Cap | XP Modifier |
|---|---|---|
| 0 (launch) | 15 | **0.25×** |
| 7 | 36 | ~0.39× |
| 14 | 57 | ~0.52× |
| 21 | 69 | ~0.66× |
| **~44** | **101** | **1.0×** (normal rate) |
| 63 | 126 (level cap) | ~1.56× |
| 84 | post-cap grind | ~2.24× |
| **96** | post-cap grind | **3.0×** (peak) |
| 97–120 | post-cap grind | **3.0×** (held at cap) |

### How It Works

The curve is a quadratic `f(t) = a·t² + b·t + 0.25` where `t = daysSinceStart / 120`.  Coefficients are re-derived each tick from `rolling_xp_modifier_max`, so changing the max live is reflected on the next daily update without a restart. The floor is always 0.25 and the curve is capped at `rolling_xp_modifier_max`.

The three design anchors are:
- `t = 0.000` → 0.25 (season start, hardcoded floor)
- `t ≈ 0.364` → 1.0 (the "normal rate" crossover, at day ~44 / level ~101)
- `t = 0.800` → `rolling_xp_modifier_max` (day 96; capped from here through season end)

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `rolling_xp_modifier_enabled` | bool | `false` | Enable automatic daily `xp_modifier` updates. Requires `rolling_level_cap_enabled` |
| `rolling_xp_modifier_max` | double | `3.0` | Peak modifier applied from day 96 through season end |
| `xp_modifier` | double | `1.0` | **Managed automatically when enabled.** Do not set manually while the rolling modifier is active |

### Notes

- `/rollingcapstatus` shows the current value, expected value for today's day, and a sync warning if they differ.
- If the values drift (e.g. the modifier was set manually mid-season), run `/forcerollingcap` to resync.
- Disabling `rolling_xp_modifier_enabled` mid-season leaves `xp_modifier` at whatever value it was last set to. Reset it manually with `/modifydouble xp_modifier <value>` if needed.

---

## 4. PvP Damage Modifier Presets

Allows defining sets of `pvp_dmg_mod` property overrides that are automatically applied when the rolling level cap crosses a configured threshold. Useful for tightening damage balance as players get stronger.

### Configuration File

`pvp_dmg_mod_presets.json` — placed in the server output directory alongside the executable. The file is loaded at startup and can be hot-reloaded without a restart.

**Format:**
```json
{
  "Presets": [
    {
      "LevelThreshold": 50,
      "Description": "Early game — moderate restrictions",
      "Properties": {
        "pk_damage_modifier": 0.8,
        "pvp_dmg_mod_melee": 0.9
      }
    },
    {
      "LevelThreshold": 100,
      "Description": "Mid game — tighter caps",
      "Properties": {
        "pk_damage_modifier": 0.7
      }
    }
  ]
}
```

- Presets are sorted by `LevelThreshold` ascending at load time.
- The **active** preset is the one with the highest threshold ≤ current level cap.
- A preset is only applied once per threshold — `pvp_dmg_mod_preset_applied_level` tracks the last applied value across restarts.
- Any `Properties` key that doesn't exist in `PropertyManager` is silently skipped (logged as a warning).

### Admin Commands

| Command | Description |
|---|---|
| `/pvpdmgpresets` | Lists all loaded presets, which is active, and which has been applied |
| `/reloadpvpdmgpresets` | Hot-reloads `pvp_dmg_mod_presets.json` from disk. Does NOT re-apply — use `/applypvpdmgpreset` after if needed |
| `/applypvpdmgpreset [threshold]` | Force-applies the preset at the given threshold. Omit argument to apply the currently active preset for the live level cap |

### Arena-Only Damage Modifiers

Every `pvp_dmg_mod_*` property has a `pvp_dmg_mod_arena_*` counterpart (e.g. `pvp_dmg_mod_dagger_cb` → `pvp_dmg_mod_arena_dagger_cb`). All default to `1.0`.

**When the defender is standing in an arena landblock, the arena value is used *instead of* the global one.** The two sets never stack — an arena fight reads the arena set and nothing else, so a value left at the default `1.0` means "no scaling in arenas", not "fall back to the global value".

The check is landblock-only: it does **not** look for a running arena event and does **not** check whether the players are in that event. Anyone taking damage on an arena landblock gets the arena values. This keeps it to a single set lookup per damage calculation.

Notes:
- Applies to melee/missile (`DamageEvent`), war/void projectiles and their variance mods (`SpellProjectile`), and void DOT ticks (`EnchantmentManager`).
- Arena values are ordinary double properties, so they can be set with `/modifydouble` and included in `pvp_dmg_mod_presets.json` like any other key.

#### Testing arena values outside an arena

`/arenatesttarget [on|off] [characterName]` (Admin) flags a player so that **damage dealt to them** resolves against the `pvp_dmg_mod_arena_*` configs anywhere in the world, exactly as if they were standing in an arena landblock.

| Command | Effect |
|---|---|
| `/arenatesttarget on Testmonkey` | Flags Testmonkey |
| `/arenatesttarget off Testmonkey` | Clears the flag |
| `/arenatesttarget Testmonkey` | Shows the current setting |
| `/arenatesttarget on` | Flags yourself |
| `/arenatesttarget list` | Lists every online player currently flagged |

The flag only redirects config lookups. It does **not** join the player to an arena event — event gating, observer rules, overtime and match damage tracking all still key off the real landblock, so a flagged player outside an arena takes damage normally rather than being blocked by the "no active event" checks.

The target must be online (the flag is read off the live player during damage calculation). It persists on the character until cleared, including across logout, so clear it when testing is done — `/arenatesttarget list` shows what is still set.

---

## 5. PvP XP on Player Kills

Open-world PK kills award XP to the killer that flows into the PvP XP category (subject to the daily PvP budget and Ancient Bottle overflow).

### Formula

```
pvpXp = baseXp × randPercent × levelDecay
```

- **`baseXp`** = 1–4% of the killer's XP-to-next-level (configurable range)
- **`randPercent`** = random value in the `[pk_xp_min_percent, pk_xp_max_percent]` range
- **`levelDecay`** = `pk_xp_level_diff_decay ^ max(0, killerLevel − victimLevel)`
  - At decay=0.85: victim 10 levels below killer → ~0.85¹⁰ ≈ 20% reward
  - Same level or victim is higher: full reward

### Eligibility Guards

- Killer and victim must be in **different allegiances** (`IsSameAllegiance` check).
- Repeat-kill cooldown: the same killer cannot earn XP from the same victim again until `pk_xp_repeat_cooldown_minutes` elapses (in-memory, resets on restart).
- Hot Dungeon bonus: if the kill occurs inside an active Hot Dungeon, `pvpXp` is multiplied by the dungeon's `XpMultiplier` before being awarded.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `pk_xp_level_diff_decay` | double | `0.85` | Exponential decay per level the victim is below the killer |
| `pk_xp_repeat_cooldown_minutes` | double | `60.0` | Minutes before the same killer earns XP from killing the same victim again |

---

## 6. XP Cap Categories

Player XP is divided into three independent buckets: **Monster**, **Quest**, and **PvP**. Each bucket has its own daily budget calculated as a fraction of the player's remaining XP headroom to the rolling cap.

### Budget Ratios

| Property | Type | Default | Description |
|---|---|---|---|
| `daily_monster_xp_category_ratio` | double | (configured) | Max fraction of remaining cap XP earnable from monster kills per window |
| `daily_quest_xp_category_ratio` | double | (configured) | Max fraction earnable from quests per window |
| `daily_pvp_xp_category_ratio` | double | `0.70` | Max fraction earnable from PvP (kills + arenas) per window |

Buckets reset when the rolling cap advances, not on a daily timer. Players further behind the cap get proportionally larger budgets.

**PvP overflow** goes into Ancient Bottles (WCID 490071). A bottle holds up to 100 million XP. Players consume it manually when their PvP budget has room.

### Catch-Up XP Boost

Characters whose lifetime total XP sits below `catchup_xp_threshold` of the current season XP cap earn a multiplier on **all** XP earned through `EarnXP` (kills, quests, exploration, fellowship shares). The multiplier scales linearly with how far behind the cap the character is — the furthest behind get the largest boost:

```
progress    = totalXp / rolling_xp_cap
band        = progress / catchup_xp_threshold          (0.0 at zero XP → 1.0 at the threshold)
multiplier  = max − band × (max − min)
```

| Player's total XP vs. cap | Multiplier (defaults) |
|---|---|
| 0 % | 5.00× |
| 17.5 % | 4.25× |
| 35 % | 3.50× |
| 52.5 % | 2.75× |
| just under 70 % | ~2.00× |
| 70 % or above | 1.00× (no boost) |

The step from 2.00× down to 1.00× at the threshold is deliberate: this is a catch-up mechanic for players who are behind, not a taper for players who have already caught up.

The boost multiplies alongside `xp_modifier`, so a 3.0× season rate and a 5.0× catch-up boost stack to 15×. Boosted XP is still subject to the global cap and the per-category budgets above — the boost lets a player reach their ceiling faster, it does not raise it. Requires an active rolling level cap (`GetCurrentXpCap() > 0`); with no season running the multiplier is always 1.0.

Players see their current boost on the `Catch-Up` line of `/season status`.

| Property | Type | Default | Description |
|---|---|---|---|
| `catchup_xp_enabled` | bool | `true` | Master on/off switch for the catch-up boost |
| `catchup_xp_threshold` | double | `0.70` | Fraction of the season cap below which the boost applies |
| `catchup_xp_max_multiplier` | double | `5.00` | Boost for a character with 0 total XP (furthest behind) |
| `catchup_xp_min_multiplier` | double | `2.00` | Boost for a character right at the threshold (least far behind) |

### Bypassing the Cap for Testing

`/grantxp` accepts an optional trailing `force` argument that ignores the cap system entirely, so a test character can be leveled past the current season cap without touching any server-wide property.

```
/grantxp 500000000 force
/grantxp Nakedmoleman 500000000 force
```

`force` skips three things: the rolling season XP cap (global remaining + per-category buckets), the `season_max_xp` safety clamp, and the max-level XP gate (XP still lands at level 126 even when `allow_xp_at_max_level` is false). A normal `/grantxp` without the argument stays fully capped — `XpType.Admin` only bypasses the per-category buckets, never the global remaining.

Notes:

- `force` is only recognized as the **last** token, so a player actually named Force can still be targeted by name (`/grantxp Force 1000`).
- Access is the same as `/grantxp` itself: Developer or higher on a live world, anyone on a test world.
- Forced grants are tagged `(season XP cap bypassed)` in both the confirmation message and the audit-channel broadcast.
- Unassigned XP is still clamped to `uint.MaxValue` (~4.29 billion). That is a client display limit, not a season cap — `TotalExperience` (which drives level) goes as high as you grant, but to spend more than 4.29 billion you must spend the pool down and grant again.

This replaces the old workaround of toggling `rolling_level_cap_enabled` off and back on, which lifted the cap for every online player during the window.

---

## 7. Hot Dungeons

Up to 3 dungeons can be Hot simultaneously. Each is selected from a pool gated by current level cap brackets, runs for 24–48 hours, and grants bonus XP, double loot, and special PvP drops.

### Rewards While Hot

| Reward | Details |
|---|---|
| XP multiplier | Monster and PK kill XP × dungeon `XpMultiplier` (applied before fellowship sharing) |
| Double loot | Two independent loot rolls per monster corpse |
| A Box | Per-kill drop chance per dungeon's `BoxDropChance` config |
| PvP drop | Cross-allegiance PK kill inside the dungeon drops a Phial of Bloody Tears + A Box on the victim corpse |

### Dungeon Pool

Defined in `HotDungeonManager.cs` (`PossibleDungeons` list). Each entry has:
- `Landblock` — upper-16-bit landblock ID
- `MinLevel` / `MaxLevel` — cap range in which the dungeon is eligible (MaxLevel = 0 means no upper limit)
- `XpMultiplier` — kill XP multiplier while hot (e.g. `2.5` = 2.5× XP)
- `BoxDropChance` — per-kill Box drop probability (0.0–1.0)

> **⚠ Note:** The dungeon pool currently uses placeholder data. Real dungeon entries must be filled in before launch.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `hot_dungeon_enabled` | bool | `false` | Master on/off switch. Requires Infiltration ruleset |
| `hot_dungeon_interval` | double | `7800` | Min seconds before a new dungeon can auto-roll after one was previously activated |
| `hot_dungeon_duration` | double | `7200` | Total seconds a hot dungeon stays active |
| `hot_dungeon_roll_delay` | double | `1200` | Seconds between each auto-roll attempt while slots are available |
| `hot_dungeon_chance` | double | `0.33` | Probability (0–1) a new dungeon is selected on each roll attempt |
| `hot_dungeon_bonus_xp` | double | `1.0` | Legacy extra XP flat bonus (1.0 = +100%). Prefer per-dungeon `XpMultiplier` |
| `hot_dungeon_webhook` | string | `""` | Discord webhook for Hot Dungeon activation/expiry announcements |

### Admin Commands

| Command | Description |
|---|---|
| `/SwitchHotDungeon` | Forces an immediate roll for a new Hot Dungeon |
| `/ForceHotDungeon` | Forces your current landblock to become a Hot Dungeon |
| `/ProlongHotDungeon` | Extends all active Hot Dungeons by 1 hour |
| `/hotdungeons` | *(player command)* Lists all currently active Hot Dungeons with XP multipliers and time remaining |

### Tick Behavior

- Initializes with a random first-roll delay of 30 min–3 hours after server start (so players don't wait 12+ hours on a fresh boot).
- Active dungeons expire independently; each tracks its own `ExpiresAt` timestamp.
- Hourly re-announcements fire for each active dungeon until it expires.

### Zerg Control

While a dungeon is hot it is automatically added to the zerg-control list with a cap of **9 players per allegiance** (same mechanic as the Abandoned Mine), and removed from the list when it stops being hot. This is wired in `HotDungeonManager` (`ZergControlMaxPerAllegiance`) via `ZergControlLandblocks.AddDynamicLandblock` / `RemoveDynamicLandblock`.

---

## 8. Town Control

Town Control is a structured PvP objective system. Eligible allegiances compete to control three towns: **Arwic**, **Al-Jalima**, and **Tou-Tou**. Control is contested by killing boss creatures; the killing allegiance captures the town and gains access to vendors and other rewards.

### Conflict Flow

1. An **init boss** spawns at a town. Any eligible allegiance can attack it.
2. When the init boss dies, the killing allegiance triggers a **conflict** and a **conflict boss** spawns.
3. When the conflict boss dies, the attacking allegiance **captures the town**.
4. Broadcasts and Discord notifications fire at each phase transition.
5. HP threshold broadcasts fire at 50%, 20%, and 5% remaining on the conflict boss.

### Eligibility

Only allegiances whose monarch GUID appears in `town_control_alleglist` can initiate conflicts. GUIDs are unsigned integers separated by commas.

```
/modifystring town_control_alleglist "1234567,8901234,5678901"
```

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `town_control_alleglist` | string | `""` | Comma-separated monarch GUIDs of eligible allegiances |
| `town_control_globals_webhook` | string | `""` | Discord webhook for conflict start/end broadcasts |
| `town_control_enable_debug_log` | bool | `false` | Writes verbose Town Control diagnostics to the server log (`TownControl` logger) |

### Database Tables (`ace_log`)

| Table | Contents |
|---|---|
| `town_control_town` | Current controller, last conflict timestamps, vendor state per town |
| `town_control_event` | Full audit log of every conflict event (phase, attacker, timestamp) |

> **Migration:** Run `Database/Updates/Log/AddTownControlFeature.sql` on any instance that doesn't have the tables yet.

---

## 9. Season Leaderboard

Weekly Sunday snapshots capture the top 10 players across 13 scored categories. Rewards are held until claimed with `/season rewards`.

### Admin Commands

| Command | Description |
|---|---|
| `/seasons forcemilestone` | Forces an immediate weekly milestone snapshot regardless of day. Broadcasts results in-game and to Discord |
| `/seasons resetcache` | Flushes all in-memory leaderboard and player standing caches |
| `/seasons status` | Shows current week number, last milestone date, cache entry counts, and active streak count |

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `season_cache_ttl_minutes` | long | `5` | How long (minutes) the top-10 cache is considered fresh before a DB re-fetch |
| `season_milestone_webhook` | string | `""` | Discord webhook for Sunday milestone announcements |

### ELO Decay

The 1v1 and 2v2 leaderboard score **is** the ELO rating — wins, matches played and 2v2 survivals are tracked as stats but contribute nothing to rank.

Decay runs once per calendar day from `ArenaManager.Tick()`. The rate is set by how many matches the player completed **in that same format** over the trailing 7 days, counted from `arena_player` joined to finished `arena_event` rows (status 5 or 6; cancelled events do not count):

| Matches in last 7 days | 1v1 daily decay | 2v2 daily decay |
|---|---|---|
| 0 | 5% | 3% |
| 1 – 2 | 3% | 1% |
| 3 – 9 | 1% | none |
| 10+ | none | none |

Decay applies to the rating **above the 1500 baseline** only (1800 with no matches loses 5% of 300 = 15 points), and never drops a rating below 1500. Ratings already at or under 1500 are untouched.

**Team ratings (`arena_team_stats`) do not decay at all.**

Each row's `last_decay_datetime` is stamped every time the job examines it — and by a match result — whether or not decay was owed. The job skips any row already stamped today, so a mid-day server restart cannot apply a second day of decay. Tiers live in `ArenaRanking.DecayTiers1v1` / `DecayTiers2v2`.

---

## 10. Tinkering Lotto

When enabled, every tinkering attempt has a chance to trigger a special bonus outcome beyond the normal tink result. The lotto fires at tink time (not at item creation) and sends a bonus message to the player if it triggers.

### Active Salvage Types

| Salvage | Lotto Effect |
|---|---|
| Steel | Bonus Armor Level (jackpot: +10 AL; normal: +1–5 AL); chance at Creature Resist/Slayer rating |
| Iron | Bonus +1 damage (capped at 1 per item) |
| Granite | Bonus +1 variance improvement |
| Opal | Bonus cast bonus |
| Mahogany | Bonus melee defense bonus |
| Velvet | Bonus melee defense bonus |
| Brass | Bonus range defense bonus |
| Aquamarine / Black Garnet / Emerald / Imperial Topaz / Jet / Red Garnet / White Sapphire | Resistance/Cleavage imbu |
| Sunstone / Fire Opal / Black Opal | ARC/SC/BL bonus |
| Zircon / Peridot / Yellow Topaz | Defense imbue bonus |

Green Garnet lotto is currently disabled (stub code exists, commented out).

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `tinker_lotto_enabled` | bool | `false` | Enables the Tinkering Lotto system |

---

## 11. Tinker Character Designation

Players can flag a level-1 character as a **Tinker** using `/FlagTinker`. This is a permanent, one-per-account designation that converts the character into a crafting specialist.

### What Gets Applied on Flag

- `GameplayMode` is set to `Tinker` (30000)
- All crafting skills are **auto-specialized and maxed** with XP:
  - Item Tinkering, Weapon Tinkering, Armor Tinkering, Magic Item Tinkering
  - Alchemy, Lockpick, Fletching, Cooking
- All combat/offensive skills are **untrained** (the character cannot function as a combatant)
- All attributes are **maxed** with XP and vitals are refreshed

### Restrictions on Tinker Characters

- Cannot train new skills (blocked at the server)
- Cannot specialize skills post-creation
- No vitae penalty on death

### Admin Notes

- The `/FlagTinker` command is player-issued but admins should be aware it's **irreversible**. There is no admin undo command — if a player flags incorrectly, a character rebuild via DB edit is the only recourse.
- The one-per-account guard checks all characters on the account, including offline ones. If the check needs to be bypassed for testing, it requires a direct DB intervention (clear `GameplayMode = 30000` on the existing Tinker character).

---

## 12. Discord Webhooks

All webhooks are set via string properties in `shard_config`. Leave empty to disable that channel. Each channel can route to a different Discord webhook URL.

| Property | What It Posts |
|---|---|
| `turbine_chat_webhook` | In-game general chat messages (General channel) |
| `turbine_chat_webhook_audit` | Admin/audit log events (admin commands, IP binding actions, cap changes, etc.) |
| `pk_kill_webhook` | Open-world PK kills (player kills and hardcore PKL kills) |
| `hot_dungeon_webhook` | Hot Dungeon activation, hourly re-announce, and expiry |
| `town_control_globals_webhook` | Town Control conflict start, boss deaths, capture events |
| `season_milestone_webhook` | Weekly Sunday season milestone snapshots and leader announcements |
| `arena_globals_webhook` | Arena match global broadcasts |
| `movement_violation_webhook` | Anti-cheat movement violation alerts (all types: speed, geometry, jump, door ghost, scripts) |

**Format for all channels:** Plain text messages via HTTP POST. The `DiscordWebhookManager` uses `HttpClient` with fire-and-forget async dispatch — webhook failures are logged but do not affect gameplay.

---

## 13. Anti-Cheat (Movement Enforcement)

All movement checks are **disabled by default**. Enable `enforce_player_movement_speed` first — nothing else in this section fires without it.

### Recommended configuration (detection-based)

Leave `enforce_player_movement` **off** (the stock default). It is a blunt physics rubber-band that rejects *every* client position the server physics disagrees with — including harmless collisions with mobs, other players, and terrain slopes — and is the historical source of collision/hill rubber-banding. The anti-cheat is handled instead by the **detection checks** below, which correct position only on a *proven* violation (speed hack, wall-clip, door-clip).

```
/modifybool enforce_player_movement        false   # off — no blunt collision/terrain rubber-band
/modifybool enforce_player_movement_speed  true    # master gate for all detection checks
/modifybool enforce_player_movement_raycast true    # wall-clip detection (terrain-robust)
/modifybool enforce_player_door_collision  true    # REQUIRED for doors when the above is off
/modifybool enforce_player_movement_avg    true    # sustained speed-hack detection
```

With `enforce_player_movement` off, **`enforce_player_door_collision` is the only thing stopping players from walking through closed doors** — make sure it is on.

### Master Switches

| Property | Description |
|---|---|
| `enforce_player_movement` | Blunt physics rubber-band: rejects any client position that fails physics validation, including mob/player/terrain collisions. **Recommended off** (see above); when off, the detection checks below still run and are the anti-cheat |
| `enforce_player_movement_speed` | Gates all scoring and detection checks. Must be ON for anything below to fire — this, not `enforce_player_movement`, is the real master switch |
| `movement_violation_kick` | Kick when violation counter ≥ 10. Score ≥ 50 always kicks regardless of this flag |
| `movement_violation_webhook` | Discord webhook URL for real-time alerts. Leave blank to disable |
| `movement_packet_rate_limit` | Max movement packets/sec before flood detection fires (default: 75). Legitimate players reach ~35/s on fast machines and while glitch-running; do not set at or below ~40 |

### Physics-Based Checks

| Property | What It Detects |
|---|---|
| `enforce_player_movement_avg` | Sliding-window average horizontal speed over 3 s and 15 s windows, measured against a **time-integrated allowance**: each segment between samples is budgeted at `4.0 × effective run rate × segment time`, where effective rate is the server-side run rate times the engine's own **1.248× strafe-run multiplier** for segments where the player was actually strafing. Legit glitch-runners are budgeted correctly; a quickness-cheating forward runner is measured against plain forward speed. Forward ceilings are configurable: `movement_avg_ceiling_3s` (default **1.30**, burst headroom) and `movement_avg_ceiling_15s` (default **1.15**, the primary sustained-hack detector). **Strafe segments** are capped separately (flat, not stacked on the ceiling) so faking the sidestep flag can't buy ceiling-stacked speed: `movement_avg_strafe_ceiling_15s` (default **1.30** = legit 1.248× + margin) and `movement_avg_strafe_ceiling_3s` (default **1.45**, looser for burst headroom). Lower the 15 s strafe ceiling toward 1.26 to tighten (below ~1.25 risks real strafe-runners; theoretical floor is 1.248). **Decisive sustained kick:** `movement_avg_sustained_kick_windows` (default **3**) consecutive 15 s windows over the ceiling kicks outright, independent of the suspicion score — ~45 s of sustained over-ceiling average is not achievable legitimately (glitch-runners never trip the ceiling), and blatant overages (≥ 50% over) kick one window sooner. This is what catches a moderate quickness hack quickly instead of over minutes. Score decay is also **suppressed for 20 s after any average-speed violation**, so a sustained cheat climbs monotonically rather than having its gains cancelled between window fires; a one-off false positive still decays once the grace lapses. False-positive protections: per-segment rates absorb buff-expiry/road-exit/exhaustion transients, the buffer is cleared on every teleport, scoring is suppressed during rubber-band recovery, and each window scores at most once per its own length |
| `enforce_player_movement_raycast` | Wall-clip detection — flags a blocked transition **only** when the contact normal is wall-like (near-horizontal; Z below the walkable-floor threshold). Walkable slopes and steps have floor-like normals and are never flagged, so this is safe to run with `enforce_player_movement` off (where it is the sole wall defense) — hills do not false-fire. Doors are handled separately (`enforce_player_door_collision`), never here. 2-second cooldown after first hit prevents cascade false kicks in tight corridors |
| `enforce_player_jump_height` | Jump apex cap via InqJumpVelocity(Strength, Jump). Fires if apex exceeds max height × 1.5 (50% lag fudge). Same-landblock only |
| `enforce_player_door_collision` | Closed-door collision. Rubber-bands the player back so they cannot pass through a closed door. **Enforcement only — no suspicion score and no kick**; a closed door is treated like a wall (a player leaning on one, often from door-state desync, is simply stopped). Logging is throttled to once per 5 s per player |
| `enforce_player_spawn_collision` | Spawn overlap detection. +4 score — lowest weight. Server-side spawn timing can coincide |

> **Speed checks measure horizontal distance.** Both the per-packet `speed_packet` check and the `enforce_player_movement_avg` windows compare *horizontal* (2-D) displacement against the run budget, since run rate governs horizontal ground speed. This removes the false positives that used to fire on slopes (where the vertical climb inflated 3-D distance past the budget): climbing a hill can no longer exceed the flat budget, so the budget itself can be held tight on flat ground where a client-side quickness hack is most visible. Downhill and airborne momentum are still covered by the server-computed physics velocity, which the client cannot spoof.
>
> **Per-packet budget floor.** The `speed_packet` budget is never below `4.0 × effective run rate × elapsed time (capped 1.5 s) × 1.25` — full legitimate-speed coverage of the time since the last checked packet. This absorbs the post-physics-failure catch-up case (hills zero the physics velocity and the next packet spans several rejected ones) without loosening steady-state detection. Pacing packets to ride the floor doesn't evade detection: the average-speed windows integrate the same allowance without the headroom.

### Script Detection Checks

These are **heuristic** checks — statistical inferences rather than provable physics violations. They **score and log only; they do not rubber-band** the player (see "Hard vs. soft checks" below). A sustained scripter still accrues score to the kick threshold, but a legitimate player who trips one is not jerked around.

| Property | What It Detects |
|---|---|
| `enforce_player_timing_regularity` | Inter-packet timing regularity. CV < 0.015 over a 4-second window flags bot-level precision. Human hands: CV ≈ 0.15–0.40. AC client fixed-rate: CV ≈ 0.02–0.06. Scripts: CV < 0.005. Do NOT raise above 0.04 |
| `enforce_player_packet_rate` | Packet flood. Fires above `movement_packet_rate_limit` (default 75/s). Legitimate clients reach ~35/s on fast machines / while glitch-running; scripts flood at 100+/s. Scoring is throttled to once per second so a brief burst cannot spike the score |
| `enforce_player_reversal_detection` | Inhuman direction reversal. A qualifying detection needs 4 consecutive buffer entries with all three intervals < 66 ms, all three steps > 0.2 units of displacement, and two adjacent headings both within 10° of 180°. A single detection is **not** scored — it only counts toward a sustained threshold (4 detections within 2 s), so brief glitch-running does not score while continuous kiting/dodge scripts do |

### Hard vs. soft checks

| Class | Checks | On violation |
|---|---|---|
| **Hard** (provable physics) | `speed_packet`, `geometry`, `spawn_ghost`, `jump_height` | Rubber-band the player back to the last confirmed position **and** score |
| **Enforcement-only** (physical barrier) | `door_ghost` | Rubber-band the player back — **no score, no kick**. A closed door is treated like a wall: the player is stopped from passing through, not punished |
| **Soft** (heuristic) | `speed_avg_3s/15s`, `script_timing`, `script_packet_rate`, `script_reversal` | Score and log only — **no rubber-band** |

The split exists so legitimate-but-unusual movement (glitch-running, high framerates, sticky-target combat facing) that trips a heuristic isn't visibly disrupted, while genuine wall-walks and speed hacks are still corrected on the spot. Soft checks still contribute to the kick threshold, so a sustained scripter is still removed.

**Collisions with mobs, players, and terrain are not rubber-banded** in the recommended configuration (`enforce_player_movement` off). The physics engine trusts the client position for these; only the detection checks correct position, and only on a proven violation. This is what keeps players from being rubber-banded when running through monster packs, brushing other players, or crossing hills. (When `enforce_player_movement` is *on*, the engine reverts to stock behaviour and rubber-bands every such disagreement — the reason it is recommended off.)

**Melee sticky-chase exemption:** the per-packet `speed_packet` check is additionally waived (no rubber-band, no score) while a player is actively engaged in melee against a live target within melee range. Sticky auto-face and hill terrain routinely make legitimate melee movement read as "too fast" or "wrong location." The exemption is bounded and cannot be used as a speed-hack bypass: it requires melee combat mode with an active attack sequence and an in-range living target, and the 15-second average-speed check still applies throughout.

### Suspicion Score System

Score accumulates on each violation and decays during clean movement: −3 per heartbeat (~5 s), rising to −6 per heartbeat after ~15 s of clean movement. Decay runs whenever the score did not rise since the previous heartbeat, **regardless of violation type** — so an occasional false positive fades within a heartbeat or two instead of ratcheting permanently toward a kick, while a genuine cheater who keeps the score climbing never benefits from decay.

| Violation Type | Score Gain |
|---|---|
| `speed_packet` | `overage × 10`, max 15 (borderline: ×0.5) |
| `speed_avg_3s` | proportional with +3/consecutive-window streak floor, max 10 — at most once per 3 s |
| `speed_avg_15s` | proportional with +3/consecutive-window streak floor, max 15 — at most once per 15 s |
| `geometry` | +5 |
| `jump_height` | `overage × 10`, max 15 |
| `door_ghost` | none — enforcement only (rubber-band, no score/kick) |
| `spawn_ghost` | +4 |
| `script_timing` | +6 |
| `script_packet_rate` | +4 |
| `script_reversal` | +7 |

**Score ≥ 50** → immediate kick, always. **Counter ≥ 10** + `movement_violation_kick=true` → configurable kick.

### Database Table (`ace_log.movement_violation_log`)

| Column | Type | Notes |
|---|---|---|
| `id` | INT UNSIGNED | Auto-increment PK |
| `character_id` | INT UNSIGNED | Player GUID (indexed) |
| `character_name` | VARCHAR(255) | |
| `account_name` | VARCHAR(255) | Indexed |
| `violation_type` | VARCHAR(64) | Indexed |
| `observed_speed` | FLOAT | Measured value (units vary by type) |
| `allowed_speed` | FLOAT | Configured/computed limit |
| `suspicion_score` | FLOAT | Running score at time of violation |
| `location` | VARCHAR(512) | Landblock + XYZ string |
| `violation_datetime` | DATETIME | UTC, indexed |

### Useful Queries

```sql
-- All violations for a suspect, oldest first:
SELECT violation_datetime, violation_type, observed_speed, allowed_speed, suspicion_score, location
FROM movement_violation_log WHERE account_name = 'ACCOUNT' ORDER BY violation_datetime;

-- Top offenders last 7 days, grouped by type:
SELECT account_name, character_name, violation_type, COUNT(*) AS hits, MAX(suspicion_score) AS peak_score
FROM movement_violation_log
WHERE violation_datetime > DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY account_name, character_name, violation_type
ORDER BY hits DESC;

-- Accounts that ever crossed the kick threshold:
SELECT account_name, character_name, MAX(suspicion_score) AS peak
FROM movement_violation_log
GROUP BY account_name, character_name
HAVING peak >= 50 ORDER BY peak DESC;
```

---

## 14. Bounty System

| Property | Type | Default | Description |
|---|---|---|---|
| `bounty_system_enabled` | bool | `true` | Master on/off for the bounty system |
| `writ_of_pursuit_enabled` | bool | `true` | Enable Writs of Pursuit (player-placed custom bounties) |
| `bounty_allow_all_locations` | bool | `true` | Allow bounty contracts to be valid at any location (recommended for ClassicPvP) |
| `bounty_allow_logged_out` | bool | `false` | Allow offline players to be bounty targets |
| `bounty_pk_timer_active_enabled` | bool | `true` | Extend PK timer when a hunter is near their bounty target |
| `bounty_expirations_enabled` | bool | `true` | Enable contract expiration |
| `bounty_expiration_time` | long | `60` | Minutes until a contract expires after purchase |
| `bounty_cooldown_expiration_time` | long | `0` | Minutes a hunter must wait after turning in a bounty before buying another (0 = no cooldown) |

---

## 16. Loot-to-Weenie Export

Captures a live loot-generated item and writes it out as a permanent weenie SQL file in the content folder. Use this to freeze an interesting or well-rolled item into a static weenie that can be spawned, placed as a vendor item, or given as a quest reward.

### Usage

1. ID the item in-game (`Alt+click` or use the Assessment skill on it).
2. Run `@loot-to-weenie` as an admin.

```
@loot-to-weenie           — allocates the next available WCID (≥ 1,000,000)
@loot-to-weenie <wcid>    — writes to the given WCID, overwriting any existing file
```

The last item you appraised is always used as the source.

### What It Does

- Verifies the item has an `ItemWorkmanship` property (confirms it is loot-generated, not a static world weenie).
- Allocates the next available WCID in the custom range (≥ 1,000,000), queried live from the world database.
- Copies all Biota properties (Int, Bool, Float, String, DID, AnimPart, Palette, TextureMap, SpellBook) into a new weenie template. Live-object instance references (owner GUID, wielder GUID, container GUID) are **excluded** — these have no meaning in a static weenie.
- Exports the weenie as a `.sql` file into the content folder under the appropriate subfolder for the item's WeenieType/ItemType (e.g. `content/sql/weenies/MeleeWeapon/Sword/`).

### After Export

The SQL file is written to disk and immediately imported into the world database. The weenie cache is cleared and reloaded, so the result is live without a restart. No further action is required.

When a WCID is passed explicitly, any existing SQL file(s) for that WCID are deleted from the content folder before the new file is written, so stale files from a previous item name do not accumulate.

### Notes

- The command uses `CurrentAppraisalTarget`, which is the last item the admin's character appraised. If you've appraised multiple items in succession, only the most recent one is captured.
- WCID assignment is based on `MAX(class_Id)` among all weenies with class_Id ≥ 1,000,000 at the time the command runs. Because the weenie is imported immediately after export, successive uses of the command will always increment correctly. If two admins run the command at exactly the same moment, both may read the same max before either import completes — coordinate accordingly.
- The exported file name follows the standard convention: `{WCID} {Name} - {ClassName}.sql`.

---

## 17. Spell Management

### Grant School Spells

Grants all spells of a specific magic school and level to an online target player.

```
@grantschoolspells <player name> <school> <level>
```

| Argument | Valid Values |
|---|---|
| `player name` | Any online character name (multi-word names supported) |
| `school` | `War`, `Life`, `Creature`, `Item`, `Void` (case-insensitive) |
| `level` | `1` – `8` |

**Examples:**

```
@grantschoolspells Jimmy War 7
@grantschoolspells Jimmy Life 6
@grantschoolspells Some Player Creature 5
```

The target must be online. Spells are added silently (no purple particle effect per spell) but the player's spellbook updates immediately. Already-known spells are skipped.

> **Note:** For Infiltration ruleset, levels 1–7 are the valid range. Level 8 exists in the enum but no Infiltration spells are defined at that tier.

### Beneficial Spell Duration Modifier

A server-wide multiplier that scales the duration of all beneficial spells at cast time. Useful for extending buff uptime during events or seasons without modifying individual spell data.

| Property | Type | Default | Description |
|---|---|---|---|
| `positive_spell_duration_modifier` | double | `1.0` | Multiplier on beneficial spell duration. Result is rounded up to the nearest second. `1.5` = 50% longer, `2.0` = double. |

Applies to newly cast spells and refreshed spells (re-casting while the buff is still active). Does **not** affect damage-over-time spells, weapon spells cast during combat, or item enchantments applied at equip time.

Changes take effect on the next cast — already-active enchantments are not retroactively adjusted.

```
/modifydouble positive_spell_duration_modifier 1.5
```

---

## 15. Admin Command Quick Reference

### Rolling Cap & XP

| Command | Summary |
|---|---|
| `/startrollingcap` | Start the season rolling cap from today |
| `/forcerollingcap` | Force-recalculate rolling_xp_cap (and xp_modifier if enabled) |
| `/rollingcapstatus` | Show cap status, season day, XP modifier state |
| `/grantxp [name] <amount> [force]` | Grant XP; `force` bypasses the season cap and max-level gate (testing) |
| `/pvpdmgpresets` | List pvp_dmg_mod presets and active one |
| `/reloadpvpdmgpresets` | Hot-reload pvp_dmg_mod_presets.json |
| `/applypvpdmgpreset [n]` | Force-apply preset at threshold n |

### Hot Dungeons

| Command | Summary |
|---|---|
| `/SwitchHotDungeon` | Force a new Hot Dungeon roll |
| `/ForceHotDungeon` | Make your current landblock Hot |
| `/ProlongHotDungeon` | Extend all active Hot Dungeons by 1 hour |

### Account / IP

| Command | Summary |
|---|---|
| `/checkipbinding <account>` | Show IP binding, shared-account counts, allowances, and change history |
| `/clearipbinding <account>` | Remove IP binding and reset monthly counter |
| `/setipallowance <ip> <count>` | Set how many accounts may bind to an IP (default 1) |

### Season

| Command | Summary |
|---|---|
| `/seasons forcemilestone` | Force a weekly milestone snapshot now |
| `/seasons resetcache` | Flush all leaderboard caches |
| `/seasons status` | Show season manager status |

### Spells

| Command | Summary |
|---|---|
| `@grantschoolspells <player> <school> <level>` | Grant all spells of a school+level to an online player |

### Content

| Command | Summary |
|---|---|
| `/loot-to-weenie` | Capture the last ID'd loot item as a weenie SQL file in the content folder |

### Dungeon Bosses

| Command | Summary |
|---|---|
| `/dungeonboss list` | List active dungeon bosses with level, HP and exact location |
| `/dungeonboss spawn [name]` | Force-spawn a boss at your location (random available if unnamed); no global broadcast |
| `/dungeonboss tele [name]` | Teleport to an active boss (first active boss if unnamed) |
| `/dungeonboss remove [name]` | Despawn matching active boss(es), or all if unnamed |

---

## 18. Dungeon Bosses

Random scaled bosses that replace a normal monster spawn in an active Hot Dungeon or the Abandoned Mine (Subway). A normal monster spawn from a generator in an eligible landblock has a small chance to be promoted to a boss whose combat stats are scaled to the current season level cap. A location-free flavor message is broadcast globally on spawn (players have to hunt for it); on death the boss scatters currency and grants XP + normal loot.

### How It Works

- **Trigger:** hooks the generator spawn path (`GeneratorProfile.Spawn`) and rolls per eligible monster spawn.
- **Eligible landblocks:** any active Hot Dungeon, plus the Abandoned Mine (`0x01C9`).
- **Gates (in order):** feature enabled → hostile monster → eligible landblock → global cooldown since the last boss → one boss per landblock → no duplicate boss weenie active → roll chance.
- **Scaling:** continuous (no bands). Health, damage, armor, attributes, skills and XP scale smoothly with the level cap, then by per-archetype multipliers and the global difficulty/defense knobs. Defensive skills are deliberately kept below a near-maxed character's offense so bosses resist/evade sometimes, not constantly — tune with `dungeon_boss_defense_mult`.
- **Roster (5):** The Gravewalker, Vaeth'ren the Emberlord, Rendmaw, Aggregate Prime, Nharim Dul the Whispering Death. Defined in `Entity/DungeonBoss/DungeonBosses.cs`; weenies `940001`–`940005` in `Content/sql/weenies/DungeonBosses/`.

### Rewards on Kill

| Reward | Delivery |
|---|---|
| A Box | Scattered on the ground around the corpse (contestable). Count = `dungeon_boss_box_count` |
| PK Trophies | Awarded to the inventory of **every player who damaged the boss** (Bonded — cannot be floored). Count = `dungeon_boss_trophy_count` |
| Phials of Bloody Tears | Awarded to the inventory of **every player who damaged the boss** (Bonded + Attuned). Count = `dungeon_boss_phial_count` |
| Normal loot | Generated on the corpse from `treasure_death` profile `940000` (set as each boss's `DeathTreasureType`) |
| XP | Scaled `XpOverride`, flows to damagers as normal |

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `dungeon_boss_enabled` | bool | `false` | Master on/off switch. Requires Infiltration ruleset |
| `dungeon_boss_spawn_chance` | double | `0.0005` | Per-eligible-monster-spawn probability a normal monster becomes a boss |
| `dungeon_boss_min_seconds_between` | long | `1800` | Global minimum seconds between any two boss spawns |
| `dungeon_boss_difficulty_mult` | double | `1.0` | Global multiplier on health/damage/skills on top of level-cap scaling |
| `dungeon_boss_health_exponent` | double | `1.4` | Exponent for health scaling vs the level cap |
| `dungeon_boss_damage_mult` | double | `1.0` | Extra multiplier on boss melee (body-part) damage |
| `dungeon_boss_defense_mult` | double | `1.0` | Multiplier on defensive skills (evade/resist frequency). Lower if bosses resist too often |
| `dungeon_boss_armor_mult` | double | `1.0` | Multiplier on natural armor, which mitigates **melee and missile only** — spell damage ignores armor entirely. Lower if weapons hit bosses for too little; raise to make bosses tankier against weapons without touching health, damage or magic |
| `dungeon_boss_trophy_count` | long | `10` | PK Trophies awarded to each participant on kill |
| `dungeon_boss_box_count` | long | `3` | A Boxes scattered on the ground on kill |
| `dungeon_boss_phial_count` | long | `3` | Phials awarded to each participant on kill |
| `dungeon_boss_max_age_hours` | long | `4` | Safety cap: release a boss's slot if it has been tracked this long (e.g. its landblock unloaded) |
| `dungeon_boss_webhook` | string | `""` | Discord webhook for boss spawn/slain announcements |

### Admin Commands

| Command | Description |
|---|---|
| `/dungeonboss list` | List active dungeon bosses with level, HP and exact location |
| `/dungeonboss spawn [name]` | Force-spawn a boss at your location, bypassing the roll/cooldown gates (random available boss if no name). Does **not** send the global broadcast, so it's safe for testing |
| `/dungeonboss tele [name]` | Teleport to an active boss (matched by name or wcid; first active boss if no name) |
| `/dungeonboss remove [name]` | Despawn matching active boss(es), or all active bosses if no name. No rewards are dropped |

### Boss Appearances

Each boss mirrors the model, animation, sound and physics of an existing creature. Its appearance properties (`Setup`, `MotionTable`, `SoundTable`, `CombatTable`, `PaletteBase`, `ClothingBase`, `Icon`, `PhysicsEffectTable`, `CreatureType`, `DefaultScale`) are copied verbatim from the reference weenie:

| Boss | Looks like | Reference wcid |
|---|---|---|
| The Gravewalker | Phantasm | `24325` |
| Vaeth'ren the Emberlord | Controlled Flamma | `20024` |
| Rendmaw | Tusker | *(unchanged)* |
| Aggregate Prime | Basalt Golem | `11994` |
| Nharim Dul, the Whispering Death | Shadow Captain | `6554` |

To re-skin a boss, copy those DIDs from the new reference weenie into the boss's SQL file in `Content/sql/weenies/DungeonBosses/`. **Pick a reference with its own dedicated creature `Setup`.** Creatures built on the generic human setup (`0x02000001`) carry no geometry of their own — their whole appearance comes from the clothing table, and if the client dat has no clothing entry for that setup the boss renders as an untextured naked human. Don't forget `PaletteTemplate` (int type 3) alongside `PaletteBase`/`ClothingBase`; without it the colour set is never applied. Combat stats are unaffected — they're authored at reference level 275 and scaled at spawn. After changing a model, verify it renders with `/dungeonboss spawn <name>`: if the setup is missing from the client dat the boss fails to enter the world and the failure is logged by name.

---

## 19. Missile Tracking (Experimental)

Four independent changes to how bow / crossbow / thrown projectiles are aimed, plus one to how often player positions are broadcast. **Every one defaults to off** — with no properties set, missile behavior is exactly what it has always been.

Each is gated separately so they can be A/B tested in isolation. Turn on **one at a time**, play a session, and only add the next once you're satisfied nothing else moved.

### The underlying problem

Arrows do not home. One firing solution is computed at launch and the projectile is pure ballistics under gravity after that — this is correct retail behavior and none of these changes alter it. What the changes address is the **prediction horizon** (how far into the future the server has to guess where the target will be) and the **hit envelope** (how much of the target the arrow can actually touch).

A player's collision volume is two spheres of radius 0.48 at z 0.475 and z 1.35, height 1.835. With an arrow radius of 0.10 that is a **0.58 m hit envelope**. Measured prediction horizons against that envelope:

| weapon | 20 m | 30 m | 40 m |
|---|---|---|---|
| bow (27.3 m/s) | 0.81 s horizon → 4.0 m error on a strafe reversal | 1.19 s → 6.0 m | 1.59 s → 8.0 m |
| bow + fast missiles (32.8 m/s) | 0.65 s → 3.2 m | 0.99 s → 5.0 m | 1.31 s → 6.6 m |
| thrown (18.6 m/s) | 1.51 s → 7.6 m | 2.24 s → 11.2 m | out of range |

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `missile_fresh_solution` | bool | `false` | **Fix 1.** Recalculate the firing solution (velocity, spawn origin, orientation) at the instant the projectile spawns, instead of before the turn and aim animation |
| `missile_lead_fallback` | bool | `false` | **Fix 2.** When the quartic intercept solver finds no solution against a moving target, fall back to the lateral solver instead of silently dropping to a zero-lead stationary aim |
| `missile_lead_fallback_log` | bool | `false` | **Fix 2 diagnostics.** Log every quartic intercept failure with distance, target velocity and what the fallback produced. Works independently of `missile_lead_fallback` — turn this on alone first to measure how often the case fires before changing behavior. Noisy |
| `missile_aim_center_mass` | bool | `false` | **Fix 4.** Aim at the center of a player target's collision spheres rather than at the top of the head / the gap between spheres. Player targets only |
| `missile_aim_center_mass_high` | double | `0.75` | Fraction of target height aimed at for AttackHeight **High**. Only used when `missile_aim_center_mass` is on |
| `missile_aim_center_mass_medium` | double | `0.62` | Fraction of target height aimed at for AttackHeight **Medium**. Only used when `missile_aim_center_mass` is on |
| `missile_aim_center_mass_low` | double | `0.27` | Fraction of target height aimed at for AttackHeight **Low**. Only used when `missile_aim_center_mass` is on |
| `player_update_position_threshold` | double | `1.0` | **Fix 5.** Seconds between forced position broadcasts for a moving player. `1.0` is the stock retail-estimated value; lower to `0.2`–`0.33` for tighter PvP sync at a bandwidth cost |

### Fix 1 — stale firing solution (`missile_fresh_solution`)

The firing solution used to be computed *before* the turn and the aim animation, then applied after them. Measured from `client_portal.dat` (motion table `0900020D`), that gap is:

| stance | aim animation length |
|---|---|
| Bow / Crossbow, level shot | 0.033 s |
| Bow / Crossbow, elevated (15°–45°) | 0.067 – 0.267 s |
| Bow / Crossbow, steep (90°) | 0.567 s |
| **Thrown / Atlatl, every aim level** | **0.378 s** |

Plus rotate time on repeat attacks against a circling target. Both the intercept prediction *and* the spawn origin were stale by that much, so the arrow also left from where the shooter had been rather than where they were.

With this on, the solution is re-solved at spawn time. `aimLevel` (and therefore the spawn offset) is deliberately **reused** from the earlier pass — the animation has already played, so the offset has to stay consistent with what the client rendered. If the re-solve fails because the target ran out of range mid-animation, the original solution is kept rather than misfiring; the attack was already committed at that point.

Biggest effect on thrown weapons, near-free for level bow shots. Applies to monster archers too.

### Fix 2 — zero-lead fallback (`missile_lead_fallback`)

The quartic intercept solver returns no solution across a band that is still **well inside** the weapon's max range. Stock behavior falls through to the stationary solver, which aims at where the target is standing *right now* — a guaranteed miss after a 1–2 s flight, with no log line.

Where lead silently dies against a fleeing target:

| weapon | lead lost past | actual max range |
|---|---|---|
| thrown (18.6 m/s) | 21 – 30 m | ~35 m |
| bow (27.3 m/s) | 51 – 61 m | ~76 m |

Closest approach of the arrow to the aim point, integrated through the same math the physics engine uses (0.58 m envelope):

| case | stock (zero-lead) | with fallback |
|---|---|---|
| thrown 26 m, target fleeing 6 m/s | 6.03 m miss | 0.05 m |
| thrown 30 m, fleeing 6 m/s | 8.16 m | 0.25 m |
| bow 60 m, fleeing 6 m/s | 8.97 m | 0.42 m |

> **Known side effect.** No solution exists at the weapon's actual speed — that is precisely *why* the quartic failed — so the fallback velocity is necessarily faster than the weapon's `MaximumVelocity`. Measured within each weapon's real range against a target fleeing at 6 m/s: **thrown +10% at 22 m rising to +18%** at its ~35 m limit, **bows +10% at 55 m rising to +16%** at their ~76 m limit. Well under the engine's hard velocity cap of 50, but these long shots do land slightly sooner than a strict reading of the weapon's stated velocity implies. This only affects shots that currently miss 100% of the time.

Note the two solvers in `Trajectory.cs` take **opposite gravity sign conventions** (`solve_ballistic_arc` wants positive-down; `solve_ballistic_arc_lateral` wants a signed acceleration). This is commented at the call site — worth knowing before touching that code.

### Fix 4 — aim point (`missile_aim_center_mass`)

Stock aim point is `target.Height / GetAimHeight()`, which for a player puts the **High** aim point at the exact top of the upper collision sphere and the **Medium** aim point in the gap between the two spheres. Both are low-tolerance spots:

| attack height | stock aim z | stock lateral tolerance | center-mass | new tolerance |
|---|---|---|---|---|
| High | 1.835 m (top of head) | **0.318 m** | 0.75 × H = 1.376 m | **0.580 m** |
| Medium | 0.918 m (waist gap) | **0.386 m** | 0.62 × H = 1.138 m | **0.540 m** |
| Low | 0.612 m | 0.564 m | 0.27 × H = 0.495 m | 0.580 m |

So stock high attacks have ~45% less lateral tolerance than low attacks for identical prediction error.

**Player targets only.** The fractions are derived from the player collision model specifically. Monsters use a wide variety of setups — many are a single sphere where the existing `Height/2` is already center of mass — and reusing player-tuned fractions there would be a regression rather than a fix. Monster targets always use stock behavior regardless of this setting.

The three fractions are tunable at runtime without a rebuild if you want to shift where arrows visibly land on the model.

### Fix 5 — position broadcast rate (`player_update_position_threshold`)

This one changes what the shooter **sees**, not what the server computes.

Between forced broadcasts, every client dead-reckons other players from their `MoveToState`. Where the server's authoritative position drifts from what other clients are showing, a projectile aimed correctly at the server position can visibly fly at empty space on the shooter's screen. It also governs how much other players glitch around during powerslides.

> ⚠️ **This property reaches less than its name suggests — read before tuning.** It gates only the **MoveToState-derived** broadcast path. Every **AutonomousPosition** packet from the client sets `RequestedLocationBroadcast` and broadcasts *unconditionally*, regardless of this setting:
>
> ```csharp
> if (RequestedLocationBroadcast || DateTime.UtcNow - LastUpdatePosition >= MoveToState_UpdatePosition_Threshold)
>     SendUpdatePosition();                                        // broadcast to everyone in range
> else
>     Session.Network.EnqueueSend(new GameMessageUpdatePosition(this));   // moving player only
> ```
>
> Clients send AutonomousPosition continuously while moving, so a moving player's position is already broadcast far more often than once a second. This threshold is a **backstop for the gaps between autopos packets**, not the primary broadcast rate. The real drift window is bounded by the client's autopos cadence, not by this value.
>
> **Measure before tuning.** If the observed broadcast rate for a moving player is already 10+/s, lowering this changes almost nothing and only adds load. Effective broadcast rate is `min(autopos arrival rate, player physics tick rate)`.

Cost scales as **(moving players) × (players who can see them)** — quadratic in a zerg. `GameMessageUpdatePosition` is 68 bytes of body (retail pcap max) plus ~16 bytes of fragment header. A 30-player siege with everyone moving and in range of each other is ~900 messages/s ≈ 75 KB/s at one broadcast per player per second; the same fight at 0.2 s would be ~4500 msg/s ≈ 378 KB/s **if** the threshold were the only source — in practice autopos already dominates, so the marginal cost of lowering it is much smaller than that ceiling, and so is the marginal benefit.

If you do tune it, step to `0.33` first and measure the delta before considering `0.2`.

**Test this one separately from the other three** — it improves perception rather than hit rate, and if you change it at the same time as a targeting fix you will not be able to tell a perception improvement from a hit-rate improvement in player reports.

### War Magic Tracking — why none of the missile fixes apply

The four missile fixes above are deliberately **not** applied to spell projectiles. Spells do not have any of the same defects:

| Missile defect | Spell equivalent? |
|---|---|
| Stale firing solution (Fix 1) | **No.** `HandleCastSpell` → `CreateSpellProjectile` → `CalculateProjectileVelocity` → `LaunchSpellProjectiles` all run synchronously, *after* the windup animation has completed. There is no animation gap between solving and spawning. The only delay path is `spell.SpellDelay != 0` (delayed metaspells), which is intentional |
| Zero-lead fallback (Fix 2) | **No.** Spells already use `solve_ballistic_arc_lateral`, which finds a solution whenever the horizontal intercept quadratic does. At 15 m/s against a ~6 m/s runner it always does. There is also an existing zero-velocity retry and a `dir * speed` final fallback |
| Aim point on the envelope edge (Fix 4) | **No.** `ProjHeight = 2/3` puts the aim point at 1.223 m, which is 0.127 m from the upper collision sphere center — **0.566 m lateral tolerance out of a possible 0.580 m.** Already effectively optimal |

**War magic is not under-tracking — it is the slowest projectile in the game.** Standard war bolts (`flamebolt` 1499, `lightningbolt` 1635, `shockwave` 1634) have `MaximumVelocity` **15 m/s**, against 18.6 for thrown and 24.9–27.3 for bows:

| distance | war bolt flight (15 m/s) | bow flight (27.3 m/s) |
|---|---|---|
| 20 m | 1.33 s | 0.74 s |
| 30 m | 2.00 s | 1.12 s |
| 40 m | 2.67 s | 1.53 s |

So war magic carries roughly **1.8× the prediction horizon of a bow** — a strafe reversal at 30 m displaces the predicted point by ~10 m for a bolt versus ~6 m for an arrow. Bolts being dodgeable is inherent to that speed, not a bug. Raising it means editing `MaximumVelocity` on the bolt weenies, which is a balance change, not a fix.

Streak spells (`shockwavestreak` 7267 and friends) are 45 m/s and barely leadable at all by comparison.

### "The bolt hit me but looked like it missed"

The direct lever for this is the existing **`spell_projectile_ethereal`** property (default `false`), not `player_update_position_threshold`.

With it **off**, the *client* runs its own collision for the bolt against its own dead-reckoned copy of the target. When client and server disagree on where the target was, the client's bolt sails past while the server registers a hit — exactly this symptom.

With it **on**, spell projectiles are broadcast to clients as ethereal (`WorldObject_Networking.cs:304`), so the client never runs collision on them at all. The server sends an authoritative stop-velocity plus explode script on impact, and the visual matches the server's decision.

Note this checks `this is SpellProjectile`, so it affects **war magic only** — arrows, bolts and thrown weapons are unaffected by it.

A partial mitigation is already applied unconditionally in `SpellProjectile.ProjectileImpact()` — velocity is zeroed and a `GameMessageVectorUpdate` broadcast on impact, which the in-code comment notes also fixes ghost projectiles sailing through the target in default mode. Enabling ethereal mode is the fuller version of the same idea.

### Suggested rollout order

1. `missile_lead_fallback_log` alone — measure how often the zero-lead case actually fires on live, changing nothing.
2. `missile_fresh_solution` — biggest win for thrown, near-free for bows.
3. `missile_aim_center_mass` — widest hit envelope gain, especially on high attacks.
4. `missile_lead_fallback` — fixes the "sometimes it doesn't lead at all" cases.
5. `player_update_position_threshold` — separately, last, and watch bandwidth.

### Related existing knobs

| Property | Notes |
|---|---|
| `fast_missile_modifier` | Default `1.2`. Only applies to players who have the **UseFastMissiles** client option enabled. Prediction horizon scales as 1/speed, so this is the bluntest available lever |
| `trajectory_alt_solver` | Default `false`. Switches missiles *and* spell projectiles to `Trajectory2`. **Bypasses Fix 2 entirely** — the fallback lives on the primary solver path |
