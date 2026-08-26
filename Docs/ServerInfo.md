# 🗺️ ClassicPvP — Server Info & Mechanics Guide

> This is the **living reference** for how ClassicPvP works right now. It is kept current as mechanics change — older behavior is replaced in place rather than dated. For the history of changes over time, see **[ReleaseNotes.md](ReleaseNotes.md)**.

---

## 🚀 Getting Started

### Server Info

| Field | Value |
|-------|-------|
| **URL** | doctide.online |
| **Port** | 9000 |
| **Name** | Classic PvP |
| **Type** | ACE |

### Client Setup

1. Download the `.7z` file from [mega.nz/folder/xi4jiKjJ#jpuTVa7CQYyNxyp-UHC_GA](https://mega.nz/folder/xi4jiKjJ#jpuTVa7CQYyNxyp-UHC_GA)
2. Unzip it with 7-Zip (free utility — google it if you don't have it)
3. Go to `C:\Turbine` and make a copy of your `Asheron's Call` folder. Name the copy **ClassicPvP**
4. Copy the DAT and client (`.exe`) files you downloaded and paste them into `C:\Turbine\ClassicPvP`, overwriting the existing files
5. In **Thwarg Launcher**, click the three dots next to the client path at the bottom and select `C:\Turbine\ClassicPvP\acclient_Infiltration.exe`
6. You're ready — log in to ClassicPvP. To return to an End of Retail server like Doctide, switch the path back to `C:\Turbine\Asheron's Call\acclient.exe`

For more detail, see the full [Getting Started guide](GettingStarted.md).

---

## 🕹️ Server Era — February 2005 (Infiltration Patch)

ClassicPvP is set in **February 2005**, specifically the **Infiltration patch era** of Asheron's Call. This is the classic, raw PvP experience — before years of power creep, skill consolidation, and late-game systems changed the game's identity.

### Weapon Skills
Weapons use the **original, pre-consolidation skill system**. There is no "Light Weapons" or "Heavy Weapons" umbrella — each weapon type has its own dedicated skill:

> Sword · Axe · Mace · Spear · Dagger · Staff · Bow · Crossbow · Thrown Weapons · Unarmed Combat

Every build that relies on weapons invests in a specific skill, which shapes both your identity and your spec choices meaningfully.

### What's NOT Here (EoR Systems)
The following systems belong to the **End of Retail** era and **do not exist** on ClassicPvP:

- ❌ Enlightenment system
- ❌ Void Magic, Summoning, Dual Wield, Two-Handed Combat, Sneak Attack, and other post-Infiltration skills
- ❌ Ratings
- ❌ Equipment Sets
- ❌ Level 8 Spells
- ❌ XP Augmentations
- ❌ Luminance / Luminance Augmentations
- ❌ Cloaks
- ❌ Aetheria
- ❌ Stipends

If you're coming from a more modern server, a lot of the late-game rating bloat is simply gone. Combat is cleaner.

---

## 🔒 Account Restrictions

ClassicPvP enforces a **strict one-account-per-player** policy, backed by the server itself — not just the rules.

- **One IP address, one account.** Each IP may only be associated with a single account. Playing multiple accounts from the same connection is not permitted.
- **IP tracking is automatic.** Every time you log in, your IP is recorded against your account. If your IP changes — because your ISP rotated it, you switched networks, or anything else — the new IP is simply added to your account's list and login proceeds normally. There is no penalty for IP changes.
- **What is blocked:** if you connect from an IP that is already registered to a *different* account, your login will be rejected and you will be prompted to contact an admin.

Common legitimate causes for an IP conflict: a household member plays on the same internet connection, you're connecting from a location another player has used (library, café, friend's house), or a VPN exit node was previously used by another player. Admins can review the binding history and resolve conflicts.

The intent is simple: no multi-boxing, no alt-army farming, no market manipulation through alts. Everyone plays on a level field.

> **For admins:** see **Section 1** of the Admin Guide for the `enforce_account_ip_binding` property, the IP whitelist, and the `/checkipbinding` and `/clearipbinding` commands.

---

## 🔓 Freeing a Stuck Character

Occasionally a character can get **stuck in the world** — it stays "in game" and won't log out (for example, if something went wrong during a death or a teleport). When that happens you don't need to wait for an admin: log onto **another character on the same account** and run

`/ForceLogoffStuckCharacter <stuck character name>`

The server forces the stuck character out of the world so you can log back in on it normally.

- It only works on a character on **your own account** — you can't target yourself, and you can't force off another player's character.
- **Run it twice if needed.** The first run asks the character to log off cleanly and gives it about a minute. If it's still stuck, run the command again — the second run forcibly removes it. This is intentional: the first attempt tries for a clean save before the second one forces the issue.

---

## 🏠 Housing

ClassicPvP relaxes the retail purchase requirements so housing is broadly accessible:

- **No character level requirement.** You can buy any dwelling regardless of your level.
- **No account-age requirement.** The 15-day account age gate does not apply to any house.
- **No allegiance-rank requirement.** Mansions can be purchased without any allegiance rank.
- **No purchase cooldown.** The 30-day wait between purchases does not apply.
- **One house per character** (rather than one per account).

> **For admins:** the level requirement is controlled by the `house_min_level` property and the mansion rank requirement by `mansion_min_rank` (for each: `-1` uses the slumlord's value, `0` disables it, `>0` sets a custom minimum). ClassicPvP defaults both to `0`.

---

## 📈 Rolling Level Cap

ClassicPvP uses a **server-wide rolling level cap**. Every player on the server shares the same ceiling — the maximum level you can achieve goes up on a set daily schedule, and no amount of grinding lets you get ahead of it. If you're at the cap, XP stops accumulating until the next advance. When that happens, you'll receive a chat message letting you know.

The cap opens at **level 15** on launch day. The early season moves fast — you're gaining several levels a day — then the pace slows as you approach the endgame.

> **Reading the tables below:** two things to know.
>
> - **Everything advances _daily_, not weekly.** The level cap and the XP rate both step forward **once every day**, shortly after midnight UTC — the server checks every 15 minutes, so the new values land within 15 minutes of the UTC date rolling over. The tables list rows a week apart only because a 121-row table is unreadable — they are sampled milestones, not the schedule. Every day in between gets its own increase; use `/season status` to see today's exact values.
> - **Every "Day N" is the season day as `/season status` displays it** — launch day is **Day 1**, and the season runs through **Day 121**.

**Sample milestones** (one row per week — the cap still rises every single day):

| Season Day | Level Cap |
|------------|-----------|
| Launch (Day 1) | **15** |
| Day 8 | 36 |
| Day 15 | 57 |
| Day 22 | 69 |
| Day 29 | 80 |
| Day 36 | 90 |
| Day 43 | 101 |
| Day 50 | 111 |
| Day 57 | 121 |
| **Day 61** | **126 — level cap reached** |
| Days 61–121 | Post-cap XP grind |
| After Day 121 | Season cap frozen |

The cap climbs **every day**, in three phases: **+3 levels/day** through Day 16, then **+1.5/day** through Day 46, then **+1.4/day** until it reaches 126 on Day 61. (Fractional caps round up, so a +1.5/day phase alternates between +1 and +2 in practice.) The 21-level jump between the Day 1 and Day 8 rows above is simply seven consecutive daily +3s, not a single weekly unlock.

**What happens after level 126?**
The level cap tops out at 126 (the Infiltration-era maximum). Once that's reached, the rolling cap continues — but now as a raw XP ceiling rather than a level. This extra XP goes toward investing in skills and attributes beyond the level cap. This post-cap grind phase runs from Day 61 to Day 121, growing linearly to the season's total-XP ceiling, after which it is frozen for the rest of the season.

### 📊 Rolling XP Rate Bonus

As the season progresses and the level cap rises, the global XP rate bonus increases alongside it — rewarding players who stay active later in the season.

The bonus starts low in the opening days, holds near 1× (normal rate) through the mid-season, and then accelerates sharply toward the end. Players grinding during the final weeks of the season earn XP significantly faster than those who played only in the early days.

Like the level cap, **the XP rate recalculates once per day** within 15 minutes of midnight UTC — the two are updated together on the same daily tick, so they always advance at the same moment. The rate is a smooth curve evaluated at each new day, so it creeps up a little every single day rather than jumping on a weekly schedule.

**How the rate scales** (sampled weekly for readability — the rate changes daily):

| Season Stage | Approx. Level Cap | XP Rate |
|---|---|---|
| Launch (Day 1) | 15 | **0.25×** |
| Day 8 | 36 | ~0.31× |
| Day 15 | 57 | ~0.40× |
| Day 22 | 69 | ~0.51× |
| Day 43 | 101 | ~0.96× |
| **Day 45** | 104 | **1.0×** (normal rate) |
| Day 61 | 126 (level cap reached) | ~1.50× |
| Day 85 | post-cap XP grind | ~2.44× |
| **Day 97** | post-cap XP grind | **3.0×** |
| Days 97–121 | post-cap XP grind | **3.0×** (maintained) |

**What a week actually looks like, day by day**

To make the daily cadence concrete, here is every day between the Day 8 and Day 15 rows of the table above — the two rows are seven separate daily steps apart, not one weekly jump:

| Season Day | Level Cap | XP Rate |
|---|---|---|
| Day 8 | 36 | 0.31× |
| Day 9 | 39 | 0.33× |
| Day 10 | 42 | 0.34× |
| Day 11 | 45 | 0.35× |
| Day 12 | 48 | 0.36× |
| Day 13 | 51 | 0.37× |
| Day 14 | 54 | 0.39× |
| Day 15 | 57 | 0.40× |

**How the curve is built**

The rate follows a quadratic curve across the season. Let `t` be your progress through the season as a fraction — that is, `t = (Day − 1) / 120`, so `t = 0` on Day 1 and `t = 1` on Day 121. The rate is then:

```
rate = 3.151·t² + 0.917·t + 0.25     (clamped to a 0.25 floor and a 3.0× ceiling)
```

(Those two coefficients are what the default 3× maximum works out to; a different configured maximum produces different ones — see below.)

Those coefficients aren't chosen by hand — they're solved from **three anchor points** the curve is required to pass through:

| Anchor | `t` | Season Day | XP Rate |
|---|---|---|---|
| Season start | 0 | Day 1 | 0.25× |
| Normal rate | 100/275 ≈ 0.364 | ~Day 45 | 1.0× |
| Maximum rate | 220/275 = 0.800 | Day 97 | 3.0× (the configured max) |

The middle anchor falls partway through Day 44–45; since the rate only updates once per day, Day 45 is the first day you actually see 1.0× (Day 43 sits at 0.96×).

Fitting a parabola through the start and maximum anchors while forcing it to hit exactly 1.0× at the middle anchor determines the curve completely. Because the coefficients are re-derived from these anchors every time the rate updates, an admin changing the maximum rate reshapes the whole curve on the next daily tick — the 1.0× crossing stays on Day 45 and the new maximum still lands on Day 97.

The `100/275` and `220/275` fractions are inherited from the original design, which anchored the curve to levels 100 and 220 on a 275-level scale. ClassicPvP caps at level 126, so the curve is anchored to **season progress** instead, keeping the same shape.

Past Day 97 the curve is clamped, so the rate holds at 3× through the end of the season. The practical shape: gains are small and nearly linear at first, and the quadratic term takes over late — by the time the level cap is reached on Day 61 you're already at 1.5×, and the last 25 days of the season run at a flat 3×.

The maximum rate (3×) is configurable by admins and may change between seasons.

**Custom PvP rewards are exempt from this rate.** Arena match XP, PK quest XP, hometown capture XP, and open-world PK kill XP are granted as fixed percentages of your level, so they are *not* multiplied by the rolling XP rate. They neither shrink in the low-rate opening days nor balloon when the rate climbs above 1× late in the season — a given PvP reward is worth the same fraction of a level all season long.

### 🏰 Hometown Ownership XP Bonus

For **every hometown your allegiance owns**, all experience you earn is boosted by **+5%**. The bonus **stacks with no cap** — an allegiance holding four towns earns +20% XP, and so on.

This applies to **all XP sources**: monster kills, quest turn-ins, exploration, and open-world PK kills. The bonus is shared by the entire allegiance — every member benefits from the allegiance's combined holdings, regardless of who captured each town. Own more towns, level faster.

### 🚀 Catch-Up XP Boost

Falling behind the season cap — because you started late, rolled a new character, or simply had a quiet couple of weeks — comes with a built-in correction. While your **total XP is below 70% of the current season XP cap**, all the XP you earn is multiplied, and the size of the multiplier depends on how far behind you are.

| Your total XP vs. the cap | XP boost |
|---|---|
| 0% (brand new character) | **5.00×** |
| 17.5% | 4.25× |
| 35% | 3.50× |
| 52.5% | 2.75× |
| Just under 70% | **2.00×** |
| 70% or above | 1.00× (no boost) |

The multiplier is a straight line between those two endpoints, recalculated from your exact position, so it tapers naturally as you close the gap rather than dropping in steps. At 70% of the cap the boost switches off entirely — at that point you've caught up.

Because the cap itself climbs every day, the threshold moves with it. Standing still while the cap advances will eventually put you back under 70% and re-enable the boost.

**It stacks multiplicatively** with the rolling XP rate and the hometown ownership bonus. A brand-new character grinding late in the season, when the rate is at 3×, earns at 15×.

**It does not raise your ceiling.** Boosted XP still counts against the global cap and against your Monster / Quest / PK budgets — the boost gets you to those limits faster, it doesn't lift them.

**Fellowship note:** XP shared through a fellowship is boosted by the **earner's** catch-up multiplier before it is split, not each member's own. Fellowship with someone who is also behind and you both benefit; fellowship with a maxed player and their kills come to you unboosted.

---

## ⏱️ XP Cap Categories

The rolling cap isn't just one number — your XP is divided into **three separate categories**, each with its own limit. You cannot reach the global cap by grinding a single activity type. You have to mix it up.

| Category | What earns into it |
|----------|--------------------|
| 🐉 **Monster** | Creature kills · Fellowship XP from kills · Allegiance passup XP · Proficiency XP |
| 📜 **Quest** | Quest completions · Exploration XP |
| ⚔️ **PvP** | Player kills · Arena match rewards · Other PvP Focused Custom Content |

**How the limits work:**
Each category has its own budget calculated from how much XP you still need to reach the current cap — your *remaining headroom*. You can earn up to the following portion of that headroom from each category before the bucket is full:

| Category | Budget (% of remaining headroom) |
|----------|----------------------------------|
| 🐉 Monster | 60% |
| 📜 Quest | 60% |
| ⚔️ PvP | 100% |

PvP is uncapped relative to the global ceiling — if you're willing to PK, you can fill your entire remaining headroom from PvP alone. Monster and Quest are each limited to 60%, so neither can carry you to the cap on its own. Once a bucket fills, further XP of that type is blocked until the cap advances.

The global cap is still the ultimate ceiling. Maxing one category doesn't let you earn unlimited XP from the others — all three together still can't exceed your total remaining headroom for the current window.

**When do the buckets reset?**
Not at midnight. Not on a daily timer. They reset **when the rolling cap itself advances** — meaning when the server-wide level ceiling ticks up to the next level. When that happens, your buckets clear and your new budgets are calculated fresh based on however much XP gap remains between you and the new cap. Players who are further behind get proportionally larger budgets.

**Allegiance Passup XP**
XP passed up to you through your allegiance chain counts against your **Monster bucket**, the same pool as creature kills. If you're both actively grinding and receiving heavy passup from your vassals, those two sources compete for the same budget.

**PvP Overflow — Ancient Bottles**
PvP is the one category with a safety valve. If your PvP bucket is full — or you're at the global cap — some of the PvP XP you would have earned doesn't vanish entirely. Instead, **25% of it** is absorbed by **Ancient Bottles** in your inventory (if you have any); the other 75% is lost to the cap. You can then use an Ancient Bottle later when your PvP budget has room, releasing its stored XP at that point — the bottle always releases 100% of what it holds. The bottle holds up to 100 million XP and tells you how full it is as it absorbs overflow.

### Checking Your Status

Use `/season status` to see a live snapshot of the current season:

- **Season day** — how many days have elapsed since launch
- **Level cap** — the current maximum level (or "post-cap XP grind" once level 126 is reached)
- **XP cap** — the exact total-XP ceiling in effect right now
- **Next advance** — hours and minutes until the cap ticks up again
- **Catch-Up** — your current catch-up XP multiplier, or `none` once you're at or above 70% of the cap
- **XP budgets** — your Monster, Quest, and PK XP earned vs. your budget for this window, with a percentage and a `[FULL]` indicator when a bucket is exhausted

---

## 👑 Allegiance Passup XP

Allegiance XP works as it did in the Infiltration era. When your vassals earn XP, a portion accumulates for you as their patron. It is held until you log in, at which point it is delivered in a lump sum and you receive a message showing the amount.

A few things to know:
- Passup XP counts against your **Monster bucket** (same pool as creature kills). If you're actively grinding and also receiving heavy passup from your vassals, both compete for that budget.
- Passup cascades **automatically up the chain** the moment the XP is originally earned — your patron receives a share, their patron a smaller share, and so on up the tree (see the chain mechanics below). What does **not** happen is a *second* cascade when you personally collect your held passup: the lump sum delivered to you on login is not treated as freshly-earned XP, so receiving it does not generate new passup up your own chain.
- The amount of passup you can get at a time without spending it is 4.2 billion xp. If you accumulate that much and don't spend any you will start losing new earnings. 
- You may swear allegiance to a **lower-level patron**, but no passup is generated to them until they surpass their vassal's level. Once the patron out-levels the vassal, passup for that link begins automatically.

### XP Chain Mechanics (Loyalty & Leadership)

The amount of XP that passes through each link in the chain is determined by two skills — one on each end of the link.

**Loyalty** (vassal's skill) controls how much of the vassal's earned XP is *generated* for passup. **Leadership** (patron's skill) controls how much of that generated XP the patron actually *receives*. The final amount the patron gets is the product of both percentages — both sides of the link need to invest for maximum effect.

**Vassal → Patron (first hop):**
- Minimum: ~25% of earned XP passes up
- Maximum: ~90% of earned XP passes up
- Both skills cap at **291** for formula purposes (buffs count)

**Patron → Grandpatron (second hop and beyond):**
- Maximum: **10%** of whatever was received at the previous link
- Every subsequent hop applies the same reduced factors, so the chain burns out quickly regardless of skills

This reflects the behavior patched into the live servers on **January 12, 2004**. Before that patch, the second hop could pass up as much as 94%, making deep chains of well-spec'd characters extremely effective at funneling XP up to a monarch. After the patch (and on this server), the chain collapses after the first link. Loyalty and Leadership are still worth investing in for the direct vassal-to-patron link — 25% vs. 90% is a significant range — but building long XP chains to push XP deep up the tree is not viable. The second hop caps at 10% no matter what.

**Vassal count matters for Leadership.** A patron with only 1 vassal gets 25% of Leadership's bonus. The full benefit requires **4 or more vassals**.

---

## 👥 Swearing Allegiance to Same-Account Characters

You can swear allegiance to another character on your own account using the `/OfflineSwear <CharacterName>` command. Because you cannot have two characters logged in simultaneously, the target must be offline.

All normal allegiance rules apply — the target must be higher or equal level, must not already be your vassal, the account-wide allegiance lock still applies (both characters must end up in the same monarch's chain), and the **PK-trophy swear cost applies exactly as it does for a normal swear** (see below). Swearing with `/OfflineSwear` counts as one of that character's swears.

---

## 🤝 Allegiance Swear Restrictions

ClassicPvP enforces rules around allegiance oaths to prevent abuse and kill-trading between alts.

### Account-Wide Allegiance Lock

All characters on a single account must belong to the **same monarch's allegiance**. Once any character on your account has sworn to an allegiance, your other characters can only swear to someone within that same chain. Attempting to swear into a different allegiance will be blocked.

### Swear Cost (PK Trophies)

Swearing allegiance costs **PK trophies**, on a per-character count that only ever goes up:

- Your **first 3 swears are free**.
- After that, each swear costs PK trophies on a steeply rising scale — **100** for the 4th, climbing to a maximum of **10,000** by the 15th swear:

| Swear # | Cost |
|--------:|-----:|
| 1–3 | Free |
| 4 | 100 |
| 5 | 151 |
| 6 | 231 |
| 7 | 351 |
| 8 | 533 |
| 9 | 811 |
| 10 | 1,232 |
| 11 | 1,873 |
| 12 | 2,848 |
| 13 | 4,328 |
| 14 | 6,579 |
| 15+ | 10,000 |

The trophies are taken from your inventory when the oath is accepted. The count is **per character and lifetime** — it never resets, so repeatedly hopping allegiances gets expensive fast. Swearing to your own alt with `/OfflineSwear` costs the same and counts the same.

> **For admins:** the free-swear count and cost curve are set by `allegiance_free_swears` (default 3), `allegiance_swear_base_cost` (default 100), and `allegiance_swear_max_cost` (default 10000). The cost ramps from the base to the cap over 12 paid swears — with 3 free swears, the cap is reached at the 15th swear.

### What Happens to Your Vassals When You Leave

When you **break** from your allegiance or are **kicked or booted**, your **direct vassals are released** — each becomes their own monarch and keeps their own sub-vassals — and you are left with no allegiance. This only goes **one level deep**: your vassals' vassals stay sworn to your vassals and move with them into their new allegiance.

### Allegiance-Mate Alt Rewards

You cannot earn PvP rewards by killing a throwaway character parked on the account of one of your own allegiance mates. If the character you kill sits on an account that holds **another character in your allegiance**, that kill earns you nothing:

- It does **not** count toward the season **PK-kills leaderboard**, your kill/death ratio, or your kill streak.
- It does **not** advance **PK quest** or **bounty** progress.
- In the **Arena**, if any opponent you defeat is such an alt, that match pays **no rewards**.

The same principle governs hometown warfare: you cannot help attack a hometown **held by an allegiance that another character on your account belongs to**. Those characters do not count toward starting an assault, and their kills during a siege do not advance it.

---

## 🗡️ Same-Target Kill Diminishing Returns

Repeatedly killing the same player yields diminishing returns to prevent coordinated kill-trading.

| Rule | Value |
|---|---|
| Window | 1 hour |
| Kill threshold before suppression | 3 kills |
| Suppression duration | 3 hours |

Once you kill the same player more than **3 times within 1 hour**, rewards are suppressed for the next **3 hours**. During suppression:
- No PvP XP is granted for that kill
- The kill does **not** count toward season leaderboard ranking
- The kill does **not** advance PK quest progress

The killer receives a message when a kill is suppressed. The window and suppression timers are configurable by admins.

---

## 🏟️ Arena System

The Arena is a **queue-based structured PvP system** that operates independently from open-world PK. You join a queue, get matched, get teleported in, fight, and receive rewards.

### Entering the Arena
Use the `/arena` command to interact with the queue.

**Requirements to join:**
- Must be **Player Killer (PK)** status
- Must **not** be PK-tagged (no active PK timer from a recent kill)

**You don't find out who you're fighting until the match starts.** From the moment you're matched through the teleport-in countdown, `/arena info` shows only how many players are in the match, not their names — and pending matches cannot be watched with `/arena watch`. The names appear once the match actually begins. This is deliberate: knowing your draw early made it possible to duck a bad one by logging off or PK-tagging yourself, which cancels the match before it starts and so never counted as a disqualification.

### Arena Types

| Type | Format |
|------|--------|
| **1v1** | One vs. one duel |
| **2v2** | Two vs. two team duel |
| **FFA** | Free-for-All — up to 10 players, last one standing wins. At most 2 players from the same allegiance may be in the same match. |
| **Tugak** | Large Free-for-All — up to 15 players, last one standing wins. No allegiance limit per match. Prefers larger player counts before launching. Fought exclusively with the **Martyr's Hecatomb** (Health Bolt) line of spells — no weapons or other spells deal damage. Has its own separate quest achievement tracking. |
| **Group** | Team-based — organized fellowship vs. fellowship |

### Arena Combat Rules

Arenas run under specific combat restrictions that do not apply in the open world.

- **No damage before the match starts.** After you're teleported into the arena there is a short countdown before the match officially begins. During this window you can cast beneficial spells (buffs, vulns, and other preparation) but cannot deal any damage — melee, missile, magic, or damage-over-time. Damage only begins landing once the match starts.
- **Overtime healing restrictions.** If a match runs long enough to reach overtime, chugging food and potions is disabled and all other healing — heal-over-time spells, life-magic heals, and stamina-to-health transfers — is heavily reduced, weakening further as overtime continues. This forces stalled matches to a decisive finish.
- **Ineptitude spells are suppressed.** Creature enchantment debuffs (inepts) and all item enchantment spells are blocked in arena matches. Only the three defense-lowering spell categories are permitted — Magic Defense Lowering, Melee Defense Lowering, and Missile Defense Lowering. This prevents NPC pets, item procs, or other external debuff sources from influencing match outcomes.
- **Healing kit bonuses are capped in 1v1 matches.** The skill bonus from a healing kit is capped at 150 effective bonus skill, and the restoration multiplier is capped at 1.5×. High-end healing kits still function — they just can't fully carry a fight in the structured 1v1 format.
- **Tugak War is spell-only.** In Tugak War the only weapon is the **Martyr's Hecatomb** (Health Bolt) line of spells, tiers I through VII. Any other harmful spell you try to cast on an opponent simply fails ("you cannot affect anyone"), and weapon attacks and damage-over-time do nothing — everyone fights on equal footing with the same spell.
- **Corpses rot fast and are open to everyone.** A corpse left in an arena landblock decays far faster than in the open world (a few minutes rather than an hour or more), and it is **lootable by anyone**, not just the killer. Grab a fallen opponent's drops quickly — your rivals can loot them too. (See **Corpse Looting Rights** below for the general rule this is based on.)

### Arena Rewards (Winners)

| Type | XP (of level) | PK Trophies | Phials of Bloody Tears | Darkbeat Keys |
|------|-----|-------------|----------------------|------------|
| **1v1** | 10% | 5 | 1 | 1 |
| **2v2** | 15% | 5 | 1 | 1 |
| **FFA** | 35% | 5 | 3 | 5 |
| **Tugak** | 35% | 5 | 3 | 5 |
| **Group** | 30% (60% on a clean 1st-place win) | 5 per member (15 on a clean 1st-place win) | 1 per member (3 on a clean 1st-place win) | 2 per member (6 on a clean 1st-place win) |

A "clean 1st-place win" means your team placed 1st **and** the match wasn't against your own allegiance — same-allegiance group matches never get the 3× bonus, even on a win.

- Arena XP counts against your **PvP daily bucket**.
- Eliminated players should stay online until the match ends to be eligible for rewards.
- Arena XP is a **fixed percentage of your XP to the next level** and is not scaled by the seasonal rolling XP rate — the same result is worth the same fraction of a level all season long. Losers receive **3.5%** in 1v1/2v2, for FFA/Tugak non-podium finishes, and in a draw; FFA/Tugak **2nd** and **3rd** place receive 25% and 15%, and a **group loss** 10%.

### Daily PK Quest Rewards

In addition to the per-match rewards above, completing arena and PK milestones each day earns **Phials of Bloody Tears** and **PK Trophies** through the daily quest system. Quests reset each day and stack — hitting a higher threshold also awards all lower tiers. Selected highlights:

| Quest | Threshold | Phials | PK Trophies |
|-------|-----------|--------|-------------|
| Participate in arena matches | 5 / 15 / 30 / 50 | 1 / 2 / 3 / 3 | 5 / 15 / 25 / 50 |
| Win arena matches (any type) | 10 / 20 / 30 | 2 / 3 / 5 | 25 / 50 / 100 |
| Tugak War — participate | 2 / 25 matches | 1 / 5 | 15 / 75 |
| Tugak War — win | 1 / 20 wins | 1 / 5 | 25 / 75 |
| Tugak War — top 3 | 1 | 1 | 15 |
| Open world kills (opposing allegiance) | 10 / 30 | 1 / 3 | 20 / 60 |
| Complete bounty contracts | 1 / 5 / 25 | 1 / 3 / 5 | 15 / 35 / 100 |
| Complete high priority bounties | 1 / 5 | 2 / 5 | 25 / 50 |

Town Control kill quests (PKKILL_TC_1/5/30) have been disabled and no longer appear in rotation.

### Arena Ranking

Each arena format has its own leaderboard, all viewable with `/arena rank <type>`.

#### 1v1 — ELO Rating
**Your score is your ELO rating, nothing else.** Wins and matches played no longer add anything on top — staying active is rewarded through decay instead, which is what separates a player who defends their rating from one who grinds it up and stops queueing.

- **ELO** updates after every match based on the rating difference between you and your opponent. Starting ELO is 1500.
- **ELO decay** runs once a day, and how hard it hits depends on **how many 1v1 matches you completed in the last 7 days**:

| 1v1 matches in the last 7 days | Daily decay |
|---|---|
| None at all | **5%** |
| 1 – 2 | **3%** |
| 3 – 9 | **1%** |
| 10 or more | **none** |

- Decay only touches the part of your rating **above 1500**, never the whole thing. At 1800 with no matches all week, the 5% comes off the 300 points above baseline — you lose 15, not 90. No amount of decay drops you below 1500.
- Only **1v1** matches count toward your 1v1 tier. Playing 2v2 or FFA does not slow your 1v1 decay — each format is tracked independently.
- Decay is written directly to the database each day, so the stored ELO is always your current effective rating.

Use `/arena rank 1v1` to see the leaderboard.

#### 2v2 — Individual + Team Rankings
2v2 tracks two separate leaderboards:

**Individual** — your score is your 2v2 ELO rating, same as 1v1. Decay works the same way but is **gentler**, because 2v2 needs a partner to be online and draws fewer players:

| 2v2 matches in the last 7 days | Daily decay |
|---|---|
| None at all | **3%** |
| 1 – 2 | **1%** |
| 3 or more | **none** |

As in 1v1, decay applies only to the portion above 1500, and only 2v2 matches count toward your 2v2 tier.

**Team pairs** — your performance as a specific two-player combination is tracked separately. A team's score is the team's ELO, based on the average of both players' individual ELOs at match time.
- Winning teams gain ELO; losing teams lose ELO
- **Team ratings never decay** — a pair that stops playing together keeps its rating

Use `/arena rank 2v2` for individual standings, `/arena rank 2v2team` for team pair standings.

#### FFA & Tugak — Placement Points
FFA and Tugak use a **points-based leaderboard**. Points accumulate across all events you participate in — there is no ELO and no decay.

| Finish Place | Points Awarded |
|---|---|
| 🥇 1st | **100** |
| 🥈 2nd | **50** |
| 🥉 3rd | **25** |
| 4th and beyond | **5** (participation) |
| Disqualified | **0** |

Use `/arena rank ffa` or `/arena rank tugak` to see those leaderboards.

---

## ⚔️ PvP Combat Rules

### Logout Penalty
Logging out during PvP does not protect you. Any spell projectile (War spells, Void spells) that hits a player who is actively logging out will **critical hit 100% of the time** — matching existing melee behavior. Pulling the plug to escape a spell in flight doesn't work.

### Portal Space Behavior
Melee swings and missile attacks can **initiate against targets who are in portal space** (the purple bubble state). The attack animation and windup begin normally, but damage is not applied until the target exits portal space. This matches retail behavior — you could already be mid-swing when a target finishes porting in, and the attack resolves the moment they materialize.

### Dispel Protection After Taking Damage
For a window after being struck in PK combat, your own dispel spells will **not remove vulnerability spells** on your target. This prevents the tactic of attacking someone, getting hit once, then immediately dispelling their vulns to bleed off the damage setup. The protection window is 5 minutes by default and is configurable by admins.

### Jump Spam
Jumping rapidly in succession triggers accelerated stamina drain. After exceeding the jump threshold within a rolling 10-second window, every subsequent jump costs PK-rate stamina for a short penalty period. This eliminates the movement speed advantage gained through rapid jump-chaining.

### Wand Monkeying Disabled
A caster's **built-in spell** (the spell baked into a wand, orb, or other casting implement via its item spell) deals **no damage to other players**. This disables "wand monkeying" in PvP. Regular war magic cast from your own spellbook is unaffected, and built-in caster spells still function normally against creatures — the zero-damage rule applies only when the target is another player.

### PK Trophy Drops
Killing another player in open-world PvP has a chance to drop a **PK Trophy** on their corpse, subject to a few limits:

- **Level range** — no trophy drops if the victim is above the level 126 cap, or if the victim is more than **15 levels below** the killer. This keeps low-level twinks from being farmed by much higher-level killers.
- **Same allegiance** — no trophy drops if the killer and victim share the same monarch.
- **Rate limit (victim-side)** — a given victim can have at most **3 trophies** dropped on their corpse(s) within a rolling **1-hour** window, and at most **10 per day**. Once either limit is hit, further kills on that victim stop producing trophies until the window/day resets.

### Corpse Looting Rights
When you're killed by another player, your corpse is initially **locked to your killer** — only they (or someone you've `/permit`ted) may loot it. Once the corpse has decayed for roughly **20 minutes**, it opens up and **any** player can take whatever remains. Because corpses in arena landblocks are set to rot fast (a few minutes), they reach that open state almost immediately — so arena kills are effectively free-for-all loot the moment they drop.

### Shields Stay Active Out of Combat
An equipped shield contributes its armor level to your defense **even in peace mode** — you don't have to be in combat stance for the shield to protect you. The normal shield rules still apply: it only mitigates attacks coming from your **front** (a 180° frontal arc — anything hitting you from the side or behind ignores the shield), and it works against **both other players and monsters**. There is no shield skill on the Infiltration ruleset, so nothing needs to be trained or specialized — simply wielding a shield is enough.

**Exception:** inside a **1v1 arena**, this does not apply — your shield only counts while you're actually in combat stance.

---

## 🛡️ Enhanced Anti-Cheat

ClassicPvP runs a number of anti-cheat and anti-abuse systems beyond standard emulator defaults.

- **IP Binding** — accounts accumulate IP addresses over time. A login from an IP already registered to a *different* account is rejected automatically (see **Account Restrictions** above).
- **Movement Validation** — the server independently validates player movement against server-side speed limits, so client-side speed and quickness hacks are detected and corrected rather than trusted. The system is terrain-aware — it accounts for legitimate movement over hills and uneven ground while still catching artificially fast movement — and repeat offenders are logged and removed.
- **Comprehensive Server Logging** — the server runs a dedicated logging database that records:
  - All tinkering attempts (success and failure)
  - All PK kill events
  - All Arena match participation and results
  - All rare item drops
  - Account and character login/logout sessions
  - Stuck character force-logoff events
- This gives admins a full audit trail to investigate suspicious activity, item duplication concerns, or systemic exploits.
- Rate limiting is applied to exploit-sensitive player commands to prevent abuse through rapid automated input.
- **War Detect Countermeasure** — TurnTo motions between two PK players use an absolute compass heading rather than a target GUID in the network packet. Plugins that parse network data to identify your spell target (commonly called "War Detect") are unable to extract any player identity from these packets.

---

## 🎯 Bounty System

The Bounty System is a player-driven PvP economy that creates persistent, targeted hunting objectives on top of open-world PK combat.

### How It Works
1. Visit the **Bounty Hunter NPC** with a **Bounty Purchase Token**.
2. You receive a **Bounty Contract** for a randomly assigned eligible PK player (drawn from online players, excluding your own allegiance and players on cooldown with you).
3. Hunt your target. Kill them to mark the contract complete.
4. Return the completed contract to the Bounty Hunter NPC. Completing a bounty **refunds 100 PK Trophies** — the full cost of the Purchase Token — so a successful hunt pays for itself. High Priority contracts pay the custom Writ of Pursuit reward on top of this.

If a contract **expires** (for example, the target is no longer available), returning it to the Bounty Hunter NPC compensates you **25 PK Trophies** for your time.

### Rules & Restrictions
- You must be in a **whitelisted allegiance** to participate in the bounty system.
- You cannot be assigned a target from your own allegiance.
- You cannot be assigned a target from the same IP address as you.
- There is a **maximum number of active contracts** you can hold at once.
- After turning in a completed contract, a **cooldown** prevents you from immediately purchasing another.
- Targets have a per-hunter cooldown — you cannot be repeatedly assigned the same player back-to-back.

### Proximity Mechanic
If you spot your bounty target in the world (or they spot you), their **PK timer refreshes** — preventing them from using portals or recalls to escape the encounter. Proximity to your hunter puts you at risk even if you haven't been directly attacked.

### Writs of Pursuit — High Priority Targets
Any player can place a **bounty with a custom reward** on a specific enemy using a **Writ of Pursuit**:

1. Obtain a Writ of Pursuit item.
2. Inscribe it in the format: `PlayerName:Amount`. The reward amount must be between **200** and **1000** PK Trophies.
3. Turn it in to the Bounty Hunter NPC along with the specified currency amount.
4. That player is flagged as a **High Priority Target** server-wide.
5. Any player who already has a contract on that target sees their contract upgraded.
6. The first bounty hunter to complete the contract receives the custom reward and a **server-wide broadcast**.

High Priority Targets have an increased chance of being assigned to new Bounty Contracts.

### Achievement Tracking
The bounty system tracks milestones over time, including:
- Unique players hunted
- Repeat contracts on the same target
- Speed completions (multiple kills within short windows)
- Kill streak targets broken (hunting players on hot streaks)

---

## 🗡️ Creature Slayer & Creature Resistance Ratings

These are **gear-based rating systems** active in the Infiltration ruleset, sourced from items and tinkering.

### Creature Slayer Rating
Increases your damage dealt to **a specific creature type** (e.g., Undead, Shadow, Lugian). The rating accumulates additively across all equipped items that carry it.

> Formula: `(100 + Slayer Rating) / 100 = damage multiplier against that creature type`
> Example: A Slayer Rating of 25 vs. Undead means +25% damage to Undead.

### Creature Resistance Rating
Reduces incoming damage **from a specific creature type**. Also gear-based and additive across equipment.

> Formula: `100 / (100 + Resist Rating) = incoming damage multiplier`
> Example: A Resist Rating of 25 vs. Shadow means you take ~80% of Shadow creature damage instead of 100%.

Both ratings only apply to **players** — monsters do not carry these ratings. They are a meaningful gearing consideration when farming specific content or building a focused PvE loadout.

Note: Many of the ratings introduced in later retail patches (Crit Rating, Damage Resistance Rating, Healing Boost Rating, etc.) **do not function** in this ruleset — they are entirely disabled. Creature Slayer and Creature Resist are among the few rating systems that **are** active and worth building around.

---

## 🏆 Season Leaderboards

ClassicPvP tracks a **Season leaderboard** across 12 categories spanning both arena and open-world PvP. Every week the top players in each category are recognized and rewarded.

### Leaderboard Categories

#### Arena
| Category | What It Ranks |
|---|---|
| **1v1 Arena** | 1v1 ELO rating |
| **2v2 Arena** | 2v2 ELO rating |
| **FFA Arena** | Lifetime placement points across all FFA events |
| **Tugak Arena** | Lifetime placement points across all Tugak events |
| **Group Arena** | Total Group arena wins |
| **Arena Wins** | Total wins across all arena types combined |
| **Arena Kills** | Total kills recorded inside arena matches |
| **Arena Matches** | Total arena matches played (any type) |

#### Open World
| Category | What It Ranks |
|---|---|
| **PK Kills** | Total open-world player kills |
| **K/D Ratio** | Kill/death ratio (minimum 10 kills to qualify) |
| **Kill Streak** | Best consecutive open-world kill streak without dying |
| **Bounty Hunter** | Total bounty contracts completed |

#### Overall
| Category | What It Ranks |
|---|---|
| **Season Champion** | Weighted rank-points across 11 categories (all except Arena Kills) |

The Season Champion score gives more weight to categories that require skill and consistency. **Arena Kills** is tracked on the leaderboard but does not contribute to the Season Champion score. The 11 weighted categories are:

| Category | Weight |
|---|---|
| PK Kills | 2.5 |
| Arena Wins | 2.0 |
| Kill Streak | 1.75 |
| Bounty Hunter | 1.25 |
| 1v1 Arena, 2v2 Arena, Group Arena | 1.0 each |
| K/D Ratio | 0.75 |
| FFA Arena, Tugak Arena, Arena Matches | 0.5 each |

For each category you are ranked in, you earn `max(0, 11 − rank)` rank-points, multiplied by the category's weight. Your Season Champion score is the total across all 11 categories.

### Weekly Milestones

Every **Sunday**, the server automatically snapshots the top 10 players in each category. This is the weekly **milestone**.

- A server-wide broadcast announces the #1 finisher in each category.
- A full **top 10 in every category**, along with the reward legend, is posted to the ClassicPvP Discord Season channel.
- The **top 10** players in each category earn rewards for that week.

**Milestone rewards by rank:**

| Rank | XP | A-Boxes | Darkbeat Keys | Phials of Bloody Tears | PK Trophies |
|---|---|---|---|---|---|
| 🥇 1st | +200% to next level | 10 | 10 | 20 | 250 |
| 🥈 2nd | +100% to next level | 5 | 5 | 10 | 100 |
| 🥉 3rd | +75% to next level | 3 | 3 | 5 | 50 |
| 4th–10th | +50% to next level | 1 | 1 | 3 | 25 |

Rewards are **not delivered automatically** — you must claim them with `/season rewards`. Unclaimed rewards accumulate and can be collected at any time.

### Commands

| Command | Description |
|---|---|
| `/season status` | Season day, current level cap, and your XP budget usage |
| `/season top` | Current #1 leader in every category |
| `/season top <category>` | Full top 10 for a specific category |
| `/season stats` | Your rank in every leaderboard category |
| `/season stats <name>` | Another player's standings |
| `/season rewards` | Collect any unclaimed weekly milestone reward items |
| `/season info` | Category list and descriptions |
| `/season help` | Full help text and category aliases |

**Category shorthand aliases** — you can type `/season top <alias>` or just `/season <alias>`:

| Alias(es) | Category |
|---|---|
| `1v1` | 1v1 Arena |
| `2v2` | 2v2 Arena |
| `ffa` | FFA Arena |
| `tugak` | Tugak Arena |
| `group` | Group Arena |
| `wins` | Arena Wins |
| `matches`, `veteran` | Arena Matches |
| `reaper`, `kills` | PK Kills |
| `kd`, `ratio`, `precision` | K/D Ratio |
| `streak`, `unstoppable` | Kill Streak |
| `bounty`, `bountyhunter` | Bounty Hunter |
| `champion` | Season Champion |

---

## 🔥 Hot Dungeons

Periodically, up to **3 dungeons** across Dereth will become **Hot** — offering bonus experience and extra loot for players who venture inside.

### How It Works

- Every **12–36 hours**, a new dungeon is selected from a curated list and becomes Hot.
- Each Hot Dungeon stays active for **24–48 hours**, then expires independently.
- A **global broadcast** announces each dungeon when it becomes Hot, and again every hour while it remains active. A final announcement goes out when the dungeon cools down.
- Use the command **`/hotdungeons`** at any time to see all currently active Hot Dungeons, their XP multipliers, and time remaining.

### Dungeon Eligibility

Each dungeon in the pool has a **level bracket** (minimum and maximum server level cap). A dungeon only becomes eligible when the rolling level cap falls within that bracket, ensuring the featured content is always appropriate for the current progression stage of the season.

### Rewards While a Dungeon is Hot

| Reward | Details |
|--------|---------|
| **XP Multiplier** | All monster and PK kills inside the dungeon have their XP multiplied (multiplier varies per dungeon, ranging from 1.5× to 4×). The multiplier is applied before fellowship sharing. |
| **Double Loot** | Monster corpses receive two independent loot rolls, effectively doubling item generation. |
| **A Box** | Each monster kill has a per-dungeon configurable chance to drop **A Box** on the corpse. |
| **Salvage Bonus** | Salvaging items while standing inside a Hot Dungeon yields **double the material** (2× units). Applies to whatever you salvage there, regardless of where the items were looted. |
| **Loot Quality** | Weapons rolled inside a Hot Dungeon have a small chance to come out at the best value available for their wield requirement (the wield requirement itself is unchanged). Each of a weapon's damage stats is rolled separately, so a weapon may upgrade one, all, or none of them: damage and damage variance on melee and thrown, damage mod and elemental damage bonus on bows, crossbows and atlatls, and elemental damage mod and mana conversion on casters. |
| **Loot Mix** | Armor drops lean toward single-slot pieces (helms, breastplates, girths, gauntlets, pauldrons, tassets, bracers, sollerets, shields) over multi-slot pieces. Weapons replace some low-value filler, and most mundane clutter is replaced with equipment. The number of items dropped is unchanged. |
| **PK Rewards** | When a PK kill occurs inside a Hot Dungeon between players of **different allegiances**, the victim's corpse will contain a **Phial of Bloody Tears** and **A Box**. |

### Logout Delay in Hot Dungeons

The extra rewards come with extra risk. While you are standing inside an active Hot Dungeon, **logging out is delayed** — the same pending-logout timer that applies to Player Killers now applies to everyone. When you log out, your character stays frozen in the world for a short time before actually leaving, so you can't instantly escape a dangerous situation by quitting.

**Recalls are not affected.** Portal recall, lifestone recall spells, and recall chat commands like `/lifestone` all work normally — this only delays a straight logout.

### Zerg Control in Hot Dungeons

While a dungeon is Hot, it becomes a **zerg-controlled area** — each allegiance is capped at **9 players** inside at the same time. If a 10th member of your allegiance tries to enter, they will be blocked. If you're already inside and a 10th member slips in, the most recently teleported players will be booted to their lifestone automatically.

This mechanic prevents large allegiances from overwhelming a Hot Dungeon and ensures smaller groups have a fair chance at the bosses and loot.

---

## 🔩 Abandoned Mine (Subway)

The Abandoned Mine — home to Darkbeat and Anti Parazi — has its own standing access restrictions, separate from the Hot Dungeon zerg-control rules above.

- **PK only.** You must be **Player Killer (PK)** status to enter.
- **No recall or summon.** The portal cannot be reached via recall or summoning — you have to walk through it directly.
- **Permanent zerg control.** The Abandoned Mine is always a zerg-controlled area, capped at **5 players per allegiance** at the same time.

---

## 🏘️ Allegiance Hometown Capture

Allegiances can conquer and hold **towns across Dereth** through a two-phase PvP assault system.

### Owning Towns

Any allegiance member can walk up to a **Bind Stone** in an unowned town and use it to **claim the town for free**. Once claimed, the town becomes your allegiance's hometown and all members can recall there.

- `/ah` — Recalls to a random town owned by your allegiance
- `/ahtown <name>` — Recalls to a specific owned town (e.g. `/ahtown Arwic`)
- `/towns` — Lists all 25 capturable towns and their current ownership status

### Capturing an Enemy Town

To take a town owned by a rival allegiance, use the Bind Stone to begin the assault.

**Phase 1 — Perimeter Control (up to 60 minutes), at the town Bind Stone**
- Phase 1 begins **automatically** when at least **2 members** of a single attacking allegiance are within **5 meters** of the Bind Stone and no other enemy allegiances are within **10 meters** — no player action required
- If an enemy PK enters within 10 meters, a warning is broadcast. If they remain for **30 continuous seconds**, Phase 1 progress resets. Leaving the area before 30 seconds have passed cancels the threat with no penalty.
- Hold the zone for **4 uninterrupted minutes** to trigger Phase 2
- Failing to reach Phase 2 within 60 minutes announces a global failure and applies a **3-hour cooldown** on that town for your allegiance

**Phase 2 — Destroy the Bind Stone (30 minutes), inside the town's Meeting Hall**
- When Phase 1 completes, the outdoor Bind Stone goes dark and the fight **moves into the town's Meeting Hall**. Take the Meeting Hall portal in — that is where the attackable Bind Stone appears
- **The Meeting Hall portal ignores the PK timer while Phase 2 is running**, so neither side can be locked out of the fight by being tagged on repeat. Outside Phase 2 the normal PK timer applies
- **Meeting Halls are permanently zerg-controlled** — each allegiance is capped at **7 players** inside a hall at the same time, whether or not a conflict is live. An 8th member is blocked at the portal, and if one slips in, the most recently teleported players are booted to their lifestone. Each hall has its own independent cap
- The Bind Stone becomes attackable — chip down its HP with melee, missile, or war magic
- Breaching Phase 2 immediately awards each attacking-allegiance member near the outdoor stone **5 PK Trophies**
- **All damage types are equal** — no element (slashing, fire, cold, etc.) is more or less effective than another, for both physical and magic
- **Melee and missile damage is reduced** so that a weapon user's DPS stays in line with a mage's, rather than vastly outpacing it. War magic is unaffected
- **Damage falls off with distance** — attacks deal full damage within **15 meters** of the Bind Stone, taper off beyond that, and deal **nothing past 20 meters**. You must fight up close to bring it down
- **You must clear the defenders out of the hall to damage it.** While **any player who is not in the attacking allegiance** is anywhere inside the Meeting Hall, all attacker damage to the stone is **reduced by 90%**. Standing next to defenders and burning the stone anyway ("peacing" past them) doesn't work — you have to drive them out of the hall first
- **Defenders can mend the stone.** A defending-allegiance member who attacks their own Bind Stone doesn't damage it — instead they **heal it by 10%** of the damage they would have dealt
- Bind Stone HP scales with the current rolling level cap
- Each kill on the defending allegiance **inside the Meeting Hall** deals **5% max HP** bonus damage to the Bind Stone
- Each kill on the attacking allegiance **inside the Meeting Hall** **heals the Bind Stone** by 5% max HP
- **Both sides earn PK Trophies for holding the hall.** While inside the Meeting Hall during Phase 2, attackers and defenders each receive **1 PK Trophy per minute** of participation
- **Repelled attack** — if the defenders hold the hall with **at least 2 defenders and no non-defenders inside it for 10 continuous minutes**, Phase 2 ends early as a repelled attack: **Defenders win** and receive the defense rewards. Any non-defender — an attacker *or* a neutral third party — entering the hall resets the repel timer
- Destroy the Bind Stone within 30 minutes → **Attackers win**
- Survive 30 minutes with the Bind Stone intact → **Defenders win**; the outdoor Bind Stone returns and becomes unattackable again

Two allegiances cannot attack the same town simultaneously. An allegiance can maintain at most **2 active assaults** at once.

### Cooldowns & Protection

| Event | Cooldown |
|---|---|
| Phase 1 timeout (failed to reach Phase 2) | 3 hours — attacking allegiance only |
| Phase 2 failure (Bind Stone survived) | 6 hours — attacking allegiance only |
| Successful capture | 8 hours — new owner protected from attack (configurable) |

### Rewards

Winners **inside the Meeting Hall** at the moment of resolution share the rewards. **Defenders are rewarded more generously than attackers** — attackers already gain the town itself on a successful capture, so holding a town pays out the larger loot:

| Reward | Attackers (capture) | Defenders (hold) |
|---|---|---|
| PK Trophies (split among players) | 40 | 80 |
| MMDs (split among players) | 20 | 40 |
| XP to next level (per player) | 5% | 15% |
| Phials of Bloody Tears (per player) | — | 1 |
| Darkbeat Keys (per player) | — | 2 |

Losing allegiance PKs **inside the Meeting Hall** at the moment of resolution are **smited**.

### Using the Bind Stone

Clicking (using) the Bind Stone at any time gives you a status message:
- **Unowned town** — instantly claims it for your allegiance
- **Your town** — confirms ownership and prompts you to defend
- **Enemy town, Phase 1 active** — informs you that an assault is already in progress
- **Enemy town, Phase 2 active** — informs you that the Bind Stone creature is under attack
- **Enemy town, no active assault** — shows any cooldown or blacklist block reason, or tells you the gather requirements to trigger Phase 1

During **Phase 2**, the real Bind Stone becomes invisible (cloaked) and an attackable **Bind Stone creature** appears in its place. Destroying the creature ends Phase 2 and awards the town to the attackers. If the creature survives the 30-minute timer — or the defenders hold the area long enough to repel the attack — the town remains with the defenders.

### Allegiance Blacklist

Server admins can suspend an allegiance from participating in hometown warfare via the blacklist. Blacklisted allegiances cannot initiate Phase 1 and are informed when they attempt to do so.

### Open-World PK Kill XP

When you kill an enemy PK in the open world (different allegiance, no diminishing returns), you earn PvP XP calculated as follows:

**Base XP:**
```
Base XP = 5–10% of your XP-to-next-level (random roll per kill)
```
The random roll is re-rolled on every kill, so repeated kills against the same target vary slightly each time.

**Level gap penalty:**
If the victim is below your level, the base XP is multiplied by a decay factor for each level of difference:
```
Modifier = 0.85 ^ (your level − victim's level)
```
Killing someone at or above your level applies no penalty. Killing someone 5 levels below you reduces the reward to ~44% of base; 10 levels below ~20%.

**Bonuses (applied after the decay modifier, all stack):**

| Condition | Effect |
|---|---|
| Hot Dungeon kill | × dungeon XP multiplier (1.5× – 4×, varies per dungeon) |
| +5% per hometown your allegiance owns | Passive, stacks, no cap — applies to **all** XP, not just PK kills (see [Hometown Ownership XP Bonus](#-hometown-ownership-xp-bonus)) |
| Active hometown conflict on the kill landblock (either phase) | × 2 |
| Allegiance-size penalty (see below) | × 1.00 – 0.10 based on your allegiance's online headcount |

**Diminishing returns:**
Killing the same player more than **3 times within a 1-hour window** suppresses all rewards from that target for **3 hours**. No XP, no quest credit, no season credit. You'll receive a message when a kill is suppressed.

### Allegiance-Size PK XP Penalty

To keep PvP rewarding for small, tight-knit allegiances and to discourage stacking one giant "zerg" allegiance, **all PK XP is scaled down by how many of your allegiance's members are currently online** at the moment you earn it. The more allies you have logged in, the smaller each PK reward.

| Allegiance members online | PK XP earned |
|---|---|
| 10 or fewer | 100% |
| 11 | 95% |
| 12 | 90% |
| 13 | 80% |
| 14 | 70% |
| 15 | 50% |
| 16 | 30% |
| 17 or more | 10% |

**This applies to every source of PK XP** — open-world kills, arena rewards, PK quests, hometown-capture rewards, and the XP you drain from a victim's **Ancient Bottle** on a kill (that drain is a kill reward, so it is reduced like any other PK XP). Solo players and small allegiances are unaffected. The one exemption is **drinking your own Ancient Bottle** to release stored XP — that experience was already earned, so it is never reduced by this penalty.

The count includes you, and it is your own allegiance's online headcount that matters — not the victim's or anyone else's.

---

## 🔄 Respeccing Skills & Attributes

Skills and attributes are changed with **gems** picked up from the **Temple of Enlightenment** (raise / specialize) and the **Temple of Forgetfulness** (lower / unspecialize). The gems themselves are free — what limits you is a **pickup timer** stamped on your character when you take one.

There are now **three independent timers**. They do not share a cooldown with each other, so using one does not delay the others.

| Gem group | Allowance | Timer |
|-----------|-----------|-------|
| **Combat skill gems** — Enlightenment & Forgetfulness | **3 gems per window** | **7 days** |
| **Non-combat skill gems** — currently Cooking | 1 gem | 14 days |
| **Attribute gems** — Gem of Raising / Gem of Lowering | 1 of each | 14 days |

### Combat Skill Gems — 3 Every 7 Days

The combat skill gems cover **Axe, Bow, Crossbow, Dagger, Mace, Spear, Staff, Sword, Thrown Weapon, Unarmed Combat, War Magic and Life Magic**, in both Enlightenment and Forgetfulness flavors.

All 12 skills and both flavors draw from the **same pool of 3**. It doesn't matter how you spend them — three Forgetfulness gems, three Enlightenment gems, or any mix across any combat skills. A full unspec-and-respec of one skill costs you two of the three.

**The 7-day clock starts on your first pickup, not your last.** Take one gem on Monday and you have until the following Monday to take the other two, whichever days you like. When that window closes the allowance resets to 3 and the next gem you take opens a fresh window.

If you try to take a fourth gem inside the window, the game tells you how long remains before the window rolls over.

### Non-Combat Skill and Attribute Gems

These are unchanged: **one pickup every 14 days**, each on its own timer. The **Cooking Gem of Enlightenment** is the only non-combat skill gem currently in use.

Attribute gems work in pairs — a **Gem of Raising** and a **Gem of Lowering** are combined to transfer up to **10 points** from one attribute to another. Raising and Lowering are tracked separately, so a full transfer needs one pickup from each.

### Skipping the Wait

The **Skill and Attribute Reset Gem**, sold by **Darkbeat** for **20 Phials of Bloody Tears**, clears **all** of your respec pickup timers at once — combat, non-combat and attribute alike — letting you go straight back to the temples.

On top of the phial cost it consumes **PK Trophies**, and that price **escalates every time you use one**: 100 for your first, then 35% more each time, capped at 10,000.

| Use # | PK Trophies |
|-------|-------------|
| 1st | 100 |
| 2nd | 135 |
| 3rd | 182 |
| 4th | 246 |
| 5th | 332 |

The escalation is permanent and per character — it never decays back down.

---

## 🛒 Vendors

### Darkbeat

**Darkbeat** is a special vendor located in the Afterlife area. He accepts **Phials of Bloody Tears** as currency (not pyreals) and sells rare crafting and upgrade items. Phials are earned through PK quests, arena rewards, and hometown captures.

| Item | Cost (Phials) | Description |
|------|--------------|-------------|
| Imbue Altering Morph Gem | 20 | Randomizes a weapon's imbue between Crippling Blow, Armor Rending, and Critical Strike. |
| Empyrean Tuning Fork | 25 | Randomizes the legendary cantrips on armor, jewelry, or shields that already have legendaries. One use per item. |
| Fetish of the Dark Idols | 25 | Combine with a loot-generated atlatl, bow, or crossbow to add a Magic Absorbing property at the cost of a Melee Defense penalty. The weapon can be imbued before the Fetish is applied, but not after; non-imbue tinkers work either way. |
| Slayer Upgrade Gem | 25 | Upgrades an existing slayer damage bonus to 1.8 on weapons that rolled a slayer via the tinkering lottery. |
| Racial Requirement Morph Gem | 30 | Strips the racial requirement off armor, a weapon, or a caster — both the racial activation requirement on its spells and any racial wield requirement. The item is left with no racial restriction at all. |
| Allegiance Rank Requirement Morph Gem | 30 | Removes the allegiance rank needed to activate an item's spells ("Activation requires allegiance rank 6"), leaving it with no rank requirement. Unlike Silk tinkering, it does not consume a tinker and does not raise the item's Arcane Lore requirement. |
| Lesser Impenetrability Morph Gem | 30 | Adds Impenetrability to loot-gen or rare armor: 3% Major, 97% Minor. Repeatable on the same piece — on armor that already has Minor, each use is a 3% roll to upgrade it to Major. Will not apply to armor that already has Major or better. |
| Ancient Bottle | 50 | Absorbs 25% of PvP XP overflow up to 100M. Bonded & Attuned. |
| Ancient Empyrean Tool | 75 | Guarantees the next tinker will not fail. |
| Empyrean Jeweler's Sawblade | 50 | Randomizes the slot of a ring, bracelet, or necklace between finger, wrist, and neck. |
| Oil of Creature Slaying | 75 | Adds a random slayer (1.8 damage bonus) to a weapon or magic caster that does not already have one. |
| Skill and Attribute Reset Gem | 20 | Clears quest stamps for the Temple of Enlightenment and Temple of Forgetfulness. Each use costs an escalating number of PK Trophies (see below). Bonded & Attuned. |

---

### Anti Parazi

**Anti Parazi** is a vendor located in the Abandoned Mine alongside Darkbeat. He accepts **PK Trophies** as currency (not pyreals) and sells bounty consumables and item requirement morph gems. PK Trophies are earned at a higher rate than Phials, reflected in Anti Parazi's pricing.

| Item | Cost (PK Trophies) | Description |
|------|-------------------|-------------|
| Bounty Purchase Token | 100 | Used to purchase a Bounty Contract from the Bounty Hunter NPC. |
| Writ of Pursuit | 200 | Inscribe with `PlayerName:Amount` and turn in to flag a player as a High Priority Target. |
| Workmanship Morph Gem | 500 | Randomizes the Workmanship of a loot item (1–10). |
| Arcane Lore Morph Gem | 350 | 75% chance to reduce Arcane Lore requirement by 5–25; 15% chance of no effect; 10% chance to increase it by 5–15. |
| Missile Defense Requirement Morph Gem | 400 | Removes an item's Missile Defense requirement — both the activation requirement and any Missile Defense wield requirement. |
| Melee Defense Requirement Morph Gem | 400 | Removes an item's Melee Defense requirement — both the activation requirement and any Melee Defense wield requirement. |
| Player Wield Requirement Morph Gem | 500 | Removes the wield restriction binding an item to a specific player. |
| Slayer Morph Gem | 35 | Randomizes the creature-slayer type on a loot-gen weapon or caster that already has a slayer, or on loot-gen armor with a Creature Slayer Rating. |
| Creature Resistance Morph Gem | 25 | Randomizes the creature-resistance type on loot-gen armor/jewelry that has a Creature Resist Rating. |
| A Dick (Vitae Removal) | 1 | Eat it to remove your Vitae penalty (no XP granted). Does nothing if you have no penalty. |

> **Level Requirement Removal Morph Gem** has been discontinued — no item in the Infiltration era has a level requirement, so it never had a use.

> **Impenetrability Morph Gem** — not sold by either vendor. Obtainable only from **Mythic Mystery Boxes**. It adds Impenetrability to loot-gen or rare armor at **33% Major / 67% Minor**, and will not apply to armor that already has any Impenetrability cantrip. The **Lesser Impenetrability Morph Gem** (Darkbeat, Rare Mystery Box) is the repeatable, lower-odds alternative.

**Vitae Removal.** Anti Parazi also stocks **A Dick**, a consumable that costs **1 PK Trophy**. Eat it to clear your **Vitae penalty** — no XP is granted, it just removes the penalty. If you have no Vitae penalty, eating it does nothing and the item is not consumed.

---

### Custom Character Titles — `/buytitle`

Spend PK Trophies to give your character a **custom title**. Use `/BuyTitle <New Title>` in game — the title is applied to your character immediately and costs **200 PK Trophies** per purchase. New titles are screened against the server's taboo word filter, so disallowed words are rejected.

---

### Darkbeat's Storage Locker

The Storage Locker is a locked chest that always contains one tier 6 loot item, plus three additional rolls per opening. Each opening also has an independent **~20% chance to contain a Sturdy Iron Key** on top of everything else.

Each of the three rolls lands on either a **Massive Mana Stone (50% chance)** or the bonus table below (50% chance, split among its entries):

| Item | Chance per roll |
|---|---|
| Massive Mana Stone | 50% |
| PK Trophies ×20 | 15% |
| Phials of Bloody Tears ×2 | 8% |
| Trade Note ×25 (250,000 pyreals) | 6% |
| Salvage bag (11 types, ~0.5% each) | 5.5% |
| Foolproof tinkering gem (14 types, ~0.25% each) | 3.5% |
| Treated Healing Kit | 2.5% |
| Tumerok Salted Meat ×20 | 2.5% |
| Mana Philtre ×20 | 2.5% |
| Stamina Philtre ×20 | 2.5% |
| A Box | 2% |

The salvage bags are distributed evenly across 11 types (all full WS10, 100-unit bags):

| Salvage | Use |
|---------|-----|
| Sunstone | Armor Rend |
| Red Garnet | Fire Rend |
| Black Garnet | Pierce Rend |
| Imperial Topaz | Slash Rend |
| Jet | Lightning Rend |
| Aquamarine | Cold Rend |
| White Sapphire | Bludgeon Rend |
| Emerald | Acid Rend |
| Fire Opal | Crippling Blow |
| Black Opal | Critical Strike |
| Bloodstone | Minor Endurance (jewelry only) |

### Skill and Attribute Reset Gem — PK Trophy Cost

Using the gem requires both the Phial purchase price **and** an additional PK Trophy cost paid at the time of use. The trophy cost scales exponentially with each use:

| Use # | PK Trophies |
|-------|-------------|
| 1st | 100 |
| 2nd | ~135 |
| 3rd | ~182 |
| 4th | ~246 |
| 5th+ | Continues growing (~1.35× per use, capped at 10,000) |

The gem is consumed on use. If you do not have enough PK Trophies in your inventory, the gem is not consumed and you are told the current cost.

---

## 📦 Mystery Boxes

The Common, Rare, and Mythic Mystery Boxes each contain a weighted loot table of currencies, salvage, and morph gems.

**A Box tiers.** Opening **A Box** rolls: 10% A Dick, 64% Common Mystery Box, 25% Rare Mystery Box, 1% Mythic Mystery Box.

### Common Mystery Box

| Item | Chance |
|------|--------|
| Workmanship Morph Gem | ~2.9% |
| Missile Defense Requirement Morph Gem | ~2.9% |
| Melee Requirement Morph Gem | ~2.9% |
| Player Wield Requirement Morph Gem | ~2.9% |
| Sturdy Iron Key | ~8.8% |
| Arcane Lore Morph Gem | ~8.8% |
| Steel Salvage (WS10, 100 units) | ~8.8% |
| Granite Salvage (WS10, 100 units) | ~8.8% |
| Iron Salvage (WS10, 100 units) | ~8.8% |
| Opal Salvage (WS10, 100 units) | ~8.8% |
| Rare Mystery Box | ~8.8% |
| MMD ×1 | ~8.8% |
| PK Trophies ×10 | ~8.8% |
| Bounty Purchase Token | ~8.8% |

Darkbeat's Lost Storage Key, Green Garnet Salvage, and the Level Requirement Removal Morph Gem no longer drop from Common Mystery Boxes.

### Rare Mystery Box

| Item | Chance |
|------|--------|
| Workmanship Morph Gem | ~5.3% |
| Missile Defense Requirement Morph Gem | ~5.3% |
| Melee Requirement Morph Gem | ~5.3% |
| Player Wield Requirement Morph Gem | ~5.3% |
| Slayer Upgrade Morph Gem | ~5.3% |
| Lesser Impenetrability Morph Gem | ~5.3% |
| Slayer Morph Gem | ~1.8% |
| Creature Resistance Morph Gem | ~1.8% |
| Sunstone Salvage WS10 — Armor Rend | ~3.5% |
| Red Garnet Salvage WS10 — Fire Rend | ~3.5% |
| Black Garnet Salvage WS10 — Pierce Rend | ~3.5% |
| Imperial Topaz Salvage WS10 — Slash Rend | ~3.5% |
| Jet Salvage WS10 — Lightning Rend | ~3.5% |
| Aquamarine Salvage WS10 — Cold Rend | ~3.5% |
| White Sapphire Salvage WS10 — Bludgeon Rend | ~3.5% |
| Emerald Salvage WS10 — Acid Rend | ~3.5% |
| Fire Opal Salvage WS10 — Crippling Blow | ~3.5% |
| Black Opal Salvage WS10 — Critical Strike | ~3.5% |
| Bloodstone Salvage WS10 — Minor Endurance (jewelry only) | ~3.5% |
| Sturdy Iron Keys ×3 | ~5.3% |
| Darkbeat's Lost Storage Key | ~5.3% |
| Mythic Mystery Box | ~5.3% |
| MMDs ×5 | ~5.3% |
| PK Trophies ×30 | ~5.3% |

All salvage bags are full WS10 bags (100 units). Ancient Bottle no longer drops from Rare Mystery Boxes — it's Mythic-only now. The Slayer Upgrade Morph Gem moved in here from the Mythic Mystery Box, and the Level Requirement Removal Morph Gem no longer drops here at all. The Slayer Morph Gem and Creature Resistance Morph Gem appear here at their rarest (~1.8% each) — they're more common in the Mythic box. The Lesser Impenetrability Morph Gem was added at the standard morph-gem weight, which shifted every other slot down slightly.

### Mythic Mystery Box

| Item | Chance |
|------|--------|
| Ancient Bottle (XP Bottle) | ~3.1% |
| Impenetrability Morph Gem | ~9.4% |
| Oil of Creature Slaying | ~9.4% |
| Skill and Attribute Reset Gem | ~9.4% |
| Imbue Altering Morph Gem | ~9.4% |
| Slayer Morph Gem | ~9.4% |
| Creature Resistance Morph Gem | ~9.4% |
| Racial Requirement Morph Gem | ~9.4% |
| Allegiance Rank Requirement Morph Gem | ~9.4% |
| MMDs ×20 | ~9.4% |
| PK Trophies ×250 | ~9.4% |
| Shimmering Skeleton Key | ~3.1% |

The Slayer Upgrade Morph Gem moved out to the Rare Mystery Box; Oil of Creature Slaying takes its slot here. The Slayer Morph Gem and Creature Resistance Morph Gem also drop here at ~9.4% each — far more likely than in the Rare box. The Racial Requirement and Allegiance Rank Requirement Morph Gems drop here as well, and are otherwise only available from Darkbeat.

> **Shimmering Skeleton Key** — a single-use key that unlocks **any** locked door or chest, no matter the lock. It crumbles to dust after one use and is **slippery**, so it drops on death (into your corpse for a killer to loot). Obtainable only from the Mythic Mystery Box.

---

## 🐗 Tusker Tusk & Olthoi Pincer Turn-In Timers

The repeat timer on the Tusker Tusk and Olthoi Pincer turn-in quests is **20 hours**, so you can farm and turn in tusks and pincers frequently rather than waiting weeks between rewards.

This covers all 14 Tusker Tusk turn-ins and all 8 Olthoi Pincer turn-ins (Harvester, Gardener, Soldier, Legionary, Eviscerator, Worker, Warrior, and Mutilator pincers turned in to Behdo Yii).

---

## 🔧 Tinker Characters — `/FlagTinker`

You can dedicate a character to be a **pure crafting specialist** using the `/FlagTinker` command. A Tinker is a support/crafting alt with every tinkering and crafting skill maxed out — perfect for salvaging, imbuing, and tinkering gear for yourself and your allegiance without having to level a combat character first.

### How to Flag a Tinker

Log in a **brand-new level 1 character** and type `/FlagTinker`. That's it. The conversion is applied instantly.

**Requirements:**
- The character must be **level 1** (a character that has already earned levels cannot be converted).
- Your account must **not already have a Tinker** — you get **one Tinker per account**.

> ⚠️ **This is permanent and irreversible.** There is no un-flag command. Only run `/FlagTinker` on a character you intend to keep as a dedicated crafter.

### What You Get

When you flag a Tinker, the character is instantly transformed:

- ✅ **All eight crafting skills plus Arcane Lore are specialized and maxed** — Item Tinkering, Weapon Tinkering, Armor Tinkering, Magic Item Tinkering, Alchemy, Lockpick, Fletching, Cooking, and Arcane Lore.
- ✅ **All attributes are maxed** (Strength, Endurance, Coordination, Quickness, Focus, Self) and your health, stamina, and mana are refreshed to full.
- ✅ **A Tinkering Trinket** is placed in your inventory. It buffs all six attributes and every crafting skill (level-7 aptitudes), additionally carries **Major cantrips** for all six attributes and the four tinkering skills (Item, Weapon, Armor, and Magic Item Tinkering), and also grants **Brilliance**. Re-running `/FlagTinker` patches any new trinket buffs onto a trinket you already have, so existing Tinkers don't need a fresh one to pick up additions like this.
- ❌ **All combat skills are removed** — every weapon skill, shield, and all offensive magic (War, Void, Life, Creature Enchantment, Item Enchantment) is untrained. A Tinker is not built to fight.

> 🔁 **Already a Tinker?** Re-run `/FlagTinker` on an existing Tinker to pick up the latest upgrades (Arcane Lore specialization and the trinket's Major cantrips). It's safe to run again — nothing is reset. If your trinket is equipped, re-equip it or relog to apply the new cantrips.

### Living as a Tinker

- 🛡️ **No vitae on death.** Tinker characters never suffer the vitae experience penalty when they die — a mistake at the crafting bench or a stray death costs you nothing.
- 🔒 **Skills are locked.** A Tinker cannot train or specialize any new skills. Your crafting kit is set the moment you flag, and that's your loadout for good.
- 👑 **No allegiance passup.** A Tinker does not pass XP up the allegiance chain to its patron.
- ⚔️ **No PK XP.** A Tinker never earns PvP/PK experience from any source — player kills, Ancient Bottle drains, PK quests, and PK gems all yield nothing. Tinkers are crafters, not combatants.
- 🏟️ **No arenas.** A Tinker cannot join arena events. Attempting to queue returns "Tinker characters cannot join arena events."

The intent is simple: a Tinker is a maxed-out crafting workstation in character form. Flag one, park it in your allegiance, and let it handle all your tinkering, salvaging, and item work.

---

## 🎰 Tinkering Lottery

Every **successful** tinker rolls a bonus lottery based on the **salvage type** you used. Winnings are applied on top of the tinker's normal effect and are broadcast to everyone nearby: *"<name> won the tinkering lottery!"* Losing rolls are silent, and a failed tinker never rolls at all.

Most salvage types have their own reward table. The two families below are documented here in full; the rest (Steel, Iron, Granite, Green Garnet, Opal, Mahogany, Velvet, Brass, the rending gems, the imbue gems, and the defense imbue gems) each roll their own bonuses.

**Minor attribute cantrip salvage** — Agate (Focus), Bloodstone (Endurance), Carnelian (Strength), Lapis Lazuli (Willpower), Smokey Quartz (Coordination), Rose Quartz (Quickness):

| Prize | Chance |
|---|---|
| Extra maximum mana on the item | ~40% |
| Slower mana burn rate (up to ~40 extra seconds) | ~40% |
| Upgrade that salvage's Minor cantrip to Moderate | 5% |
| Creature Resistance Rating (if the item has none) | 10% |
| Creature Slayer Rating (if the item has none) | 10% |

The mana pool and mana burn prizes come off the same roll, so an item can win one or the other but never both.

**Heritage and rank salvage** — Ebony, Porcelain, Teak (change the racial requirement) and Silk (removes the allegiance rank requirement):

| Prize | Chance |
|---|---|
| +10–20 Armor Level (armor only) | 50% |
| Jackpot: a further +10–20 Armor Level | 15%, WS10 salvage on a WS≤6 item |
| Creature Resistance Rating (if the item has none) | 10% |
| Creature Slayer Rating (if the item has none) | 10% |

Winning a Jackpot on the heritage and rank salvage requires both conditions — workmanship 10 salvage *and* a target item of workmanship 6 or lower.
