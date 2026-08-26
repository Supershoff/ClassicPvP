/* Admin Test Staff 400 Max -- clone of wcid 338 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950010;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950010, 'ace950010-adminteststaff400max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950010,   1, 1) /* ItemType */
     , (950010,   3, 4) /* PaletteTemplate */
     , (950010,   5, 450) /* EncumbranceVal */
     , (950010,   8, 90) /* Mass */
     , (950010,   9, 1048576) /* ValidLocations */
     , (950010,  16, 1) /* ItemUseable */
     , (950010,  19, 130) /* Value */
     , (950010,  44, 26) /* Damage */
     , (950010,  45, 4) /* DamageType */
     , (950010,  46, 2) /* DefaultCombatStyle */
     , (950010,  47, 6) /* AttackType */
     , (950010,  48, 10) /* WeaponSkill */
     , (950010,  49, 22) /* WeaponTime */
     , (950010,  51, 1) /* CombatUse */
     , (950010,  93, 1044) /* PhysicsState */
     , (950010, 105, 1) /* ItemWorkmanship */
     , (950010, 131, 75) /* MaterialType */
     , (950010, 150, 103) /* HookPlacement */
     , (950010, 151, 2) /* HookType */
     , (950010, 158, 2) /* WieldRequirements */
     , (950010, 159, 10) /* WieldSkillType */
     , (950010, 160, 400) /* WieldDifficulty */
     , (950010, 169, 101189388) /* TsysMutationData */
     , (950010, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950010,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950010,  21, 1.33) /* WeaponLength */
     , (950010,  22, 0.25) /* DamageVariance */
     , (950010,  29, 1.15) /* WeaponDefense */
     , (950010,  39, 0.67) /* DefaultScale */
     , (950010,  62, 1.15) /* WeaponOffense */
     , (950010, 149, 1.025) /* WeaponMissileDefense */
     , (950010, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950010,   1, 'Admin Test Staff 400 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950010,   1, 0x0200013D) /* Setup */
     , (950010,   3, 0x20000014) /* SoundTable */
     , (950010,   6, 0x04000BEF) /* PaletteBase */
     , (950010,   7, 0x10000153) /* ClothingBase */
     , (950010,   8, 0x060016B1) /* Icon */
     , (950010,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950010,  36, 0x0E00001D) /* MutateFilter */
     , (950010,  46, 0x3800000E) /* TsysMutationFilter */;
