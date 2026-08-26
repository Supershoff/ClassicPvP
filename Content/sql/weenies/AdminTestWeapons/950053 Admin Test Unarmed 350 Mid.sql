/* Admin Test Unarmed 350 Mid -- clone of wcid 4190 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950053;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950053, 'ace950053-admintestunarmed350mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950053,   1, 1) /* ItemType */
     , (950053,   3, 20) /* PaletteTemplate */
     , (950053,   5, 135) /* EncumbranceVal */
     , (950053,   8, 90) /* Mass */
     , (950053,   9, 1048576) /* ValidLocations */
     , (950053,  16, 1) /* ItemUseable */
     , (950053,  19, 50) /* Value */
     , (950053,  44, 18) /* Damage */
     , (950053,  45, 4) /* DamageType */
     , (950053,  46, 1) /* DefaultCombatStyle */
     , (950053,  47, 1) /* AttackType */
     , (950053,  48, 13) /* WeaponSkill */
     , (950053,  49, 17) /* WeaponTime */
     , (950053,  51, 1) /* CombatUse */
     , (950053,  93, 1044) /* PhysicsState */
     , (950053, 105, 1) /* ItemWorkmanship */
     , (950053, 131, 64) /* MaterialType */
     , (950053, 150, 103) /* HookPlacement */
     , (950053, 151, 2) /* HookType */
     , (950053, 158, 2) /* WieldRequirements */
     , (950053, 159, 13) /* WieldSkillType */
     , (950053, 160, 350) /* WieldDifficulty */
     , (950053, 169, 101254146) /* TsysMutationData */
     , (950053, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950053,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950053,  21, 0.52) /* WeaponLength */
     , (950053,  22, 0.6) /* DamageVariance */
     , (950053,  29, 1.16) /* WeaponDefense */
     , (950053,  39, 0.8) /* DefaultScale */
     , (950053,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950053,   1, 'Admin Test Unarmed 350 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950053,   1, 0x0200061D) /* Setup */
     , (950053,   3, 0x20000014) /* SoundTable */
     , (950053,   6, 0x04000BEF) /* PaletteBase */
     , (950053,   7, 0x10000175) /* ClothingBase */
     , (950053,   8, 0x06001A40) /* Icon */
     , (950053,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950053,  36, 0x0E00001D) /* MutateFilter */
     , (950053,  46, 0x38000006) /* TsysMutationFilter */;
