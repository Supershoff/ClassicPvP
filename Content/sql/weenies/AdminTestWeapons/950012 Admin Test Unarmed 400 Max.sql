/* Admin Test Unarmed 400 Max -- clone of wcid 4190 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950012;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950012, 'ace950012-admintestunarmed400max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950012,   1, 1) /* ItemType */
     , (950012,   3, 20) /* PaletteTemplate */
     , (950012,   5, 135) /* EncumbranceVal */
     , (950012,   8, 90) /* Mass */
     , (950012,   9, 1048576) /* ValidLocations */
     , (950012,  16, 1) /* ItemUseable */
     , (950012,  19, 50) /* Value */
     , (950012,  44, 26) /* Damage */
     , (950012,  45, 4) /* DamageType */
     , (950012,  46, 1) /* DefaultCombatStyle */
     , (950012,  47, 1) /* AttackType */
     , (950012,  48, 13) /* WeaponSkill */
     , (950012,  49, 15) /* WeaponTime */
     , (950012,  51, 1) /* CombatUse */
     , (950012,  93, 1044) /* PhysicsState */
     , (950012, 105, 1) /* ItemWorkmanship */
     , (950012, 131, 64) /* MaterialType */
     , (950012, 150, 103) /* HookPlacement */
     , (950012, 151, 2) /* HookType */
     , (950012, 158, 2) /* WieldRequirements */
     , (950012, 159, 13) /* WieldSkillType */
     , (950012, 160, 400) /* WieldDifficulty */
     , (950012, 169, 101254146) /* TsysMutationData */
     , (950012, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950012,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950012,  21, 0.52) /* WeaponLength */
     , (950012,  22, 0.5) /* DamageVariance */
     , (950012,  29, 1.2) /* WeaponDefense */
     , (950012,  39, 0.8) /* DefaultScale */
     , (950012,  62, 1.15) /* WeaponOffense */
     , (950012, 149, 1.025) /* WeaponMissileDefense */
     , (950012, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950012,   1, 'Admin Test Unarmed 400 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950012,   1, 0x0200061D) /* Setup */
     , (950012,   3, 0x20000014) /* SoundTable */
     , (950012,   6, 0x04000BEF) /* PaletteBase */
     , (950012,   7, 0x10000175) /* ClothingBase */
     , (950012,   8, 0x06001A40) /* Icon */
     , (950012,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950012,  36, 0x0E00001D) /* MutateFilter */
     , (950012,  46, 0x38000006) /* TsysMutationFilter */;
