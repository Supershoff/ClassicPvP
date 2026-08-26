/* Admin Test Staff 370 Max -- clone of wcid 338 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950030;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950030, 'ace950030-adminteststaff370max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950030,   1, 1) /* ItemType */
     , (950030,   3, 4) /* PaletteTemplate */
     , (950030,   5, 450) /* EncumbranceVal */
     , (950030,   8, 90) /* Mass */
     , (950030,   9, 1048576) /* ValidLocations */
     , (950030,  16, 1) /* ItemUseable */
     , (950030,  19, 130) /* Value */
     , (950030,  44, 24) /* Damage */
     , (950030,  45, 4) /* DamageType */
     , (950030,  46, 2) /* DefaultCombatStyle */
     , (950030,  47, 6) /* AttackType */
     , (950030,  48, 10) /* WeaponSkill */
     , (950030,  49, 22) /* WeaponTime */
     , (950030,  51, 1) /* CombatUse */
     , (950030,  93, 1044) /* PhysicsState */
     , (950030, 105, 1) /* ItemWorkmanship */
     , (950030, 131, 75) /* MaterialType */
     , (950030, 150, 103) /* HookPlacement */
     , (950030, 151, 2) /* HookType */
     , (950030, 158, 2) /* WieldRequirements */
     , (950030, 159, 10) /* WieldSkillType */
     , (950030, 160, 370) /* WieldDifficulty */
     , (950030, 169, 101189388) /* TsysMutationData */
     , (950030, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950030,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950030,  21, 1.33) /* WeaponLength */
     , (950030,  22, 0.25) /* DamageVariance */
     , (950030,  29, 1.15) /* WeaponDefense */
     , (950030,  39, 0.67) /* DefaultScale */
     , (950030,  62, 1.15) /* WeaponOffense */
     , (950030, 149, 1.025) /* WeaponMissileDefense */
     , (950030, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950030,   1, 'Admin Test Staff 370 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950030,   1, 0x0200013D) /* Setup */
     , (950030,   3, 0x20000014) /* SoundTable */
     , (950030,   6, 0x04000BEF) /* PaletteBase */
     , (950030,   7, 0x10000153) /* ClothingBase */
     , (950030,   8, 0x060016B1) /* Icon */
     , (950030,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950030,  36, 0x0E00001D) /* MutateFilter */
     , (950030,  46, 0x3800000E) /* TsysMutationFilter */;
