/* Admin Test Staff 350 Mid -- clone of wcid 338 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950051;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950051, 'ace950051-adminteststaff350mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950051,   1, 1) /* ItemType */
     , (950051,   3, 4) /* PaletteTemplate */
     , (950051,   5, 450) /* EncumbranceVal */
     , (950051,   8, 90) /* Mass */
     , (950051,   9, 1048576) /* ValidLocations */
     , (950051,  16, 1) /* ItemUseable */
     , (950051,  19, 130) /* Value */
     , (950051,  44, 19) /* Damage */
     , (950051,  45, 4) /* DamageType */
     , (950051,  46, 2) /* DefaultCombatStyle */
     , (950051,  47, 6) /* AttackType */
     , (950051,  48, 10) /* WeaponSkill */
     , (950051,  49, 25) /* WeaponTime */
     , (950051,  51, 1) /* CombatUse */
     , (950051,  93, 1044) /* PhysicsState */
     , (950051, 105, 1) /* ItemWorkmanship */
     , (950051, 131, 75) /* MaterialType */
     , (950051, 150, 103) /* HookPlacement */
     , (950051, 151, 2) /* HookType */
     , (950051, 158, 2) /* WieldRequirements */
     , (950051, 159, 10) /* WieldSkillType */
     , (950051, 160, 350) /* WieldDifficulty */
     , (950051, 169, 101189388) /* TsysMutationData */
     , (950051, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950051,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950051,  21, 1.33) /* WeaponLength */
     , (950051,  22, 0.4) /* DamageVariance */
     , (950051,  29, 1.11) /* WeaponDefense */
     , (950051,  39, 0.67) /* DefaultScale */
     , (950051,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950051,   1, 'Admin Test Staff 350 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950051,   1, 0x0200013D) /* Setup */
     , (950051,   3, 0x20000014) /* SoundTable */
     , (950051,   6, 0x04000BEF) /* PaletteBase */
     , (950051,   7, 0x10000153) /* ClothingBase */
     , (950051,   8, 0x060016B1) /* Icon */
     , (950051,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950051,  36, 0x0E00001D) /* MutateFilter */
     , (950051,  46, 0x3800000E) /* TsysMutationFilter */;
