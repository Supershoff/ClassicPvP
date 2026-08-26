# 📜 ClassicPvP — Release Notes

> **This is a running changelog**, not a reference doc. Changes are grouped into versioned sections (v1.01, v1.02, ...), newest first — the original launch content lives at the very bottom as **v1.00 (Launch Edition)**. The top-most version is marked **(in development)** while changes are still being added to it; once it's deployed to production, the marker is removed, its date is filled in, and a new version is started at the top. Versions only increment on explicit instruction, not automatically per day.
>
> New here? See **[GettingStarted.md](GettingStarted.md)** for how to connect to the server.
> For the current state of every mechanic (kept continuously up to date), see **[ServerInfo.md](ServerInfo.md)**.

---

## 🩹 v1.21 (in development)

---

## 🩹 v1.20 — August 23, 2026

### 🏹 Missile Tracking — Experimental Changes, Off By Default

Missile tracking for bows, crossbows and thrown weapons has been a long-standing complaint: arrows that visibly fly nowhere near the target, and twitching side to side throwing off tracking far more than it should.

Digging into it turned up several genuine issues in how the server aims a projectile — the firing solution being calculated *before* the aim animation plays rather than at the moment the arrow leaves the bow, target leading silently switching itself off at longer ranges, and the aim point for high attacks sitting right on the edge of the target's hit volume instead of in the middle of it. A separate issue affects how often your client is told where other players actually are, which is why an arrow can look like it missed when the server thought it connected.

**None of it is live yet.** Every change is behind its own switch and **the original behavior remains the default**. They will be enabled one at a time on the test server so each can be measured in isolation — the whole point is to be able to tell which change did what, and to catch any side effect before it reaches live PvP.

Nothing about missile combat changes for you today. Expect these to start appearing in later patch notes as they are individually turned on, with details of what each one actually did.

### 🐛 Fixed: Pack Dolls Never Rotting Off The Ground

Anything you drop on the ground is supposed to clean itself up after about five minutes. A handful of items never did — pack dolls, plush toys, the Carved Tusker Statue, the Small Olthoi Grub, the Wedding Cake. Their item data carried a **never rot** flag inherited from retail, so the server skipped right past them when sweeping the landblock.

On its own that's a curiosity. Dropped by the thousand in one spot it's a lag bomb — every pack doll runs full creature animations, and a dense pile of them will drag the framerate down for everyone in the area, which is exactly what was happening.

**All 23 affected items now rot on the normal timer**, and the piles already sitting on the ground have been cleared out.

---

## 🩹 v1.19 — August 22, 2026

### 🏰 Hometown Phase 2 Moves Indoors — Into the Meeting Halls

Phase 2 used to be fought in the open, on top of the town's Bind Stone. Whoever brought the bigger pile of bodies to an open field usually won, and the fight sprawled across a hundred meters of terrain.

**Phase 2 now happens inside the town's Meeting Hall.**

When Phase 1 completes, the outdoor Bind Stone goes dark and the fight moves indoors. Take the Meeting Hall portal — that's where the attackable Bind Stone appears, and it's the only way in.

- **The Meeting Hall portal ignores the PK timer while Phase 2 is running.** Neither side can be locked out of a siege by being tagged on repeat. Outside of Phase 2 the normal timer applies, so the halls are not a general escape hatch from PvP.
- **Every distance rule became a presence rule.** Holding the hall, blocking a repel, earning participation trophies, qualifying for rewards, getting smited on a loss, and the bonus damage from kills — all of it is now simply "are you in the hall". No more counting meters.
- **Damage falloff still works on distance.** Full damage within **15 meters** of the stone, nothing past **20**. The hall entrance sits about **36 meters** from the Bind Stone, so attackers have to push most of the way in before they can land anything. Walking through the portal is not the same as being in the fight.
- **Kills only count inside the hall.** A kill on a defender still knocks **5% max HP** off the Bind Stone, and a kill on an attacker still heals it by the same — but only if it happens in the hall. Fighting out in the town proper is ordinary PvP now.

### 👥 Meeting Halls Are Zerg Controlled — 7 Per Allegiance

A Meeting Hall is a small dungeon with a single entrance, which makes it very easy to simply wall off with numbers.

Every Meeting Hall is now a **permanently zerg-controlled area, capped at 7 players per allegiance** — whether or not a siege is running. An 8th member who portals in is bounced straight back to their lifestone, and if an allegiance somehow ends up over the cap anyway, the most recently teleported players are sent home until it's back to 7.

Each hall has its **own independent cap**. Holding Holtburg's hall doesn't cost you any of your allowance in Arwic.

### 🎯 Hometown Phase 1 — Much Tighter Contest Range

Phase 1 used to sweep **50 meters** for enemies. In practice that covered most of a town square: anyone drifting through the area could stall an assault without ever committing to the fight, and a rival allegiance idling well away from the stone could block one from starting at all.

**That detection range is now 10 meters.** To contest a Phase 1 you have to actually stand on the Bind Stone with the attackers, not hover at the edge of radar.

The rest is unchanged — you still need **2+ members within 5 meters** to make progress, an enemy still has to hold the area for **30 continuous seconds** to reset it, and you still need **4 uninterrupted minutes** to reach Phase 2.

### 🐛 Fixed: Captures Ending "Inconclusively"

Destroying a Bind Stone could end the siege with **"The assault has ended inconclusively"** instead of awarding the capture — no town, no rewards, nothing for either side.

The resolution smites the losing allegiance, and that smite crashed on any defender who had actually been fought during the siege. Since the crash happened partway through the payout, it took the whole result down with it. The same bug also broke the admin `@smite` command.

Fixed, and the payout is now hardened: one player failing to be rewarded or smited no longer costs everyone else their rewards or voids a decided outcome.

---

## 🩹 v1.18 — August 22, 2026

### 🔄 Combat Skill Respecs — 3 Gems Every 7 Days

Respeccing a combat skill used to mean **one gem every 14 days**, shared with every other skill and sitting on the same timer. Want to unspec Sword and spec Axe? That's two gems to unspec and untrain Sword and another to spec Axe, quite a long wait.

Combat skills now have **their own timer: 3 gems every 7 days.**

The pool of 3 is shared across **Enlightenment and Forgetfulness** and across **every combat skill** — Axe, Bow, Crossbow, Dagger, Mace, Spear, Staff, Sword, Thrown Weapon, Unarmed Combat, War Magic and **Life Magic**. Spend them however you want: three unspecs, three specs, or any mix. That Sword-to-Axe swap is now possible within a single 7 day quest timer.

**The clock starts on your first gem, not your last.** Grab one on Monday and you have until the following Monday for the other two, whenever suits you. When the week is up the allowance goes back to 3. If you reach for a fourth early, the game tells you exactly how long is left.

**Everything else is unchanged and now on a separate timer.** Non-combat skills and attribute gems stay at one pickup every 14 days — and because they're separate, spending your combat gems no longer blocks an attribute transfer, or the other way around.

The **Skill and Attribute Reset Gem** from Darkbeat still wipes the lot in one go, combat timer included.

### 🔥 Hot Dungeons — Better Loot, Less Junk

Loot rolled inside an active **Hot Dungeon** is now weighted toward the gear people actually keep.

**More high-end weapons.** Weapons that drop in a Hot Dungeon have a small chance to come out at the **best damage and damage variance available for their wield requirement**. This does not change what wield requirement a weapon rolls — a 250 wield weapon is still a 250 wield weapon, it just has a shot at being the best possible version of one. It's a rare roll by design, not something you'll see every trip.

This applies to **every weapon type** — melee weapons, thrown, **bows, crossbows and atlatls**, and **casters including wands, orbs and sceptres**.

**More single-slot armor.** Armor drops now lean toward **single-slot pieces** — helms, breastplates, girths, gauntlets, pauldrons, tassets, bracers and sollerets — instead of multi-slot pieces like coats, cuirasses, shirts, sleeves, leggings and boots. Shields are in the favored group too.

**Less junk.** Weapons also turn up more often in place of low-value filler, and most of the mundane clutter — spell components, lockpicks, healing kits — is replaced with real equipment instead. **Corpses don't carry fewer items**, they just carry a better mix.

All of it is tunable, so expect the exact rates to get adjusted as we watch it land.

### 🛡️ Impenetrability Morph Gems — Reworked, Plus a New Lesser Version

The **Impenetrability Morph Gem** was close to a lottery ticket: a **3%** shot at Major Impenetrability and a **97%** consolation Minor. Most people burned one and walked away with the Minor.

That gem now rolls **33% Major / 67% Minor**. Everything else about it is unchanged — it's still one shot per piece, and it still refuses to apply to armor that already has *any* Impenetrability cantrip on it.

The old odds didn't disappear. They moved to a new item.

The **Lesser Impenetrability Morph Gem** keeps the original **3% Major / 97% Minor** roll, but with a twist that makes it worth grinding: **you can use it on the same piece over and over.**

- On armor with **no Impenetrability**, it adds Minor (97%) or Major (3%).
- On armor that already has **Minor Impenetrability**, you're rolling that same **3%** to *upgrade* it to **Major**. Miss the roll and the gem is spent — nothing comes off the armor, but you're out a gem.
- On armor that already has **Major** (or Epic, Legendary, or Prodigal), the gem won't apply and says so.

So the Mythic-box gem is now a clean one-in-three shot, and the Lesser gem is a slow grind to the same place.

Both gems still only apply to loot-generated or rare armor and underclothes that already carry magic.

While we were in there, the misspelled **"Impenatrability"** on the original gem was fixed. Same item — it just reads correctly now.

### 🎁 Where to Get the Lesser Gem

The **Lesser Impenetrability Morph Gem** turns up in two places:

- **Rare Mystery Box** — a new prize slot at **5.3%**. Adding it nudged every other slot down a hair: the 5.6% prizes are now **5.3%**, salvage bags went **3.7% → 3.5%**, and the Slayer / Creature Resistance gems **1.9% → 1.8%**. Nothing was removed from the box.
- **Darkbeat** — **30 Phials of Bloody Tears**.

The full-strength Impenetrability Morph Gem still comes from the **Mythic Mystery Box** and nowhere else.

### 💰 Slayer and Creature Resistance Morph Gems — Much Cheaper

Both sat at **100 PK Trophies** at **Anti Parazi**, which almost nobody was paying.

- **Slayer Morph Gem** → **35 PK Trophies**
- **Creature Resistance Morph Gem** → **25 PK Trophies**

### 🏹 Fetish of the Dark Idols — Now Sold by Darkbeat

**Darkbeat** now carries the **Fetish of the Dark Idols** for **25 Phials of Bloody Tears**.

Combine it with any loot-generated atlatl, bow, or crossbow to add a **Magic Absorbing** property at the cost of a **Melee Defense** penalty. The weapon can be imbued *before* the Fetish goes on but not after; non-imbue tinkers work either way.

### 🕵️ No More Peeking at Your Matchup — or Ducking It

You no longer see who you've been matched against until the match actually starts.

Players were queueing, getting matched, immediately running `/arena info` to see the draw, and — if it looked rough — logging off or PK-tagging themselves by attacking someone. That cancels the match before it starts, which meant **no disqualification and no penalty**. Free re-roll on your opponent.

From matchmaking through the teleport-in countdown, `/arena info` now shows only a player count for a pending match, and pending matches can't be watched with `/arena watch` either. Names appear the moment the match begins, and everything after that — the match-started global, the results, the leaderboards — is unchanged.

### 🏆 Arena Ranking — Your Score Is Now Just Your ELO

The 1v1 and 2v2 leaderboards no longer add **wins** and **matches played** on top of your rating. **Your score is your ELO, full stop.** The 2v2 survival bonus is gone from scoring too. All three are still tracked and still shown in `/arena stats` — they just don't inflate your rank anymore.

**Nobody's ELO changed.** Your rating is exactly what it was; only the way the leaderboard reads it has changed. In practice that means players who had climbed on volume rather than rating will drop, and a high-rated player who queues less will no longer be buried under grinders.

