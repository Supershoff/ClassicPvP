/* Random Dungeon Boss: Nharim Dul, the Whispering Death (hybrid war/life caster)
   Model/movement/sound mirrored from Shadow Captain (wcid 6554). Combat stats authored at reference level
   275, scaled to the season level cap at spawn (DungeonBossManager.ScaleBossToCap).
   Radar hidden. Rewards (scattered currency + XP) handled in code. For normal
   generated loot, set a valid DeathTreasureType DID (type 35) on the commented line. */

DELETE FROM `weenie` WHERE `class_Id` = 940005;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (940005, 'dungeonbossnharimdul', 10, '2026-08-07 00:00:00') /* Creature */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (940005,   1,         16) /* ItemType - Creature */
     , (940005,   2,         22) /* CreatureType - Shadow (matches Shadowy Warrior) */
     , (940005,   3,         39) /* PaletteTemplate - Black (matches Shadow Captain) */
     , (940005,   6,         -1) /* ItemsCapacity */
     , (940005,   7,         -1) /* ContainersCapacity */
     , (940005,  16,          1) /* ItemUseable - No */
     , (940005,  25,        275) /* Level (reference; overwritten to level cap at spawn) */
     , (940005,  27,          0) /* ArmorType - None */
     , (940005,  40,          1) /* CombatMode - NonCombat */
     , (940005,  68,         13) /* TargetingTactic - Random, LastDamager, TopDamager */
     , (940005,  93,    4195336) /* PhysicsState - ReportCollisions, Gravity, EdgeSlide (matches Shadow Captain) */
     , (940005, 101,        183) /* AiAllowedCombatStyle - Unarmed, OneHanded, OneHandedAndShield, Bow, Crossbow, ThrownWeapon */
     , (940005, 133,          1) /* ShowableOnRadar - ShowNever (hunt for it) */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (940005,   1, False) /* Stuck */
     , (940005,   6, False) /* AiUsesMana */
     , (940005,  11, False) /* IgnoreCollisions */
     , (940005,  12, True ) /* ReportCollisions */
     , (940005,  13, False) /* Ethereal */
     , (940005,  19, True ) /* Attackable */
     , (940005,  52, False) /* AiImmobile */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (940005,   1,      5) /* HeartbeatInterval */
     , (940005,   2,      0) /* HeartbeatTimestamp */
     , (940005,   3,     10) /* HealthRate */
     , (940005,   4,    100) /* StaminaRate */
     , (940005,   5,     50) /* ManaRate */
     , (940005,  31,     45) /* VisualAwarenessRange */
     , (940005,  39,      1) /* DefaultScale - Shadow Captain's natural size */
     , (940005,  54,      5) /* UseRadius */
     , (940005,  55,     70) /* HomeRadius */
     , (940005,  80,    1.5) /* AiUseMagicDelay - casts more often (war DPS lever) */
     , (940005, 104,     45) /* ObviousRadarRange */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (940005,   1, 'Nharim Dul, the Whispering Death') /* Name */
     , (940005,   5, 'Dungeon Boss') /* Template */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
/* Appearance/movement/sound mirrored from Shadow Captain (wcid 6554).
   Uses the dedicated shadow setup 0x0200071B (shared by Umbris Shadow, Shadow Captain,
   Shadow Wraith, ...) rather than the generic human setup 0x02000001 the Shadowy Warrior
   used: a human setup carries no geometry of its own, so when the dat has no clothing
   entry for it the creature renders as an untextured naked human. */
VALUES (940005,   1, 0x0200071B) /* Setup */
     , (940005,   2, 0x09000093) /* MotionTable */
     , (940005,   3, 0x20000002) /* SoundTable */
     , (940005,   4, 0x30000000) /* CombatTable */
     , (940005,   6, 0x0400007E) /* PaletteBase */
     , (940005,   7, 0x1000019F) /* ClothingBase */
     , (940005,   8, 0x06001BBE) /* Icon */
     , (940005,  22, 0x34000063) /* PhysicsEffectTable */
     , (940005,  35,     940000) /* DeathTreasureType - Dungeon Boss Loot Profile */;

