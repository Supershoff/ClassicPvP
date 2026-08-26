/* Admin Test Unarmed 370 Mid -- clone of wcid 4190 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950033;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950033, 'ace950033-admintestunarmed370mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950033,   1, 1) /* ItemType */
     , (950033,   3, 20) /* PaletteTemplate */
     , (950033,   5, 135) /* EncumbranceVal */
     , (950033,   8, 90) /* Mass */
     , (950033,   9, 1048576) /* ValidLocations */
     , (950033,  16, 1) /* ItemUseable */
     , (950033,  19, 50) /* Value */
     , (950033,  44, 21) /* Damage */
     , (950033,  45, 4) /* DamageType */
     , (950033,  46, 1) /* DefaultCombatStyle */
     , (950033,  47, 1) /* AttackType */
     , (950033,  48, 13) /* WeaponSkill */
     , (950033,  49, 17) /* WeaponTime */
     , (950033,  51, 1) /* CombatUse */
     , (950033,  93, 1044) /* PhysicsState */
     , (950033, 105, 1) /* ItemWorkmanship */
     , (950033, 131, 64) /* MaterialType */
     , (950033, 150, 103) /* HookPlacement */
     , (950033, 151, 2) /* HookType */
     , (950033, 158, 2) /* WieldRequirements */
     , (950033, 159, 13) /* WieldSkillType */
     , (950033, 160, 370) /* WieldDifficulty */
     , (950033, 169, 101254146) /* TsysMutationData */
     , (950033, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950033,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950033,  21, 0.52) /* WeaponLength */
     , (950033,  22, 0.6) /* DamageVariance */
     , (950033,  29, 1.16) /* WeaponDefense */
     , (950033,  39, 0.8) /* DefaultScale */
     , (950033,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950033,   1, 'Admin Test Unarmed 370 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950033,   1, 0x0200061D) /* Setup */
     , (950033,   3, 0x20000014) /* SoundTable */
     , (950033,   6, 0x04000BEF) /* PaletteBase */
     , (950033,   7, 0x10000175) /* ClothingBase */
     , (950033,   8, 0x06001A40) /* Icon */
     , (950033,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950033,  36, 0x0E00001D) /* MutateFilter */
     , (950033,  46, 0x38000006) /* TsysMutationFilter */;