Staying active is now enforced by decay instead, which is the part that actually got teeth.

### 📉 ELO Decay Now Scales With How Much You've Played

Decay used to be a flat **3% per day of your whole rating** after three quiet days. It's now tied to **how many matches you've played in the last 7 days**, checked once a day, and it only eats the part of your rating **above 1500**.

**1v1:**

| Matches in the last 7 days | Daily decay |
|---|---|
| None at all | **5%** |
| 1 – 2 | **3%** |
| 3 – 9 | **1%** |
| 10 or more | **none** |

**2v2** is gentler, since it needs a partner online:

| Matches in the last 7 days | Daily decay |
|---|---|
| None at all | **3%** |
| 1 – 2 | **1%** |
| 3 or more | **none** |

The "above 1500" part matters: at **1800 ELO** with no matches all week, the 5% comes off the **300 points above baseline** — you lose **15**, not 90. And no amount of decay can drop you below 1500.

Only matches in the **same format** count toward that format's tier — a week of 2v2 does nothing for your 1v1 decay, and vice versa.

**2v2 team pairs no longer decay at all.** Your rating as a specific duo now stands until you play as that duo again.

### 👹 Rendmaw Hits a Little Softer

**Rendmaw**'s melee damage has been dialed back by **10%**. He was landing harder than any other Dungeon Boss by a wide margin, to the point where a bad swing could end a fight outright. He's still the heaviest hitter of the five — just not by as much.

---

## 🩹 v1.17 — August 13, 2026

### 🩸 Heads Up: Drain Health Coming to 1v1 Arenas

The infrastructure is in place to tune how effective **drain health spells** are inside **1v1 arena** matches. It's a no-op for now — drains work exactly as they always have — but expect a **nerf to drain health effectiveness in arenas** once testing settles on a value.

### 👹 Fixed Dungeon Bosses Getting Tankier the Longer the Server Ran

Dungeon Boss armor and melee damage were meant to be scaled once per spawn, but a bug caused each new spawn to compound on top of the last spawn's already-scaled values instead of the original numbers — so a boss's effective armor (and the damage it hit for) quietly crept upward the longer the server stayed up, with melee and missile hits landing for less and less over time. Magic damage was unaffected, which is why bosses could feel fine to casters but increasingly spongy to weapon users. This has been fixed; bosses now scale correctly from their authored values on every spawn.

### 🏟️ New Arena — Xarabydun Lifestone

A new arena location, **Xarabydun Lifestone**, has been added to the queue. It hosts **2v2**, **FFA**, **Tugak**, and **Group** matches, with ten starting positions spread around the room. When you queue for those formats you may now be matched into Xarabydun Lifestone or one of the existing arenas — the system picks an open location automatically.

The dungeon's lifestone and portals have been removed, and any lifestone or portal ties players had inside it are cleared. If that lifestone was your tie, you'll need to re-tie somewhere else.

### 💰 Skill and Attribute Reset Gem — Cheaper

The **Skill and Attribute Reset Gem** now costs **20 Phials of Bloody Tears** from Darkbeat, down from 50. Its escalating PK Trophy cost per use is unchanged.

### 🔥 Hot Dungeons — Logout Delay

The extra rewards inside a Hot Dungeon now come with extra risk. While you're standing in an active Hot Dungeon, **logging out is delayed** just like it is for Player Killers — your character stays frozen in the world for a short time before actually leaving. No more instantly quitting to escape a bad spot.

**Recalls still work normally** — portal recall, lifestone recall spells, and commands like `/lifestone` are unaffected. This only delays a straight logout.

### 🎰 Tinkering Lottery — Two New Salvage Families Can Win

The tinkering lottery now fires on ten more salvage types. As always, it only rolls on a **successful** tinker, and any winnings are announced to everyone nearby.

**Minor attribute cantrip salvage** — Agate (Focus), Bloodstone (Endurance), Carnelian (Strength), Lapis Lazuli (Willpower), Smokey Quartz (Coordination), and Rose Quartz (Quickness) can now win:

- A chunk of extra **maximum mana** on the item, or a **slower mana burn rate** — up to about 40 extra seconds per tick. One or the other, not both.
- A **5% chance to upgrade that salvage's Minor cantrip to its Moderate version** — Minor Focus becomes Moderate Focus, and so on. The item has to already have the matching Minor.
- A **10% chance each** at a **Creature Resistance Rating** or a **Creature Slayer Rating**, if the item doesn't already have one.

**Heritage and rank salvage** — Ebony, Porcelain, and Teak (which change an item's racial requirement) and Silk (which removes its allegiance rank requirement) can now win:

- A **50% chance at +10–20 Armor Level** on armor.
- **Jackpot:** using **workmanship 10 salvage** on an item of **workmanship 6 or lower** adds a further 15% chance at another +10–20 AL on top.
- The same **10% each** at a Creature Resistance or Creature Slayer Rating.

### 🧬 New Morph Gem — Racial Requirement Removal

A new **Racial Requirement Morph Gem** strips the racial restriction off armor, weapons, and casters. That breastplate that only activates its spells for a Sho? Apply the gem and it activates for anybody — the item is left with no racial requirement at all, covering both the racial activation requirement on its spells and any racial wield requirement.

Sold by **Darkbeat for 30 Phials of Bloody Tears**, and it also drops from the **Mythic Mystery Box** (~9.4%). Like the other requirement-removal gems, it works on quest and rare gear as well as loot-gen items.

### 🎖️ New Morph Gem — Allegiance Rank Requirement Removal

Its companion, the **Allegiance Rank Requirement Morph Gem**, clears the rank gate off items that spawn with *"Activation requires allegiance rank 6"* and similar. The item keeps its spells and is left with no rank requirement at all, so it works no matter where you sit in your monarchy — or whether you're in one.

Silk tinkering already removed this requirement, but it burns a tinker and resets the item's Arcane Lore to its Spellcraft. The gem does neither.

Sold by **Darkbeat for 30 Phials of Bloody Tears**, and it also drops from the **Mythic Mystery Box** (~9.4%). Like the other requirement-removal gems, it works on quest and rare gear as well as loot-gen items.

> Adding both new gems to the Mythic Mystery Box nudged every other prize in it down slightly — the weight-3 prizes go from ~10.3% to ~9.4%, and the Ancient Bottle and Shimmering Skeleton Key from ~3.4% to ~3.1%.

### 👑 Allegiance Swearing — Now Costs PK Trophies

The allegiance swear **cooldown is gone**, replaced by a **PK-trophy cost** that rises the more you hop allegiances. Every character gets its **first 3 swears free**; after that each swear costs trophies from your inventory on a steep curve — **100** for the 4th, then climbing to a cap of **10,000** by the 15th swear (roughly: 4th = 100, 7th = 351, 10th = 1,232, 13th = 4,328, 15th+ = 10,000). The count is per character and never resets. Swearing to your own alt with `/OfflineSwear` costs and counts the same, so it can't be used to dodge the fee.

The old re-swear cooldown, the "3 free chain re-arranges," and the timed lockout are all removed. The account-wide rule still stands: all characters on an account must be in the same allegiance (or unsworn).

### 👑 Leaving an Allegiance — Vassals Released One Level

When you **break** from your allegiance or are **kicked or booted**, your **direct vassals are now released** and each becomes their own monarch (keeping their own sub-vassals), and you are left unsworn. This only cascades **one level** — your vassals' vassals stay with your vassals. Previously your whole sub-tree followed you out.

---

## 🩹 v1.16 — August 10, 2026

### 🔩 Abandoned Mine — Zerg Cap Lowered to 5

The zerg-control cap on the Abandoned Mine (Subway) has been lowered from **9 players per allegiance** to **5**.

### 👹 Dungeon Bosses — Take Roughly 2× Melee/Missile Damage

Dungeon Bosses were mitigating far more physical damage than intended — some were letting as little as 4% of a melee or missile hit through. Their armor has been roughly halved, so weapon damage now lands noticeably harder across all five bosses. Their resistance to spell damage is unchanged.

### 🛡️ Anti-Cheat — Closed a Door/Wall Jump-Clip Exploit

Fixed a movement exploit that allowed a player to bypass a closed door or wall by jumping through it under specific timing.

### 💎 Creature Slayer & Creature Resistance Morph Gems — Fixed Wrong Target Type

These two morph gems were flagged with an incorrect target type, causing them to be rejected on some items they should have been usable on. They now work correctly.

### 🏰 Hometown Control — Defenders' Mended Hits No Longer Also Damage the Bind Stone

Fixed a bug where a defender mending the Bind Stone (or a non-PK's attack being reflected) still dealt its full damage to the stone at the same time as the heal — quietly undermining the "defenders mend the stone" mechanic. Mended and reflected hits now correctly deal no damage to the stone.

### 📦 Steel Chest & Sturdy Steel Chest — Now Regenerate Instantly on Close

These chests now re-lock and reroll their contents the moment you close them, the same way Darkbeat's Storage Locker does, instead of waiting out their normal respawn timer. (this was content deployed manually after v1.15 but before v1.16 was released)

### 🔧 Tinkering Trinket Now Also Grants Brilliance

The **Tinkering Trinket** carries the **Brilliance** buff alongside its existing attribute and crafting buffs. Existing Tinkers can pick it up on their current trinket by re-running `/FlagTinker`, which patches new trinket buffs onto an already-flagged Tinker in place.

### 🚀 Catch-Up XP — Start Late, Catch Up Fast

Rolling a new character mid-season, or joining the server weeks after launch, no longer means grinding from behind forever. If your **total XP is under 70% of the current season XP cap**, every point of XP you earn is **multiplied** — and the further behind you are, the bigger the multiplier.

| Your total XP vs. the season cap | XP boost |
|---|---|
| Just starting out (0%) | **5×** |
| 35% of cap | **3.5×** |
| Halfway there (52.5%) | **2.75×** |
| Just under 70% | **2×** |
| 70% of cap or above | no boost |

The boost slides smoothly between those points — it isn't a set of tiers, it recalculates from exactly how far behind the cap you are, and it eases off on its own as you close the gap. Once you cross 70% of the cap you're considered caught up and earn at the normal rate.

**It stacks with everything else.** The catch-up boost multiplies on top of the season XP rate and your allegiance's hometown bonus. Late in the season, when the rate is running at 3×, a fresh character can be earning **15× XP**.

**It doesn't raise your ceiling.** The global cap and your Monster / Quest / PK budgets are unchanged — the boost simply gets you to them a lot faster.

Check your current multiplier on the **Catch-Up** line of `/season status`.

---

## 🩹 v1.15 — August 7, 2026

### ⚔️ PK XP Zerg Penalty — Thresholds Raised

The **PK XP zerg penalty** (introduced in v1.14) now allows more allegiance members online before the reduction kicks in. Full XP now holds up to **10 online** (was 5), and the curve shifts up to match:

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

Everything else about the penalty is unchanged — see v1.14 below for the full mechanics.

### 🌱 Fixed Monster & Loot Respawns Not Triggering

A global change to the generator system had broken automatic respawning under the Infiltration ruleset — cleared monster and loot spawns in some areas could fail to regenerate. This has been fixed; spawns regenerate normally again.

### 👹 Dungeon Bosses Stalk the Hot Dungeons and the Abandoned Mine

Powerful named bosses now roam at random. Any monster spawning in an active Hot Dungeon or the Abandoned Mine has a small chance to be a **Dungeon Boss** instead — one of five: **The Gravewalker**, **Vaeth'ren the Emberlord**, **Rendmaw**, **Aggregate Prime**, and **Nharim Dul, the Whispering Death**.

- **Hunt for them.** When one appears, a message goes out to the whole world — but it never says *where*. The bosses don't show on radar, so you'll have to go looking.
- **Always a fair fight.** Their strength scales to the current season level cap, so they're a real challenge whether the season is young or maxed.
- **Rewards worth the hunt.** Every player who lands a hit shares in the spoils: PK Trophies and Phials of Bloody Tears go straight to your pack, a scatter of Boxes drops on the ground to fight over, and the corpse carries a rich haul of loot on top of a hefty XP payout.

### 🔥 Hot Dungeons Now Zerg-Controlled (Capped at 9 per Allegiance)

Active Hot Dungeons now enforce the same **zerg-control limit as the Abandoned Mine** — each allegiance can have a **maximum of 9 players** inside at the same time. If a 10th member of your allegiance tries to enter, they are blocked. If a 10th already slipped in, the most recently teleported players are booted to their lifestone.


### 🏰 Hometown Phase 2 — Trophies, Repels, and No More "Peacing"

Phase 2 of a hometown assault gets a big overhaul:

- **Breach bonus** — every attacking-allegiance member near the Bind Stone gets **5 PK Trophies** the moment Phase 2 begins.
- **Hold-the-line trophies** — while within **50 meters** of the Bind Stone during Phase 2, both attackers and defenders earn **1 PK Trophy per minute** of participation.
- **Repelled attacks** — defenders can now end a siege early. If they hold the Bind Stone with **at least 2 defenders and no non-defenders within 50 meters for 10 straight minutes**, the attack is repelled: the defenders win and take the full defense rewards. Any non-defender — an attacker or a neutral third party — coming within 50 meters resets the timer.
- **No more "peacing" the stone** — while **any player who isn't in the attacking allegiance** is within **100 meters** of the Bind Stone, attacker damage to it is **cut by 90%**. You have to actually drive the defenders out of the area before you can burn it down.
- **Defenders mend the stone** — if a defender attacks their own Bind Stone, it no longer takes damage. Instead it **heals by 10%** of the damage that hit would have dealt.

### 🏟️ Arena PK Quests Now Count for Every Match Type

Some Daily arena PK quests were not crediting correctly.  This has been fixed.

### 🚪 Abandoned Mine — PK Only, No Recall/Summon, Zerg-Controlled

The portal to the Abandoned Mine now requires **Player Killer (PK)** status to use, and can no longer be reached via **recall** or **summoning** — you must walk through it directly.

The Abandoned Mine (Subway) is also now a **zerg-controlled area**, capping each allegiance at **9 players** inside at once.

### 🔥 Burning Coal Removed

**Burning Coal** has been removed from the game.

---

## 🩹 v1.14 — August 3, 2026

### 🗡️ Creature Slayer No Longer Rolls Moar or Remoran

**Moar** and **Remoran** have been removed from the pool of creature types a **Creature Slayer** weapon can be attuned to. Existing weapons already bearing those slayers are unaffected; they just can no longer be rolled going forward.

### 🪃 Slayer Morph Gems Now Work on Atlatls

The **slayer morph gem** previously refused atlatls, accepting only melee weapons, casters, bows, and thrown weapons. Atlatls are now valid targets, so throwers can attune a slayer to their weapon like everyone else.

### 🚫 No More Housing-Permission Chat Spam

Players were abusing the house guest and storage permission controls to **flood a rival's chat** — rapidly adding and removing someone from their storage/guest list fires a "*so-and-so has granted/revoked your access*" message at the target each time. A short rate limit now applies to housing and related permission commands: you can only issue one every few seconds, so the notifications can no longer be spammed. This covers all the housing permission actions (guests, storage, allegiance access, open/closed status, hooks) as well as friends lists, corpse-looting permissions, allegiance officer/ban lists, and squelch changes.

On top of that, **squelching a player now also blocks their housing guest/storage notifications** — the "added/removed you from the guest list" and "granted/revoked your storage access" messages. So if someone slips past the rate limit, just squelch them and you won't see the messages at all.

### 🍾 Ancient Bottles Now Capture Only 25% of Post-Cap PK XP

The **Ancient Bottle** used to be a near-perfect safety valve: every point of PvP XP you earned past your daily PvP or global cap was stashed in the bottle, so you eventually recovered **100%** of it. That made hitting the cap almost meaningless for PvPers. Now, when PvP XP overflows the cap, **only 25% of it is captured** into your Ancient Bottles — the other 75% is lost to the cap, just like every other XP category.

Drinking a bottle is unchanged: it still releases **100%** of whatever it holds, up to your available cap headroom. This only reduces how much goes *in*. XP already sitting in your bottles is unaffected.

### ⚔️ PK XP Now Scales Down With Your Allegiance's Online Size

To keep PvP rewarding for small, tight-knit allegiances — and to discourage everyone piling into one giant "zerg" allegiance — **all PK XP is now reduced based on how many of your allegiance's members are online** when you earn it. The more allies you have logged in, the smaller each PK reward.

| Allegiance members online | PK XP earned |
|---|---|
| 5 or fewer | 100% |
| 6 | 95% |
| 7 | 90% |
| 8 | 80% |
| 9 | 70% |
| 10 | 50% |
| 11 | 30% |
| 12 or more | 10% |

This applies to **every source of PK XP** — open-world kills, arena rewards, PK quests, hometown captures, and the XP you drain from a victim's **Ancient Bottle** when you kill them (that drain is a kill reward, so it's cut like everything else). Solo players and small allegiances feel nothing; large ones take a steep cut. The only exemption is **drinking your own Ancient Bottle** to release stored XP — that experience was already earned, so it's never reduced.

### 🏰 Hometown XP Bonus — Fixed and Expanded to All XP

The **+5% XP per hometown your allegiance owns** bonus was not being applied correctly — many owners (including solo allegiance leaders) got no bonus at all because the game looked up town ownership using the wrong allegiance key. This is now fixed: ownership is resolved the same way everywhere, so the bonus reliably applies to every owner.

The bonus also now applies to **all experience you earn** — monster kills and quest turn-ins included — not just open-world PK kills. Each hometown your allegiance owns adds **+5%** (stacking, no cap) to the XP from every source.

---

## 🩹 v1.13 — July 17, 2026

### 🎯 Bounties — Turn the Tables on Your Hunter

If a bounty hunter comes for you and **you kill them instead**, their contract now **fails** and drops onto their corpse. Loot it and turn it in to the Bounty Collector to claim **100 PK Trophies** — a reward for surviving the hunt. Only you, the target named on the contract, can cash it in; to anyone else it's a worthless scrap. The hunter, meanwhile, loses the contract entirely.

### 💎 Defense Requirement Morph Gems Now Also Strip Wield Requirements

The **Missile Defense Requirement Morph Gem** and **Melee Defense Requirement Morph Gem** previously only removed a defense skill's **activation** requirement (the skill you needed to *use* the item's magic). They now also remove a matching **wield** requirement — the defense-skill threshold you needed just to equip the item. Other wield requirements (level, attributes, a different skill) are left alone. An item that had *only* the wield requirement is now a valid target for the gem, whereas before it would have been rejected.

