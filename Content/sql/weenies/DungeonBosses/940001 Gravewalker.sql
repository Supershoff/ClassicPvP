/* Random Dungeon Boss: The Gravewalker (undead melee bruiser + drain flavor)
   Model/movement/sound mirrored from Phantasm (wcid 24325). Combat stats are authored at the reference
   level (DungeonBosses.ReferenceLevel = 275) and scaled DOWN to the current season
   level cap at spawn time by DungeonBossManager.ScaleBossToCap. Radar is hidden so
   players must hunt for it. DefaultScale is the reference creature's own natural size.

   Rewards: scattered currency + big XP are handled in code (DungeonBossManager).
   For "normal generated loot", set a DeathTreasureType DID valid for your world DB
   on the commented line below (type 35). */

DELETE FROM `weenie` WHERE `class_Id` = 940001;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (940001, 'dungeonbossgravewalker', 10, '2026-08-07 00:00:00') /* Creature */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (940001,   1,         16) /* ItemType - Creature */
     , (940001,   2,         14) /* CreatureType - Undead (matches Phantasm) */
     , (940001,   3,          8) /* PaletteTemplate - Green (matches Phantasm) */
     , (940001,   6,         -1) /* ItemsCapacity */
     , (940001,   7,         -1) /* ContainersCapacity */
     , (940001,  16,          1) /* ItemUseable - No */
     , (940001,  25,        275) /* Level (reference; overwritten to level cap at spawn) */
     , (940001,  27,          0) /* ArmorType - None */
     , (940001,  40,          1) /* CombatMode - NonCombat */
     , (940001,  68,         13) /* TargetingTactic - Random, LastDamager, TopDamager */
     , (940001,  93,       1032) /* PhysicsState - ReportCollisions, Gravity */
     , (940001, 101,        183) /* AiAllowedCombatStyle - Unarmed, OneHanded, OneHandedAndShield, Bow, Crossbow, ThrownWeapon */
     , (940001, 133,          1) /* ShowableOnRadar - ShowNever (hunt for it) */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (940001,   1, False) /* Stuck */
     , (940001,   6, False) /* AiUsesMana */
     , (940001,  11, False) /* IgnoreCollisions */
     , (940001,  12, True ) /* ReportCollisions */
     , (940001,  13, False) /* Ethereal */
     , (940001,  19, True ) /* Attackable */
     , (940001,  52, False) /* AiImmobile */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (940001,   1,      5) /* HeartbeatInterval */
     , (940001,   2,      0) /* HeartbeatTimestamp */
     , (940001,   3,     10) /* HealthRate */
     , (940001,   4,    100) /* StaminaRate */
     , (940001,   5,     20) /* ManaRate */
     , (940001,  31,     40) /* VisualAwarenessRange */
     , (940001,  39,    1.1) /* DefaultScale - Phantasm's natural size (not inflated) */
     , (940001,  54,      5) /* UseRadius */
     , (940001,  55,     70) /* HomeRadius */
     , (940001, 104,     40) /* ObviousRadarRange */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (940001,   1, 'The Gravewalker') /* Name */
     , (940001,   5, 'Dungeon Boss') /* Template */;

/* Appearance/movement/sound mirrored from Phantasm (wcid 24325). */
INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (940001,   1, 0x02000197) /* Setup */
     , (940001,   2, 0x09000017) /* MotionTable */
     , (940001,   3, 0x20000016) /* SoundTable */
     , (940001,   4, 0x30000000) /* CombatTable */
     , (940001,   6, 0x04000742) /* PaletteBase */
     , (940001,   7, 0x10000066) /* ClothingBase */
     , (940001,   8, 0x06001226) /* Icon */
     , (940001,  22, 0x34000028) /* PhysicsEffectTable */
     , (940001,  35,     940000) /* DeathTreasureType - Dungeon Boss Loot Profile */;

INSERT INTO `weenie_properties_attribute` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`)
VALUES (940001,   1, 440, 0, 0) /* Strength */
     , (940001,   2, 480, 0, 0) /* Endurance */
     , (940001,   3, 360, 0, 0) /* Quickness */
     , (940001,   4, 400, 0, 0) /* Coordination */
     , (940001,   5, 380, 0, 0) /* Focus */
     , (940001,   6, 420, 0, 0) /* Self */;

INSERT INTO `weenie_properties_attribute_2nd` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`, `current_Level`)
VALUES (940001,   1, 40000, 0, 0, 40000) /* MaxHealth */
     , (940001,   3, 50000, 0, 0, 50000) /* MaxStamina */
     , (940001,   5, 50000, 0, 0, 50000) /* MaxMana */;

INSERT INTO `weenie_properties_skill` (`object_Id`, `type`, `level_From_P_P`, `s_a_c`, `p_p`, `init_Level`, `resistance_At_Last_Check`, `last_Used_Time`)
VALUES (940001,  6, 0, 3, 0, 300, 0, 0) /* MeleeDefense    Specialized */
     , (940001,  7, 0, 3, 0, 300, 0, 0) /* MissileDefense  Specialized */
     , (940001, 15, 0, 3, 0, 300, 0, 0) /* MagicDefense    Specialized */
     , (940001, 44, 0, 3, 0, 300, 0, 0) /* HeavyWeapons    Specialized */;

/* base_Armor is the ONLY physical mitigation input for creatures: melee/missile use
   SkillFormula.CalcArmorMod(base_Armor * ArmorModVs<Type>), where ArmorModVs* are
   creature-level float properties defaulting to 1.0. The armor_Vs_* columns below are
   read only for ARMOR ITEMS (Armor.cs) and are inert on a creature - do not tune them.
   Spell damage never touches armor at all (it uses GetResistanceMod), so base_Armor
   adjusts melee/missile without affecting magic. */
INSERT INTO `weenie_properties_body_part` (`object_Id`, `key`, `d_Type`, `d_Val`, `d_Var`, `base_Armor`, `armor_Vs_Slash`, `armor_Vs_Pierce`, `armor_Vs_Bludgeon`, `armor_Vs_Cold`, `armor_Vs_Fire`, `armor_Vs_Acid`, `armor_Vs_Electric`, `armor_Vs_Nether`, `b_h`, `h_l_f`, `m_l_f`, `l_l_f`, `h_r_f`, `m_r_f`, `l_r_f`, `h_l_b`, `m_l_b`, `l_l_b`, `h_r_b`, `m_r_b`, `l_r_b`)
VALUES (940001,  0,  4,   0,    0,  375, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0) /* Head */
     , (940001,  1,  4,   0,    0,  375, 700, 700, 700, 700, 700, 700, 700, 700, 2, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0) /* Chest */
     , (940001,  2,  4,   0,    0,  375, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0) /* Abdomen */
     , (940001,  3,  4,   0,    0,  375, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0) /* UpperArm */
     , (940001,  4,  4,   0,    0,  375, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0) /* LowerArm */
     , (940001,  5,  4, 88, 0.75,  375, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0) /* Hand */
     , (940001,  6,  4,   0,    0,  375, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18) /* UpperLeg */
     , (940001,  7,  4,   0,    0,  375, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6) /* LowerLeg */
     , (940001,  8,  4, 88, 0.75,  375, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22) /* Foot */;
