/* Admin Test Dagger 370 Mid -- clone of wcid 22440 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950029;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950029, 'ace950029-admintestdagger370mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950029,   1, 1) /* ItemType */
     , (950029,   3, 20) /* PaletteTemplate */
     , (950029,   5, 200) /* EncumbranceVal */
     , (950029,   9, 1048576) /* ValidLocations */
     , (950029,  16, 1) /* ItemUseable */
     , (950029,  19, 100) /* Value */
     , (950029,  44, 22) /* Damage */
     , (950029,  45, 3) /* DamageType */
     , (950029,  46, 2) /* DefaultCombatStyle */
     , (950029,  47, 6) /* AttackType */
     , (950029,  48, 4) /* WeaponSkill */
     , (950029,  49, 34) /* WeaponTime */
     , (950029,  51, 1) /* CombatUse */
     , (950029,  93, 1044) /* PhysicsState */
     , (950029, 105, 1) /* ItemWorkmanship */
     , (950029, 131, 64) /* MaterialType */
     , (950029, 150, 103) /* HookPlacement */
     , (950029, 151, 2) /* HookType */
     , (950029, 158, 2) /* WieldRequirements */
     , (950029, 159, 4) /* WieldSkillType */
     , (950029, 160, 370) /* WieldDifficulty */
     , (950029, 169, 101254146) /* TsysMutationData */
     , (950029, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950029,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950029,  21, 0.4) /* WeaponLength */
     , (950029,  22, 0.6) /* DamageVariance */
     , (950029,  29, 1.11) /* WeaponDefense */
     , (950029,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950029,   1, 'Admin Test Dagger 370 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950029,   1, 0x02000E49) /* Setup */
     , (950029,   3, 0x20000014) /* SoundTable */
     , (950029,   6, 0x04000BEF) /* PaletteBase */
     , (950029,   7, 0x10000415) /* ClothingBase */
     , (950029,   8, 0x06002900) /* Icon */
     , (950029,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950029,  36, 0x0E00001D) /* MutateFilter */
     , (950029,  46, 0x38000031) /* TsysMutationFilter */;
