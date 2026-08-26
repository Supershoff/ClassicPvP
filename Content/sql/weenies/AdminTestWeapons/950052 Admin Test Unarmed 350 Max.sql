/* Admin Test Unarmed 350 Max -- clone of wcid 4190 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950052;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950052, 'ace950052-admintestunarmed350max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950052,   1, 1) /* ItemType */
     , (950052,   3, 20) /* PaletteTemplate */
     , (950052,   5, 135) /* EncumbranceVal */
     , (950052,   8, 90) /* Mass */
     , (950052,   9, 1048576) /* ValidLocations */
     , (950052,  16, 1) /* ItemUseable */
     , (950052,  19, 50) /* Value */
     , (950052,  44, 20) /* Damage */
     , (950052,  45, 4) /* DamageType */
     , (950052,  46, 1) /* DefaultCombatStyle */
     , (950052,  47, 1) /* AttackType */
     , (950052,  48, 13) /* WeaponSkill */
     , (950052,  49, 15) /* WeaponTime */
     , (950052,  51, 1) /* CombatUse */
     , (950052,  93, 1044) /* PhysicsState */
     , (950052, 105, 1) /* ItemWorkmanship */
     , (950052, 131, 64) /* MaterialType */
     , (950052, 150, 103) /* HookPlacement */
     , (950052, 151, 2) /* HookType */
     , (950052, 158, 2) /* WieldRequirements */
     , (950052, 159, 13) /* WieldSkillType */
     , (950052, 160, 350) /* WieldDifficulty */
     , (950052, 169, 101254146) /* TsysMutationData */
     , (950052, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950052,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950052,  21, 0.52) /* WeaponLength */
     , (950052,  22, 0.5) /* DamageVariance */
     , (950052,  29, 1.2) /* WeaponDefense */
     , (950052,  39, 0.8) /* DefaultScale */
     , (950052,  62, 1.15) /* WeaponOffense */
     , (950052, 149, 1.025) /* WeaponMissileDefense */
     , (950052, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950052,   1, 'Admin Test Unarmed 350 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950052,   1, 0x0200061D) /* Setup */
     , (950052,   3, 0x20000014) /* SoundTable */
     , (950052,   6, 0x04000BEF) /* PaletteBase */
     , (950052,   7, 0x10000175) /* ClothingBase */
     , (950052,   8, 0x06001A40) /* Icon */
     , (950052,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950052,  36, 0x0E00001D) /* MutateFilter */
     , (950052,  46, 0x38000006) /* TsysMutationFilter */;
