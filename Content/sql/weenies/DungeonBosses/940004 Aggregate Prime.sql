/* Random Dungeon Boss: Aggregate Prime (armor + HP wall, slow, lower damage)
   Model/movement/sound mirrored from Basalt Golem (wcid 11994). Combat stats authored at reference level
   275, scaled to the season level cap at spawn (DungeonBossManager.ScaleBossToCap).
   Radar hidden. Rewards (scattered currency + XP) handled in code. For normal generated
   loot, set a valid DeathTreasureType DID (type 35) on the commented line below. */

DELETE FROM `weenie` WHERE `class_Id` = 940004;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (940004, 'dungeonbossaggregateprime', 10, '2026-08-07 00:00:00') /* Creature */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (940004,   1,         16) /* ItemType - Creature */
     , (940004,   2,         13) /* CreatureType - Golem (matches Basalt Golem) */
     , (940004,   3,          4) /* PaletteTemplate - Brown (matches Basalt Golem) */
     , (940004,   6,         -1) /* ItemsCapacity */
     , (940004,   7,         -1) /* ContainersCapacity */
     , (940004,  16,          1) /* ItemUseable - No */
     , (940004,  25,        275) /* Level (reference; overwritten to level cap at spawn) */
     , (940004,  27,          0) /* ArmorType - None */
     , (940004,  40,          2) /* CombatMode - Melee (matches Basalt Golem) */
     , (940004,  68,         13) /* TargetingTactic - Random, LastDamager, TopDamager */
     , (940004,  93,       1032) /* PhysicsState - ReportCollisions, Gravity */
     , (940004, 101,        183) /* AiAllowedCombatStyle - Unarmed, OneHanded, OneHandedAndShield, Bow, Crossbow, ThrownWeapon */
     , (940004, 133,          1) /* ShowableOnRadar - ShowNever (hunt for it) */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (940004,   1, False) /* Stuck */
     , (940004,   6, False) /* AiUsesMana */
     , (940004,  11, False) /* IgnoreCollisions */
     , (940004,  12, True ) /* ReportCollisions */
     , (940004,  13, False) /* Ethereal */
     , (940004,  19, True ) /* Attackable */
     , (940004,  52, False) /* AiImmobile */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (940004,   1,      5) /* HeartbeatInterval */
     , (940004,   2,      0) /* HeartbeatTimestamp */
     , (940004,   3,     10) /* HealthRate */
     , (940004,   4,    100) /* StaminaRate */
     , (940004,   5,     20) /* ManaRate */
     , (940004,  31,     40) /* VisualAwarenessRange */
     , (940004,  39,      1) /* DefaultScale - Basalt Golem's natural size (it has no scale override) */
     , (940004,  54,      5) /* UseRadius */
     , (940004,  55,     70) /* HomeRadius */
     , (940004, 104,     40) /* ObviousRadarRange */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (940004,   1, 'Aggregate Prime') /* Name */
     , (940004,   5, 'Dungeon Boss') /* Template */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
/* Appearance/movement/sound mirrored from Basalt Golem (wcid 11994). */
VALUES (940004,   1, 0x020007D8) /* Setup */
     , (940004,   2, 0x09000081) /* MotionTable */
     , (940004,   3, 0x20000015) /* SoundTable */
     , (940004,   4, 0x30000008) /* CombatTable */
     , (940004,   6, 0x04000F6A) /* PaletteBase */
     , (940004,   7, 0x1000031F) /* ClothingBase */
     , (940004,   8, 0x06001224) /* Icon */
     , (940004,  22, 0x3400005F) /* PhysicsEffectTable */
     , (940004,  35,     940000) /* DeathTreasureType - Dungeon Boss Loot Profile */;

INSERT INTO `weenie_properties_attribute` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`)
VALUES (940004,   1, 460, 0, 0) /* Strength */
     , (940004,   2, 560, 0, 0) /* Endurance */
     , (940004,   3, 300, 0, 0) /* Quickness */
     , (940004,   4, 360, 0, 0) /* Coordination */
     , (940004,   5, 360, 0, 0) /* Focus */
     , (940004,   6, 400, 0, 0) /* Self */;

INSERT INTO `weenie_properties_attribute_2nd` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`, `current_Level`)
VALUES (940004,   1, 40000, 0, 0, 40000) /* MaxHealth */
     , (940004,   3, 50000, 0, 0, 50000) /* MaxStamina */
     , (940004,   5, 50000, 0, 0, 50000) /* MaxMana */;

INSERT INTO `weenie_properties_skill` (`object_Id`, `type`, `level_From_P_P`, `s_a_c`, `p_p`, `init_Level`, `resistance_At_Last_Check`, `last_Used_Time`)
VALUES (940004,  6, 0, 3, 0, 300, 0, 0) /* MeleeDefense    Specialized */
     , (940004,  7, 0, 3, 0, 300, 0, 0) /* MissileDefense  Specialized */
     , (940004, 15, 0, 3, 0, 300, 0, 0) /* MagicDefense    Specialized */
     , (940004, 44, 0, 3, 0, 300, 0, 0) /* HeavyWeapons    Specialized */;

/* base_Armor is the ONLY physical mitigation input for creatures: melee/missile use
   SkillFormula.CalcArmorMod(base_Armor * ArmorModVs<Type>), where ArmorModVs* are
   creature-level float properties defaulting to 1.0. The armor_Vs_* columns below are
   read only for ARMOR ITEMS (Armor.cs) and are inert on a creature - do not tune them.
   Spell damage never touches armor at all (it uses GetResistanceMod), so base_Armor
   adjusts melee/missile without affecting magic. */
INSERT INTO `weenie_properties_body_part` (`object_Id`, `key`, `d_Type`, `d_Val`, `d_Var`, `base_Armor`, `armor_Vs_Slash`, `armor_Vs_Pierce`, `armor_Vs_Bludgeon`, `armor_Vs_Cold`, `armor_Vs_Fire`, `armor_Vs_Acid`, `armor_Vs_Electric`, `armor_Vs_Nether`, `b_h`, `h_l_f`, `m_l_f`, `l_l_f`, `h_r_f`, `m_r_f`, `l_r_f`, `h_l_b`, `m_l_b`, `l_l_b`, `h_r_b`, `m_r_b`, `l_r_b`)
VALUES (940004,  0,  4,   0,    0, 480, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0) /* Head */
     , (940004,  1,  4,   0,    0, 480, 700, 700, 700, 700, 700, 700, 700, 700, 2, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0) /* Chest */
     , (940004,  2,  4,   0,    0, 480, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0) /* Abdomen */
     , (940004,  3,  4,   0,    0, 480, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0) /* UpperArm */
     , (940004,  4,  4,   0,    0, 480, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0) /* LowerArm */
     , (940004,  5,  4,  90, 0.75, 480, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0) /* Hand */
     , (940004,  6,  4,   0,    0, 480, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18) /* UpperLeg */
     , (940004,  7,  4,   0,    0, 480, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6) /* LowerLeg */
     , (940004,  8,  4,  90, 0.75, 480, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22) /* Foot */;
