/* Admin Test Unarmed 370 Max -- clone of wcid 4190 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950032;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950032, 'ace950032-admintestunarmed370max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950032,   1, 1) /* ItemType */
     , (950032,   3, 20) /* PaletteTemplate */
     , (950032,   5, 135) /* EncumbranceVal */
     , (950032,   8, 90) /* Mass */
     , (950032,   9, 1048576) /* ValidLocations */
     , (950032,  16, 1) /* ItemUseable */
     , (950032,  19, 50) /* Value */
     , (950032,  44, 22) /* Damage */
     , (950032,  45, 4) /* DamageType */
     , (950032,  46, 1) /* DefaultCombatStyle */
     , (950032,  47, 1) /* AttackType */
     , (950032,  48, 13) /* WeaponSkill */
     , (950032,  49, 15) /* WeaponTime */
     , (950032,  51, 1) /* CombatUse */
     , (950032,  93, 1044) /* PhysicsState */
     , (950032, 105, 1) /* ItemWorkmanship */
     , (950032, 131, 64) /* MaterialType */
     , (950032, 150, 103) /* HookPlacement */
     , (950032, 151, 2) /* HookType */
     , (950032, 158, 2) /* WieldRequirements */
     , (950032, 159, 13) /* WieldSkillType */
     , (950032, 160, 370) /* WieldDifficulty */
     , (950032, 169, 101254146) /* TsysMutationData */
     , (950032, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950032,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950032,  21, 0.52) /* WeaponLength */
     , (950032,  22, 0.5) /* DamageVariance */
     , (950032,  29, 1.2) /* WeaponDefense */
     , (950032,  39, 0.8) /* DefaultScale */
     , (950032,  62, 1.15) /* WeaponOffense */
     , (950032, 149, 1.025) /* WeaponMissileDefense */
     , (950032, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950032,   1, 'Admin Test Unarmed 370 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950032,   1, 0x0200061D) /* Setup */
     , (950032,   3, 0x20000014) /* SoundTable */
     , (950032,   6, 0x04000BEF) /* PaletteBase */
     , (950032,   7, 0x10000175) /* ClothingBase */
     , (950032,   8, 0x06001A40) /* Icon */
     , (950032,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950032,  36, 0x0E00001D) /* MutateFilter */
     , (950032,  46, 0x38000006) /* TsysMutationFilter */;
