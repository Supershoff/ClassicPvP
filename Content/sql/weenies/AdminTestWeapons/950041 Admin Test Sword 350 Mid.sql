/* Admin Test Sword 350 Mid -- clone of wcid 350 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950041;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950041, 'ace950041-admintestsword350mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950041,   1, 1) /* ItemType */
     , (950041,   3, 20) /* PaletteTemplate */
     , (950041,   5, 550) /* EncumbranceVal */
     , (950041,   8, 220) /* Mass */
     , (950041,   9, 1048576) /* ValidLocations */
     , (950041,  16, 1) /* ItemUseable */
     , (950041,  19, 340) /* Value */
     , (950041,  44, 43) /* Damage */
     , (950041,  45, 3) /* DamageType */
     , (950041,  46, 2) /* DefaultCombatStyle */
     , (950041,  47, 6) /* AttackType */
     , (950041,  48, 11) /* WeaponSkill */
     , (950041,  49, 42) /* WeaponTime */
     , (950041,  51, 1) /* CombatUse */
     , (950041,  93, 1044) /* PhysicsState */
     , (950041, 105, 1) /* ItemWorkmanship */
     , (950041, 131, 64) /* MaterialType */
     , (950041, 150, 103) /* HookPlacement */
     , (950041, 151, 2) /* HookType */
     , (950041, 158, 2) /* WieldRequirements */
     , (950041, 159, 11) /* WieldSkillType */
     , (950041, 160, 350) /* WieldDifficulty */
     , (950041, 169, 101255170) /* TsysMutationData */
     , (950041, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950041,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950041,  21, 0.95) /* WeaponLength */
     , (950041,  22, 0.45) /* DamageVariance */
     , (950041,  29, 1.11) /* WeaponDefense */
     , (950041,  39, 1.1) /* DefaultScale */
     , (950041,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950041,   1, 'Admin Test Sword 350 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950041,   1, 0x02000146) /* Setup */
     , (950041,   3, 0x20000014) /* SoundTable */
     , (950041,   6, 0x04000BEF) /* PaletteBase */
     , (950041,   7, 0x1000013A) /* ClothingBase */
     , (950041,   8, 0x06001657) /* Icon */
     , (950041,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950041,  36, 0x0E00001D) /* MutateFilter */
     , (950041,  46, 0x38000005) /* TsysMutationFilter */;