### 🛡️ Shields Now Protect You Out of Combat

Your equipped shield now adds its armor level to your defense **even in peace mode** — you no longer have to be in combat stance for the shield to count. As always, it only blocks attacks coming from your **front** (a 180° frontal arc), and it now does so against **both players and monsters**. No shield skill is required — just wield a shield.

The one exception is the **1v1 arena**, where shields still only work while you're in combat stance.

### 🔓 Free a Stuck Character Yourself — `/ForceLogoffStuckCharacter`

If one of your characters gets **stuck in the world** and won't log out, you no longer have to wait for an admin. Log onto **another character on the same account** and run:

`/ForceLogoffStuckCharacter <stuck character name>`

The server will force the stuck character out of the world. For safety it only works on a character **on your own account** (you can't target yourself or anyone else's character).

**Important:** the first run asks the character to log off gracefully and gives it about a minute to do so. If it's still stuck, **run the command a second time** — the second run forcibly removes it. This two-step behavior is intentional: the first attempt tries for a clean save before the second one forces the issue.

---

## 🩹 v1.12 — July 17, 2026

### ⚰️ Arena Corpses — Faster Rot and Open to Everyone

Corpses left in an **arena landblock** now decay much faster than in the open world (a few minutes instead of an hour or more), and they're **lootable by anyone** — not just the killer. Loot your fallen opponents quickly, because your rivals can grab their drops too.

This rides on a broader looting rule that now applies everywhere: a player-kill corpse, initially locked to its killer, **opens up to all players after it has decayed for about 20 minutes**. Arena corpses hit that threshold almost immediately thanks to their short rot timer.

### 🔧 Tinkers — Arcane Lore Specialization and Major Cantrips on the Trinket

Tinker characters (`/FlagTinker`) now get **Arcane Lore specialized and maxed** alongside their crafting skills — no more falling short on appraising or using high-spellcraft gear. Their **Tinkering Trinket** now also carries **Major cantrips** for all six attributes and the four tinkering skills (Item, Weapon, Armor, and Magic Item Tinkering), stacking on top of its existing level-7 aptitude buffs.

**Existing Tinkers:** just re-run `/FlagTinker` on your Tinker to receive these upgrades — it's safe to run again and resets nothing. If your trinket is equipped, re-equip it (or relog) to apply the new cantrips.

### 🔧 Tinkers — Locked Out of PK XP and Arenas

Tinker characters (`/FlagTinker`) are dedicated crafters, and are now fully excluded from PvP progression. A Tinker can no longer **earn any PK XP** from any source — player kills, Ancient Bottle drains, PK quests, and PK gems all yield nothing — and can no longer **join arena events**. This closes the door on using a locked-down, vitae-immune crafter as a risk-free PvP alt.

### 🏘️ Hometown Control — Capture Protection Reduced to 8 Hours

A town that has just been captured is now protected from re-attack for **8 hours**, down from 24. Towns come back into play the same day they change hands, so a capture holds the map for an evening rather than a full day.

*Applied as a live server config change ahead of this release — it is already in effect in production, not waiting on the v1.12 deploy.*

### 🔇 Moderation — Global Chat Gag

Added a `@globalchatgag` / `@globalchatungag` admin command that silences a character in the global chat channels (General/Trade/LFG/Society/Olthoi/Roleplay) only — local say, emotes, and tells still work. The gag persists across logout.

### 🐛 Hometown Control — Bind Stone Destruction Fix

Fixed a bug where a town's bind stone could fail to be destroyed, leaving a hometown control event stuck and the town unable to be attacked. Bind stones now reliably resolve when destroyed.

---

## 🩹 v1.11 — July 12, 2026

### 🎯 Bounties — Completion Now Pays You Back

Completing a bounty contract now **refunds 100 PK Trophies** — the full cost of the Bounty Purchase Token — so a successful hunt pays for itself. This is on top of your PK-quest progress, and High Priority (Writ of Pursuit) contracts still pay their custom reward on top of the refund.

To balance this, the consolation payout when a contract **expires** (target no longer available) has been reduced to **25 PK Trophies**.

### 🎯 Bounties — Better Location Hints for Targets Inside Buildings

The bounty location finder used to give up and print a raw landblock/cell id whenever your target ducked inside a building, hut, or tunnel — even out on the open landscape, where the radar also hides your coordinates. It now recognizes those surface structures and reports the **map coordinates** of the spot (e.g. `9.7N, 40.9E (inside a structure)`), accurate to within the immediate area. Genuine underground dungeons still resolve to their name, and only truly unmapped dungeons fall back to an id.

### 🔥 Hot Dungeons — Salvage Bonus

Salvaging items while you're **inside an active Hot Dungeon** now yields **double the material** (2× units). It applies to whatever you salvage there, no matter where the items were originally looted — so it's worth hauling your salvage runs into a Hot Dungeon before breaking everything down.

---

## 🩹 v1.10 — July 9, 2026

### 🏘️ Hometown Control — Capture & Defense Rewards Rebalanced

Hometown battle rewards now differ by side, with **defenders paid more than attackers**. A successful capture already hands the attackers the town itself, so the loot pool is weighted toward the allegiance that shows up to hold the line.

