using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;

using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.DungeonBoss;
using ACE.Server.Factories;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Managers
{
    /// <summary>
    /// One currently-active dungeon boss, tracked per landblock.
    /// </summary>
    public class ActiveDungeonBoss
    {
        public ushort Landblock { get; set; }
        public uint WeenieId { get; set; }
        public string Name { get; set; }
        public Creature Boss { get; set; }
        public DateTime SpawnTime { get; set; }

        /// <summary>True until the boss has successfully entered the world (see ConfirmBossSpawn).</summary>
        public bool Pending { get; set; }
    }

    /// <summary>
    /// Random Dungeon Bosses.
    ///
    /// Hijacks normal monster spawns from generators (see GeneratorProfile.Spawn) inside
    /// active Hot Dungeons or the Abandoned Mine, and — subject to a global cooldown, a
    /// one-boss-per-landblock rule, a no-duplicate-weenie rule and a low roll chance —
    /// replaces the monster with a boss whose combat stats are scaled to the current
    /// season level cap. A location-free global message is broadcast on spawn; on death
    /// a set amount of currency is scattered on the ground and a slain message is sent.
    /// </summary>
    public static class DungeonBossManager
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>Abandoned Mine (Subway) landblock — always eligible in addition to Hot Dungeons.</summary>
        public const ushort AbandonedMineLandblock = 0x01C9;

        private const float ScatterRadius = 3.0f;
        private const double StaleGraceSeconds = 15.0;   // don't reap a boss racing its own EnterWorld
        private const double TickIntervalSeconds = 5.0;

        // Registry of active bosses, keyed by landblock (enforces one boss per landblock).
        private static readonly ConcurrentDictionary<ushort, ActiveDungeonBoss> _activeBosses = new ConcurrentDictionary<ushort, ActiveDungeonBoss>();

        // Guards the stateful spawn gates (cooldown / landblock / weenie availability / roll / register).
        private static readonly object _spawnLock = new object();

        // Global timestamp (unix) of the most recent successful boss spawn.
        private static double _lastBossSpawnTimeUnix = 0;

        private static DateTime _lastTick = DateTime.MinValue;

        // ── Spawn hijack ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called from GeneratorProfile.Spawn() for each freshly created spawn object.
        /// If all gates pass, replaces <paramref name="wo"/> with a scaled boss creature
        /// (not yet in world — the caller positions and EnterWorld()s it as normal) and
        /// returns true. Returns false (the overwhelming majority of the time) to leave
        /// the normal monster untouched.
        /// </summary>
        public static bool TryPromoteToBoss(WorldObject generator, ref WorldObject wo)
        {
            try
            {
                if (!PropertyManager.GetBool("dungeon_boss_enabled").Item)
                    return false;

                if (ConfigManager.Config.Server.WorldRuleset != Ruleset.Infiltration)
                    return false;

                // Only replace hostile monsters (not players, NPCs, vendors, items, etc.)
                if (!(wo is Creature) || wo is Player || !wo.Attackable)
                    return false;

                if (generator?.Location == null)
                    return false;

                var landblock = (ushort)generator.Location.Landblock;
                if (!IsEligibleLandblock(landblock))
                    return false;

                lock (_spawnLock)
                {
                    var now = Time.GetUnixTime();

                    // Global cooldown between any two boss spawns.
                    var minBetween = PropertyManager.GetLong("dungeon_boss_min_seconds_between").Item;
                    if (now < _lastBossSpawnTimeUnix + minBetween)
                        return false;

                    // One boss per landblock.
                    if (_activeBosses.ContainsKey(landblock))
                        return false;

                    // No two bosses of the same weenie active at once.
                    var activeWeenieIds = _activeBosses.Values.Select(b => b.WeenieId).ToHashSet();
                    var def = DungeonBosses.RollAvailableBoss(activeWeenieIds);
                    if (def == null)
                        return false;

                    // Roll the (low) spawn chance.
                    var chance = PropertyManager.GetDouble("dungeon_boss_spawn_chance").Item;
                    if (ThreadSafeRandom.Next(0f, 1f) > chance)
                        return false;

                    // Build the boss.
                    var bossWeenie = DatabaseManager.World.GetCachedWeenie(def.WeenieId);
                    if (bossWeenie == null)
                    {
                        log.Error($"DungeonBossManager: boss weenie {def.WeenieId} ({def.Name}) not found in world database — cannot spawn.");
                        return false;
                    }

                    if (!(WorldObjectFactory.CreateNewWorldObject(bossWeenie) is Creature boss))
                    {
                        log.Error($"DungeonBossManager: failed to create boss creature for weenie {def.WeenieId} ({def.Name}).");
                        return false;
                    }

                    var levelCap = GetCurrentLevelCap();
                    ScaleBossToCap(boss, levelCap, def);

                    _activeBosses[landblock] = new ActiveDungeonBoss
                    {
                        Landblock = landblock,
                        WeenieId  = def.WeenieId,
                        Name      = def.Name,
                        Boss      = boss,
                        SpawnTime = DateTime.UtcNow,
                        Pending   = true,   // cooldown + broadcast happen on ConfirmBossSpawn
                    };

                    // Hand the boss back to the generator in place of the normal monster.
                    // The caller positions and EnterWorld()s it, then calls ConfirmBossSpawn.
                    wo = boss;

                    log.Info($"DungeonBossManager: promoted spawn to {def.Name} (wcid {def.WeenieId}) on landblock 0x{landblock:X4} at level cap {levelCap} (pending EnterWorld).");
                    return true;
                }
            }
            catch (Exception ex)
            {
                log.Error($"DungeonBossManager.TryPromoteToBoss exception: {ex}");
                return false;
            }
        }

        private static bool IsEligibleLandblock(ushort landblock)
        {
            if (landblock == AbandonedMineLandblock)
                return true;

            return HotDungeonManager.IsHotDungeon(landblock, out _);
        }

        /// <summary>
        /// Called from GeneratorProfile.Spawn() after a promoted boss has been positioned
        /// and EnterWorld()'d. On success: starts the global cooldown and sends the spawn
        /// broadcast. On failure (e.g. the model/setup isn't in the dat): releases the slot
        /// immediately without consuming the cooldown or announcing a phantom boss, and logs
        /// it so bad model data is visible.
        /// </summary>
        public static void ConfirmBossSpawn(WorldObject wo, bool success)
        {
            try
            {
                var entry = _activeBosses.Values.FirstOrDefault(b => ReferenceEquals(b.Boss, wo));
                if (entry == null)
                    return;

                if (success)
                {
                    entry.Pending = false;
                    _lastBossSpawnTimeUnix = Time.GetUnixTime();

                    var def = DungeonBosses.Get(entry.WeenieId);
                    if (def != null)
                        Broadcast(def.SpawnMessage);

                    log.Info($"DungeonBossManager: {entry.Name} (wcid {entry.WeenieId}) entered the world on 0x{entry.Landblock:X4}.");
                }
                else
                {
                    _activeBosses.TryRemove(entry.Landblock, out _);
                    log.Warn($"DungeonBossManager: {entry.Name} (wcid {entry.WeenieId}) FAILED to enter the world on 0x{entry.Landblock:X4} " +
                             $"(likely its model/setup is not present in this dat). Slot released; no cooldown consumed.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"DungeonBossManager.ConfirmBossSpawn exception: {ex}");
            }
        }

        // ── Scaling ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Continuous stat scaling. The boss weenie is authored at
        /// <see cref="DungeonBosses.ReferenceLevel"/>; every combat-relevant stat is
        /// scaled by a smooth function of the current level cap, then by the boss's
        /// archetype multipliers and the global difficulty knob. No level bands.
        /// </summary>
        public static void ScaleBossToCap(Creature boss, int levelCap, DungeonBossDef def)
        {
            if (boss == null || def == null)
                return;

            if (levelCap <= 0)
                levelCap = DungeonBosses.ReferenceLevel;

            StripUnresolvableSpells(boss, def);

            var difficulty = (float)PropertyManager.GetDouble("dungeon_boss_difficulty_mult").Item;
            var healthExp  = (float)PropertyManager.GetDouble("dungeon_boss_health_exponent").Item;
            var dmgMult    = (float)PropertyManager.GetDouble("dungeon_boss_damage_mult").Item;
            var armorMult  = (float)PropertyManager.GetDouble("dungeon_boss_armor_mult").Item;
            if (difficulty <= 0) difficulty = 1.0f;

            // 1.0 at the reference level; floored so early-season bosses don't round to nothing.
            float capRatio = Math.Max(0.10f, (float)levelCap / DungeonBosses.ReferenceLevel);

            // ── Attributes: linear with cap ──
            float attrFactor = capRatio * difficulty;
            foreach (var attr in boss.Attributes.Values)
                attr.StartingValue = ScaleU(attr.StartingValue, attrFactor, 10);

            // ── Vitals: health superlinear (main tankiness knob), stamina/mana linear ──
            float healthFactor = (float)Math.Pow(capRatio, healthExp) * def.HealthMult * difficulty;
            ScaleVital(boss, PropertyAttribute2nd.MaxHealth,  healthFactor);
            ScaleVital(boss, PropertyAttribute2nd.MaxStamina, capRatio * difficulty);
            ScaleVital(boss, PropertyAttribute2nd.MaxMana,    capRatio * difficulty);

            // ── Skills: set effective TOTAL skill levels, split offense/defense ──
            // A creature's skill Base = attribute-derived portion + InitLevel (+ Ranks=0).
            // We compute a TARGET total (what actually governs hit chance and magic-resist
            // frequency) and then subtract the attribute contribution, so the final level
            // tracks the target regardless of the boss's scaled attributes. Defenses are
            // kept deliberately below a near-maxed player's offense so bosses resist/evade
            // sometimes but not constantly; tune live with dungeon_boss_defense_mult.
            var defenseMult = (float)PropertyManager.GetDouble("dungeon_boss_defense_mult").Item;
            if (defenseMult <= 0) defenseMult = 1.0f;

            uint targetOffense = (uint)Math.Max(1, (100 + levelCap * 1.7)  * def.OffenseMult * difficulty);
            uint targetDefense = (uint)Math.Max(1, ( 90 + levelCap * 1.15) * def.DefenseMult * defenseMult);
            foreach (var skill in boss.Skills.Values)
            {
                uint target;
                if (DefenseSkills.Contains(skill.Skill))
                    target = targetDefense;
                else if (OffenseSkills.Contains(skill.Skill))
                    target = targetOffense;
                else
                    continue;   // leave non-combat skills (Run, Jump, etc.) untouched

                var attrPart = AttributeFormula.GetFormula(boss, skill.Skill, false);
                skill.InitLevel = (uint)Math.Max(0, (int)target - (int)attrPart);
            }

            // ── Body parts: melee damage + natural armor ──
            // BaseArmor mitigates melee/missile only — CreatureBodyPart.GetEffectiveArmorVsType
            // feeds it through SkillFormula.CalcArmorMod, while spell damage goes through
            // GetResistanceMod and never touches armor. So armor_mult tunes weapon tankiness
            // in isolation, whereas difficulty_mult moves health/damage/skills with it.
            float bodyDmgFactor = capRatio * def.DamageMult * dmgMult * difficulty;
            float armorFactor   = (float)Math.Sqrt(capRatio) * def.ArmorMult * difficulty * armorMult;
            if (boss.Biota.PropertiesBodyPart != null)
            {
                GivePrivateBodyParts(boss);

                foreach (var bp in boss.Biota.PropertiesBodyPart.Values)
                {
                    bp.DVal            = ScaleI(bp.DVal, bodyDmgFactor);
                    bp.BaseArmor       = ScaleI(bp.BaseArmor, armorFactor);
                    bp.ArmorVsSlash    = ScaleI(bp.ArmorVsSlash, armorFactor);
                    bp.ArmorVsPierce   = ScaleI(bp.ArmorVsPierce, armorFactor);
                    bp.ArmorVsBludgeon = ScaleI(bp.ArmorVsBludgeon, armorFactor);
                    bp.ArmorVsCold     = ScaleI(bp.ArmorVsCold, armorFactor);
                    bp.ArmorVsFire     = ScaleI(bp.ArmorVsFire, armorFactor);
                    bp.ArmorVsAcid     = ScaleI(bp.ArmorVsAcid, armorFactor);
                    bp.ArmorVsElectric = ScaleI(bp.ArmorVsElectric, armorFactor);
                    bp.ArmorVsNether   = ScaleI(bp.ArmorVsNether, armorFactor);
                }
            }

            // ── Level (examine) + XP override (kill reward) ──
            boss.SetProperty(PropertyInt.Level, levelCap);
            long xp = (long)(levelCap * levelCap * 200L * difficulty);
            boss.SetProperty(PropertyInt.XpOverride, (int)Math.Min(Math.Max(xp, 1L), int.MaxValue));

            // Spawn at full, recomputed vitals.
            boss.Vitals[PropertyAttribute2nd.MaxHealth].Current  = boss.Vitals[PropertyAttribute2nd.MaxHealth].MaxValue;
            boss.Vitals[PropertyAttribute2nd.MaxStamina].Current = boss.Vitals[PropertyAttribute2nd.MaxStamina].MaxValue;
            boss.Vitals[PropertyAttribute2nd.MaxMana].Current    = boss.Vitals[PropertyAttribute2nd.MaxMana].MaxValue;
        }

        private static void ScaleVital(Creature boss, PropertyAttribute2nd vital, float factor)
        {
            if (boss.Vitals.TryGetValue(vital, out var v))
                v.StartingValue = ScaleU(v.StartingValue, factor, 1);
        }

        private static uint ScaleU(uint value, float factor, uint min)
        {
            var scaled = (long)Math.Round(value * (double)factor);
            if (scaled < min) scaled = min;
            if (scaled > uint.MaxValue) scaled = uint.MaxValue;
            return (uint)scaled;
        }

        private static int ScaleI(int value, float factor)
        {
            return (int)Math.Round(value * (double)factor);
        }

        private static readonly HashSet<Skill> DefenseSkills = new HashSet<Skill>
        {
            Skill.MeleeDefense, Skill.MissileDefense, Skill.MagicDefense,
        };

        private static readonly HashSet<Skill> OffenseSkills = new HashSet<Skill>
        {
            // magic schools
            Skill.WarMagic, Skill.LifeMagic, Skill.CreatureEnchantment, Skill.ItemEnchantment, Skill.VoidMagic,
            // retired-era weapon skills (Infiltration ruleset)
            Skill.Axe, Skill.Bow, Skill.Crossbow, Skill.Dagger, Skill.Mace, Skill.Sling,
            Skill.Spear, Skill.Staff, Skill.Sword, Skill.ThrownWeapon, Skill.UnarmedCombat,
            // modern weapon skills (in case a template uses them)
            Skill.HeavyWeapons, Skill.LightWeapons, Skill.FinesseWeapons, Skill.MissileWeapons, Skill.TwoHandedCombat,
        };

        // ── Death ────────────────────────────────────────────────────────────────

        /// <summary>Returns true if this creature instance is a currently-tracked dungeon boss.</summary>
        public static bool IsActiveBoss(Creature creature)
        {
            if (creature == null || !DungeonBosses.IsDungeonBoss(creature.WeenieClassId))
                return false;

            return _activeBosses.Values.Any(b => ReferenceEquals(b.Boss, creature));
        }

        /// <summary>
        /// Called from Creature.OnDeath when a tracked dungeon boss dies. Frees the slot,
        /// broadcasts the slain message (killer named, location withheld) and scatters
        /// the configured currency reward on the ground around the corpse.
        /// </summary>
        public static void HandleBossDeath(Creature boss, DamageHistoryInfo killer)
        {
            try
            {
                var entry = _activeBosses.Values.FirstOrDefault(b => ReferenceEquals(b.Boss, boss));
                if (entry == null)
                    return;

                _activeBosses.TryRemove(entry.Landblock, out _);

                var killerName = string.IsNullOrWhiteSpace(killer?.Name) ? "an unknown challenger" : killer.Name;
                Broadcast($"{entry.Name} has been slain by {killerName}! The threat has passed... for now.");
                log.Info($"DungeonBossManager: {entry.Name} (wcid {entry.WeenieId}) slain on 0x{entry.Landblock:X4} by {killerName}.");

                GrantRewards(boss, entry.Name);
            }
            catch (Exception ex)
            {
                log.Error($"DungeonBossManager.HandleBossDeath exception: {ex}");
            }
        }

        private static void GrantRewards(Creature boss, string bossName)
        {
            if (boss?.Location == null)
                return;

            // A Box is not bonded — scatter it on the ground around the corpse (contestable).
            ScatterItem(boss.Location, CustomWeenieId.ABox, (int)PropertyManager.GetLong("dungeon_boss_box_count").Item);

            // PK Trophies (Bonded) and Phials (Bonded + Attuned) cannot be placed on the
            // ground, so award them directly to the inventory of each player who damaged
            // the boss.
            var trophyCount = (int)PropertyManager.GetLong("dungeon_boss_trophy_count").Item;
            var phialCount  = (int)PropertyManager.GetLong("dungeon_boss_phial_count").Item;
            if (trophyCount <= 0 && phialCount <= 0)
                return;

            foreach (var info in boss.DamageHistory.Damagers)
            {
                if (info == null || !info.IsPlayer)
                    continue;
                if (!(info.TryGetAttacker() is Player player))
                    continue;

                AwardCurrency(player, CustomWeenieId.PkTrophy,           trophyCount, bossName);
                AwardCurrency(player, CustomWeenieId.PhialOfBloodyTears, phialCount,  bossName);
            }
        }

        private static void AwardCurrency(Player player, uint wcid, int count, string bossName)
        {
            if (count <= 0 || player == null)
                return;

            var item = WorldObjectFactory.CreateNewWorldObject(wcid);
            if (item == null)
                return;

            if (count > 1)
                item.SetStackSize(count);

            if (player.TryCreateInInventoryWithNetworking(item))
                player.Session?.Network?.EnqueueSend(new GameMessageSystemChat($"You receive {count}x {item.Name} for your part in slaying {bossName}!", ChatMessageType.Broadcast));
            else
                item.Destroy();   // inventory full — Bonded items can't be dropped to the ground
        }

        private static void ScatterItem(Position deathLoc, uint wcid, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = WorldObjectFactory.CreateNewWorldObject(wcid);
                if (item == null)
                {
                    log.Warn($"DungeonBossManager: failed to create reward item wcid {wcid} — skipping remaining.");
                    return;
                }

                item.Location = new Position(deathLoc);
                item.Location.PositionZ += 0.05f;
                item.ScatterPos = new ACE.Server.Physics.Common.SetPosition(
                    new ACE.Server.Physics.Common.Position(item.Location),
                    ACE.Server.Physics.Common.SetPositionFlags.RandomScatter, ScatterRadius);

                var success = item.EnterWorld();
                item.ScatterPos = null;

                if (!success)
                    item.Destroy();
            }
        }

        // ── Tick (safety sweep) ─────────────────────────────────────────────────────

        /// <summary>
        /// Wired into WorldManager.Tick(). Releases the slot for any boss whose world
        /// object has been destroyed / its landblock unloaded (e.g. all players left,
        /// or a rare failed EnterWorld), and for any boss older than the configured
        /// max age. Killed bosses are removed synchronously in HandleBossDeath, so this
        /// only cleans up non-death disappearances.
        /// </summary>
        public static void Tick()
        {
            if (_activeBosses.IsEmpty)
                return;

            if (DateTime.UtcNow.AddSeconds(-TickIntervalSeconds) < _lastTick)
                return;

            _lastTick = DateTime.UtcNow;

            try
            {
                var maxAgeHours = PropertyManager.GetLong("dungeon_boss_max_age_hours").Item;
                var now = DateTime.UtcNow;

                foreach (var kvp in _activeBosses.ToArray())
                {
                    var entry = kvp.Value;
                    var boss  = entry.Boss;

                    var ageSeconds = (now - entry.SpawnTime).TotalSeconds;

                    // A pending boss hasn't entered the world yet (CurrentLandblock is null by
                    // design); ConfirmBossSpawn resolves it synchronously, so don't reap it early.
                    if (entry.Pending && ageSeconds < 30)
                        continue;

                    var stale  = boss == null || boss.IsDestroyed || boss.CurrentLandblock == null;
                    var tooOld = ageSeconds > maxAgeHours * 3600.0;

                    if ((stale && ageSeconds > StaleGraceSeconds) || tooOld)
                    {
                        if (_activeBosses.TryRemove(kvp.Key, out _))
                            log.Info($"DungeonBossManager: released slot for {entry.Name} on 0x{kvp.Key:X4} (stale={stale}, tooOld={tooOld}, age={(int)ageSeconds}s).");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"DungeonBossManager.Tick exception: {ex}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static int GetCurrentLevelCap()
        {
            var xpCap = RollingLevelCapManager.GetCurrentXpCap();
            if (xpCap <= 0)
                return DungeonBosses.ReferenceLevel;

            var cap = RollingLevelCapManager.GetDisplayLevelCap(xpCap);
            return cap > 0 ? cap : DungeonBosses.ReferenceLevel;
        }

        private static void Broadcast(string msg)
        {
            PlayerManager.BroadcastToAll(new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast), suppressWebhook: true);
            PlayerManager.LogBroadcastChat(Channel.AllBroadcast, null, msg);
            DiscordWebhookManager.SendDungeonBoss(msg);
        }

        /// <summary>Read-only snapshot of currently active bosses (for admin/status display).</summary>
        public static IReadOnlyCollection<ActiveDungeonBoss> GetActiveBosses() => _activeBosses.Values.ToList();

        // ── Admin API (/dungeonboss) ────────────────────────────────────────────────

        /// <summary>
        /// Replaces the boss's body-part collection with a private deep copy before it is scaled.
        ///
        /// A spawned WorldObject does NOT get its own body parts: WorldObject's ctor calls
        /// WeenieConverter.ConvertToBiota(weenie, guid, false, referenceWeenieCollectionsForCommonProperties: true),
        /// and under that flag body parts alone are assigned by reference straight off the cached
        /// Weenie (attributes, vitals, skills and the spellbook are all cloned). Scaling BaseArmor
        /// and DVal in place therefore rewrites the CACHED WEENIE, so the next spawn scales the
        /// already-scaled numbers and the error compounds for the lifetime of the process:
        /// with Aggregate Prime's 1.6x armor archetype, armor is 1.6x after one spawn, 2.6x after
        /// two, 4.1x after three — melee and missile hit for less and less, while magic is
        /// unaffected because spell damage never reads armor.
        /// </summary>
        private static void GivePrivateBodyParts(Creature boss)
        {
            var shared = boss.Biota.PropertiesBodyPart;
            if (shared == null)
                return;

            var copy = new Dictionary<CombatBodyPart, PropertiesBodyPart>(shared.Count);
            foreach (var kvp in shared)
                copy.Add(kvp.Key, kvp.Value.Clone());

            boss.Biota.PropertiesBodyPart = copy;
        }

        /// <summary>
        /// Removes any spellbook entry whose spell id does not resolve to both a DAT SpellBase and
        /// a row in the world `spell` table. Monster_Magic does not guard against this outside the
        /// CustomDM ruleset: it constructs the Spell and dereferences it, so a single bad id throws
        /// a NullReferenceException on every Monster_Tick and the boss stops acting entirely.
        /// Dropping the entry keeps the boss functional and logs the bad data loudly.
        /// </summary>
        private static void StripUnresolvableSpells(Creature boss, DungeonBossDef def)
        {
            var spellBook = boss.Biota?.PropertiesSpellBook;
            if (spellBook == null || spellBook.Count == 0)
                return;

            List<int> bad = null;
            foreach (var entry in spellBook)
            {
                if (new Spell(entry.Key).NotFound)
                    (bad ??= new List<int>()).Add(entry.Key);
            }

            if (bad == null)
                return;

            foreach (var spellId in bad)
                spellBook.Remove(spellId);

            log.Error($"DungeonBossManager: {def.Name} (wcid {def.WeenieId}) has unresolvable spell id(s) " +
                      $"{string.Join(", ", bad)} in its spellbook - removed so the monster tick does not throw. " +
                      $"Fix the spellbook in Content/sql/weenies/DungeonBosses/.");
        }

        /// <summary>Comma-separated list of roster boss names, for admin usage/help.</summary>
        public static string RosterNames() => string.Join(", ", DungeonBosses.Roster.Select(b => b.Name));

        private static DungeonBossDef FindDef(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return null;

            if (uint.TryParse(search, out var id))
            {
                var byId = DungeonBosses.Get(id);
                if (byId != null)
                    return byId;
            }

            return DungeonBosses.Roster.FirstOrDefault(b => b.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Admin: force-spawn a boss at the given location, bypassing the roll/cooldown gates
        /// (still enforces one boss per landblock and no duplicate weenie). Does not send the
        /// global spawn broadcast, to avoid spamming players while testing. Returns a status
        /// string for the admin.
        /// </summary>
        public static string AdminSpawn(Position location, string bossName)
        {
            if (location == null)
                return "You must be in the world to spawn a dungeon boss.";

            DungeonBossDef def;
            if (string.IsNullOrWhiteSpace(bossName))
            {
                var activeWeenieIds = _activeBosses.Values.Select(b => b.WeenieId).ToHashSet();
                def = DungeonBosses.RollAvailableBoss(activeWeenieIds);
                if (def == null)
                    return "Every roster boss is already active. Remove one first, or name a specific boss.";
            }
            else
            {
                def = FindDef(bossName);
                if (def == null)
                    return $"No boss matching '{bossName}'. Options: {RosterNames()}.";
            }

            var landblock = (ushort)location.Landblock;
            if (_activeBosses.ContainsKey(landblock))
                return $"A dungeon boss is already active on landblock 0x{landblock:X4}. Use '/dungeonboss remove' first.";
            if (_activeBosses.Values.Any(b => b.WeenieId == def.WeenieId))
                return $"{def.Name} is already active elsewhere.";

            var bossWeenie = DatabaseManager.World.GetCachedWeenie(def.WeenieId);
            if (bossWeenie == null)
                return $"Boss weenie {def.WeenieId} ({def.Name}) not found in the world database. Did the SQL deploy?";

            if (!(WorldObjectFactory.CreateNewWorldObject(bossWeenie) is Creature boss))
                return $"Failed to create boss creature for {def.Name} (wcid {def.WeenieId}).";

            var levelCap = GetCurrentLevelCap();
            ScaleBossToCap(boss, levelCap, def);

            var lb = LandblockManager.GetLandblock(location.LandblockId, false);
            boss.Location = new Position(location);
            boss.CurrentLandblock = lb;

            if (!boss.EnterWorld())
            {
                boss.Destroy();
                return $"{def.Name} FAILED to enter the world at {location.ToLOCString()} — its model/setup is likely not present in this dat.";
            }

            _activeBosses[landblock] = new ActiveDungeonBoss
            {
                Landblock = landblock,
                WeenieId  = def.WeenieId,
                Name      = def.Name,
                Boss      = boss,
                SpawnTime = DateTime.UtcNow,
                Pending   = false,
            };
            _lastBossSpawnTimeUnix = Time.GetUnixTime();

            log.Info($"DungeonBossManager: admin force-spawned {def.Name} (wcid {def.WeenieId}) at {location.ToLOCString()} (cap {levelCap}).");
            return $"Spawned {def.Name} (wcid {def.WeenieId}) at level cap {levelCap}, HP {boss.Health.MaxValue:N0}. No global broadcast sent.";
        }

        /// <summary>Admin: multi-line summary of active bosses with their exact locations.</summary>
        public static string AdminList()
        {
            var bosses = _activeBosses.Values.ToList();
            if (bosses.Count == 0)
                return "No dungeon bosses are currently active.";

            var sb = new StringBuilder();
            sb.AppendLine($"Active Dungeon Bosses ({bosses.Count}):");
            foreach (var b in bosses)
            {
                var loc = b.Boss?.Location;
                var hp  = b.Boss != null ? $"{b.Boss.Health.Current:N0}/{b.Boss.Health.MaxValue:N0}" : "?";
                var lvl = b.Boss?.Level?.ToString() ?? "?";
                sb.AppendLine($"  {b.Name} (0x{b.Landblock:X4}) — lvl {lvl}, HP {hp}{(b.Pending ? " [pending]" : "")} — {(loc != null ? loc.ToLOCString() : "n/a")}");
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Admin: returns a copy of the location of an active boss (matched by name/wcid, or the
        /// first active boss if <paramref name="search"/> is empty), for teleporting to it.
        /// </summary>
        public static Position GetBossLocation(string search, out string bossName)
        {
            bossName = null;

            ActiveDungeonBoss entry;
            if (string.IsNullOrWhiteSpace(search))
                entry = _activeBosses.Values.FirstOrDefault(b => b.Boss?.Location != null);
            else
                entry = _activeBosses.Values.FirstOrDefault(b =>
                    b.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || b.WeenieId.ToString() == search);

            if (entry?.Boss?.Location == null)
                return null;

            bossName = entry.Name;
            return new Position(entry.Boss.Location);
        }

        /// <summary>Admin: despawn active boss(es) matched by name/wcid, or all if empty. No rewards.</summary>
        public static string AdminRemove(string search)
        {
            var matches = string.IsNullOrWhiteSpace(search)
                ? _activeBosses.Values.ToList()
                : _activeBosses.Values.Where(b =>
                    b.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || b.WeenieId.ToString() == search).ToList();

            if (matches.Count == 0)
                return "No matching active dungeon boss.";

            foreach (var entry in matches)
            {
                _activeBosses.TryRemove(entry.Landblock, out _);
                if (entry.Boss != null && !entry.Boss.IsDestroyed)
                    entry.Boss.Destroy();
            }

            return $"Removed {matches.Count} dungeon boss(es): {string.Join(", ", matches.Select(m => m.Name))}.";
        }
    }
}
