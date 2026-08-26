/* Admin Test Mace 370 Max -- clone of wcid 331 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950024;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950024, 'ace950024-admintestmace370max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950024,   1, 1) /* ItemType */
     , (950024,   3, 20) /* PaletteTemplate */
     , (950024,   5, 675) /* EncumbranceVal */
     , (950024,   8, 450) /* Mass */
     , (950024,   9, 1048576) /* ValidLocations */
     , (950024,  16, 1) /* ItemUseable */
     , (950024,  19, 260) /* Value */
     , (950024,  44, 38) /* Damage */
     , (950024,  45, 4) /* DamageType */
     , (950024,  46, 2) /* DefaultCombatStyle */
     , (950024,  47, 4) /* AttackType */
     , (950024,  48, 5) /* WeaponSkill */
     , (950024,  49, 30) /* WeaponTime */
     , (950024,  51, 1) /* CombatUse */
     , (950024,  93, 1044) /* PhysicsState */
     , (950024, 105, 1) /* ItemWorkmanship */
     , (950024, 131, 64) /* MaterialType */
     , (950024, 150, 103) /* HookPlacement */
     , (950024, 151, 2) /* HookType */
     , (950024, 158, 2) /* WieldRequirements */
     , (950024, 159, 5) /* WieldSkillType */
     , (950024, 160, 370) /* WieldDifficulty */
     , (950024, 169, 101189386) /* TsysMutationData */
     , (950024, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950024,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950024,  21, 0.62) /* WeaponLength */
     , (950024,  22, 0.25) /* DamageVariance */
     , (950024,  29, 1.15) /* WeaponDefense */
     , (950024,  62, 1.15) /* WeaponOffense */
     , (950024, 149, 1.025) /* WeaponMissileDefense */
     , (950024, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950024,   1, 'Admin Test Mace 370 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950024,   1, 0x0200013A) /* Setup */
     , (950024,   3, 0x20000014) /* SoundTable */
     , (950024,   6, 0x04000BEF) /* PaletteBase */
     , (950024,   7, 0x10000150) /* ClothingBase */
     , (950024,   8, 0x0600161B) /* Icon */
     , (950024,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950024,  36, 0x0E00001D) /* MutateFilter */
     , (950024,  46, 0x38000003) /* TsysMutationFilter */;