INSERT INTO `weenie_properties_attribute` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`)
VALUES (940005,   1, 380, 0, 0) /* Strength */
     , (940005,   2, 420, 0, 0) /* Endurance */
     , (940005,   3, 400, 0, 0) /* Quickness */
     , (940005,   4, 400, 0, 0) /* Coordination */
     , (940005,   5, 560, 0, 0) /* Focus */
     , (940005,   6, 560, 0, 0) /* Self */;

INSERT INTO `weenie_properties_attribute_2nd` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`, `current_Level`)
VALUES (940005,   1, 40000, 0, 0, 40000) /* MaxHealth */
     , (940005,   3, 50000, 0, 0, 50000) /* MaxStamina */
     , (940005,   5, 60000, 0, 0, 60000) /* MaxMana */;

INSERT INTO `weenie_properties_skill` (`object_Id`, `type`, `level_From_P_P`, `s_a_c`, `p_p`, `init_Level`, `resistance_At_Last_Check`, `last_Used_Time`)
VALUES (940005,  6, 0, 3, 0, 300, 0, 0) /* MeleeDefense        Specialized */
     , (940005,  7, 0, 3, 0, 300, 0, 0) /* MissileDefense      Specialized */
     , (940005, 15, 0, 3, 0, 300, 0, 0) /* MagicDefense        Specialized */
     , (940005, 31, 0, 3, 0, 300, 0, 0) /* CreatureEnchantment Specialized */
     , (940005, 33, 0, 3, 0, 300, 0, 0) /* LifeMagic           Specialized */
     , (940005, 34, 0, 3, 0, 300, 0, 0) /* WarMagic            Specialized */;

/* Probability convention (Monster_Magic.GetProbabilityAny): a value > 2.0 is cast
   chance (value - 2.0), so 2.10 = 10% per attack. Every spell id below is verified
   present in the `spell` table - a missing row makes Spell._spell null and throws
   a NullReferenceException on every Monster_Tick. */
INSERT INTO `weenie_properties_spell_book` (`object_Id`, `spell`, `probability`)
VALUES (940005,   80, 2.16)  /* Lightning Bolt VI */
     , (940005,  106, 2.10)  /* Shock Blast VI */
     , (940005, 2738, 2.10)  /* Lightning Arc VII */
     , (940005, 1242, 2.05)  /* Drain Health Other VI */
     , (940005, 1089, 2.10)  /* Lightning Vulnerability Other VI */;

/* base_Armor is the ONLY physical mitigation input for creatures: melee/missile use
   SkillFormula.CalcArmorMod(base_Armor * ArmorModVs<Type>), where ArmorModVs* are
   creature-level float properties defaulting to 1.0. The armor_Vs_* columns below are
   read only for ARMOR ITEMS (Armor.cs) and are inert on a creature - do not tune them.
   Spell damage never touches armor at all (it uses GetResistanceMod), so base_Armor
   adjusts melee/missile without affecting magic. */
INSERT INTO `weenie_properties_body_part` (`object_Id`, `key`, `d_Type`, `d_Val`, `d_Var`, `base_Armor`, `armor_Vs_Slash`, `armor_Vs_Pierce`, `armor_Vs_Bludgeon`, `armor_Vs_Cold`, `armor_Vs_Fire`, `armor_Vs_Acid`, `armor_Vs_Electric`, `armor_Vs_Nether`, `b_h`, `h_l_f`, `m_l_f`, `l_l_f`, `h_r_f`, `m_r_f`, `l_r_f`, `h_l_b`, `m_l_b`, `l_l_b`, `h_r_b`, `m_r_b`, `l_r_b`)
VALUES (940005,  0,  4,   0,    0,  320, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0) /* Head */
     , (940005,  1,  4,   0,    0,  320, 700, 700, 700, 700, 700, 700, 700, 700, 2, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0) /* Chest */
     , (940005,  2,  4,   0,    0,  320, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0) /* Abdomen */
     , (940005,  3,  4,   0,    0,  320, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0) /* UpperArm */
     , (940005,  4,  4,   0,    0,  320, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0) /* LowerArm */
     , (940005,  5,  4,  115, 0.75,  320, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0) /* Hand */
     , (940005,  6,  4,   0,    0,  320, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18) /* UpperLeg */
     , (940005,  7,  4,   0,    0,  320, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6) /* LowerLeg */
     , (940005,  8,  4,  115, 0.75,  320, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22) /* Foot */;