| Reward | Attackers (capture) | Defenders (hold) |
|---|---|---|
| PK Trophies (split among winners) | 40 | 80 |
| MMDs (split among winners) | 20 | 40 |
| XP to next level (per player) | 5% | 15% |
| Phials of Bloody Tears (per player) | — | 1 |
| Darkbeat Keys (per player) | — | 2 |

Eligibility is unchanged — winners within 100 meters of the Bind Stone share the rewards, and losing PKs in range are still smited.

### 🐛 War Magic — Ring & Wall Spells No Longer Bug You Out

Casting a **ring** or **wall** war magic spell could leave your character stuck in the casting state, forcing a relog to recover. This is fixed — these spells now cast cleanly like any other war magic.

### 👑 Allegiance — Swear to a Lower-Level Patron

You can now swear allegiance to a **lower-level** character. No allegiance XP is passed up to that patron until they surpass their vassal's level, at which point passup begins automatically.

### 💎 Morph Gems — Remove Defense Requirements from Covenant & Quest Gear

The **Melee Defense** and **Missile Defense** requirement-removal gems can now be applied to **covenant and quest items**, not just loot-generated gear. If a target has no matching defense requirement to remove, you'll now get a clear message saying so instead of a generic failure.

---

## 🩹 v1.09 — July 9, 2026

### 🏟️ New Arena — Rat Lair

A new arena location, **Rat Lair**, has been added to the queue. It hosts **1v1**, **2v2**, and **Tugak** matches. When you queue for those formats you may now be matched into either Rat Lair or the existing arena — the system picks an open location automatically.

### 🏠 Housing — Purchase Requirements Removed

Housing purchase gates have been lifted:

- **No minimum character level** to purchase any house — buy any dwelling regardless of your level.
- **No allegiance rank required** to purchase a mansion.
- **No account-age limit** on buying any house.

---

## 🩹 v1.08 — July 9, 2026

### 🛡️ Movement Anti-Cheat — Terrain- and Combat-Aware

The movement validation system has been overhauled to sharply reduce rubber-banding for honest players while tightening detection of speed cheats:

- **Hills and uneven terrain** no longer cause rubber-banding. The server now recognizes when a position disagreement is just terrain (running up and down slopes) rather than an attempt to walk through walls, and lets legitimate movement flow.
- **Fighting in monster packs** is much smoother. Melee characters no longer get snapped around while brawling inside a group of mobs, and colliding with the specific enemy you're attacking — including chasing a player in PvP — is now treated as normal combat contact instead of a violation.
- **Speed cheat detection is tighter.** Sustained artificially fast movement — even modest boosts — is now reliably detected, alerted on, and removed. Legitimate movement techniques are specifically accounted for and unaffected.

Walking through walls, closed doors, and standing inside monsters remain blocked as before. No action is needed on your part — the changes apply automatically.

### 🧪 XP Cap — Fixed Skill-Use Draining Your Unassigned XP

Fixed a bug where, once you were at the daily XP cap, ordinary skill use could slowly **reduce your unassigned experience** — even while leeching or standing around.

Skill proficiency (the XP you earn automatically just from *using* skills) is meant to be self-funding — it grants a little XP and immediately spends it to raise the skill. But once you hit the daily cap, the grant was blocked while the skill-up was still charged to your **banked, unspent XP** — so every proc quietly moved XP out of your pool and into whatever skill fired.

The catch is that skills fire on their own without you doing anything active: your **defensive skills proc every time a monster swings at you**, and your magic skills proc while auto-buffing. So a character parked at a hunting spot in a fellowship — capped, taking hits all night — could wake up with their unassigned XP drained toward zero.

No XP was ever destroyed — it went into legitimate skill ranks — but it was being spent without your say-so. Now, while a category is capped, proficiency for that category simply does nothing until the cap lifts, and your unassigned XP is left untouched. This is tracked per category, so a maxed-out **monster** cap no longer affects proficiency even if your quest or PvP XP still has room.

### 🏘️ Hometown Control — Bind Stone Combat Rebalance

Phase 2 Bind Stone combat has been retuned so every class contributes fairly, instead of fire-arrow archers deleting the stone in seconds:

- **All damage types are now equal.** No element (slashing, fire, cold, acid, etc.) is more or less effective than any other against the Bind Stone — for both physical attacks and war magic. Previously the elemental damage types bypassed the physical resistance, letting elemental arrows hit far harder than they should.
- **Melee and missile damage is reduced** so a weapon user's DPS stays in line with a mage's rather than dwarfing it. War magic is unaffected — mages deal the same as before.
- **Damage now falls off with distance.** Attacks deal full damage within **15 meters** of the Bind Stone, taper off beyond that, and deal **nothing past 20 meters** — for weapons and magic alike. You have to fight up close to bring it down, which keeps attackers exposed to the defenders.

### 👑 Allegiance Swearing — Re-Arranging Your Chain

Two changes make it easier to organize your allegiance without burning the 7-day swear cooldown:

- **Same-account swearing is now cooldown-free.** Using `/OfflineSwear` to swear to another character on your own account no longer starts the swear cooldown and is never blocked by one. Organizing your own alts into a chain is always free.
- **Three free re-swears within your own allegiance.** You can now break and swear back into the **same allegiance** up to **three times** without triggering the cooldown — enough to re-arrange the order of your chain (for example, swearing under a different patron beneath the same monarch). Once those three are used up, the normal 7-day cooldown applies. Genuinely swearing into a *different* allegiance still costs the cooldown, and doing so refreshes your three free re-swears for the new allegiance.

This keeps chain organization flexible while still preventing players from rapidly breaking and re-swearing to farm reward mechanics.

---

## 🩹 v1.07 — July 7, 2026

### 💰 Darkbeat & Anti Parazi — Vendor Price Adjustments

Costs on a few vendor items have been rebalanced:

- **Ancient Empyrean Tool** (Darkbeat): 50 → **75** Phials of Bloody Tears
- **Skill and Attribute Reset Gem** (Darkbeat): 100 → **50** Phials of Bloody Tears
- **Workmanship Morph Gem** (Anti Parazi): 300 → **500** PK Trophies

### 🤖 VirindiTank — Slow Buffing and Repeated Vulns/Banes Fixed

Fixed the long-standing issues with **VirindiTank** on ClassicPvP: 1–2 second pauses between each buff, re-casting vulns on targets that were already vulned, and re-casting banes that were already on your armor.

The cause: the chat message that confirms a spell landed ("You cast Imperil Other VI on Drudge Slinker") had drifted from the retail format — it had gained a trailing period and material-prefixed item names (e.g. "your **Steel** Celdon Breastplate."). VTank parses that exact line to know a spell succeeded, so every enchantment cast looked like a failure to it: buffs stalled until VTank's internal timeout, and vulns/banes were cast again and again. War spells were unaffected because they don't produce that message, which is why hunting mostly worked while buffing didn't.

The message now matches retail exactly, and VTank registers casts immediately — buff cycles run at full speed and debuffs/banes are cast once.

### 📦 Loot Box Rebalance

The **A Box → Common/Rare/Mythic Mystery Box** loot chain has been rebalanced:

- **A Box** — Mythic Mystery Box chance reduced from 5% to **1%**; the freed 4% moves to the Common Mystery Box tier (60% → **64%**). Rare stays at 25%, A Dick stays at 10%.
- **Common Mystery Box** — no longer drops Darkbeat's Lost Storage Key; its MMD reward cut from ×5 to **×1**.
- **Rare Mystery Box** — Ancient Bottle removed (moved to Mythic only); gains Darkbeat's Lost Storage Key and the Slayer Upgrade Morph Gem (moved in from Mythic); MMDs cut from ×20 to **×5**, PK Trophies cut from ×100 to **×30**.
- **Mythic Mystery Box** — Slayer Upgrade Morph Gem moved out to Rare, replaced by **Oil of Creature Slaying**; MMDs cut from ×50 to **×20**, PK Trophies cut from ×1000 to **×250**.

### 🏆 PK Quest Reward Rebalance

Rewards across most arena, open-world, and bounty PK quests have been scaled down — lower XP percentages, Darkbeat Key counts, and PK Trophy/Box payouts on most tiers (open-world kill and bounty PK Trophy rewards saw the largest cuts). See [ServerInfo.md](ServerInfo.md#daily-pk-quest-rewards) for current values.

The three **Town Control kill quests** (PKKILL_TC_1/5/30) have been disabled and no longer appear in daily quest rotation.

### 🗑️ Level Requirement Removal Morph Gem — Discontinued

No item in the Infiltration era carries a level requirement, so this gem never had a real use. It's been pulled from sale on **Anti Parazi** and removed from the **Common** and **Rare Mystery Box** loot tables; the remaining weight in each was redistributed proportionally across the other entries.

### 🗝️ Shimmering Skeleton Key — Now Functional

The **Shimmering Skeleton Key** (from the Mythic Mystery Box) previously did nothing — its base template only opened one obscure lock code and had no universal-unlock flag. It now works as intended:

- **Opens any lock** — use it on any locked door or chest to unlock it instantly, regardless of the lock's difficulty.
- **Single use** — the key crumbles to dust after one unlock.
- **Drops on death** — the key is slippery, so if you're slain it falls to your corpse for the killer to loot.

### 💎 New Morph Gems — Slayer & Creature Resistance Randomizers

Two new morph gems let you re-roll the creature type on slayer/resistance gear:

- **Slayer Morph Gem** — randomizes the creature-slayer type on a loot-gen weapon or caster that already has a slayer, or on loot-gen armor that has a Creature Slayer Rating.
- **Creature Resistance Morph Gem** — randomizes the creature-resistance type on loot-gen armor/jewelry that has a Creature Resist Rating.

Both are sold on **Anti Parazi** for **100 PK Trophies** each, and drop from **Rare Mystery Boxes** (~1.9% each) and **Mythic Mystery Boxes** (~11.5% each).

### 🔧 Empyrean Tuning Fork — Impenetrability Fix

Fixed a bug where the **Empyrean Tuning Fork** (which re-rolls the Major cantrips on armor) could apply **Major Heart Thirst** — a *weapon* damage cantrip — to armor, due to a mis-mapped spell ID.

The bonus Impenetrability roll has also been reworked: if the item doesn't already have Impenetrability, there's now a **10% chance** to add one, split **1:3 Major** / **2:3 Minor** (~3.3% Major, ~6.7% Minor). Items that already carry Impenetrability are left alone so it never stacks.

### 🏹 Shooting From Portal Space Fixed

Fixed a long-standing exploit where an archer could enter combat and fire arrows while still in **portal space** — the "purple bubble" state before a character finishes materializing out of a portal or recall. Missile attacks are now blocked while teleporting, both at the moment an attack is started and mid-sequence if a portal is entered during a shot. Melee and magic already had this protection.

---

## 🩹 v1.06 — July 7, 2026

### 🏟️ Arena — Same-Allegiance Rewards Restored

A recent anti-alt-farming change had unintentionally stopped **same-allegiance arena matches** from paying out any rewards. In the arena, that block now applies only when an opponent is **not** in your allegiance but is a throwaway parked on an account that holds one of your allegiance-mates. Fighting an actual member of your own allegiance in the arena rewards normally again — still subject to the existing limit of **15 same-allegiance rewards per day**.

### 📦 Darkbeat Chest — Loot Quality Reduced

The loot quality of Darkbeat's Lost Storage chest (opened with Darkbeat's Lost Storage Keys) has been reduced — its loot quality modifier drops from **0.5** to **0.425**, slightly lowering the average quality of the items it rolls.

---

## 🩹 v1.05 — July 6, 2026

### 🏰 Hometown Control — Bind Stone No Longer Heals Over Time

During a Phase 2 siege, the Bind Stone no longer passively regenerates health. Its HP now changes only through the intended siege mechanics — dropping when a defender is killed and recovering when an attacker is killed — so a sustained assault can't be undone by slow background healing.

### 🏰 Hometown Control — Bind Stone Health Reduced

The Bind Stone's health during Phase 2 has been reduced by **20%** across all level caps. It still scales with the rolling level cap; it's simply a bit faster to bring down, shortening a clean unopposed siege from roughly 22 minutes to about 18.

