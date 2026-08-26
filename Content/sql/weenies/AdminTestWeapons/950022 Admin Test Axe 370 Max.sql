/* Admin Test Axe 370 Max -- clone of wcid 301 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950022;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950022, 'ace950022-admintestaxe370max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950022,   1, 1) /* ItemType */
     , (950022,   3, 20) /* PaletteTemplate */
     , (950022,   5, 800) /* EncumbranceVal */
     , (950022,   8, 320) /* Mass */
     , (950022,   9, 1048576) /* ValidLocations */
     , (950022,  16, 1) /* ItemUseable */
     , (950022,  19, 360) /* Value */
     , (950022,  44, 42) /* Damage */
     , (950022,  45, 1) /* DamageType */
     , (950022,  46, 2) /* DefaultCombatStyle */
     , (950022,  47, 4) /* AttackType */
     , (950022,  48, 1) /* WeaponSkill */
     , (950022,  49, 45) /* WeaponTime */
     , (950022,  51, 1) /* CombatUse */
     , (950022,  93, 1044) /* PhysicsState */
     , (950022, 105, 1) /* ItemWorkmanship */
     , (950022, 131, 64) /* MaterialType */
     , (950022, 150, 103) /* HookPlacement */
     , (950022, 151, 2) /* HookType */
     , (950022, 158, 2) /* WieldRequirements */
     , (950022, 159, 1) /* WieldSkillType */
     , (950022, 160, 370) /* WieldDifficulty */
     , (950022, 169, 101189386) /* TsysMutationData */
     , (950022, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950022,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950022,  21, 0.75) /* WeaponLength */
     , (950022,  22, 0.4) /* DamageVariance */
     , (950022,  29, 1.15) /* WeaponDefense */
     , (950022,  62, 1.15) /* WeaponOffense */
     , (950022, 149, 1.025) /* WeaponMissileDefense */
     , (950022, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950022,   1, 'Admin Test Axe 370 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950022,   1, 0x02000125) /* Setup */
     , (950022,   3, 0x20000014) /* SoundTable */
     , (950022,   6, 0x04000BEF) /* PaletteBase */
     , (950022,   7, 0x10000143) /* ClothingBase */
     , (950022,   8, 0x06001639) /* Icon */
     , (950022,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950022,  30, 0x00000058) /* PhysicsScript */
     , (950022,  36, 0x0E00001D) /* MutateFilter */
     , (950022,  46, 0x38000002) /* TsysMutationFilter */;
