/* Admin Test Spear 400 Max -- clone of wcid 348 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950006;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950006, 'ace950006-admintestspear400max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950006,   1, 1) /* ItemType */
     , (950006,   3, 20) /* PaletteTemplate */
     , (950006,   5, 700) /* EncumbranceVal */
     , (950006,   8, 140) /* Mass */
     , (950006,   9, 1048576) /* ValidLocations */
     , (950006,  16, 1) /* ItemUseable */
     , (950006,  19, 170) /* Value */
     , (950006,  44, 40) /* Damage */
     , (950006,  45, 2) /* DamageType */
     , (950006,  46, 2) /* DefaultCombatStyle */
     , (950006,  47, 2) /* AttackType */
     , (950006,  48, 9) /* WeaponSkill */
     , (950006,  49, 22) /* WeaponTime */
     , (950006,  51, 1) /* CombatUse */
     , (950006,  93, 1044) /* PhysicsState */
     , (950006, 105, 1) /* ItemWorkmanship */
     , (950006, 131, 64) /* MaterialType */
     , (950006, 150, 103) /* HookPlacement */
     , (950006, 151, 2) /* HookType */
     , (950006, 158, 2) /* WieldRequirements */
     , (950006, 159, 9) /* WieldSkillType */
     , (950006, 160, 400) /* WieldDifficulty */
     , (950006, 169, 101188618) /* TsysMutationData */
     , (950006, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950006,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950006,  21, 1.5) /* WeaponLength */
     , (950006,  22, 0.45) /* DamageVariance */
     , (950006,  29, 1.15) /* WeaponDefense */
     , (950006,  62, 1.15) /* WeaponOffense */
     , (950006, 149, 1.025) /* WeaponMissileDefense */
     , (950006, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950006,   1, 'Admin Test Spear 400 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950006,   1, 0x02000144) /* Setup */
     , (950006,   3, 0x20000014) /* SoundTable */
     , (950006,   6, 0x04000BEF) /* PaletteBase */
     , (950006,   7, 0x10000138) /* ClothingBase */
     , (950006,   8, 0x0600164D) /* Icon */
     , (950006,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950006,  36, 0x0E00001D) /* MutateFilter */
     , (950006,  46, 0x38000004) /* TsysMutationFilter */;