### 🏰 Hometown Control — Allegiances Named in Announcements

Global and Discord announcements for hometown assaults, captures, and defenses now name both the **attacking** and **defending allegiances**, rather than an individual player. For example: "The Dark Legion is attempting to wrest control of Yaraq from The Iron Fist! Phase 1 has begun." Allegiances without a custom name are identified by their monarch (e.g. "Bob's Allegiance").

### 🏰 Hometown Control — Capture Rewards Now Awarded

Fixed a bug where destroying a town's Bind Stone and capturing the town granted **no rewards** to the attacking allegiance, and failed to smite the losing defenders. The reward step was looking at the wrong landblock and silently bailing out. Attackers within range of the Bind Stone now correctly receive their capture rewards (PK Trophies, MMDs, a Phial of Bloody Tears, Darkbeat Keys, and bonus XP), and defeated defenders nearby are smited as intended.

### 🪄 Wand Monkeying Disabled in PvP

A caster's **built-in spell** — the spell baked into a wand, orb, or other casting implement — now deals **no damage to other players**. This shuts down "wand monkeying," where players leaned on a caster's innate spell instead of casting their own war magic. Normal war magic cast from your spellbook is completely unaffected, and the change applies to PvP only — built-in caster spells still work as before against creatures.

### 💰 Pyreal Coins Now Stack to 25,000

Pyreal coins now stack up to **25,000** (up from 10,000), matching End of Retail. Fewer stacks means less pack clutter and easier trading. Your existing coin stacks are updated automatically — no action needed.

### 🛡️ Alt-Farming — No Rewards for Killing Allegiance-Mates' Alts

You can no longer farm PvP rewards off throwaway characters parked on your allegiance mates' accounts. If the character you kill sits on an account that holds another character in **your** allegiance, the kill now earns nothing: it does not count toward the season **PK-kills leaderboard**, your K/D, or your kill streak, and it does not advance **PK quests** or **bounty contracts**. The same applies in the **Arena** — if any opponent you defeat is one of these alts, the match pays out no rewards.

This also extends to **hometown warfare**: you cannot help assault a hometown held by an allegiance that another character on your account belongs to. Those characters no longer count toward starting a siege, and their kills won't advance one.

### 🏟️ Arena XP Rewards Retuned

Arena match XP payouts have been rebalanced:

- **1v1 win:** 20% → **10%** of a level
- **2v2 win:** 20% → **15%** of a level
- **1v1 / 2v2 loss:** 5% → **3.5%**
- **FFA / Tugak non-podium finish (4th+):** 5% → **3.5%**
- **Draws:** 15% → **3.5%**

FFA/Tugak podium finishes (2nd 25%, 3rd 15%), group results, and all item rewards (PK Trophies, Phials, Darkbeat Keys) are unchanged. Arena XP remains a fixed fraction of a level, independent of the seasonal rolling XP rate.

### 🏆 Open-World PK Trophy Drops

Killing another player out in the world can now drop a **PK Trophy** on their corpse. A few limits keep it from being farmed:

- No trophy if the victim is above the level 126 cap, or more than **15 levels below** the killer.
- No trophy if the killer and victim share the same monarch.
- A given victim can only have **3 trophies** dropped on their corpse(s) per rolling hour, and **10 per day**.

---

## 🩹 v1.04 — July 5, 2026

### 🛡️ Anti-Cheat — Tighter Movement Speed Enforcement

The server's movement speed enforcement has been improved to better catch client-side speed and quickness hacks. The checks are now terrain-aware: they account for legitimate movement over hills and uneven ground, which previously caused occasional rubber-banding for honest players, while holding a tighter limit on open ground where cheating is most obvious. The result is fewer false corrections during normal play and a smaller window for artificially fast movement to slip through. No action is needed on your part — the change applies automatically.

### 🚪 Closed Doors No Longer Disconnect You

Walking into a closed door is now treated purely as a physical barrier — like bumping a wall. You're still stopped from passing through a closed door, but doing so will no longer count against you or risk a disconnect. This removes a rare case where players pressed against a door (usually from a brief mismatch over whether the door was open) could be disconnected by the anti-cheat system.

### ⚗️ PvP XP Overflow — No Longer Lost at the Daily Cap

Fixed a bug where PvP reward XP could silently vanish instead of filling your **Ancient Bottles**. Overflow was only being captured when your PvP category bucket was full; if you had already hit the **global daily XP cap** (or the maximum level), any PvP reward — arena wins, PK quests, hometown captures, open-world PK kills — was lost entirely, even with an empty bottle in your pack. Now PvP overflow always tops off an Ancient Bottle that has room, from every cap, matching how the system was always meant to work. If you have no bottle (or all are full), there is nothing to catch it, as before.

---

## 🩹 v1.03 — July 4, 2026

### 💰 Pyreal Stacks — Currency Fix

Fixed a bug where some pyreal stacks would refuse to act as money — they wouldn't count toward your cash when you opened a vendor, couldn't be spent or sold, and couldn't be merged into another stack. The only way to get rid of an affected stack was to drop it on the ground.

The cause was a leftover piece of logic from another ruleset that tagged certain coin stacks as "restricted" and quietly excluded them from your usable currency. That restriction should never apply on ClassicPvP, and it no longer does — every pyreal stack now counts as spendable money again.

Any affected stacks already in your pack will start working the moment this patch goes live. If you still have a stubborn stack, simply merge it into a normal pyreal stack to clear the old tag. No character changes are needed — the fix applies automatically.

### 🏟️ Arena — No Damage Until the Match Starts

Fixed arena matches so that no damage can be dealt during the pre-match countdown after players are teleported into the arena. You can still cast beneficial spells — buffs, vulns, and other preparation — while you wait, but melee, missile, magic, and damage-over-time will not land on your opponent until the match officially begins. Everyone now starts the fight on equal footing.

### 🏟️ Arena — Overtime Rules Now Enforced

When an arena match reaches overtime, its healing restrictions are now actually applied. Chugging food and potions is disabled, and all other healing — heal-over-time spells, life-magic heals, and stamina-to-health transfers — is heavily reduced and continues to weaken as overtime goes on. Overtime now reliably drives a stalled match to a decisive finish instead of dragging on.

### 🏟️ Arena — In-Match Damage Now Tracked

Damage dealt and taken inside arena matches is now recorded again, so damage-based arena achievements and per-match combat stats work correctly.

### 🏟️ Tugak War — Spell-Only Combat

Tugak War is now fought exclusively with the **Martyr's Hecatomb** (Health Bolt) line of spells, tiers I through VII. Any other harmful spell you try to cast on an opponent now fails outright ("you cannot affect anyone"), and weapon attacks and damage-over-time do nothing inside a Tugak War match — everyone competes on equal footing using the same spell.

### 🎯 Bounty — You Must Earn the Kill

Fixed a bug where a bounty contract could complete even when you had no part in the kill. Previously, if anyone killed your target, your contract was marked complete — regardless of whether you were in the area or dealt any damage.

Now a contract only completes if **you** earned the kill: either you land the killing blow, or you deal at least **25%** of the target's total damage **and** are within visible range of them when they die. Standing across the map while someone else does the work no longer counts.

### 🩸 Anti Parazi — Burn Your Vitae for a PK Trophy

Anti Parazi now sells **A Dick** for **1 PK Trophy**. Eat it and it burns away your **Vitae penalty** — "(Vitae Removed) Now get back in there and make me proud..." No XP is granted; it simply clears the penalty. If you have no Vitae penalty, eating it does nothing and the item is not consumed. Pick it up from his shop alongside his morph gems and bounty items.

### ⚔️ PvP Reward XP — Decoupled from the Season Rate

Custom PvP reward XP — **arena matches, PK quests, hometown captures, and open-world PK kills** — is no longer multiplied by the seasonal rolling XP rate. Previously these rewards were scaled by the same global rate that ramps from ~0.25× early in the season up to 3× at the end, which made them feel tiny in the opening weeks and would have inflated them dramatically later on.

These rewards are now granted as **fixed percentages of your XP to the next level**, so a given result is worth the same fraction of a level all season long — they no longer shrink in the low-rate opening weeks, nor inflate late in the season when the rate climbs above 1×. The reward percentages themselves are unchanged (for example an arena 1v1 win grants 20% of a level, a loss 5%; a hometown capture grants 10%; an open-world PK kill grants 5–10%).

### 🏰 Hometown Control — Phase 1 Timer Fix

Fixed the Allegiance Hometown Phase 1 countdown, which announced "0s until Phase 2" after only one minute even though Phase 2 didn't begin until several minutes later. The countdown now matches the real Phase 1 hold duration.

---

## 🩹 v1.02 — July 4, 2026

### 🏘️ Allegiance Hometown — Bind Stone Combat Fixes

During a Phase 2 siege, the Bind Stone could not be attacked with missile weapons, shutting archers out of the fight entirely. Missile attacks now work against it. Physical damage against the Bind Stone was also out of balance, and has been retuned.

---

## 🩹 v1.01 — July 3, 2026

### 👑 Allegiance Swearing — Monarch Fix

Fixed a bug that prevented certain monarchs from swearing allegiance. If you became a monarch because another character on your account swore to you with the `OfflineSwear` command — but you had never sworn allegiance to anyone yourself — the game would incorrectly block you from swearing into another allegiance, claiming another character on your account was sworn elsewhere.

These monarchs can now swear allegiance normally. When a monarch swears in, their entire allegiance (including the account characters beneath them) follows along to the new monarch, so the one-allegiance-per-account rule is still honored. No character changes are needed — the fix applies to everyone automatically.

### 🐗 Hot Dungeon Box Drops Reduced

Box drop rates from Hot Dungeon kills have been **reduced by roughly 75%**. Boxes were dropping more often than intended, so every Hot Dungeon's per-kill box chance has been scaled down while keeping the relative balance between dungeons intact (denser dungeons still drop fewer boxes per kill than sparse ones).

This affects only the random per-kill box drops. The guaranteed box awarded for a PK kill inside a Hot Dungeon is unchanged.

### 🏟️ Arena Matchmaking — Low-Level Fix

Fixed a bug that prevented low-level players from ever being matched into arena events. The level-range calculation used to pair players of similar levels miscalculated for anyone below level 20, producing an impossibly high minimum level that no opponent could satisfy.

As a result, a queue full of low-level players would never start a match no matter how many were waiting. Matchmaking now correctly pairs low-level players, and no character changes are needed — the fix applies automatically.

### 🏟️ Arena Rewards — Fix for Losing a Match

Fixed a bug where players who lost an arena match by dying often received no end-of-match rewards. A death that ended the match wasn't being recorded at the moment it happened, so once the fallen player respawned the system mistook it for them leaving the arena early and disqualified them, which stripped their reward eligibility.

Deaths are now recorded correctly, so losers reliably receive their participation rewards (and kill/death totals are tracked properly). No character changes are needed — the fix applies automatically.

---

## 🩹 v1.00 — July 3, 2026 (Launch Edition)

### 🚀 Getting Started

#### Server Info

| Field | Value |
|-------|-------|
| **URL** | doctide.online |
| **Port** | 9000 |
| **Name** | Classic PvP |
| **Type** | ACE |

#### Client Setup

