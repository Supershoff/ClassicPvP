/* Admin Test Staff 350 Max -- clone of wcid 338 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950050;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950050, 'ace950050-adminteststaff350max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950050,   1, 1) /* ItemType */
     , (950050,   3, 4) /* PaletteTemplate */
     , (950050,   5, 450) /* EncumbranceVal */
     , (950050,   8, 90) /* Mass */
     , (950050,   9, 1048576) /* ValidLocations */
     , (950050,  16, 1) /* ItemUseable */
     , (950050,  19, 130) /* Value */
     , (950050,  44, 20) /* Damage */
     , (950050,  45, 4) /* DamageType */
     , (950050,  46, 2) /* DefaultCombatStyle */
     , (950050,  47, 6) /* AttackType */
     , (950050,  48, 10) /* WeaponSkill */
     , (950050,  49, 22) /* WeaponTime */
     , (950050,  51, 1) /* CombatUse */
     , (950050,  93, 1044) /* PhysicsState */
     , (950050, 105, 1) /* ItemWorkmanship */
     , (950050, 131, 75) /* MaterialType */
     , (950050, 150, 103) /* HookPlacement */
     , (950050, 151, 2) /* HookType */
     , (950050, 158, 2) /* WieldRequirements */
     , (950050, 159, 10) /* WieldSkillType */
     , (950050, 160, 350) /* WieldDifficulty */
     , (950050, 169, 101189388) /* TsysMutationData */
     , (950050, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950050,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950050,  21, 1.33) /* WeaponLength */
     , (950050,  22, 0.25) /* DamageVariance */
     , (950050,  29, 1.15) /* WeaponDefense */
     , (950050,  39, 0.67) /* DefaultScale */
     , (950050,  62, 1.15) /* WeaponOffense */
     , (950050, 149, 1.025) /* WeaponMissileDefense */
     , (950050, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950050,   1, 'Admin Test Staff 350 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950050,   1, 0x0200013D) /* Setup */
     , (950050,   3, 0x20000014) /* SoundTable */
     , (950050,   6, 0x04000BEF) /* PaletteBase */
     , (950050,   7, 0x10000153) /* ClothingBase */
     , (950050,   8, 0x060016B1) /* Icon */
     , (950050,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950050,  36, 0x0E00001D) /* MutateFilter */
     , (950050,  46, 0x3800000E) /* TsysMutationFilter */;
