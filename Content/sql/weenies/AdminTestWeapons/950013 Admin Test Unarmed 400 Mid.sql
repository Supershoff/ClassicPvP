/* Admin Test Unarmed 400 Mid -- clone of wcid 4190 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950013;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950013, 'ace950013-admintestunarmed400mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950013,   1, 1) /* ItemType */
     , (950013,   3, 20) /* PaletteTemplate */
     , (950013,   5, 135) /* EncumbranceVal */
     , (950013,   8, 90) /* Mass */
     , (950013,   9, 1048576) /* ValidLocations */
     , (950013,  16, 1) /* ItemUseable */
     , (950013,  19, 50) /* Value */
     , (950013,  44, 24) /* Damage */
     , (950013,  45, 4) /* DamageType */
     , (950013,  46, 1) /* DefaultCombatStyle */
     , (950013,  47, 1) /* AttackType */
     , (950013,  48, 13) /* WeaponSkill */
     , (950013,  49, 17) /* WeaponTime */
     , (950013,  51, 1) /* CombatUse */
     , (950013,  93, 1044) /* PhysicsState */
     , (950013, 105, 1) /* ItemWorkmanship */
     , (950013, 131, 64) /* MaterialType */
     , (950013, 150, 103) /* HookPlacement */
     , (950013, 151, 2) /* HookType */
     , (950013, 158, 2) /* WieldRequirements */
     , (950013, 159, 13) /* WieldSkillType */
     , (950013, 160, 400) /* WieldDifficulty */
     , (950013, 169, 101254146) /* TsysMutationData */
     , (950013, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950013,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950013,  21, 0.52) /* WeaponLength */
     , (950013,  22, 0.6) /* DamageVariance */
     , (950013,  29, 1.16) /* WeaponDefense */
     , (950013,  39, 0.8) /* DefaultScale */
     , (950013,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950013,   1, 'Admin Test Unarmed 400 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950013,   1, 0x0200061D) /* Setup */
     , (950013,   3, 0x20000014) /* SoundTable */
     , (950013,   6, 0x04000BEF) /* PaletteBase */
     , (950013,   7, 0x10000175) /* ClothingBase */
     , (950013,   8, 0x06001A40) /* Icon */
     , (950013,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950013,  36, 0x0E00001D) /* MutateFilter */
     , (950013,  46, 0x38000006) /* TsysMutationFilter */;