1. Download the `.7z` file from [mega.nz/folder/xi4jiKjJ#jpuTVa7CQYyNxyp-UHC_GA](https://mega.nz/folder/xi4jiKjJ#jpuTVa7CQYyNxyp-UHC_GA)
2. Unzip it with 7-Zip (free utility — google it if you don't have it)
3. Go to `C:\Turbine` and make a copy of your `Asheron's Call` folder. Name the copy **ClassicPvP**
4. Copy the DAT and client (`.exe`) files you downloaded and paste them into `C:\Turbine\ClassicPvP`, overwriting the existing files
5. In **Thwarg Launcher**, click the three dots next to the client path at the bottom and select `C:\Turbine\ClassicPvP\acclient_Infiltration.exe`
6. You're ready — log in to ClassicPvP. To return to an End of Retail server like Doctide, switch the path back to `C:\Turbine\Asheron's Call\acclient.exe`

For more detail, see the full [Getting Started guide](GettingStarted.md).

---

### 🕹️ Server Era — February 2005 (Infiltration Patch)

ClassicPvP is set in **February 2005**, specifically the **Infiltration patch era** of Asheron's Call. This is the classic, raw PvP experience — before years of power creep, skill consolidation, and late-game systems changed the game's identity.

#### Weapon Skills
Weapons use the **original, pre-consolidation skill system**. There is no "Light Weapons" or "Heavy Weapons" umbrella — each weapon type has its own dedicated skill:

> Sword · Axe · Mace · Spear · Dagger · Staff · Bow · Crossbow · Thrown Weapons · Unarmed Combat

Every build that relies on weapons invests in a specific skill, which shapes both your identity and your spec choices meaningfully.

#### What's NOT Here (EoR Systems)
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

### 🔒 Account Restrictions

ClassicPvP enforces a **strict one-account-per-player** policy, backed by the server itself — not just the rules.

- **One IP address, one account.** Each IP may only be associated with a single account. Playing multiple accounts from the same connection is not permitted.
- **IP tracking is automatic.** Every time you log in, your IP is recorded against your account. If your IP changes — because your ISP rotated it, you switched networks, or anything else — the new IP is simply added to your account's list and login proceeds normally. There is no penalty for IP changes.
- **What is blocked:** if you connect from an IP that is already registered to a *different* account, your login will be rejected and you will be prompted to contact an admin.

Common legitimate causes for an IP conflict: a household member plays on the same internet connection, you're connecting from a location another player has used (library, café, friend's house), or a VPN exit node was previously used by another player. Admins can review the binding history and resolve conflicts.

The intent is simple: no multi-boxing, no alt-army farming, no market manipulation through alts. Everyone plays on a level field.

---

### 📈 Rolling Level Cap

ClassicPvP uses a **server-wide rolling level cap**. Every player on the server shares the same ceiling — the maximum level you can achieve goes up on a set daily schedule, and no amount of grinding lets you get ahead of it. If you're at the cap, XP stops accumulating until the next advance. When that happens, you'll receive a chat message letting you know.

The cap opens at **level 15** on launch day. The early season moves fast — you're gaining several levels a day — then the pace slows as you approach the endgame.

**Week-by-week milestones:**

| Season Day | Level Cap |
|------------|-----------|
| Launch (Day 0) | **15** |
| Day 7 | 36 |
| Day 14 | 57 |
| Day 21 | 69 |
| Day 28 | 80 |
| Day 35 | 90 |
| Day 42 | 101 |
| Day 49 | 111 |
| Day 56 | 121 |
| **~Day 60** | **126 — level cap reached** |
| Days 60–120 | Post-cap XP grind |
| Day 121+ | Season cap frozen |

**What happens after level 126?**
The level cap tops out at 126 (the Infiltration-era maximum). Once that's reached, the rolling cap continues — but now as a raw XP ceiling rather than a level. This extra XP goes toward investing in skills and attributes beyond the level cap. This post-cap grind phase runs until approximately day 120, after which the ceiling is frozen for the rest of the season.

#### 📊 Rolling XP Rate Bonus

As the season progresses and the level cap rises, the global XP rate bonus increases alongside it — rewarding players who stay active later in the season.

The bonus starts low in the opening days, holds near 1× (normal rate) through the mid-season, and then accelerates sharply toward the end. Players grinding during the final weeks of the season earn XP significantly faster than those who played only in the early days.

**How the rate scales:**

| Season Stage | Approx. Level Cap | XP Rate |
|---|---|---|
| Launch (Day 0) | 15 | **0.25×** |
| Day 7 | 36 | ~0.39× |
| Day 14 | 57 | ~0.52× |
| Day 21 | 69 | ~0.66× |
| Day ~44 | 101 | **1.0×** (normal rate) |
| Day 63 | 126 (level cap reached) | ~1.56× |
| Day 84 | post-cap XP grind | ~2.24× |
| **Day 96** | post-cap XP grind | **3.0×** |
| Days 96–120 | post-cap XP grind | **3.0×** (maintained) |

The rate accelerates on a quadratic curve — the gains are small at first and ramp up faster as the season matures. By the time level cap is reached (~day 60), you are already earning at 1.5× base rate. The last stretch of the season hits 3× and holds there through the final weeks.

The maximum rate (3×) is configurable by admins and may change between seasons.

---

### ⏱️ XP Cap Categories

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
PvP is the one category with a safety valve. If your PvP bucket is full — or you're at the global cap — any PvP XP you would have earned doesn't vanish. Instead, it is absorbed by **Ancient Bottles** in your inventory (if you have any). You can then use an Ancient Bottle later when your PvP budget has room, releasing its stored XP at that point. The bottle holds up to 100 million XP and tells you how full it is as it absorbs overflow.

#### Checking Your Status

Use `/season status` to see a live snapshot of the current season:

- **Season day** — how many days have elapsed since launch
- **Level cap** — the current maximum level (or "post-cap XP grind" once level 126 is reached)
- **XP cap** — the exact total-XP ceiling in effect right now
- **Next advance** — hours and minutes until the cap ticks up again
- **XP budgets** — your Monster, Quest, and PK XP earned vs. your budget for this window, with a percentage and a `[FULL]` indicator when a bucket is exhausted

---

### 👑 Allegiance Passup XP

Allegiance XP works as it did in the Infiltration era. When your vassals earn XP, a portion accumulates for you as their patron. It is held until you log in, at which point it is delivered in a lump sum and you receive a message showing the amount.

A few things to know:
- Passup XP counts against your **Monster bucket** (same pool as creature kills). If you're actively grinding and also receiving heavy passup from your vassals, both compete for that budget.
- Passup cascades **automatically up the chain** the moment the XP is originally earned — your patron receives a share, their patron a smaller share, and so on up the tree (see the chain mechanics below). What does **not** happen is a *second* cascade when you personally collect your held passup: the lump sum delivered to you on login is not treated as freshly-earned XP, so receiving it does not generate new passup up your own chain.
- The amount of passup you can get at a time without spending it is 4.2 billion xp. If you accumulate that much and don't spend any you will start losing new earnings. 

#### XP Chain Mechanics (Loyalty & Leadership)

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

### 👥 Swearing Allegiance to Same-Account Characters

You can swear allegiance to another character on your own account using the `/OfflineSwear <CharacterName>` command. Because you cannot have two characters logged in simultaneously, the target must be offline.

All normal allegiance rules apply — the target must be higher or equal level, must not already be your vassal, and the account-wide allegiance lock still applies (both characters must end up in the same monarch's chain).

---

### 🤝 Allegiance Swear Restrictions

ClassicPvP enforces rules around allegiance oaths to prevent abuse and kill-trading between alts.

#### Account-Wide Allegiance Lock

All characters on a single account must belong to the **same monarch's allegiance**. Once any character on your account has sworn to an allegiance, your other characters can only swear to someone within that same chain. Attempting to swear into a different allegiance will be blocked.

#### Swear Cooldown

After swearing allegiance, a **30-day cooldown** applies before you can swear again.

- Your **first oath ever** is free — no cooldown is set.
- The cooldown applies to voluntary changes only. If your patron or someone above them in the chain **breaks their oath**, causing you to be broken from your allegiance involuntarily, you can re-swear back into the **original allegiance chain** without waiting.
- If your **monarch moves their entire allegiance** by swearing to a new patron, that is their oath change — your relationship to your own patron is unchanged and no cooldown is triggered for you.

#### Break Cascade & Account Protection

If someone above you in the chain breaks and it would leave your account with characters in two different allegiances, the server automatically breaks the affected character from their patron.

When this cascade propagates downward:
- Characters sworn to another character **on the same account** as their patron are **not broken** from that bond — the same-account relationship is preserved.
- The cascade continues through them, severing any **different-account** vassals further down the chain.

---

### 🗡️ Same-Target Kill Diminishing Returns

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

### 🏟️ Arena System

The Arena is a **queue-based structured PvP system** that operates independently from open-world PK. You join a queue, get matched, get teleported in, fight, and receive rewards.

#### Entering the Arena
Use the `/arena` command to interact with the queue.

**Requirements to join:**
- Must be **Player Killer (PK)** status
- Must **not** be PK-tagged (no active PK timer from a recent kill)

#### Arena Types

| Type | Format |
|------|--------|
| **1v1** | One vs. one duel |
| **2v2** | Two vs. two team duel |
| **FFA** | Free-for-All — up to 10 players, last one standing wins. At most 2 players from the same allegiance may be in the same match. |
| **Tugak** | Large Free-for-All — up to 15 players, last one standing wins. No allegiance limit per match. Prefers larger player counts before launching. Has its own separate quest achievement tracking. |
| **Group** | Team-based — organized fellowship vs. fellowship |

#### Arena Combat Rules

Arenas run under specific combat restrictions that do not apply in the open world.

- **Ineptitude spells are suppressed.** Creature enchantment debuffs (inepts) and all item enchantment spells are blocked in arena matches. Only the three defense-lowering spell categories are permitted — Magic Defense Lowering, Melee Defense Lowering, and Missile Defense Lowering. This prevents NPC pets, item procs, or other external debuff sources from influencing match outcomes.
- **Healing kit bonuses are capped in 1v1 matches.** The skill bonus from a healing kit is capped at 150 effective bonus skill, and the restoration multiplier is capped at 1.5×. High-end healing kits still function — they just can't fully carry a fight in the structured 1v1 format.

#### Arena Rewards (Winners)

| Type | XP | PK Trophies | Phials of Bloody Tears | Darkbeat Keys |
|------|-----|-------------|----------------------|------------|
| **1v1** | Level-proportional | 5 | 1 | 1 |
| **2v2** | Level-proportional | 5 | 1 | 1 |
| **FFA** | Level-proportional (2×) | 5 | 3 | 5 |
| **Tugak** | Level-proportional (2×) | 5 | 3 | 5 |
| **Group** | — | 5 per member | 1 per member | 2 per member |

- Arena XP counts against your **PvP daily bucket**.
- Eliminated players should stay online until the match ends to be eligible for rewards.
- Rewards are scaled to your level range and the current rolling cap.

#### Daily PK Quest Rewards

In addition to the per-match rewards above, completing arena and PK milestones each day earns **Phials of Bloody Tears** and **PK Trophies** through the daily quest system. Quests reset each day and stack — hitting a higher threshold also awards all lower tiers. Selected highlights:

| Quest | Threshold | Phials | PK Trophies |
|-------|-----------|--------|-------------|
| Participate in arena matches | 5 / 15 / 30 / 50 | 1 / 2 / 3 / 5 | 5 / 25 / 50 / 100 |
| Win arena matches (any type) | 10 / 20 / 30 | 2 / 3 / 5 | 40 / 100 / 200 |
| Tugak War — participate | 2 / 25 matches | 1 / 5 | 15 / 75 |
| Tugak War — win | 1 / 20 wins | 1 / 5 | 25 / 75 |
| Tugak War — top 3 | 1 | 1 | 15 |
| Open world kills (opposing allegiance) | 10 / 30 | 1 / 3 | 20 / 100 |
| Complete bounty contracts | 1 / 5 / 25 | 1 / 3 / 5 | 25 / 50 / 100 |
| Complete high priority bounties | 1 / 5 | 2 / 5 | 25 / 50 |
| Town Control kills | 1 / 5 / 30 | — / 2 / 3 | 15 / 25 / 50 |

#### Arena Ranking

Each arena format has its own leaderboard, all viewable with `/arena rank <type>`.

##### 1v1 — Composite Score
1v1 uses a **composite score** rather than raw ELO, designed to reward players who stay active rather than those who grind a good rating and stop queueing to protect it.

**Your score = ELO + (Wins × 8) + (Matches Played × 2)**

- **ELO** updates after every match based on the rating difference between you and your opponent. Starting ELO is 1500.
- **ELO decay** — if you stop playing, your ELO drops **3% per day** once you've gone **3 or more consecutive days without a 1v1 match**, floored at 1500. Decay is written directly to the database each day, so the stored ELO is always your current effective rating. Playing a 2v2 does **not** stop your 1v1 decay clock — each format is tracked independently.
- **Win bonus (+8 per win)** and **match bonus (+2 per match played)** mean an active player with a slightly lower ELO can outrank an inactive player with a higher one.

Use `/arena rank 1v1` to see the leaderboard.

##### 2v2 — Individual + Team Rankings
2v2 tracks two separate leaderboards:

**Individual** — same composite formula as 1v1, plus a **survival bonus**:
- **+30 per match where you were not eliminated** as part of the winning team
- Score = ELO + (Wins × 8) + (Matches × 2) + (Times Survived × 30)
- Decay rules are the same: 3% per day after a 3-day grace period, tracked separately from your 1v1 rating

**Team pairs** — your performance as a specific two-player combination is tracked separately. A team's score uses the same composite formula, with the team's ELO based on the average of both players' individual ELOs at match time.
- Winning teams gain ELO; losing teams lose ELO
- Team ELO also decays if the pair goes inactive; playing with a different partner does not stop the decay clock for this pair
- Survival bonus also applies at the team level

Use `/arena rank 2v2` for individual standings, `/arena rank 2v2team` for team pair standings.

##### FFA & Tugak — Placement Points
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

### ⚔️ PvP Combat Rules

#### Logout Penalty
Logging out during PvP does not protect you. Any spell projectile (War spells, Void spells) that hits a player who is actively logging out will **critical hit 100% of the time** — matching existing melee behavior. Pulling the plug to escape a spell in flight doesn't work.

#### Portal Space Behavior
Melee swings and missile attacks can **initiate against targets who are in portal space** (the purple bubble state). The attack animation and windup begin normally, but damage is not applied until the target exits portal space. This matches retail behavior — you could already be mid-swing when a target finishes porting in, and the attack resolves the moment they materialize.

#### Dispel Protection After Taking Damage
For a window after being struck in PK combat, your own dispel spells will **not remove vulnerability spells** on your target. This prevents the tactic of attacking someone, getting hit once, then immediately dispelling their vulns to bleed off the damage setup. The protection window is 5 minutes by default and is configurable by admins.

#### Jump Spam
Jumping rapidly in succession triggers accelerated stamina drain. After exceeding the jump threshold within a rolling 10-second window, every subsequent jump costs PK-rate stamina for a short penalty period. This eliminates the movement speed advantage gained through rapid jump-chaining.

---

### 🛡️ Enhanced Anti-Cheat

ClassicPvP runs a number of anti-cheat and anti-abuse systems beyond standard emulator defaults.

- **IP Binding** — as described above, accounts accumulate IP addresses over time. A login from an IP already registered to a *different* account is rejected automatically.
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

### 🎯 Bounty System

The Bounty System is a player-driven PvP economy that creates persistent, targeted hunting objectives on top of open-world PK combat.

#### How It Works
1. Visit the **Bounty Hunter NPC** with a **Bounty Purchase Token**.
2. You receive a **Bounty Contract** for a randomly assigned eligible PK player (drawn from online players, excluding your own allegiance and players on cooldown with you).
3. Hunt your target. Kill them to mark the contract complete.
4. Return the completed contract to the Bounty Hunter NPC to collect your reward.

#### Rules & Restrictions
- You must be in a **whitelisted allegiance** to participate in the bounty system.
- You cannot be assigned a target from your own allegiance.
- You cannot be assigned a target from the same IP address as you.
- There is a **maximum number of active contracts** you can hold at once.
- After turning in a completed contract, a **cooldown** prevents you from immediately purchasing another.
- Targets have a per-hunter cooldown — you cannot be repeatedly assigned the same player back-to-back.

#### Proximity Mechanic
If you spot your bounty target in the world (or they spot you), their **PK timer refreshes** — preventing them from using portals or recalls to escape the encounter. Proximity to your hunter puts you at risk even if you haven't been directly attacked.

#### Writs of Pursuit — High Priority Targets
Any player can place a **bounty with a custom reward** on a specific enemy using a **Writ of Pursuit**:

1. Obtain a Writ of Pursuit item.
2. Inscribe it in the format: `PlayerName:Amount`
3. Turn it in to the Bounty Hunter NPC along with the specified currency amount.
4. That player is flagged as a **High Priority Target** server-wide.
5. Any player who already has a contract on that target sees their contract upgraded.
6. The first bounty hunter to complete the contract receives the custom reward and a **server-wide broadcast**.

High Priority Targets have an increased chance of being assigned to new Bounty Contracts.

#### Achievement Tracking
The bounty system tracks milestones over time, including:
- Unique players hunted
- Repeat contracts on the same target
- Speed completions (multiple kills within short windows)
- Kill streak targets broken (hunting players on hot streaks)

---

### 🗡️ Creature Slayer & Creature Resistance Ratings

These are **gear-based rating systems** active in the Infiltration ruleset, sourced from items and tinkering.

#### Creature Slayer Rating
Increases your damage dealt to **a specific creature type** (e.g., Undead, Shadow, Lugian). The rating accumulates additively across all equipped items that carry it.

> Formula: `(100 + Slayer Rating) / 100 = damage multiplier against that creature type`
> Example: A Slayer Rating of 25 vs. Undead means +25% damage to Undead.

#### Creature Resistance Rating
Reduces incoming damage **from a specific creature type**. Also gear-based and additive across equipment.

> Formula: `100 / (100 + Resist Rating) = incoming damage multiplier`
> Example: A Resist Rating of 25 vs. Shadow means you take ~80% of Shadow creature damage instead of 100%.

Both ratings only apply to **players** — monsters do not carry these ratings. They are a meaningful gearing consideration when farming specific content or building a focused PvE loadout.

Note: Many of the ratings introduced in later retail patches (Crit Rating, Damage Resistance Rating, Healing Boost Rating, etc.) **do not function** in this ruleset — they are entirely disabled. Creature Slayer and Creature Resist are among the few rating systems that **are** active and worth building around.

---

### 🏆 Season Leaderboards

ClassicPvP tracks a **Season leaderboard** across 12 categories spanning both arena and open-world PvP. Every week the top players in each category are recognized and rewarded.

#### Leaderboard Categories

##### Arena
| Category | What It Ranks |
|---|---|
| **1v1 Arena** | Composite score (ELO + wins + matches) |
| **2v2 Arena** | Composite score (ELO + wins + matches + survival bonus) |
| **FFA Arena** | Lifetime placement points across all FFA events |
| **Tugak Arena** | Lifetime placement points across all Tugak events |
| **Group Arena** | Total Group arena wins |
| **Arena Wins** | Total wins across all arena types combined |
| **Arena Kills** | Total kills recorded inside arena matches |
| **Arena Matches** | Total arena matches played (any type) |

##### Open World
| Category | What It Ranks |
|---|---|
| **PK Kills** | Total open-world player kills |
| **K/D Ratio** | Kill/death ratio (minimum 10 kills to qualify) |
| **Kill Streak** | Best consecutive open-world kill streak without dying |
| **Bounty Hunter** | Total bounty contracts completed |

##### Overall
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

#### Weekly Milestones

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

#### Commands

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

### 🔥 Hot Dungeons

Periodically, up to **3 dungeons** across Dereth will become **Hot** — offering bonus experience and extra loot for players who venture inside.

#### How It Works

- Every **12–36 hours**, a new dungeon is selected from a curated list and becomes Hot.
- Each Hot Dungeon stays active for **24–48 hours**, then expires independently.
- A **global broadcast** announces each dungeon when it becomes Hot, and again every hour while it remains active. A final announcement goes out when the dungeon cools down.
- Use the command **`/hotdungeons`** at any time to see all currently active Hot Dungeons, their XP multipliers, and time remaining.

#### Dungeon Eligibility

Each dungeon in the pool has a **level bracket** (minimum and maximum server level cap). A dungeon only becomes eligible when the rolling level cap falls within that bracket, ensuring the featured content is always appropriate for the current progression stage of the season.

#### Rewards While a Dungeon is Hot

| Reward | Details |
|--------|---------|
| **XP Multiplier** | All monster and PK kills inside the dungeon have their XP multiplied (multiplier varies per dungeon, ranging from 1.5× to 4×). The multiplier is applied before fellowship sharing. |
| **Double Loot** | Monster corpses receive two independent loot rolls, effectively doubling item generation. |
| **A Box** | Each monster kill has a per-dungeon configurable chance to drop **A Box** on the corpse. |
| **PK Rewards** | When a PK kill occurs inside a Hot Dungeon between players of **different allegiances**, the victim's corpse will contain a **Phial of Bloody Tears** and **A Box**. |

*This document will be updated as new systems and content are added. Stay tuned.*

---

### 🏘️ Allegiance Hometown Capture

Allegiances can now conquer and hold **towns across Dereth** through a two-phase PvP assault system. The old single-hometown bindstone has been replaced entirely.

#### Owning Towns

Any allegiance member can walk up to a **Bind Stone** in an unowned town and use it to **claim the town for free**. Once claimed, the town becomes your allegiance's hometown and all members can recall there.

- `/ah` — Recalls to a random town owned by your allegiance
- `/ahtown <name>` — Recalls to a specific owned town (e.g. `/ahtown Arwic`)
- `/towns` — Lists all 25 capturable towns and their current ownership status

#### Capturing an Enemy Town

To take a town owned by a rival allegiance, use the Bind Stone to begin the assault.

**Phase 1 — Perimeter Control (up to 60 minutes)**
- Phase 1 begins **automatically** when at least **2 members** of a single attacking allegiance are within **5 meters** of the Bind Stone and no other enemy allegiances are within **50 meters** — no player action required
- If an enemy PK enters within 50 meters, a warning is broadcast. If they remain for **30 continuous seconds**, Phase 1 progress resets. Leaving the area before 30 seconds have passed cancels the threat with no penalty.
- Hold the zone for **4 uninterrupted minutes** to trigger Phase 2
- Failing to reach Phase 2 within 60 minutes announces a global failure and applies a **3-hour cooldown** on that town for your allegiance

**Phase 2 — Destroy the Bind Stone (30 minutes)**
- The Bind Stone becomes attackable — hit it with melee weapons to chip down its HP
- Bind Stone HP scales with the current rolling level cap
- Each kill on the defending allegiance in the combat zone deals **5% max HP** bonus damage to the Bind Stone
- Each kill on the attacking allegiance in the combat zone **heals the Bind Stone** by 5% max HP
- Destroy the Bind Stone within 30 minutes → **Attackers win**
- Survive 30 minutes with the Bind Stone intact → **Defenders win**; the Bind Stone heals and becomes unattackable again

Two allegiances cannot attack the same town simultaneously. An allegiance can maintain at most **2 active assaults** at once.

#### Cooldowns & Protection

| Event | Cooldown |
|---|---|
| Phase 1 timeout (failed to reach Phase 2) | 3 hours — attacking allegiance only |
| Phase 2 failure (Bind Stone survived) | 6 hours — attacking allegiance only |
| Successful capture | 24 hours — new owner protected from attack (configurable) |

#### Rewards

Winners within **100 meters of the Bind Stone** (on the town landblock or an adjacent one) at the moment of resolution receive:

- **40–120 PK Trophies** split among eligible players
- **10–30 MMDs** split among eligible players
- **1 Phial of Bloody Tears** per player
- **3 Darkbeat Keys** per player
- **10% of XP to next level** per player

Losing allegiance PKs within **100 meters of the Bind Stone** at the moment of resolution are **smited**.

#### Using the Bind Stone

Clicking (using) the Bind Stone at any time gives you a status message:
- **Unowned town** — instantly claims it for your allegiance
- **Your town** — confirms ownership and prompts you to defend
- **Enemy town, Phase 1 active** — informs you that an assault is already in progress
- **Enemy town, Phase 2 active** — informs you that the Bind Stone creature is under attack
- **Enemy town, no active assault** — shows any cooldown or blacklist block reason, or tells you the gather requirements to trigger Phase 1

During **Phase 2**, the real Bind Stone becomes invisible (cloaked) and an attackable **Bind Stone creature** appears in its place. Destroying the creature ends Phase 2 and awards the town to the attackers. If the creature survives the 30-minute timer the town remains with the defenders.

#### Allegiance Blacklist

Server admins can suspend an allegiance from participating in hometown warfare via the blacklist. Blacklisted allegiances cannot initiate Phase 1 and are informed when they attempt to do so.

#### Open-World PK Kill XP

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
| +5% per hometown your allegiance owns | Passive, stacks, no cap |
| Active hometown conflict on the kill landblock (either phase) | × 2 |

**Diminishing returns:**
Killing the same player more than **3 times within a 1-hour window** suppresses all rewards from that target for **3 hours**. No XP, no quest credit, no season credit. You'll receive a message when a kill is suppressed.

---

### 🛒 Vendors

#### Darkbeat

**Darkbeat** is a special vendor located in the Afterlife area. He accepts **Phials of Bloody Tears** as currency (not pyreals) and sells rare crafting and upgrade items. Phials are earned through PK quests, arena rewards, and hometown captures.

| Item | Cost (Phials) | Description |
|------|--------------|-------------|
| Imbue Altering Morph Gem | 20 | Randomizes a weapon's imbue between Crippling Blow, Armor Rending, and Critical Strike. |
| Empyrean Tuning Fork | 25 | Randomizes the legendary cantrips on armor, jewelry, or shields that already have legendaries. One use per item. |
| Slayer Upgrade Gem | 25 | Upgrades an existing slayer damage bonus to 1.8 on weapons that rolled a slayer via the tinkering lottery. |
| Ancient Bottle | 50 | Absorbs PvP XP overflow up to 100M. Bonded & Attuned. |
| Ancient Empyrean Tool | 50 | Guarantees the next tinker will not fail. |
| Empyrean Jeweler's Sawblade | 50 | Randomizes the slot of a ring, bracelet, or necklace between finger, wrist, and neck. |
| Oil of Creature Slaying | 75 | Adds a random slayer (1.8 damage bonus) to a weapon or magic caster that does not already have one. |
| Skill and Attribute Reset Gem | 100 | Clears quest stamps for the Temple of Enlightenment and Temple of Forgetfulness. Each use costs an escalating number of PK Trophies (see below). Bonded & Attuned. |

---

#### Anti Parazi

**Anti Parazi** is a vendor located in the Abandoned Mine alongside Darkbeat. He accepts **PK Trophies** as currency (not pyreals) and sells bounty consumables and item requirement morph gems. PK Trophies are earned at a higher rate than Phials, reflected in Anti Parazi's pricing.

| Item | Cost (PK Trophies) | Description |
|------|-------------------|-------------|
| Bounty Purchase Token | 100 | Used to purchase a Bounty Contract from the Bounty Hunter NPC. |
| Writ of Pursuit | 200 | Inscribe with `PlayerName:Amount` and turn in to flag a player as a High Priority Target. |
| Workmanship Morph Gem | 300 | Randomizes the Workmanship of a loot item (1–10). |
| Arcane Lore Morph Gem | 350 | 75% chance to reduce Arcane Lore requirement by 5–25; 15% chance of no effect; 10% chance to increase it by 5–15. |
| Missile Defense Requirement Morph Gem | 400 | Removes the Missile Defense activation requirement from an item. |
| Melee Defense Requirement Morph Gem | 400 | Removes the Melee Defense activation requirement from an item. |
| Player Wield Requirement Morph Gem | 500 | Removes the wield restriction binding an item to a specific player. |
| Level Requirement Removal Morph Gem | 750 | Removes the level requirement from armor or jewelry (cannot be used on weapons). |

> **Impenetrability Morph Gem** — no longer sold by either vendor. Obtainable only from **Mythic Mystery Boxes**.

---

#### Custom Character Titles — `/buytitle`

Spend PK Trophies to give your character a **custom title**. Use `/BuyTitle <New Title>` in game — the title is applied to your character immediately and costs **200 PK Trophies** per purchase. New titles are screened against the server's taboo word filter, so disallowed words are rejected.

---

#### Darkbeat's Storage Locker

The Storage Locker is a locked chest that always contains one tier 6 loot item and up to three randomly selected bonus items per opening. Each opening also has an independent **~20% chance to contain a Sturdy Iron Key**.

Each opening makes three independent rolls from the bonus table. Each roll has a 10% cumulative chance to land on a salvage bag, distributed evenly across 11 salvage types (~0.91% each):

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

All salvage bags are full WS10 (100-unit) bags. Other possible bonus items include foolproof tinkering gems, Trade Notes, PK Trophies, Phials of Bloody Tears, consumables, and Massive Mana Stones.

#### Skill and Attribute Reset Gem — PK Trophy Cost

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

### 📦 Mystery Boxes

The Common, Rare, and Mythic Mystery Boxes each contain a weighted loot table of currencies, salvage, and morph gems.

#### Common Mystery Box

| Item | Chance |
|------|--------|
| Workmanship Morph Gem | ~2.4% |
| Missile Defense Requirement Morph Gem | ~2.4% |
| Melee Requirement Morph Gem | ~2.4% |
| Player Wield Requirement Morph Gem | ~2.4% |
| Level Requirement Removal Morph Gem | ~2.4% |
| Darkbeat's Lost Storage Key | ~7.3% |
| Sturdy Iron Key | ~7.3% |
| Arcane Lore Morph Gem | ~7.3% |
| Steel Salvage (WS10, 100 units) | ~7.3% |
| Granite Salvage (WS10, 100 units) | ~7.3% |
| Iron Salvage (WS10, 100 units) | ~7.3% |
| Green Garnet Salvage (WS10, 100 units) | ~7.3% |
| Opal Salvage (WS10, 100 units) | ~7.3% |
| Rare Mystery Box | ~7.3% |
| MMDs ×5 | ~7.3% |
| PK Trophies ×10 | ~7.3% |
| Bounty Purchase Token | ~7.3% |

#### Rare Mystery Box

| Item | Chance |
|------|--------|
| Ancient Bottle (XP Bottle) | ~2.0% |
| Workmanship Morph Gem | ~6.0% |
| Missile Defense Requirement Morph Gem | ~6.0% |
| Melee Requirement Morph Gem | ~6.0% |
| Player Wield Requirement Morph Gem | ~6.0% |
| Level Requirement Removal Morph Gem | ~6.0% |
| Sunstone Salvage WS10 — Armor Rend | ~4.0% |
| Red Garnet Salvage WS10 — Fire Rend | ~4.0% |
| Black Garnet Salvage WS10 — Pierce Rend | ~4.0% |
| Imperial Topaz Salvage WS10 — Slash Rend | ~4.0% |
| Jet Salvage WS10 — Lightning Rend | ~4.0% |
| Aquamarine Salvage WS10 — Cold Rend | ~4.0% |
| White Sapphire Salvage WS10 — Bludgeon Rend | ~4.0% |
| Emerald Salvage WS10 — Acid Rend | ~4.0% |
| Fire Opal Salvage WS10 — Crippling Blow | ~4.0% |
| Black Opal Salvage WS10 — Critical Strike | ~4.0% |
| Bloodstone Salvage WS10 — Minor Endurance (jewelry only) | ~4.0% |
| Sturdy Iron Keys ×3 | ~6.0% |
| Mythic Mystery Box | ~6.0% |
| MMDs ×20 | ~6.0% |
| PK Trophies ×100 | ~6.0% |

All salvage bags are full WS10 bags (100 units).

#### Mythic Mystery Box

| Item | Chance |
|------|--------|
| Ancient Bottle (XP Bottle) | ~5.0% |
| Impenetrability Morph Gem | ~15.0% |
| Slayer Upgrade Gem | ~15.0% |
| Skill and Attribute Reset Gem | ~15.0% |
| Imbue Altering Morph Gem | ~15.0% |
| MMDs ×50 | ~15.0% |
| PK Trophies ×1000 | ~15.0% |
| Shimmering Skeleton Key | ~5.0% |

---

### 🔒 One-Account-Per-IP Enforcement

ClassicPvP now enforces that each IP address can only be associated with one account, helping prevent account sharing while staying fair to players with dynamic IPs or VPN hiccups.

#### How It Works

Every time you log in, your IP is recorded against your account. If you log in from a new IP — because your ISP changed it, you switched networks, or anything else — that IP is simply added to your account's list and login proceeds normally. There is no penalty for IP changes.

What **is** blocked: if an IP you're connecting from is already registered to a **different** account, your login will be rejected with a message to contact an admin. This is the core protection against account sharing.

#### If You're Blocked

If you receive a message saying your IP is registered to another account, contact an administrator. Common legitimate causes:

- A household member plays on the same internet connection
- You're connecting from a location (library, café, friend's house) that another player has also used
- A VPN exit node was previously used by another player

Admins can review the binding history and whitelist your IP or clear conflicting bindings as appropriate.

#### For Admins

See **Section 1** of the Admin Guide for full details on the `enforce_account_ip_binding` property, the IP whitelist, and the `/checkipbinding` and `/clearipbinding` commands.

---

### 🐗 Tusker Tusk & Olthoi Pincer Turn-In Timers

The repeat timer on the Tusker Tusk and Olthoi Pincer turn-in quests has been shortened from **21 days** to **20 hours**. You can now farm and turn in these tusks and pincers far more frequently instead of waiting weeks between rewards.

This covers all 14 Tusker Tusk turn-ins and all 8 Olthoi Pincer turn-ins (Harvester, Gardener, Soldier, Legionary, Eviscerator, Worker, Warrior, and Mutilator pincers turned in to Behdo Yii).

---

### 🔧 Tinker Characters — `/FlagTinker`

You can now dedicate a character to be a **pure crafting specialist** using the `/FlagTinker` command. A Tinker is a support/crafting alt with every tinkering and crafting skill maxed out — perfect for salvaging, imbuing, and tinkering gear for yourself and your allegiance without having to level a combat character first.

#### How to Flag a Tinker

Log in a **brand-new level 1 character** and type `/FlagTinker`. That's it. The conversion is applied instantly.

**Requirements:**
- The character must be **level 1** (a character that has already earned levels cannot be converted).
- Your account must **not already have a Tinker** — you get **one Tinker per account**.

> ⚠️ **This is permanent and irreversible.** There is no un-flag command. Only run `/FlagTinker` on a character you intend to keep as a dedicated crafter.

#### What You Get

When you flag a Tinker, the character is instantly transformed:

- ✅ **All eight crafting skills are specialized and maxed** — Item Tinkering, Weapon Tinkering, Armor Tinkering, Magic Item Tinkering, Alchemy, Lockpick, Fletching, and Cooking.
- ✅ **All attributes are maxed** (Strength, Endurance, Coordination, Quickness, Focus, Self) and your health, stamina, and mana are refreshed to full.
- ✅ **A Tinkering Trinket** is placed in your inventory.
- ❌ **All combat skills are removed** — every weapon skill, shield, and all offensive magic (War, Void, Life, Creature Enchantment, Item Enchantment) is untrained. A Tinker is not built to fight.

#### Living as a Tinker

- 🛡️ **No vitae on death.** Tinker characters never suffer the vitae experience penalty when they die — a mistake at the crafting bench or a stray death costs you nothing.
- 🔒 **Skills are locked.** A Tinker cannot train or specialize any new skills. Your crafting kit is set the moment you flag, and that's your loadout for good.
- 👑 **No allegiance passup.** A Tinker does not pass XP up the allegiance chain to its patron.

The intent is simple: a Tinker is a maxed-out crafting workstation in character form. Flag one, park it in your allegiance, and let it handle all your tinkering, salvaging, and item work.
