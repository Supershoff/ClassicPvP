/* Random Dungeon Boss: Rendmaw (fast beast striker, high evade, lower HP)
   Model frame: Tusker Queen. Combat stats authored at reference level 275, scaled to
   the season level cap at spawn (DungeonBossManager.ScaleBossToCap). Radar hidden.
   Rewards (scattered currency + XP) are handled in code. For normal generated loot,
   set a valid DeathTreasureType DID (type 35) on the commented line below. */

DELETE FROM `weenie` WHERE `class_Id` = 940003;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (940003, 'dungeonbossrendmaw', 10, '2026-08-07 00:00:00') /* Creature */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (940003,   1,         16) /* ItemType - Creature */
     , (940003,   2,         12) /* CreatureType - Cow (tusker) */
     , (940003,   6,         -1) /* ItemsCapacity */
     , (940003,   7,         -1) /* ContainersCapacity */
     , (940003,  16,          1) /* ItemUseable - No */
     , (940003,  25,        275) /* Level (reference; overwritten to level cap at spawn) */
     , (940003,  27,          0) /* ArmorType - None */
     , (940003,  40,          1) /* CombatMode - NonCombat */
     , (940003,  68,         13) /* TargetingTactic - Random, LastDamager, TopDamager */
     , (940003,  93,       1032) /* PhysicsState - ReportCollisions, Gravity */
     , (940003, 101,        183) /* AiAllowedCombatStyle - Unarmed, OneHanded, OneHandedAndShield, Bow, Crossbow, ThrownWeapon */
     , (940003, 133,          1) /* ShowableOnRadar - ShowNever (hunt for it) */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (940003,   1, False) /* Stuck */
     , (940003,   6, False) /* AiUsesMana */
     , (940003,  11, False) /* IgnoreCollisions */
     , (940003,  12, True ) /* ReportCollisions */
     , (940003,  13, False) /* Ethereal */
     , (940003,  19, True ) /* Attackable */
     , (940003,  52, False) /* AiImmobile */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (940003,   1,    1.5) /* HeartbeatInterval */
     , (940003,   2,      0) /* HeartbeatTimestamp */
     , (940003,   3,     10) /* HealthRate */
     , (940003,   4,    100) /* StaminaRate */
     , (940003,   5,     20) /* ManaRate */
     , (940003,  31,     45) /* VisualAwarenessRange */
     , (940003,  36,      1) /* ChargeSpeed */
     , (940003,  39,      1) /* DefaultScale (natural - do not increase) */
     , (940003,  54,      5) /* UseRadius */
     , (940003,  55,     80) /* HomeRadius */
     , (940003, 104,     45) /* ObviousRadarRange */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (940003,   1, 'Rendmaw') /* Name */
     , (940003,   5, 'Dungeon Boss') /* Template */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (940003,   1, 0x02000964) /* Setup */
     , (940003,   2, 0x0900000C) /* MotionTable */
     , (940003,   3, 0x20000011) /* SoundTable */
     , (940003,   4, 0x3000000B) /* CombatTable */
     , (940003,   6, 0x0400102F) /* PaletteBase */
     , (940003,   7, 0x10000262) /* ClothingBase */
     , (940003,   8, 0x06001033) /* Icon */
     , (940003,  22, 0x34000027) /* PhysicsEffectTable */
     , (940003,  35,     940000) /* DeathTreasureType - Dungeon Boss Loot Profile */;

INSERT INTO `weenie_properties_attribute` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`)
VALUES (940003,   1, 420, 0, 0) /* Strength */
     , (940003,   2, 400, 0, 0) /* Endurance */
     , (940003,   3, 520, 0, 0) /* Quickness */
     , (940003,   4, 460, 0, 0) /* Coordination */
     , (940003,   5, 340, 0, 0) /* Focus */
     , (940003,   6, 360, 0, 0) /* Self */;

INSERT INTO `weenie_properties_attribute_2nd` (`object_Id`, `type`, `init_Level`, `level_From_C_P`, `c_P_Spent`, `current_Level`)
VALUES (940003,   1, 40000, 0, 0, 40000) /* MaxHealth */
     , (940003,   3, 50000, 0, 0, 50000) /* MaxStamina */
     , (940003,   5, 50000, 0, 0, 50000) /* MaxMana */;

INSERT INTO `weenie_properties_skill` (`object_Id`, `type`, `level_From_P_P`, `s_a_c`, `p_p`, `init_Level`, `resistance_At_Last_Check`, `last_Used_Time`)
VALUES (940003,  6, 0, 3, 0, 300, 0, 0) /* MeleeDefense    Specialized */
     , (940003,  7, 0, 3, 0, 300, 0, 0) /* MissileDefense  Specialized */
     , (940003, 15, 0, 3, 0, 300, 0, 0) /* MagicDefense    Specialized */
     , (940003, 44, 0, 3, 0, 300, 0, 0) /* HeavyWeapons    Specialized */;

/* base_Armor is the ONLY physical mitigation input for creatures: melee/missile use
   SkillFormula.CalcArmorMod(base_Armor * ArmorModVs<Type>), where ArmorModVs* are
   creature-level float properties defaulting to 1.0. The armor_Vs_* columns below are
   read only for ARMOR ITEMS (Armor.cs) and are inert on a creature - do not tune them.
   Spell damage never touches armor at all (it uses GetResistanceMod), so base_Armor
   adjusts melee/missile without affecting magic. */
INSERT INTO `weenie_properties_body_part` (`object_Id`, `key`, `d_Type`, `d_Val`, `d_Var`, `base_Armor`, `armor_Vs_Slash`, `armor_Vs_Pierce`, `armor_Vs_Bludgeon`, `armor_Vs_Cold`, `armor_Vs_Fire`, `armor_Vs_Acid`, `armor_Vs_Electric`, `armor_Vs_Nether`, `b_h`, `h_l_f`, `m_l_f`, `l_l_f`, `h_r_f`, `m_r_f`, `l_r_f`, `h_l_b`, `m_l_b`, `l_l_b`, `h_r_b`, `m_r_b`, `l_r_b`)
VALUES (940003,  0,  4,   0,    0,  315, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0, 0.33,    0,    0) /* Head */
     , (940003,  1,  4,   0,    0,  315, 700, 700, 700, 700, 700, 700, 700, 700, 2, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0, 0.44, 0.17,    0) /* Chest */
     , (940003,  2,  4,   0,    0,  315, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0,    0, 0.17,    0) /* Abdomen */
     , (940003,  3,  4,   0,    0,  315, 700, 700, 700, 700, 700, 700, 700, 700, 1, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0, 0.23, 0.03,    0) /* UpperArm */
     , (940003,  4,  4,   0,    0,  315, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0,    0,  0.3,    0) /* LowerArm */
     , (940003,  5,  4, 113, 0.75,  315, 700, 700, 700, 700, 700, 700, 700, 700, 2,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0,    0,  0.2,    0) /* Hand */
     , (940003,  6,  4,   0,    0,  315, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18,    0, 0.13, 0.18) /* UpperLeg */
     , (940003,  7,  4,   0,    0,  315, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6,    0,    0,  0.6) /* LowerLeg */
     , (940003,  8,  4, 113, 0.75,  315, 700, 700, 700, 700, 700, 700, 700, 700, 3,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22,    0,    0, 0.22) /* Foot */;
